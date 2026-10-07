namespace Sample;

using System.Data;
using System.Data.SqlClient;

public class AccountService
{
    public void IncrementCeditBalance(string email, decimal amount)
    {
        string connectionString = @"Data Source=xxx.database.windows.net;Initial Catalog=xxx;User ID=xxx  ;Password=xxx ";
        using var cnn = new SqlConnection(connectionString);
        cnn.Open();
        using var command = new SqlCommand("UPDATE saleslt.customer SET CreditBalance = CreditBalance + @Amount WHERE EmailAddress = '" + email + "'", cnn);
        command.Parameters.Add("@Amount", SqlDbType.Decimal).Value = amount;
        command.ExecuteNonQuery();
    }
}