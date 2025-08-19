
using Application.Data;
using Application.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace backend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);


            var connectionString = builder.Configuration.GetConnectionString("ApplicationDbContextConnection") ?? throw new InvalidOperationException("Connection string 'ApplicationDbContextConnection' not found.");
            var jwtSecretKey = builder.Configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("Jwt:SecretKey is missing");
            var jwtValidIssuer = builder.Configuration["Jwt:ValidIssuer"] ?? throw new InvalidOperationException("Jwt:ValidIssuer is missing");
            var jwtValidAudience = builder.Configuration["Jwt:ValidAudience"] ?? throw new InvalidOperationException("Jwt:ValidAudience is missing");
            var allowedOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? throw new InvalidOperationException("Cors:Origins is missing");



            var serilogLogger = new LoggerConfiguration()
                              .ReadFrom.Configuration(builder.Configuration)
                              .CreateLogger();

            builder.Host.UseSerilog(serilogLogger, true);



            builder.Services.AddDbContext<ApplicationDbContext>(options =>
            {
                //options.UseSqlServer(coreConnectionString, o => o.UseCompatibilityLevel(120)); // 'Contains' compatibility in core 8.0

                options.UseSqlServer(connectionString);
            });

            builder.Services.AddScoped<IApplicationDbContext, ApplicationDbContext>();

            if (builder.Environment.IsDevelopment())
            {
                builder.Services.AddCors(options =>
                {
                    options.AddPolicy("AllowReactApp", builder =>
                    {
                        builder.WithOrigins(allowedOrigins)
                               .AllowAnyHeader()
                               .AllowAnyMethod()
                               .AllowCredentials();
                    });
                });
            }

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtValidIssuer,
                    ValidateAudience = true,
                    ValidAudience = jwtValidAudience,

                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSecretKey)
                    ),
                    ClockSkew = TimeSpan.Zero,

                    // Enable expiration validation
                    ValidateLifetime = true,
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = ctx =>
                    {
                        // Expect token in Authorization header as Bearer token
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = ctx =>
                    {
                        // If token expired and request is to /api/auth/refresh, allow
                        if (ctx.Exception is SecurityTokenExpiredException &&
                            ctx.Request.Path.StartsWithSegments("/api/auth/refresh"))
                        {


                            var token = ctx.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

                            var tokenHandler = new JwtSecurityTokenHandler();

                            try
                            {
                                // Validate token ignoring expiration
                                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                                {
                                    ValidateIssuer = true,
                                    ValidIssuer = jwtValidIssuer,
                                    ValidateAudience = true,
                                    ValidAudience = jwtValidAudience,

                                    ValidateIssuerSigningKey = true,
                                    IssuerSigningKey = new SymmetricSecurityKey(
                                        Encoding.UTF8.GetBytes(jwtSecretKey)
                                    ),
                                    ClockSkew = TimeSpan.Zero,

                                    ValidateLifetime = false, // <-- ignore expiration here

                                }, out SecurityToken validatedToken);


                                ctx.Principal = principal;
                                ctx.Success(); // Tell pipeline auth succeeded

                            }
                            catch
                            {
                                // Validation failed, do nothing or set failure
                            }

                        }
                        return Task.CompletedTask;
                    }
                };
            });


            builder.Services.AddAuthorization();

            builder.Services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            builder.Services.AddScoped<ComplexReadService>();
            builder.Services.AddScoped<TokenService>();

            builder.Services.Scan(selector =>
            {
                selector.FromAssemblyOf<Application.Services.BaseService>()
                        .AddClasses(filter =>
                        {
                            filter.AssignableTo(typeof(Application.Services.BaseService));
                        })
                        //.AsImplementedInterfaces()
                        .AsSelf()
                        .WithScopedLifetime();
            });

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "PhoneBook APIs", Version = "v1" });
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = @"JWT Authorization header using the Bearer scheme. \r\n\r\n 
                      Enter 'Bearer' [space] and then your token in the text input below.
                      \r\n\r\nExample: 'Bearer 12345abcdef'",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement()
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            },
                            Scheme = "oauth2",
                            Name = "Bearer",
                            In = ParameterLocation.Header,

                        },
                        new List<string>()
                    }
                });
                //var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                //var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                //options.IncludeXmlComments(xmlPath);


                // important
                options.CustomSchemaIds(type => type.ToString());
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            else
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            // This middleware serves files from wwwroot
            app.UseStaticFiles();

            app.UseRouting();

            if (app.Environment.IsDevelopment())
            {
                app.UseCors("AllowReactApp");
            }

            app.UseAuthentication();
            app.UseAuthorization();



            app.MapControllers();

            // This is a fallback for client-side routing.
            // It ensures that any request that doesn't match an API route
            // will be served the index.html file.
            app.MapFallbackToFile("index.html");

            app.Run();
        }
    }
}
