using APBD_s31722_8_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace APBD_s31722_8_API.Controllers;

[ApiController]
[Route("api/clients")]
public class ClientController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public ClientController(IConfiguration configuration)
    {
        _configuration = configuration;
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
        await using var connection = new SqlConnection(_configuration.GetConnectionString("Default"));
        await using var command = new SqlCommand(query, connection);

        command.Parameters.AddWithValue("@FirstName", clientDto.FirstName);
        command.Parameters.AddWithValue("@LastName", clientDto.LastName);
        command.Parameters.AddWithValue("@Email", clientDto.Email);
        command.Parameters.AddWithValue("@Telephone", clientDto.Telephone);
        command.Parameters.AddWithValue("@Pesel", clientDto.Pesel);

        await connection.OpenAsync();
        int newID = (int)await command.ExecuteScalarAsync();
        clientDto.Id = newID;
        return Created($"/api/clients/{newID}", new { idClient = newID });
    }
}