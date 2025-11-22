
using Microsoft.Data.SqlClient;
using RepoDb;

namespace WebAuthen.App_code;


public class CustomerDetail
{
    public required string cust_code { get; set; }
    public required string cust_ipaddress { get; set; }
    public required string cust_db_user { get; set; }
    public required string cust_db_pass { get; set; }
    public required string cust_db_name { get; set; }
}

public class DynamicConnectionService(IConfiguration configuration)
{
    private readonly IConfiguration _configuration = configuration;

    public async Task<string?> GetConnectionStringByNameAsync(string constr)
    {
        var configDbConnStr = _configuration.GetConnectionString("Connection43");
        using var connection = new SqlConnection(configDbConnStr);

        var customer = (await connection.ExecuteQueryAsync<CustomerDetail>(
                    "SELECT * FROM customer_detail WHERE cust_code = @Code",
                    new { Code = constr }
                )).FirstOrDefault();

        if (customer == null)
            return null;

        return $"Server={customer.cust_ipaddress};MultipleActiveResultSets=true;" +
               $"uid={customer.cust_db_user};pwd={customer.cust_db_pass};" +
               $"Database={customer.cust_db_name};TrustServerCertificate=true;";
    }
}

