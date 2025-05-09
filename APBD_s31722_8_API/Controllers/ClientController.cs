using APBD_s31722_8_API.Datalayer;
using APBD_s31722_8_API.Datalayer.Models;
using APBD_s31722_8_API.Utils;
using APBD_s31722_8_API.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace APBD_s31722_8_API.Controllers;

class TripClientsInfo
{
    public int MaxPeople;
    public int CurrentCount;
}

[ApiController]
[Route("api/clients")]
public class ClientController : ControllerBase
{
    
    private readonly DbClient _dbClient;
    
    private const string getClientTripsQuery = @"
                SELECT 
                    t.IdTrip,
                    t.Name,
                    t.Description,
                    t.DateFrom,
                    t.DateTo,
                    t.MaxPeople,
                    clt.RegisteredAt as RegisteredAt,
                    clt.PaymentDate as PaymentDate,
                    co.Name AS CountryName
                FROM Trip t
                JOIN Country_Trip ct ON t.IdTrip = ct.IdTrip
                JOIN Country co ON ct.IdCountry = co.IdCountry
                JOIN Client_Trip clt ON clt.IdTrip = t.IdTrip
                JOIN Client cli ON cli.IdClient = clt.IdClient
                WHERE cli.IdClient = @id
                ORDER BY t.IdTrip ASC";

    private const string tripInfoQuery = @"
                SELECT t.MaxPeople,
                COUNT(clt.IdClient) AS CurrentCount
                  FROM Trip t
                  LEFT JOIN Client_Trip clt ON clt.IdTrip = t.IdTrip
                WHERE t.IdTrip = @tripId
                GROUP BY t.MaxPeople";

    public ClientController(DbClient dbClient)
    {
        _dbClient = dbClient;
    }

    [HttpPost]
    public async Task<IActionResult> CreateClient([FromBody] ClientDto clientDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        string query = @"
                        INSERT INTO Client (FirstName,LastName,Email, Telephone,Pesel)
                        OUTPUT INSERTED.IdClient
                        VALUES (@FirstName,@LastName,@Email,@Telephone,@Pesel);";
        
        var newId = await _dbClient.ReadScalarAsync(
            query,
            new Dictionary<string, object>
            {
                {"@FirstName", clientDto.FirstName},
                {"@LastName", clientDto.LastName},
                {"@Email", clientDto.Email},
                {"@Telephone", clientDto.Telephone},
                {"@Pesel", clientDto.Pesel}
            });

        if (newId == null)
            return BadRequest("Error while creating client");
        clientDto.Id = newId.Value;
        return Created($"/api/clients/{newId}", clientDto);
    }
    
    
    [HttpGet("/api/clients/{id}/trips")]
    public async Task<IActionResult> GetTripsByClient([FromRoute] int id)
    {
        try
        {
            var result = await _dbClient
                    .ReadDataAsync<TripDto>(
                        getClientTripsQuery, 
                        (reader) => new TripDto(reader),
                        new Dictionary<string, object>
                        {
                            { "@id", id }
                        })
                    .ToListAsync();
            var trips = result.GroupBy(item => item.IdTrip)
                .Select(group => new TripView(group))
                .ToList();
            if (trips.Count == 0)
                return Empty;
            return Ok(trips);
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Server error: "+ex.Message+ex.StackTrace);
        }
    }


    [HttpPut("{id}/trips/{tripId}")]
    public async Task<IActionResult> RegisterClientToTrip([FromRoute] int id, [FromRoute] int tripId)
    {
        try
        {
            var clientExists = await _dbClient.ReadScalarAsync("SELECT COUNT(1) FROM Client WHERE IdClient = @id",
                new Dictionary<string, object> { { "@id", id } });
            if (clientExists != 1)
                return NotFound($"Client {id} not found");

            var tripExists = await _dbClient.ReadDataAsync<TripClientsInfo>(
                    tripInfoQuery,
                    reader => new TripClientsInfo
                    {
                        CurrentCount = (int)reader["CurrentCount"],
                        MaxPeople = (int)reader["MaxPeople"],
                    },
                    new Dictionary<string, object>{{"@tripId", tripId}})
                .ToListAsync();
            
            if (tripExists.Count == 0)
                return NotFound($"Trip {tripId} not found");
            var maxPeople = tripExists.First().MaxPeople;
            var currentCount = tripExists.First().CurrentCount;
            if (currentCount >= maxPeople)
                return Conflict("Maximum number of participants reached");

            var existsReg = await _dbClient.ReadScalarAsync(
                "SELECT COUNT(1) FROM Client_Trip WHERE IdClient = @id AND IdTrip = @tripId",
                new Dictionary<string, object>
                {
                    { "@id", id }, { "@tripId", tripId }
                }); 
            if (existsReg != 0)
                return Conflict($"Client {id} already registered for trip {tripId}.");

            await _dbClient.ExecuteNonQueryAsync(@"
                INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt)
                VALUES (@Id, @tripId, @RegisteredAt)",
                new Dictionary<string, object>{
                    {"@Id", id},
                    {"@tripId", tripId},
                    {"@RegisteredAt", DateTime.Now.Year*10000+DateTime.Now.Month*100+DateTime.Now.Day}
                    });

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
        var affectedRows = await _dbClient.ExecuteNonQueryAsync(
            "DELETE FROM Client_Trip WHERE IdClient = @id AND IdTrip = @tripId",
            new Dictionary<string, object>{
                {"@id", id },
                {"@tripId", tripId},
            });
            if (affectedRows == 0)
                return NotFound($"Trip {tripId} not found");

            return Ok("Ok");
    }
}