using APBD_s31722_8_API.Datalayer.Models;
using APBD_s31722_8_API.Exceptions;
using APBD_s31722_8_API.Services;
using Microsoft.AspNetCore.Mvc;

namespace APBD_s31722_8_API.Controllers;

[ApiController]
[Route("api/clients")]
public class ClientController : ControllerBase
{
    
    private readonly ClientService _clientService;

    public ClientController(ClientService clientService)
    {
        _clientService = clientService;
    }

    [HttpGet("ping")]
    public string Ping()
    {
        return "Pong";
    }

    [HttpPost]
    public async Task<IActionResult> CreateClient([FromBody] ClientDto clientDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var created = await _clientService.CreateClient(clientDto);

        if(created == null) throw new BadRequestException("Error while creating client");
        return Created($"/api/clients/{created.Id}", clientDto);
    }
    
    
    [HttpGet("{id}/trips")]
    public async Task<IActionResult> GetTripsByClient([FromRoute] int id)
    {
            var trips = await _clientService.GetTripsByClientAsync(id);
            if (!trips.Any())
                return NoContent();
            return Ok(trips);
    }


    [HttpPut("{id}/trips/{tripId}")]
    public async Task<IActionResult> RegisterClientToTrip([FromRoute] int id, [FromRoute] int tripId)
    {
            await _clientService.RegisterClientToTripAsync(id, tripId);
            return Ok("Trip registered");
    }

    [HttpDelete("{id}/trips/{tripId}")]
    public async Task<IActionResult> UnregisterClientFromTrip([FromRoute] int id, [FromRoute] int tripId)
    {
            await _clientService.UnregisterClientFromTripAsync(id, tripId);
            return Ok("Trip cancelled");
    }
}