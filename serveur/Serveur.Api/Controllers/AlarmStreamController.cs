using Microsoft.AspNetCore.Mvc;
using System.Threading.Channels;
using System.Text.Json;
using Serveur.Domain.Interfaces;

namespace Serveur.Api.Controllers
{
    [ApiController]
    [Route("api/alarms")]
    public class AlarmStreamController : ControllerBase
    {
        private readonly IConnectionManager _connectionManager;

        // Inject the Connection Manager
        public AlarmStreamController(IConnectionManager connectionManager)
        {
            _connectionManager = connectionManager;
        }

        /// <summary>
        /// Opens a real-time alarm stream for a specific client ID.
        /// Endpoint: GET /api/alarms/stream?clientId={uuid}
        /// </summary>
        /// <param name="clientId">The unique identifier of the client device.</param>
        [HttpGet("stream")]
        public async Task StreamAlarms([FromQuery] string clientId)
        {
            if (string.IsNullOrWhiteSpace(clientId) ||
                !Guid.TryParseExact(clientId, "D", out _))
            {
                HttpContext.Response.StatusCode = 400;
                await HttpContext.Response.WriteAsync("Client ID must be a valid UUID.");
                return;
            }

            var acceptHeader = HttpContext.Request.Headers.Accept.ToString();
            if (!string.IsNullOrWhiteSpace(acceptHeader) &&
                !acceptHeader.Contains("text/event-stream", StringComparison.OrdinalIgnoreCase))
            {
                HttpContext.Response.StatusCode = 406;
                await HttpContext.Response.WriteAsync("Accept must include text/event-stream.");
                return;
            }

            // Set necessary headers for SSE
            HttpContext.Response.ContentType = "text/event-stream";
            HttpContext.Response.Headers["Cache-Control"] = "no-cache";
            HttpContext.Response.Headers["Connection"] = "keep-alive";
            HttpContext.Response.Headers["X-Accel-Buffering"] = "no";
            HttpContext.Response.StatusCode = 200;
            await HttpContext.Response.StartAsync(HttpContext.RequestAborted);
            await HttpContext.Response.Body.FlushAsync(HttpContext.RequestAborted);

            // 1. Create a channel for this connection
            var channel = Channel.CreateUnbounded<Alarm>();
            var writer = channel.Writer;
            var reader = channel.Reader;

            // 2. Register the client connection
            _connectionManager.RegisterClient(clientId, writer);

            try
            {
                while (await reader.WaitToReadAsync(HttpContext.RequestAborted))
                {
                    while (reader.TryRead(out var alarm))
                    {
                        var data = JsonSerializer.Serialize(new
                        {
                            clientId = alarm.ClientId,
                            message = alarm.Message
                        });

                        await HttpContext.Response.WriteAsync(
                            $"event: alarme\ndata: {data}\n\n",
                            HttpContext.RequestAborted);
                        await HttpContext.Response.Body.FlushAsync(HttpContext.RequestAborted);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Clean shutdown when the client disconnects (Criteria S1.2)
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"[Stream] Error for client {clientId}: {ex.Message}");
            }
            finally
            {
                _connectionManager.DeregisterClient(clientId, writer);
            }
        }
    }
}
