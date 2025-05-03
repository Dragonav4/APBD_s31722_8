using APBD_s31722_8_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace APBD_s31722_8_API.Controllers;

[ApiController]
[Route("[controller]")]
public class TripsController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public TripsController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet]
    [Route("GetTrips")]
    public async Task<IActionResult> GetTrips()
    {
        List<TripDto> trips = new List<TripDto>();
        try
        {
            using (var sqlConnection =
                   new SqlConnection(_configuration.GetConnectionString("Default")))
            using (var command = new SqlCommand(@"
                SELECT 
                    t.IdTrip,
                    t.Name,
                    t.Description,
                    t.DateFrom,
                    t.DateTo,
                    t.MaxPeople,
                    c.Name AS CountryName
                FROM Trip t
                JOIN Country_Trip ct ON t.IdTrip = ct.IdTrip
                JOIN Country c ON ct.IdCountry = c.IdCountry
                ORDER BY IdTrip ASC", sqlConnection))
            {
                await sqlConnection.OpenAsync();
                var reader = await command.ExecuteReaderAsync();
                while (reader.Read())
                {
                    trips.Add(new TripDto
                    {
                        IdTrip = (int)reader["IdTrip"],
                        Name = reader["Name"].ToString()!,
                        Description = reader["Description"].ToString(),
                        DateFrom = (DateTime)reader["DateFrom"],
                        DateTo = (DateTime)reader["DateTo"],
                        MaxPeople = (int)reader["MaxPeople"],
                        Country = reader["CountryName"].ToString()!,
                    });
                }
            }

            return Ok(trips);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpGet("/api/clients/{id}/trips")]
    public async Task<IActionResult> GetTripsByClient([FromRoute] int id)
    {
        var trips = new List<TripDto>();
        try
        {
            using var sqlConnection = new SqlConnection(_configuration.GetConnectionString("Default"));
            using var command = new SqlCommand(@"
                SELECT 
                    t.IdTrip,
                    t.Name,
                    t.Description,
                    t.DateFrom,
                    t.DateTo,
                    t.MaxPeople,
                    DATEADD(SECOND, clt.RegisteredAt, '1970-01-01') AS RegisteredAt,
                    CASE WHEN clt.PaymentDate IS NULL
                         THEN NULL
                         ELSE DATEADD(SECOND, clt.PaymentDate, '1970-01-01')
                    END AS PaymentDate,
                    co.Name AS CountryName
                FROM Trip t
                JOIN Country_Trip ct ON t.IdTrip = ct.IdTrip
                JOIN Country co ON ct.IdCountry = co.IdCountry
                JOIN Client_Trip clt ON clt.IdTrip = t.IdTrip
                JOIN Client cli ON cli.IdClient = clt.IdClient
                WHERE cli.IdClient = @id
                ORDER BY t.IdTrip ASC", sqlConnection);
            command.Parameters.AddWithValue("@id", id);

            await sqlConnection.OpenAsync();
            var reader = await command.ExecuteReaderAsync();
            while (reader.Read())
            {
                trips.Add(new TripDto
                {
                    IdTrip = (int)reader["IdTrip"],
                    Name = reader["Name"].ToString()!,
                    Description = reader["Description"].ToString(),
                    DateFrom = (DateTime)reader["DateFrom"],
                    DateTo = (DateTime)reader["DateTo"],
                    MaxPeople = (int)reader["MaxPeople"],
                    Country = reader["CountryName"].ToString()!,
                    RegisteredAt = (DateTime)reader["RegisteredAt"],
                    PaymentDate = reader["PaymentDate"] != DBNull.Value ? (DateTime?)reader["PaymentDate"] : null,
                });
            }

            if (trips.Count == 0)
            {
                return NotFound($"No trips found for client {id}");
            }

            return Ok(trips);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpPut("{id}/trips/{tripId}")]
    public async Task<IActionResult> RegisterClientToTrip([FromRoute] int id, [FromRoute] int tripId)
    {
        try
        {
            await using var sqlConnection = new SqlConnection(_configuration.GetConnectionString("Default"));
            await sqlConnection.OpenAsync();
            var existsClient = new SqlCommand("SELECT COUNT(1) FROM Client WHERE IdClient = @id", sqlConnection);
            existsClient.Parameters.AddWithValue("@id", id);
            if ((int)await existsClient.ExecuteScalarAsync() == 0) return NotFound($"Client {id} not found");

            var tripCheck = new SqlCommand(@"
            SELECT t.MaxPeople,COUNT(ct.IdClient) AS CurrentAmountOfPeople
            FROM TRIP t
            LEFT JOIN Client_Trip ct ON ct.IdTrip = t.IdTrip
            WHERE t.IdTrip = @tripId
            GROUP BY t.MaxPeople", sqlConnection);
            tripCheck.Parameters.AddWithValue("@tripId", tripId);
            var reader = await tripCheck.ExecuteReaderAsync();
            if (!reader.Read())
            {
                await reader.CloseAsync();
                return NotFound($"Trip {id} not found");
            }

            var maxPeople = (int)reader["MaxPeople"];
            var currentAmountOfPeople = (int)reader["CurrentAmountOfPeople"];
            await reader.CloseAsync();
            if (currentAmountOfPeople > maxPeople) return Conflict("Max amount of people exceeded");

            var existsRegistration = new SqlCommand(@"
        SELECT COUNT(1) FROM Client_Trip WHERE IdClient = @id AND IdTrip = @tripId", sqlConnection);
            existsRegistration.Parameters.AddWithValue("@id", id);
            existsRegistration.Parameters.AddWithValue("@tripId", tripId);
            if ((int)await existsRegistration.ExecuteScalarAsync() > 0)
                return Conflict($"Client {id} already registered");

            var insert = new SqlCommand(@"
        INSERT INTO Client_Trip (IdClient, IdTrip,RegisteredAt)
        VALUES (@id, @tripId, DATEDIFF(SECOND, '1970-01-01', GETUTCDATE()))", sqlConnection);

            insert.Parameters.AddWithValue("@id", id);
            insert.Parameters.AddWithValue("@tripId", tripId);

            await insert.ExecuteNonQueryAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpDelete("{id}/trips/{tripId}")]
    public async Task<IActionResult> UnregisterClientFromTrip([FromRoute] int id, [FromRoute] int tripId)
    {
        await using var sqlConnection = new SqlConnection(_configuration.GetConnectionString("Default"));
        await sqlConnection.OpenAsync();
        
        var existsReg = new SqlCommand(@"SELECT COUNT(1) FROM Client_Trip WHERE IdClient = @id",sqlConnection);
        existsReg.Parameters.AddWithValue("@id", id);
        existsReg.Parameters.AddWithValue("@tripId", tripId);
        if((int) await existsReg.ExecuteScalarAsync() == 0) return Conflict($"Client {id} already unregistered");
        
        var delete = new SqlCommand(@"DELETE FROM Client_Trip WHERE IdClient = @id AND IdTrip = @tripId", sqlConnection);
        delete.Parameters.AddWithValue("@id", id);
        delete.Parameters.AddWithValue("@tripId", tripId);
        await delete.ExecuteNonQueryAsync();
        return NoContent();
    }
}