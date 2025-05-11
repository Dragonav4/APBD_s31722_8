using APBD_s31722_8_API.Services;
using APBD_s31722_8_API.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace APBD_s31722_8_API.Controllers;

[ApiController]
[Route("api/trips")]
public class TripsController : ControllerBase
{
    private readonly TripService _tripService;
    
    public TripsController(TripService tripService)
    {
        _tripService = tripService;
    }

    [HttpGet("")]
    public async Task<ActionResult<List<TripView>>> GetTrips()
    {
        try{
            var trips = await _tripService.GetTripsAsync();
            if (!trips.Any())
                return NoContent();
            return Ok(trips);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
}