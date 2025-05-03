using Microsoft.Data.SqlClient;

namespace APBD_s31722_8_API.Datalayer;

public class DbClient(IConfiguration configuration)
{
    public async IAsyncEnumerable<T> ReadDataAsync<T>(string query, Func<SqlDataReader, T> map, Dictionary<string, object> parameters = null)
    {
        using (var sqlConnection =
               new SqlConnection(configuration.GetConnectionString("Default")))
        using (var command = new SqlCommand(query, sqlConnection))
        {
            if (parameters?.Count > 0)
            {
                foreach (var parameter in parameters)
                {
                    command.Parameters.AddWithValue(parameter.Key, parameter.Value);
                }
            }
            await sqlConnection.OpenAsync();
            var reader = await command.ExecuteReaderAsync();
            while (reader.Read())
            {
                yield return map(reader);
            }
        }
    }

    public async Task<int?> ReadScalarAsync(string query, Dictionary<string, object> parameters = null)
    {
        using (var sqlConnection =
               new SqlConnection(configuration.GetConnectionString("Default")))
        using (var command = new SqlCommand(query, sqlConnection))
        {
            if (parameters?.Count > 0)
            {
                foreach (var parameter in parameters)
                {
                    command.Parameters.AddWithValue(parameter.Key, parameter.Value);
                }
            }
            await sqlConnection.OpenAsync();
            return (int?)await command.ExecuteScalarAsync();
        }
    }

    public async Task<int> ExecuteNonQueryAsync(string query, Dictionary<string, object> parameters = null)
    {
        using (var sqlConnection =
               new SqlConnection(configuration.GetConnectionString("Default")))
        using (var command = new SqlCommand(query, sqlConnection))
        {
            if (parameters?.Count > 0)
            {
                foreach (var parameter in parameters)
                {
                    command.Parameters.AddWithValue(parameter.Key, parameter.Value);
                }
            }
            await sqlConnection.OpenAsync();
            return await command.ExecuteNonQueryAsync();
        }
    }
}