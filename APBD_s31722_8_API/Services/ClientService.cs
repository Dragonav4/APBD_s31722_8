using APBD_s31722_8_API.Datalayer;
using APBD_s31722_8_API.Datalayer.Models;
using APBD_s31722_8_API.Exceptions;
using APBD_s31722_8_API.Utils;
using APBD_s31722_8_API.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace APBD_s31722_8_API.Services;

public class ClientService
{
    class TripClientsInfo
    {
        public int MaxPeople;
        public int CurrentCount;
    }
    private readonly DbClient _dbClient;
    
    private const string GetClientTripsQuery = @"
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

    private const string TripInfoQuery = @"
                SELECT t.MaxPeople,
                COUNT(clt.IdClient) AS CurrentCount
                  FROM Trip t
                  LEFT JOIN Client_Trip clt ON clt.IdTrip = t.IdTrip
                WHERE t.IdTrip = @tripId
                GROUP BY t.MaxPeople";

    public ClientService(DbClient dbClient)
    {
        _dbClient = dbClient;
    }
    
    public async Task<ClientDto?> CreateClient([FromBody] ClientDto clientDto)
    {
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
            throw new BadRequestException("Error while creating client");
        clientDto.Id = newId.Value;
        return clientDto;
    }

    public async Task<List<TripView>> GetTripsByClientAsync(int clientId)
    {
        var result = await _dbClient.ReadDataAsync<TripDto>(
                GetClientTripsQuery,
                reader => new TripDto(reader),
                new Dictionary<string, object> { { "@id", clientId } })
            .ToListAsync();
        return result
            .GroupBy(item => item.IdTrip)
            .Select(group => new TripView(group))
            .ToList();
    }

    public async Task RegisterClientToTripAsync(int clientId, int tripId)
    {
        var clientExists = await _dbClient.ReadScalarAsync(
            "SELECT COUNT(1) FROM Client WHERE IdClient = @id",
            new Dictionary<string, object> { { "@id", clientId } });
        if (clientExists != 1)
            throw new BadRequestException($"Client {clientId} not found");

        var tripStats = await _dbClient.ReadDataAsync<TripClientsInfo>(
                TripInfoQuery,
                reader => new TripClientsInfo
                {
                    CurrentCount = (int)reader["CurrentCount"],
                    MaxPeople = (int)reader["MaxPeople"]
                },
                new Dictionary<string, object> { { "@tripId", tripId } })
            .ToListAsync();
        if (!tripStats.Any())
            throw new BadRequestException($"Trip {tripId} not found");

        var info = tripStats.First();
        if (info.CurrentCount >= info.MaxPeople)
            throw new BadRequestException("Maximum number of participants reached");

        var alreadyRegistered = await _dbClient.ReadScalarAsync(
            "SELECT COUNT(1) FROM Client_Trip WHERE IdClient = @id AND IdTrip = @tripId",
            new Dictionary<string, object>
            {
                { "@id", clientId },
                { "@tripId", tripId }
            });
        if (alreadyRegistered != 0)
            throw new BadRequestException($"Client {clientId} already registered for trip {tripId}.");

        await _dbClient.ExecuteNonQueryAsync(@"
            INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt)
            VALUES (@Id, @tripId, @RegisteredAt)",
            new Dictionary<string, object>
            {
                { "@Id", clientId },
                { "@tripId", tripId },
                { "@RegisteredAt", DateTime.Now.Year * 10000 + DateTime.Now.Month * 100 + DateTime.Now.Day }
            });
    }

    public async Task UnregisterClientFromTripAsync(int clientId, int tripId)
    {
        var affectedRows = await _dbClient.ExecuteNonQueryAsync(
            "DELETE FROM Client_Trip WHERE IdClient = @id AND IdTrip = @tripId",
            new Dictionary<string, object>
            {
                { "@id", clientId },
                { "@tripId", tripId }
            });
        if (affectedRows == 0)
            throw new BadRequestException($"Registration for client {clientId} and trip {tripId} not found");
    }
}