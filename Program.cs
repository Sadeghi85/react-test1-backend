
using backend.Data;
using Microsoft.EntityFrameworkCore;

namespace backend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var connectionString = builder.Configuration.GetConnectionString("ContactDbContextConnection") ?? throw new InvalidOperationException("Connection string 'ContactDbContextConnection' not found.");

            builder.Services.AddDbContext<ContactDbContext>(options =>
            {
                //options.UseSqlServer(coreConnectionString, o => o.UseCompatibilityLevel(120)); // 'Contains' compatibility in core 8.0

                options.UseSqlServer(connectionString);
            });

            builder.Services.AddScoped<IContactDbContext, ContactDbContext>();

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

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
