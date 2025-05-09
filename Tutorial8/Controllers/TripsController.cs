using Microsoft.AspNetCore.Mvc;
using Tutorial8.Models.DTOs;
using Tutorial8.Services;

namespace Tutorial8.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TripsController : ControllerBase
{
    private readonly ITripsService _tripsService;

    public TripsController(ITripsService tripsService)
    {
        _tripsService = tripsService;
    }
    
    /// GET /api/trips
    [HttpGet]
    public async Task<IActionResult> GetTrips()
    {
        var trips = await _tripsService.GetTrips();
        return Ok(trips);
    }


    /// GET /api/clients/{id}/trips
    [HttpGet("/api/clients/{id}/trips")]
    public async Task<IActionResult> GetClientTrips(int id)
    {
        try
        {
            var trips = await _tripsService.GetTripsForClient(id);
            return Ok(trips);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Client with ID {id} not found.");
        }
        catch (Exception)
        {
            return StatusCode(500, "An unexpected error occurred.");
        }
    }
    
    
    /// POST /api/clients
    [HttpPost("/api/clients")]
    public async Task<IActionResult> CreateClient([FromBody] CreateClientDTO dto)
    {
        try
        {
            int newClientId = await _tripsService.CreateClientAsync(dto);
            return Created($"/api/clients/{newClientId}", new { Id = newClientId });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception)
        {
            return StatusCode(500, "An error occurred while creating the client.");
        }
    }
    
    /// PUT /api/clients/{id}/trips/{tripId}
    [HttpPut("/api/clients/{id}/trips/{tripId}")]
    public async Task<IActionResult> RegisterClientToTrip(int id, int tripId)
    {
        try
        {
            await _tripsService.RegisterClientToTripAsync(id, tripId);
            return Ok("Client successfully registered for the trip.");
        }
        catch (KeyNotFoundException)
        {
            return NotFound("Client not found.");
        }
        catch (ArgumentException)
        {
            return NotFound("Trip not found.");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception)
        {
            return StatusCode(500, "An error occurred while registering the client.");
        }
    }

    
    /// DELETE /api/clients/{id}/trips/{tripId}
    [HttpDelete("/api/clients/{id}/trips/{tripId}")]
    public async Task<IActionResult> UnregisterClientFromTrip(int id, int tripId)
    {
        try
        {
            await _tripsService.UnregisterClientFromTripAsync(id, tripId);
            return Ok("Client successfully unregistered from the trip.");
        }
        catch (KeyNotFoundException)
        {
            return NotFound("Client or registration not found.");
        }
        catch (ArgumentException)
        {
            return NotFound("Trip not found.");
        }
        catch (Exception)
        {
            return StatusCode(500, "An error occurred while unregistering the client.");
        }
    }


}