//using Microsoft.Azure.Functions.Worker;
//using Microsoft.Extensions.Logging;
//using Microsoft.Data.SqlClient;
//using Microsoft.AspNetCore.Identity;
//using SqlToDataverseSync.Services;
//using System.Text.Json;
//using System.Text;

//namespace SqlToDataverseSync;

//public class SyncFunction
//{
//    private readonly ILogger _logger;
//    private readonly SyncStateService _syncStateService = new();
//    static readonly HttpClient client = new HttpClient();




//    public SyncFunction(ILoggerFactory loggerFactory)
//    {
//        _logger = loggerFactory.CreateLogger<SyncFunction>();
//    }

//    [Function("SqlTimerFunction")]
//    public async Task Run([TimerTrigger("*/60 * * * * *")] TimerInfo timer,ILogger log)
//    {
//        _logger.LogInformation("Timer Triggered:");

//        string connStr = Environment.GetEnvironmentVariable("SqlConnectionString");
//        string flowUrl = Environment.GetEnvironmentVariable("FlowUrl");
//        long lastVersion = _syncStateService.GetLastVersion();

//        using var conn = new SqlConnection(connStr);
//        await conn.OpenAsync();
//        string query = @"
//            SELECT s.StudentId, s.FullName, s.Email, s.Age,
//                   ct.SYS_CHANGE_OPERATION
//            FROM CHANGETABLE(CHANGES Students, @lastVersion) ct
//            LEFT JOIN Students s
//            ON s.StudentId = ct.StudentId";

//        using var cmd = new SqlCommand(query, conn);
//        cmd.Parameters.AddWithValue("@lastVersion", lastVersion);

//         var reader = await cmd.ExecuteReaderAsync();
//        int count = 0;
//        while (await reader.ReadAsync())
//        {
//            var student = new
//            {
//                StudentId = reader["StudentId"],
//                FullName = reader["FullName"]?.ToString(),
//                Email = reader["Email"]?.ToString(),
//                Age = reader["Age"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Age"]),
//                Operation = reader["SYS_CHANGE_OPERATION"].ToString()
//            };

//            string json = JsonSerializer.Serialize(student);
//            var content = new StringContent(json, Encoding.UTF8, "application/json");
//            HttpResponseMessage response = await client.PostAsync(flowUrl, content);
//            log.LogInformation($"Sent {student.StudentId} → {student.Operation} → {response.StatusCode}");

//        }
//        await reader.CloseAsync();

//         // Get the latest version from the database
//         using var versionCmd = new SqlCommand("SELECT CHANGE_TRACKING_CURRENT_VERSION()", conn);
//        long currentVersion = (long)await versionCmd.ExecuteScalarAsync();
//        _syncStateService.SaveVersion(currentVersion);


//        log.LogInformation($"Saved version: {currentVersion}");




//    }
//}

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using SqlToDataverseSync.Services;
using System.Text.Json;
using System.Text;

namespace SqlToDataverseSync;

public class SyncFunction
{
    private readonly ILogger _logger;
    private readonly SyncStateService _syncStateService = new();
    private static readonly HttpClient client = new HttpClient();

    public SyncFunction(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<SyncFunction>();
    }

    [Function("SqlTimerFunction")]
    public async Task Run(
        [TimerTrigger("0 */1 * * * *")] TimerInfo timer, // every 1 minute
        ILogger log)
    {
        _logger.LogInformation("========== SYNC STARTED ==========");

        string connStr = Environment.GetEnvironmentVariable("SqlConnectionString");
        string flowUrl = Environment.GetEnvironmentVariable("FlowUrl");

        long lastVersion = _syncStateService.GetLastVersion();

        _logger.LogInformation($"Last Sync Version: {lastVersion}");

        using var conn = new SqlConnection(connStr);
        await conn.OpenAsync();

        // IMPORTANT: include SYS_CHANGE_VERSION
        string query = @"
            SELECT 
                s.StudentId,
                s.FullName,
                s.Email,
                s.Age,
                ct.SYS_CHANGE_OPERATION,
                ct.SYS_CHANGE_VERSION
            FROM CHANGETABLE(CHANGES Students, @lastVersion) ct
            LEFT JOIN Students s
                ON s.StudentId = ct.StudentId";

        using var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@lastVersion", lastVersion);

        using var reader = await cmd.ExecuteReaderAsync();

        long maxVersion = lastVersion;
        int count = 0;

        while (await reader.ReadAsync())
        {
            count++;

            string operation = reader["SYS_CHANGE_OPERATION"].ToString();
            long changeVersion = Convert.ToInt64(reader["SYS_CHANGE_VERSION"]);

            // track highest processed version
            if (changeVersion > maxVersion)
                maxVersion = changeVersion;

            var student = new
            {
                StudentId = reader["StudentId"],
                FullName = reader["FullName"]?.ToString(),
                Email = reader["Email"]?.ToString(),
                Age = reader["Age"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Age"]),
                Operation = operation
            };

            string json = JsonSerializer.Serialize(student);

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync(flowUrl, content);

            _logger.LogInformation(
                $"Sent → ID:{student.StudentId} | Op:{operation} | Ver:{changeVersion} | Status:{response.StatusCode}");
        }

        await reader.CloseAsync();

        // SAVE ONLY processed version (VERY IMPORTANT)
        if (maxVersion > lastVersion)
        {
            _syncStateService.SaveVersion(maxVersion);
            _logger.LogInformation($"Saved New Version: {maxVersion}");
        }
        else
        {
            _logger.LogInformation("No changes found");
        }

        _logger.LogInformation($"Total Records Synced: {count}");
        _logger.LogInformation("========== SYNC FINISHED ==========");
    }
}

