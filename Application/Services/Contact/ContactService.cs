using Application.Data;
using Application.Helpers;
using Application.Models.Contact;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Application.Services
{
    public class ContactService(IConfiguration configuration, IApplicationDbContext applicationDbContext) : BaseService(configuration, applicationDbContext)
    {

        public async Task<OperationResult<IEnumerable<Contact>>> GetContacts(GetContactsInput getContactsInput)
        {
            try
            {

                using var connection = new SqlConnection(configuration.GetConnectionString("ApplicationDbContextConnection"));

                string orderBy = GridHelper.BuildOrderByClause(getContactsInput.Sorting);

                var sql = string.Empty;
                var parameters = new DynamicParameters();


                parameters = new DynamicParameters();
                parameters.Add("@_pageIndex", getContactsInput.PageIndex);
                parameters.Add("@_pageSize", getContactsInput.PageSize);


                sql = $@"
        
    declare 

	@pageIndex int = @_pageIndex,
	@pageSize int = @_pageSize;


	IF OBJECT_ID('tempdb..#TempTable') IS NOT NULL 
	BEGIN 
		DROP TABLE #TempTable 
	END


	;WITH TempResult AS
	(

		select
			ID,
			Firstname,
			Lastname,
			Email,
			PhoneNumber

		from tblContact

	),
	TempResultSetCheck AS
	(
		SELECT 1 AS ResultSetCheck
	),
	TempCount AS
	(
		SELECT COUNT(*) AS  TotalRows FROM TempResult
	)
	
	SELECT IDENTITY(INT,1,1) AS [Order], *
	INTO #TempTable
	FROM 
		( SELECT * FROM TempResult,TempResultSetCheck

			-- order goes here
			ORDER BY {orderBy}

			-- paging here
			OFFSET @pageIndex * @pageSize ROWS
			FETCH NEXT @pageSize ROWS ONLY) tmp
	RIGHT JOIN TempCount ON 1 = 1  -- This ensures that TempCount will always be joined


	select ID,
			Firstname,
			Lastname,
			Email,
			PhoneNumber
	from #TempTable
	where ResultSetCheck is not null
	order by [Order] asc


	select isnull((select top 1 TotalRows from #TempTable),0) as totalCount

";

                using (var multi = await connection.QueryMultipleAsync(sql, parameters))
                {
                    var pagedResults = await multi.ReadAsync<Contact>();
                    var totalCount = await multi.ReadFirstAsync<int>();


                    return OperationResult<IEnumerable<Contact>>.Success(pagedResults, totalCount);
                }

            }
            catch (Exception ex)
            {

                return OperationResult<IEnumerable<Contact>>.Fail(ex.ToString());
            }

        }

        public async Task<OperationResult<Contact>> CreateContact(Contact input)
        {
            try
            {
                var contact = new TblContact()
                {
                    Email = input.Email,
                    Firstname = input.Firstname,
                    Lastname = input.Lastname,
                    PhoneNumber = input.PhoneNumber,
                };

                applicationDbContext.TblContacts.Add(contact);
                await applicationDbContext.SaveChangesAsync();

                var output = new Contact()
                {
                    PhoneNumber = contact.PhoneNumber,
                    Email = contact.Email,
                    Firstname = contact.Firstname,
                    Lastname = contact.Lastname,
                    Id = contact.Id,
                };

                return OperationResult<Contact>.Success(output, 1);

            }
            catch (Exception ex)
            {
                return OperationResult<Contact>.Fail(ex.ToString());
            }
        }

    }
}
