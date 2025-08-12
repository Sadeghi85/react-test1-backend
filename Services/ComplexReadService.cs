using Dapper;
using Microsoft.Data.SqlClient;

namespace backend.Services
{
    #region Dto

    public class GetUserInfoReturnModel
    {
        public int PersonId { get; set; }
        public int OfficeId { get; set; }
        public required string FullName { get; set; }
        public required string FullOfficeTitle { get; set; }

    }
    public class GetCurrentPermissionsReturnModel
    {
        public int PermissionId { get; set; }
        public required string RoleName { get; set; }
        public required string PermissionName { get; set; }
        public required string Access { get; set; }
        public required string PermissionType { get; set; }

    }
    public class GetCurrentPersonOfficeReturnModel
    {
        public int PersonOfficeId { get; set; }
        public int OfficeId { get; set; }
        public bool MainPosition { get; set; }
        public int PersonId { get; set; }
        public required string EmployeeNum { get; set; }
        public required string NationalCode { get; set; }
        public required string FullName { get; set; }

    }

    #endregion

    public partial class ComplexReadService(IConfiguration configuration)
    {

        public async Task<GetUserInfoReturnModel?> GetUserInfoAsync(string username, int officeId)
        {
            using var connection = new SqlConnection(configuration.GetConnectionString("ApplicationDbContextConnection"));

            var parameters = new DynamicParameters();
            parameters.Add("@_officeId", officeId);
            parameters.Add("@_username", username);

            var sql = @"
        
    declare @UserName nvarchar(10) = @_username;
    declare @OfficeID int = @_officeId;


    SELECT top 1
	       p.FullName, paro.OfficeTitle + ' - ' + o.OfficeTitle as FullOfficeTitle, p.PersonId, po.OfficeID
    FROM            DB_Core.dbo.tblPersonOffice AS po INNER JOIN
				    ( select top (1) 
					    _p.PersonID PersonId, _p.EmployeeNum, _p.NationalCode, _p.FullName
					    from DB_Core.dbo.tblPerson _p
					    where (_p.IsDeleted = 0)  and (_p.EmployeeNum = @UserName or _p.NationalCode = @UserName)
					    order by _p.PersonID
				    ) AS p ON p.PersonID = po.PersonID
				    inner join DB_Core.dbo.tblOffice AS o on po.OfficeID = o.OfficeID
				    inner join DB_Core.dbo.tblOffice AS paro on paro.OfficeID = o.ParentOfficeID
    WHERE     po.OfficeID = @OfficeID and (po.IsExpire = 0)

";

            return (await connection.QueryAsync<GetUserInfoReturnModel>(sql, parameters)).FirstOrDefault();

        }

        public async Task<List<GetCurrentPermissionsReturnModel>> GetCurrentPermissionsAsync(int projectId, string username, int officeId)
        {
            using var connection = new SqlConnection(configuration.GetConnectionString("ApplicationDbContextConnection"));

            var parameters = new DynamicParameters();
            parameters.Add("@_projectId", projectId);
            parameters.Add("@_username", username);
            parameters.Add("@_officeId", officeId);

            var sql = @"
        
    declare @ProjectId int = @_projectId;
	declare @UserName nvarchar(10) = @_username;
	declare @OfficeId int = @_officeId;

    SELECT   
			PermissionID PermissionId, RoleName, FormName PermissionName, Access, PermissionType
	FROM            DB_Core.dbo.vwDLProjectPermissionFast
	WHERE        (ProjectID = @ProjectId) and OfficeID = @OfficeId and (EmployeeNum = @UserName or NationalCode = @UserName)



";

            return (await connection.QueryAsync<GetCurrentPermissionsReturnModel>(sql, parameters))
            .ToList();
        }

        public async Task<List<GetCurrentPersonOfficeReturnModel>> GetCurrentPersonOfficeAsync(string username)
        {
            using var connection = new SqlConnection(configuration.GetConnectionString("ApplicationDbContextConnection"));

            var parameters = new DynamicParameters();
            parameters.Add("@_username", username);

            var sql = @"
        
	declare @UserName nvarchar(10) = @_username;

    
	SELECT po.PersonOfficeID PersonOfficeId, po.OfficeID OfficeId, po.MainPosition,
		   p.PersonID PersonId, p.EmployeeNum, p.NationalCode, p.FullName
	FROM            DB_Core.dbo.tblPersonOffice AS po INNER JOIN
					( select top (1) 
						_p.PersonID PersonId, _p.EmployeeNum, _p.NationalCode, _p.FullName
						from DB_Core.dbo.tblPerson _p
						where (_p.IsDeleted = 0)  and (_p.EmployeeNum = @UserName or _p.NationalCode = @UserName)
						order by _p.PersonID
					) AS p ON p.PersonID = po.PersonID
	WHERE     (po.IsExpire = 0)


";

            return (await connection.QueryAsync<GetCurrentPersonOfficeReturnModel>(sql, parameters))
                   .ToList();
        }
    }
}
