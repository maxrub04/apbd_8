using Tutorial8.Models.DTOs;

namespace Tutorial8.Services;

public interface ITripsService
{
    Task<List<TripDTO>> GetTrips();
    Task<List<ClientTripDTO>> GetTripsForClient(int clientId);
    Task<int> CreateClientAsync(CreateClientDTO dto);
    Task RegisterClientToTripAsync(int clientId, int tripId);
    Task UnregisterClientFromTripAsync(int clientId, int tripId);
}