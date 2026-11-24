using Serveur.Domain.Interfaces;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Serveur.Application
{
    /// <summary>
    /// Manages the real-time connections for alarm push (SSE).
    /// Stores a channel writer for each connected client ID.
    /// </summary>
    public class ConnectionManager : IConnectionManager
    {
        // Concurrent dictionary to store client IDs and a list of their output channels (writers).
        private readonly ConcurrentDictionary<string, List<ChannelWriter<Alarm>>> _clientChannels = new();
        private readonly Lock _lock = new();

        private static string NormalizeClientId(string clientId)
        {
            if (string.IsNullOrWhiteSpace(clientId))
            {
                throw new ArgumentException("Client ID is required.", nameof(clientId));
            }

            if (!Guid.TryParse(clientId, out var guid))
            {
                throw new ArgumentException("Client ID must be a valid UUID.", nameof(clientId));
            }

            return guid.ToString("D");
        }
        
        public void RegisterClient(string clientId, ChannelWriter<Alarm> writer)
        {
            ArgumentNullException.ThrowIfNull(writer);
            var normalizedClientId = NormalizeClientId(clientId);

            lock (_lock)
            {
                if (!_clientChannels.ContainsKey(normalizedClientId))
                {
                    _clientChannels[normalizedClientId] = new List<ChannelWriter<Alarm>>();
                }
                _clientChannels[normalizedClientId].Add(writer);
            }

            Console.WriteLine($"[ConnectionManager] Client {normalizedClientId} registered successfully.");
        }

        public void DeregisterClient(string clientId, ChannelWriter<Alarm> writer)
        {
            ArgumentNullException.ThrowIfNull(writer);
            var normalizedClientId = NormalizeClientId(clientId);

            lock (_lock)
            {
                if (_clientChannels.TryGetValue(normalizedClientId, out var writers))
                {
                    writers.Remove(writer);
                    if (writers.Count == 0)
                    {
                        _clientChannels.TryRemove(normalizedClientId, out _);
                    }
                }
            }
            writer.TryComplete();
            Console.WriteLine($"[ConnectionManager] Client {normalizedClientId} deregistered.");
        }

        public int PublishAlarm(Alarm alarmData, string senderClientId)
        {
            int recipientCount = 0;
            var normalizedSenderClientId = NormalizeClientId(senderClientId);

            // Snapshot of the dictionary to iterate over safely
            List<KeyValuePair<string, List<ChannelWriter<Alarm>>>> snapshot;
            lock (_lock)
            {
                snapshot = new List<KeyValuePair<string, List<ChannelWriter<Alarm>>>>(_clientChannels);
            }

            foreach (var currentClientId in snapshot.Select(kvp => kvp.Key))
            {
                if (currentClientId == normalizedSenderClientId)
                {
                    continue;
                }

                // Take a snapshot of the writers for this client
                List<ChannelWriter<Alarm>> writersSnapshot;
                lock (_lock)
                {
                    if (_clientChannels.TryGetValue(currentClientId, out var writers))
                    {
                        writersSnapshot = new List<ChannelWriter<Alarm>>(writers);
                    }
                    else
                    {
                        continue;
                    }
                }

                foreach (var writer in writersSnapshot)
                {
                    if (writer.TryWrite(alarmData))
                    {
                        recipientCount++;
                    }
                    else
                    {
                        // If writing fails, the channel is likely closed/completed.
                        // We should clean up this broken connection.
                        DeregisterClient(currentClientId, writer);
                    }
                }
            }

            return recipientCount;
        }
    }
}
