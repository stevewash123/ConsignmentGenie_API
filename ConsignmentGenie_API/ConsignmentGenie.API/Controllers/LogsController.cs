using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class LogsController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public LogsController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<IActionResult> GetLogs([FromQuery] int limit = 20)
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");

            using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT timestamp, level, message, exception
                FROM logs
                ORDER BY timestamp DESC
                LIMIT @limit";

            using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@limit", limit);

            var logs = new List<object>();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                logs.Add(new
                {
                    timestamp = reader.IsDBNull(0) ? (DateTime?)null : reader.GetDateTime(0),
                    level = reader.IsDBNull(1) ? (int?)null : reader.GetInt32(1),
                    message = reader.IsDBNull(2) ? null : reader.GetString(2),
                    exception = reader.IsDBNull(3) ? null : reader.GetString(3)
                });
            }

            return Ok(new { success = true, data = logs });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("schema")]
    public async Task<IActionResult> GetTableSchema()
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");

            using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT column_name, data_type
                FROM information_schema.columns
                WHERE table_name = 'logs'
                ORDER BY ordinal_position";

            using var command = new NpgsqlCommand(query, connection);
            var columns = new List<object>();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                columns.Add(new
                {
                    name = reader.GetString(0),
                    type = reader.GetString(1)
                });
            }

            return Ok(new { success = true, data = columns });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchLogs([FromQuery] string search, [FromQuery] int limit = 20)
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");

            using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT timestamp, level, message, exception
                FROM logs
                WHERE (message ILIKE @search OR exception ILIKE @search)
                ORDER BY timestamp DESC
                LIMIT @limit";

            using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@search", $"%{search}%");
            command.Parameters.AddWithValue("@limit", limit);

            var logs = new List<object>();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                logs.Add(new
                {
                    timestamp = reader.IsDBNull(0) ? (DateTime?)null : reader.GetDateTime(0),
                    level = reader.IsDBNull(1) ? (int?)null : reader.GetInt32(1),
                    message = reader.IsDBNull(2) ? null : reader.GetString(2),
                    exception = reader.IsDBNull(3) ? null : reader.GetString(3)
                });
            }

            return Ok(new { success = true, data = logs });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}