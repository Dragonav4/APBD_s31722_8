using System.Linq;
using APBD_s31722_8_API.Datalayer;
using APBD_s31722_8_API.Datalayer.Models;
using APBD_s31722_8_API.Utils;
using APBD_s31722_8_API.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace APBD_s31722_8_API.Controllers;

[ApiController]
[Route("api/trips")]
public class TripsController : ControllerBase
{
    private readonly DbClient _dbClient;

    private const string getAllTripsQuery = @"
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
                ORDER BY IdTrip ASC";

    public TripsController(DbClient dbClient)
    {
        _dbClient = dbClient;
    }

    [HttpGet("")]
    public async Task<ActionResult<List<TripView>>> GetTrips()
    {
        try{
        var result = await _dbClient
            .ReadDataAsync<TripDto>(getAllTripsQuery, (reader) => new TripDto(reader))
            .ToListAsync()
            ;
        return Ok(result.GroupBy(item => item.IdTrip).Select(group => new TripView(group)).ToList());
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

}