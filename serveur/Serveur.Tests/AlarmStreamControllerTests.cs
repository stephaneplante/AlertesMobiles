using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serveur.Api.Controllers;
using Serveur.Application;
using Serveur.Domain.Interfaces;
using System.Text;
using Xunit;

namespace Serveur.Tests;

public class AlarmStreamControllerTests
{
    [Fact]
    public async Task StreamAlarms_WithValidUuid_ReturnsSseHeadersAndPushesEvent()
    {
        var manager = new ConnectionManager();
        var controller = new AlarmStreamController(manager);
        var context = new DefaultHttpContext();
        context.Request.Headers.Accept = "text/event-stream";
        context.Response.Body = new MemoryStream();

        using var cancellation = new CancellationTokenSource();
        context.RequestAborted = cancellation.Token;
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = context
        };

        var streamTask = controller.StreamAlarms("11111111-1111-1111-1111-111111111111");

        await WaitUntilAsync(() => context.Response.StatusCode == StatusCodes.Status200OK);

        Assert.Equal("text/event-stream", context.Response.ContentType);
        Assert.Equal("no-cache", context.Response.Headers.CacheControl);

        manager.PublishAlarm(
            new Alarm("22222222-2222-2222-2222-222222222222", "Test alarm"),
            "22222222-2222-2222-2222-222222222222");

        await WaitUntilAsync(() =>
        {
            context.Response.Body.Position = 0;
            using var reader = new StreamReader(context.Response.Body, Encoding.UTF8, leaveOpen: true);
            return reader.ReadToEnd().Contains("event: alarme", StringComparison.Ordinal);
        });

        cancellation.Cancel();
        await streamTask;
    }

    [Theory]
    [InlineData("not-a-uuid")]
    [InlineData("")]
    public async Task StreamAlarms_WithInvalidUuid_ReturnsBadRequest(string clientId)
    {
        var controller = new AlarmStreamController(new ConnectionManager());
        var context = new DefaultHttpContext();
        context.Request.Headers.Accept = "text/event-stream";
        context.Response.Body = new MemoryStream();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = context
        };

        await controller.StreamAlarms(clientId);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8, leaveOpen: true);
        Assert.Contains("valid UUID", reader.ReadToEnd());
    }

    [Fact]
    public async Task StreamAlarms_WithWrongAcceptHeader_ReturnsNotAcceptable()
    {
        var controller = new AlarmStreamController(new ConnectionManager());
        var context = new DefaultHttpContext();
        context.Request.Headers.Accept = "application/json";
        context.Response.Body = new MemoryStream();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = context
        };

        await controller.StreamAlarms("11111111-1111-1111-1111-111111111111");

        Assert.Equal(StatusCodes.Status406NotAcceptable, context.Response.StatusCode);
    }

    private static async Task WaitUntilAsync(Func<bool> predicate)
    {
        var timeout = DateTime.UtcNow.AddSeconds(2);
        while (!predicate())
        {
            if (DateTime.UtcNow >= timeout)
            {
                throw new TimeoutException("The stream did not reach the expected state.");
            }

            await Task.Delay(10);
        }
    }
}
