using Xunit;
using Serveur.Domain.Interfaces;
using Serveur.Application;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;

namespace Serveur.Tests
{
    // NOTE: Tests use a Fake implementation to test the contract defined in the Domain layer.
    
    public class ConnectionManagerTests
    {
        [Fact]
        public void RegisterClient_ShouldSupportMultipleChannelsForSameClientId()
        {
            var connectionManager = new ConnectionManager();
            var clientId = Guid.NewGuid().ToString("D");
            var senderId = Guid.NewGuid().ToString("D");
            var first = Channel.CreateUnbounded<Alarm>();
            var second = Channel.CreateUnbounded<Alarm>();

            connectionManager.RegisterClient(clientId, first.Writer);
            connectionManager.RegisterClient(clientId, second.Writer);

            var alarm = new Alarm(senderId, "Test Alarm");
            Assert.Equal(2, connectionManager.PublishAlarm(alarm, senderId));
        }

        [Fact]
        public void DeregisterClient_ShouldRemoveOnlySelectedChannel()
        {
            var connectionManager = new ConnectionManager();
            var clientId = Guid.NewGuid().ToString("D");
            var senderId = Guid.NewGuid().ToString("D");
            var first = Channel.CreateUnbounded<Alarm>();
            var second = Channel.CreateUnbounded<Alarm>();

            connectionManager.RegisterClient(clientId, first.Writer);
            connectionManager.RegisterClient(clientId, second.Writer);
            connectionManager.DeregisterClient(clientId, first.Writer);

            Assert.Equal(1, connectionManager.PublishAlarm(new Alarm(senderId, "Test"), senderId));
        }

        [Fact]
        public async Task PublishAlarm_ShouldDeliverToAllRecipientsExceptSender()
        {
            var connectionManager = new ConnectionManager();
            var senderId = Guid.NewGuid().ToString("D");
            var recipientId = Guid.NewGuid().ToString("D");
            var sender = Channel.CreateUnbounded<Alarm>();
            var recipient = Channel.CreateUnbounded<Alarm>();

            connectionManager.RegisterClient(senderId, sender.Writer);
            connectionManager.RegisterClient(recipientId, recipient.Writer);
            var alarm = new Alarm(senderId, "Test Alarm");

            Assert.Equal(1, connectionManager.PublishAlarm(alarm, senderId));
            Assert.True(await recipient.Reader.WaitToReadAsync());
            Assert.Equal(alarm, await recipient.Reader.ReadAsync());
            Assert.False(sender.Reader.TryRead(out _));
        }

        [Fact]
        public void PublishAlarm_ShouldExcludeSenderRegardlessOfUuidCase()
        {
            var connectionManager = new ConnectionManager();
            var senderId = "11111111-1111-1111-1111-111111111111";
            var recipientId = "22222222-2222-2222-2222-222222222222";
            var sender = Channel.CreateUnbounded<Alarm>();
            var recipient = Channel.CreateUnbounded<Alarm>();

            connectionManager.RegisterClient(senderId, sender.Writer);
            connectionManager.RegisterClient(recipientId, recipient.Writer);

            Assert.Equal(
                1,
                connectionManager.PublishAlarm(
                    new Alarm(senderId, "Test"),
                    senderId.ToUpperInvariant()));
            Assert.False(sender.Reader.TryRead(out _));
            Assert.True(recipient.Reader.TryRead(out _));
        }
    }
}
