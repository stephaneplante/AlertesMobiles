using System.Threading.Channels;

namespace Serveur.Domain.Interfaces
{
    /// <summary>
    /// Interface for managing active SSE client connections.
    /// </summary>
    public interface IConnectionManager
    {
        /// <summary>
        /// Registers a new client connection.
        /// </summary>
        /// <param name="clientId">Unique ID of the client.</param>
        /// <param name="writer">The writer associated with the connection to register.</param>
        void RegisterClient(string clientId, ChannelWriter<Alarm> writer);

        /// <summary>
        /// Deregisters a client connection.
        /// </summary>
        /// <param name="clientId">Unique ID of the client.</param>
        /// <param name="writer">The writer associated with the connection to deregister.</param>
        void DeregisterClient(string clientId, ChannelWriter<Alarm> writer);

        /// <summary>
        /// Pushes an alarm event to all connected clients, excluding the sender.
        /// </summary>
        /// <param name="alarmData">The alarm message.</param>
        /// <param name="senderClientId">The ID of the client that emitted the alarm.</param>
        /// <returns>The count of recipients.</returns>
        int PublishAlarm(Alarm alarmData, string senderClientId);
    }

    // Simple data structure for the alarm
    public record Alarm(string ClientId, string Message);
}
