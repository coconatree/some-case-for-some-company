using System.Data.SqlClient;
using MilkingSystem.Core.Models;
using MilkingSystem.Core.Notifications;

namespace MilkingSystem.Core.Services;

/// <summary>
/// Main data service for the milking system.
/// Handles all database operations for animals, milking events, and weight measurements.
/// </summary>
public class DataService
{
    private string _connectionString;

    public static DataService? Instance;

    public DataService(string connectionString)
    {
        _connectionString = connectionString;
        Instance = this;
    }

    #region Animals

    public List<Animal> GetAllAnimals()
    {
        var animals = new List<Animal>();
        using (var conn = new SqlConnection(_connectionString))
        {
            conn.Open();
            var cmd = new SqlCommand("SELECT * FROM Animals", conn);
            var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                animals.Add(MapAnimal(reader));
                /*
                animals.Add(new Animal
                {
                    Id = (int)reader["Id"],
                    IdentificationNumber = reader["IdentificationNumber"].ToString()!,
                    Name = reader["Name"] as string,
                    BirthDate = reader["BirthDate"] as DateTime?,
                    CreatedAt = (DateTime)reader["CreatedAt"],
                    UpdatedAt = (DateTime)reader["UpdatedAt"]
                });
                */
            }
        }
        return animals;
    }

    public List<Animal> GetAllAnimalsOrderedByName()
    {
        var animals = new List<Animal>();
        using (var conn = new SqlConnection(_connectionString))
        {
            conn.Open();
            var cmd = new SqlCommand("SELECT * FROM Animals ORDER BY Name", conn);
            var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                animals.Add(MapAnimal(reader));
                /*
                animals.Add(new Animal
                {
                    Id = (int)reader["Id"],
                    IdentificationNumber = reader["IdentificationNumber"].ToString()!,
                    Name = reader["Name"] as string,
                    BirthDate = reader["BirthDate"] as DateTime?,
                    CreatedAt = (DateTime)reader["CreatedAt"],
                    UpdatedAt = (DateTime)reader["UpdatedAt"]
                });
                */
            }
        }
        return animals;
    }

    public Animal? GetAnimalById(int id)
    {
        using (var conn = new SqlConnection(_connectionString))
        {
            conn.Open();

            // var cmd = new SqlCommand("SELECT * FROM Animals WHERE Id = " + id, conn);

            var cmd = new SqlCommand(
                "SELECT * FROM Animals WHERE Id = @Id",
                conn
            );

            cmd.Parameters.AddWithValue("@Id", id);

            var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return MapAnimal(reader);
                /*
                return new Animal
                {
                    Id = (int)reader["Id"],
                    IdentificationNumber = reader["IdentificationNumber"].ToString()!,
                    Name = reader["Name"] as string,
                    BirthDate = reader["BirthDate"] as DateTime?,
                    CreatedAt = (DateTime)reader["CreatedAt"],
                    UpdatedAt = (DateTime)reader["UpdatedAt"]
                };
                */
            }
        }
        return null;
    }

    public bool DoesAnimalExistsById(int id)
    {
        using (var conn = new SqlConnection(_connectionString))
        {
            conn.Open();

            var cmd = new SqlCommand(
                "SELECT TOP 1 1 FROM Animals WHERE Id = @Id",
                conn
            );

            cmd.Parameters.AddWithValue("@Id", id);

            return cmd.ExecuteScalar() != null;
        }
    }

    public Animal? GetAnimalByIdentificationNumber(string identificationNumber)
    {
        using (var conn = new SqlConnection(_connectionString))
        {
            conn.Open();

            // var cmd = new SqlCommand("SELECT * FROM Animals WHERE IdentificationNumber = '" + identificationNumber + "'", conn);

            var cmd = new SqlCommand(
                "SELECT * FROM Animals WHERE IdentificationNumber = @IdentificationNumber",
                conn
            );

            cmd.Parameters.AddWithValue(
                "@IdentificationNumber",
                identificationNumber
            );

            var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                return MapAnimal(reader);

                /*
                return new Animal
                {
                    Id = (int)reader["Id"],
                    IdentificationNumber = reader["IdentificationNumber"].ToString()!,
                    Name = reader["Name"] as string,
                    BirthDate = reader["BirthDate"] as DateTime?,
                    CreatedAt = (DateTime)reader["CreatedAt"],
                    UpdatedAt = (DateTime)reader["UpdatedAt"]
                };
                */
            }
        }
        return null;
    }

    public int CreateAnimal(string identificationNumber, string? name, DateTime? birthDate)
    {
        using (var conn = new SqlConnection(_connectionString))
        {
            conn.Open();

            var cmd = new SqlCommand(@"INSERT INTO Animals (IdentificationNumber, Name, BirthDate) 
                                       OUTPUT INSERTED.Id 
                                       VALUES (@IdentificationNumber, @Name, @BirthDate)", conn);

            cmd.Parameters.AddWithValue("@IdentificationNumber", identificationNumber);
            cmd.Parameters.AddWithValue("@Name", (object?)name ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@BirthDate", (object?)birthDate ?? DBNull.Value);

            return (int)cmd.ExecuteScalar();
        }
    }

    // Mapper

    private Animal MapAnimal(SqlDataReader reader)
    {
        return new Animal
        {
            Id = (int)reader["Id"],
            IdentificationNumber = reader["IdentificationNumber"].ToString()!,
            Name = reader["Name"] as string,
            BirthDate = reader["BirthDate"] as DateTime?,
            CreatedAt = (DateTime)reader["CreatedAt"],
            UpdatedAt = (DateTime)reader["UpdatedAt"]
        };
    }

    #endregion

    #region Robots

    public List<Robot> GetAllRobots()
    {
        var robots = new List<Robot>();
        var conn = new SqlConnection(_connectionString);
        try
        {
            conn.Open();
            var cmd = new SqlCommand("SELECT * FROM Robots", conn);
            var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                /*
                robots.Add(new Robot
                {
                    Id = (int)reader["Id"],
                    Name = reader["Name"].ToString()!,
                    Location = reader["Location"] as string,
                    IsActive = (bool)reader["IsActive"],
                    CreatedAt = (DateTime)reader["CreatedAt"],
                    UpdatedAt = (DateTime)reader["UpdatedAt"]
                });
                */
                robots.Add(MapRobot(reader));
            }
        }
        catch (Exception ex)
        {
            // Swallow exception - legacy behavior

            // NOTE: These types of exceptions can be handled via an applciation exception handler.
            Console.WriteLine("Error: " + ex.Message);
        }
        finally
        {
            conn.Close();
        }
        return robots;
    }

    public Robot? GetRobotById(int id)
    {
        using (var conn = new SqlConnection(_connectionString))
        {
            conn.Open();
            var cmd = new SqlCommand("SELECT * FROM Robots WHERE Id = @Id", conn);
            cmd.Parameters.AddWithValue("@Id", id);
            var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                /*
                return new Robot
                {
                    Id = (int)reader["Id"],
                    Name = reader["Name"].ToString()!,
                    Location = reader["Location"] as string,
                    IsActive = (bool)reader["IsActive"],
                    CreatedAt = (DateTime)reader["CreatedAt"],
                    UpdatedAt = (DateTime)reader["UpdatedAt"]
                };
                */
                return MapRobot(reader);
            }
        }
        return null;
    }

    public List<Robot> GetActiveRobots()
    {
        var robots = new List<Robot>();
        using (var conn = new SqlConnection(_connectionString))
        {
            conn.Open();
            var cmd = new SqlCommand("SELECT * FROM Robots WHERE IsActive = 1", conn);
            var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                /*
                robots.Add(new Robot
                {
                    Id = (int)reader["Id"],
                    Name = reader["Name"].ToString()!,
                    Location = reader["Location"] as string,
                    IsActive = (bool)reader["IsActive"],
                    CreatedAt = (DateTime)reader["CreatedAt"],
                    UpdatedAt = (DateTime)reader["UpdatedAt"]
                });
                */
                robots.Add(MapRobot(reader));
            }
        }
        return robots;
    }

    public bool DoesActiveRobotExistsById(int id)
    {
        using (var conn = new SqlConnection(_connectionString))
        {
            conn.Open();

            var cmd = new SqlCommand(
                "SELECT TOP 1 1 FROM Robots WHERE Id = @Id",
                conn
            );

            cmd.Parameters.AddWithValue("@Id", id);

            return cmd.ExecuteScalar() != null;
        }
    }

    private Robot MapRobot(SqlDataReader reader)
    {
        return new Robot
        {
            Id = (int)reader["Id"],
            Name = reader["Name"].ToString()!,
            Location = reader["Location"] as string,
            IsActive = (bool)reader["IsActive"],
            CreatedAt = (DateTime)reader["CreatedAt"],
            UpdatedAt = (DateTime)reader["UpdatedAt"]
        };
    }

    #endregion

    #region Milking Events

    public List<MilkingEvent> GetMilkingEventsForAnimal(int animalId)
    {
        var events = new List<MilkingEvent>();
        using (var conn = new SqlConnection(_connectionString))
        {
            conn.Open();

            // var cmd = new SqlCommand("SELECT * FROM MilkingEvents WHERE AnimalId = " + animalId + " ORDER BY Timestamp DESC", conn);

            var cmd = new SqlCommand(
                @"SELECT * 
                FROM MilkingEvents
                WHERE AnimalId = @AnimalId
                ORDER BY Timestamp DESC",
                conn
            );

            cmd.Parameters.AddWithValue("@AnimalId", animalId);

            var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                events.Add(MapMilkingEvent(reader));
            }
        }
        return events;
    }

    public MilkingEvent? GetLastMilkingForAnimal(int animalId)
    {
        using (var conn = new SqlConnection(_connectionString))
        {
            conn.Open();
            var cmd = new SqlCommand("SELECT TOP 1 * FROM MilkingEvents WHERE AnimalId = @AnimalId ORDER BY Timestamp DESC", conn);
            cmd.Parameters.AddWithValue("@AnimalId", animalId);
            var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return MapMilkingEvent(reader);
            }
        }
        return null;
    }

    public RecordMilkingResult ProcessMilkingRequest(
        int animalId,
        int robotId,
        DateTime timestamp,
        decimal milkYieldLiters,
        int? duration,
        IRobotNotifier notificationService // I don't like this, but I am out of time!
    )
    {
        using var conn = new SqlConnection(_connectionString);
        conn.Open();

        using var tx = conn.BeginTransaction();

        try
        {
            // Lock the animal record
            var lockCmd = new SqlCommand(
                @"DECLARE @Result INT;

                EXEC @Result = sp_getapplock
                    @Resource = @Resource,
                    @LockMode = 'Exclusive',
                    @LockOwner = 'Transaction',
                    @LockTimeout = 5000;

                SELECT @Result;",
                conn,
                tx
            );

            lockCmd.Parameters.AddWithValue(
                "@Resource",
                $"Animal_{animalId}"
            );

            var lockResult = (int) lockCmd.ExecuteScalar()!;

            if (lockResult < 0)
            {
                tx.Rollback();

                return new RecordMilkingResult
                {
                    Success = false,
                    ErrorMessage = "Animal is being processed."
                };
            }

            // Check if recently milked
            if (notificationService.WasRecentlyMilked(animalId))
            {
                tx.Rollback();

                return new RecordMilkingResult
                {
                    Success = false,
                    ErrorMessage = "Cow has been recently milked."
                };
            }

            // Get animal identification number
            var animalCmd = new SqlCommand(
                @"SELECT IdentificationNumber
                FROM Animals
                WHERE Id = @AnimalId",
                conn,
                tx
            );

            animalCmd.Parameters.AddWithValue(
                "@AnimalId",
                animalId
            );

            var identificationNumber = (string)animalCmd.ExecuteScalar()!;

            var insertCmd = new SqlCommand(
                @"INSERT INTO MilkingEvents
                (
                    AnimalId,
                    RobotId,
                    Timestamp,
                    MilkYieldLiters,
                    Duration
                )
                VALUES
                (
                    @AnimalId,
                    @RobotId,
                    @Timestamp,
                    @MilkYieldLiters,
                    @Duration
                )",
                conn,
                tx
            );

            insertCmd.Parameters.AddWithValue("@AnimalId", animalId);
            insertCmd.Parameters.AddWithValue("@RobotId", robotId);
            insertCmd.Parameters.AddWithValue("@Timestamp", timestamp);
            insertCmd.Parameters.AddWithValue("@MilkYieldLiters", milkYieldLiters);
            insertCmd.Parameters.AddWithValue("@Duration", (object?)duration ?? DBNull.Value);

            insertCmd.ExecuteNonQuery();

            // Commit before notifying to ensure the cache is not updated if the transaction fails.
            tx.Commit();

            notificationService.NotifyMilkingCompleted(
                new MilkingNotification
                {
                    AnimalId = animalId,
                    RobotId = robotId,
                    Timestamp = timestamp,
                    AnimalIdentificationNumber = identificationNumber
                });

            return new RecordMilkingResult
            {
                Success = true
            };
        }
        catch
        {
            tx.Rollback();

            return new RecordMilkingResult
            {
                Success = false,
                ErrorMessage = "Something failed while processing the milking event."
            };
        }
    }

    public int SaveMilkingEvent(int animalId, int robotId, DateTime timestamp, decimal milkYieldLiters, int? duration)
    {
        using (var conn = new SqlConnection(_connectionString))
        {
            conn.Open();
            var cmd = new SqlCommand(@"INSERT INTO MilkingEvents (AnimalId, RobotId, Timestamp, MilkYieldLiters, Duration) 
                                       OUTPUT INSERTED.Id 
                                       VALUES (@AnimalId, @RobotId, @Timestamp, @MilkYieldLiters, @Duration)", conn);
            cmd.Parameters.AddWithValue("@AnimalId", animalId);
            cmd.Parameters.AddWithValue("@RobotId", robotId);
            cmd.Parameters.AddWithValue("@Timestamp", timestamp);
            cmd.Parameters.AddWithValue("@MilkYieldLiters", milkYieldLiters);
            cmd.Parameters.AddWithValue("@Duration", (object?)duration ?? DBNull.Value);

            return (int)cmd.ExecuteScalar();
        }
    }

    public List<MilkingEvent> GetRecentMilkingEvents(int hours = 24)
    {
        var events = new List<MilkingEvent>();
        using (var conn = new SqlConnection(_connectionString))
        {
            conn.Open();
            var cutoff = DateTime.UtcNow.AddHours(-hours);

            // var cmd = new SqlCommand("SELECT * FROM MilkingEvents WHERE Timestamp > '" + cutoff.ToString("yyyy-MM-dd HH:mm:ss") + "'", conn);

            var cmd = new SqlCommand(
                "SELECT * FROM MilkingEvents WHERE Timestamp > @Cutoff",
                conn
            );

            cmd.Parameters.AddWithValue("@Cutoff", cutoff);

            var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                events.Add(MapMilkingEvent(reader));
            }
        }
        return events;
    }

    private MilkingEvent MapMilkingEvent(SqlDataReader reader)
    {
        return new MilkingEvent
        {
            Id = (int)reader["Id"],
            AnimalId = (int)reader["AnimalId"],
            RobotId = (int)reader["RobotId"],
            Timestamp = (DateTime)reader["Timestamp"],
            MilkYieldLiters = (decimal)reader["MilkYieldLiters"],
            Duration = reader["Duration"] as int?,
            CreatedAt = (DateTime)reader["CreatedAt"]
        };
    }

    #endregion

    #region Weight Measurements

    public List<WeightMeasurement> GetWeightMeasurementsForAnimal(int animalId)
    {
        var measurements = new List<WeightMeasurement>();
        using (var conn = new SqlConnection(_connectionString))
        {
            conn.Open();

            // var cmd = new SqlCommand("SELECT * FROM WeightMeasurements WHERE AnimalId = " + animalId, conn);

            var cmd = new SqlCommand(
                "SELECT * FROM WeightMeasurements WHERE AnimalId = @AnimalId",
                conn
            );

            cmd.Parameters.AddWithValue("@AnimalId", animalId);

            var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                /*
                measurements.Add(new WeightMeasurement
                {
                    Id = (int)reader["Id"],
                    AnimalId = (int)reader["AnimalId"],
                    RobotId = (int)reader["RobotId"],
                    Timestamp = (DateTime)reader["Timestamp"],
                    WeightKg = (decimal)reader["WeightKg"],
                    CreatedAt = (DateTime)reader["CreatedAt"]
                });
                */
                measurements.Add(MapWeightMeasurement(reader));
            }
        }
        return measurements;
    }

    public int SaveWeightMeasurement(int animalId, int robotId, DateTime timestamp, decimal weightKg)
    {
        using (var conn = new SqlConnection(_connectionString))
        {
            conn.Open();
            var cmd = new SqlCommand(@"INSERT INTO WeightMeasurements (AnimalId, RobotId, Timestamp, WeightKg) 
                                       OUTPUT INSERTED.Id 
                                       VALUES (@AnimalId, @RobotId, @Timestamp, @WeightKg)", conn);

            cmd.Parameters.AddWithValue("@AnimalId", animalId);
            cmd.Parameters.AddWithValue("@RobotId", robotId);
            cmd.Parameters.AddWithValue("@Timestamp", timestamp);
            cmd.Parameters.AddWithValue("@WeightKg", weightKg);

            // Consider returning a DTO.
            return (int)cmd.ExecuteScalar();
        }
    }

    public WeightMeasurement? GetLastWeightForAnimal(int animalId)
    {
        using (var conn = new SqlConnection(_connectionString))
        {
            conn.Open();
            var cmd = new SqlCommand("SELECT TOP 1 * FROM WeightMeasurements WHERE AnimalId = @AnimalId ORDER BY Timestamp DESC", conn);
            cmd.Parameters.AddWithValue("@AnimalId", animalId);
            var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                /*
                return new WeightMeasurement
                {
                    Id = (int)reader["Id"],
                    AnimalId = (int)reader["AnimalId"],
                    RobotId = (int)reader["RobotId"],
                    Timestamp = (DateTime)reader["Timestamp"],
                    WeightKg = (decimal)reader["WeightKg"],
                    CreatedAt = (DateTime)reader["CreatedAt"]
                };
                */
                return MapWeightMeasurement(reader);
            }
        }
        return null;
    }

    private WeightMeasurement MapWeightMeasurement(SqlDataReader reader)
    {
        return new WeightMeasurement
        {
            Id = (int)reader["Id"],
            AnimalId = (int)reader["AnimalId"],
            RobotId = (int)reader["RobotId"],
            Timestamp = (DateTime)reader["Timestamp"],
            WeightKg = (decimal)reader["WeightKg"],
            CreatedAt = (DateTime)reader["CreatedAt"]
        };
    }

    #endregion

    #region Statistics - Legacy methods used by reporting

    public Dictionary<int, decimal> GetTotalMilkYieldByAnimal(DateTime from, DateTime to)
    {
        var result = new Dictionary<int, decimal>();
        var conn = new SqlConnection(_connectionString);

        conn.Open();

        // var cmd = new SqlCommand($"SELECT AnimalId, SUM(MilkYieldLiters) as Total FROM MilkingEvents WHERE Timestamp BETWEEN '{from:yyyy-MM-dd}' AND '{to:yyyy-MM-dd}' GROUP BY AnimalId", conn);

        var cmd = new SqlCommand(
            @"SELECT AnimalId,
            SUM(MilkYieldLiters) AS Total
            FROM MilkingEvents
            WHERE Timestamp BETWEEN @From AND @To
            GROUP BY AnimalId",
            conn
        );

        cmd.Parameters.AddWithValue("@From", from);
        cmd.Parameters.AddWithValue("@To", to);

        var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            result[(int)reader["AnimalId"]] = (decimal)reader["Total"];
        }

        conn.Close();

        return result;
    }

    public double GetAverageMilkYield(int animalId)
    {
        // NOTE: Validate that the animal exists.
        try
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                // var cmd = new SqlCommand("SELECT AVG(MilkYieldLiters) FROM MilkingEvents WHERE AnimalId = " + animalId, conn);

                var cmd = new SqlCommand(
                    @"SELECT AVG(MilkYieldLiters)
                    FROM MilkingEvents
                    WHERE AnimalId = @AnimalId",
                    conn
                );

                cmd.Parameters.AddWithValue("@AnimalId", animalId);

                var result = cmd.ExecuteScalar();

                if (result != DBNull.Value)
                    return Convert.ToDouble(result);
            }
        }
        catch
        {
            // Ignore errors
        }
        return 0;
    }

    #endregion
}

// Not supposed to be here, but I'm out of time and unsure where to put it.
public class RecordMilkingResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}