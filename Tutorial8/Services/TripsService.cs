using Microsoft.Data.SqlClient;
using Tutorial8.Models.DTOs;

namespace Tutorial8.Services;

/// Service for interacting with trips in the database
public class TripsService : ITripsService
{
    private readonly string _connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=APBD;Integrated Security=True;";
    
    /// Retrieves all trips with their details and associated countries
    public async Task<List<TripDTO>> GetTrips()
    {
        var trips = new List<TripDTO>();

        var query = @"
            SELECT 
                t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople,
                c.Name AS CountryName
            FROM Trip t
            LEFT JOIN Country_Trip ct ON t.IdTrip = ct.IdTrip
            LEFT JOIN Country c ON ct.IdCountry = c.IdCountry
            ORDER BY t.IdTrip";

        try
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                await conn.OpenAsync();

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    int currentTripId = -1;
                    TripDTO currentTrip = null;

                    while (await reader.ReadAsync())
                    {
                        int idTrip = reader.GetInt32(reader.GetOrdinal("IdTrip"));

                        if (idTrip != currentTripId)
                        {
                            currentTripId = idTrip;
                            currentTrip = new TripDTO
                            {
                                Id = idTrip,
                                Name = reader.GetString(reader.GetOrdinal("Name")),
                                Description = reader.GetString(reader.GetOrdinal("Description")),
                                DateFrom = reader.GetDateTime(reader.GetOrdinal("DateFrom")),
                                DateTo = reader.GetDateTime(reader.GetOrdinal("DateTo")),
                                MaxPeople = reader.GetInt32(reader.GetOrdinal("MaxPeople")),
                                Countries = new List<CountryDTO>()
                            };
                            trips.Add(currentTrip);
                        }

                        if (!reader.IsDBNull(reader.GetOrdinal("CountryName")))
                        {
                            currentTrip.Countries.Add(new CountryDTO
                            {
                                Name = reader.GetString(reader.GetOrdinal("CountryName"))
                            });
                        }
                    }
                }
            }
        }
        catch (SqlException ex)
        {
         
            throw new Exception("An error occurred while fetching trip data.", ex);
        }

        return trips;
    }

    public async Task<List<ClientTripDTO>> GetTripsForClient(int clientId)
{
    var trips = new List<ClientTripDTO>();

    // 1 - Check if client exists
    string checkClientQuery = "SELECT 1 FROM Client WHERE IdClient = @id";

    using (var conn = new SqlConnection(_connectionString))
    using (var checkCmd = new SqlCommand(checkClientQuery, conn))
    {
        checkCmd.Parameters.AddWithValue("@id", clientId);
        await conn.OpenAsync();

        var exists = await checkCmd.ExecuteScalarAsync();

        if (exists == null)
            throw new KeyNotFoundException("Client not found.");

        conn.Close();
    }

    // 2 - Get trips registered by the client
    string query = @"
        SELECT t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople,
               ct.RegisteredAt, ct.PaymentDate
        FROM Client_Trip ct
        JOIN Trip t ON ct.IdTrip = t.IdTrip
        WHERE ct.IdClient = @id";

    using (var conn = new SqlConnection(_connectionString))
    using (var cmd = new SqlCommand(query, conn))
    {
        cmd.Parameters.AddWithValue("@id", clientId);
        await conn.OpenAsync();

        using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                trips.Add(new ClientTripDTO
                {
                    TripName = reader.GetString(reader.GetOrdinal("Name")),
                    Description = reader.GetString(reader.GetOrdinal("Description")),
                    DateFrom = reader.GetDateTime(reader.GetOrdinal("DateFrom")),
                    DateTo = reader.GetDateTime(reader.GetOrdinal("DateTo")),
                    MaxPeople = reader.GetInt32(reader.GetOrdinal("MaxPeople")),
                    RegisteredAt = reader.GetDateTime(reader.GetOrdinal("RegisteredAt")),
                    PaymentDate = reader.IsDBNull(reader.GetOrdinal("PaymentDate"))
                        ? null
                        : reader.GetDateTime(reader.GetOrdinal("PaymentDate")).ToString("yyyy-MM-dd")
                });
            }
        }
    }

    return trips;
}

    public async Task<int> CreateClientAsync(CreateClientDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FirstName) ||
            string.IsNullOrWhiteSpace(dto.LastName) ||
            string.IsNullOrWhiteSpace(dto.Email) ||
            string.IsNullOrWhiteSpace(dto.Telephone) ||
            string.IsNullOrWhiteSpace(dto.Pesel))
        {
            throw new ArgumentException("All fields are required.");
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(dto.Pesel, @"^\d{11}$"))
        {
            throw new ArgumentException("Invalid PESEL format.");
        }

        const string insertQuery = @"
        INSERT INTO Client (FirstName, LastName, Email, Telephone, Pesel)
        OUTPUT INSERTED.IdClient
        VALUES (@FirstName, @LastName, @Email, @Telephone, @Pesel)";

        using (var conn = new SqlConnection(_connectionString))
        using (var cmd = new SqlCommand(insertQuery, conn))
        {
            cmd.Parameters.AddWithValue("@FirstName", dto.FirstName);
            cmd.Parameters.AddWithValue("@LastName", dto.LastName);
            cmd.Parameters.AddWithValue("@Email", dto.Email);
            cmd.Parameters.AddWithValue("@Telephone", dto.Telephone);
            cmd.Parameters.AddWithValue("@Pesel", dto.Pesel);

            await conn.OpenAsync();
            var result = await cmd.ExecuteScalarAsync();

            return (int)result;
        }
    }

    public async Task RegisterClientToTripAsync(int clientId, int tripId)
{
    using var conn = new SqlConnection(_connectionString);
    await conn.OpenAsync();

    using var transaction = conn.BeginTransaction();

    try
    {
        // Check if client exists
        var checkClientCmd = new SqlCommand("SELECT 1 FROM Client WHERE IdClient = @ClientId", conn, transaction);
        checkClientCmd.Parameters.AddWithValue("@ClientId", clientId);

        var clientExists = await checkClientCmd.ExecuteScalarAsync();
        if (clientExists == null)
            throw new KeyNotFoundException("Client not found.");

        // Check if trip exists
        var checkTripCmd = new SqlCommand("SELECT MaxPeople FROM Trip WHERE IdTrip = @TripId", conn, transaction);
        checkTripCmd.Parameters.AddWithValue("@TripId", tripId);

        var maxPeopleObj = await checkTripCmd.ExecuteScalarAsync();
        if (maxPeopleObj == null)
            throw new ArgumentException("Trip not found.");

        int maxPeople = (int)maxPeopleObj;

        // Count existing participants
        var countCmd = new SqlCommand("SELECT COUNT(*) FROM Client_Trip WHERE IdTrip = @TripId", conn, transaction);
        countCmd.Parameters.AddWithValue("@TripId", tripId);

        int currentCount = (int)(await countCmd.ExecuteScalarAsync());

        if (currentCount >= maxPeople)
            throw new InvalidOperationException("Maximum number of participants reached.");

        // Check if client is already registered
        var existsCmd = new SqlCommand("SELECT 1 FROM Client_Trip WHERE IdClient = @ClientId AND IdTrip = @TripId", conn, transaction);
        existsCmd.Parameters.AddWithValue("@ClientId", clientId);
        existsCmd.Parameters.AddWithValue("@TripId", tripId);

        var alreadyRegistered = await existsCmd.ExecuteScalarAsync();
        if (alreadyRegistered != null)
            throw new InvalidOperationException("Client is already registered for this trip.");

        // Register client
        var insertCmd = new SqlCommand(@"
            INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt)
            VALUES (@ClientId, @TripId, @Now)", conn, transaction);

        insertCmd.Parameters.AddWithValue("@ClientId", clientId);
        insertCmd.Parameters.AddWithValue("@TripId", tripId);
        insertCmd.Parameters.AddWithValue("@Now", DateTime.UtcNow);

        await insertCmd.ExecuteNonQueryAsync();
        await transaction.CommitAsync();
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}

    public async Task UnregisterClientFromTripAsync(int clientId, int tripId)
{
    using var conn = new SqlConnection(_connectionString);
    await conn.OpenAsync();

    using var transaction = conn.BeginTransaction();

    try
    {
        // Check if client exists
        var checkClientCmd = new SqlCommand("SELECT 1 FROM Client WHERE IdClient = @ClientId", conn, transaction);
        checkClientCmd.Parameters.AddWithValue("@ClientId", clientId);

        var clientExists = await checkClientCmd.ExecuteScalarAsync();
        if (clientExists == null)
            throw new KeyNotFoundException("Client not found.");

        // Check if trip exists
        var checkTripCmd = new SqlCommand("SELECT 1 FROM Trip WHERE IdTrip = @TripId", conn, transaction);
        checkTripCmd.Parameters.AddWithValue("@TripId", tripId);

        var tripExists = await checkTripCmd.ExecuteScalarAsync();
        if (tripExists == null)
            throw new ArgumentException("Trip not found.");

        // Check if registration exists
        var checkRegistrationCmd = new SqlCommand(
            "SELECT 1 FROM Client_Trip WHERE IdClient = @ClientId AND IdTrip = @TripId", conn, transaction);
        checkRegistrationCmd.Parameters.AddWithValue("@ClientId", clientId);
        checkRegistrationCmd.Parameters.AddWithValue("@TripId", tripId);

        var registrationExists = await checkRegistrationCmd.ExecuteScalarAsync();
        if (registrationExists == null)
            throw new KeyNotFoundException("Registration not found.");

        // Delete registration
        var deleteCmd = new SqlCommand(
            "DELETE FROM Client_Trip WHERE IdClient = @ClientId AND IdTrip = @TripId", conn, transaction);
        deleteCmd.Parameters.AddWithValue("@ClientId", clientId);
        deleteCmd.Parameters.AddWithValue("@TripId", tripId);

        await deleteCmd.ExecuteNonQueryAsync();
        await transaction.CommitAsync();
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}

}

