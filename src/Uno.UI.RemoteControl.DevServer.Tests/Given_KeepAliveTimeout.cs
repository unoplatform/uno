extern alias RemoteServerCore;

using System;
using System.Threading;
using System.Threading.Tasks;
using Uno.UI.RemoteControl.Messaging;

using RemoteServerCore::DevServerCore;
using RemoteControlServer = RemoteServerCore::Uno.UI.RemoteControl.Server.RemoteControlServer;

namespace Uno.UI.RemoteControl.DevServer.Tests;

[TestClass]
public sealed class Given_KeepAliveTimeout
{
	[TestMethod]
	public async Task Silent_connection_is_closed_after_keep_alive_timeout()
	{
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

		await using var devserver = InProcessDevServer.Create(options =>
		{
			// Shrink the production ~63s watchdog (KeepAliveMessage.Interval * 2.1) to a
			// sub-second window so the test runs fast.
			options.ConfigurationValues[RemoteControlServer.KeepAliveTimeoutConfigurationKey] = "300";
		});

		using var transport = devserver.ConnectApplication(ct: cts.Token);

		// The client never sends a frame (a silent / half-open peer). With the shortened
		// keep-alive timeout the server must reap the connection: once it closes its end,
		// the client-side receive completes with null. See #24206.
		var frame = await transport.ReceiveAsync(cts.Token);

		frame.Should().BeNull("the server must close a connection that receives no frame within the keep-alive timeout");
	}
}
