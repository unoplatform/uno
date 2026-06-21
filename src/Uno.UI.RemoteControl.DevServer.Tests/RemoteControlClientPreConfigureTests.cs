extern alias RemoteServerCore;

using RemoteServerCore::DevServerCore;
using Uno.UI.RemoteControl.HotReload;
using Uno.UI.RemoteControl.HotReload.Messages;
using Uno.UI.RemoteControl.Messages;
using Uno.UI.RemoteControl.Messaging;

namespace Uno.UI.RemoteControl.DevServer.Tests;

[TestClass]
[DoNotParallelize]
public sealed class RemoteControlClientPreConfigureTests
{
	[TestMethod]
	public async Task PreConfigureNextInstance_ShouldSetInstanceInNestedALC()
	{
		using var pair = FrameTransportPair.Create();

		RemoteControlClient.PreConfigureNextInstance(new RemoteControlClientOptions
		{
			SetAsDefaultInstance = true,
			EnableHotReloadProcessor = true,
			RegisterDiagnosticView = false,
			AutoRegisterAppIdentity = false,
			ConnectionTransportOverride = pair.Peer1,
		});

		var client = RemoteControlClient.Initialize(typeof(RemoteControlClientPreConfigureTests));

		try
		{
			RemoteControlClient.Instance.Should().BeSameAs(client, "PreConfigureNextInstance with SetAsDefaultInstance=true should set Instance");
		}
		finally
		{
			await client.DisposeAsync();
		}
	}

	[TestMethod]
	public async Task PreConfigureNextInstance_ShouldUseProvidedTransport()
	{
		using var pair = FrameTransportPair.Create();
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

		RemoteControlClient.PreConfigureNextInstance(new RemoteControlClientOptions
		{
			SetAsDefaultInstance = true,
			EnableHotReloadProcessor = true,
			RegisterDiagnosticView = false,
			AutoRegisterAppIdentity = false,
			ConnectionTransportOverride = pair.Peer1,
		});

		var client = RemoteControlClient.Initialize(typeof(RemoteControlClientPreConfigureTests));

		try
		{
			var message = new ProcessorsDiscovery("D:/tests/preconfigure-path");
			await client.SendMessage(message);

			var frame = await ReceiveUntilAsync(
				pair.Peer2,
				static f => f.Scope == WellKnownScopes.DevServerChannel && f.Name == ProcessorsDiscovery.Name,
				cts.Token);

			frame.Should().NotBeNull();
			frame!.TryGetContent<ProcessorsDiscovery>(out var received).Should().BeTrue();
			received!.BasePath.Should().Be("D:/tests/preconfigure-path");
		}
		finally
		{
			await client.DisposeAsync();
		}
	}

	[TestMethod]
	public async Task PreConfigureNextInstance_ShouldEnableHotReloadProcessor()
	{
		using var pair = FrameTransportPair.Create();

		RemoteControlClient.PreConfigureNextInstance(new RemoteControlClientOptions
		{
			SetAsDefaultInstance = true,
			EnableHotReloadProcessor = true,
			RegisterDiagnosticView = false,
			AutoRegisterAppIdentity = false,
			ConnectionTransportOverride = pair.Peer1,
		});

		var client = RemoteControlClient.Initialize(typeof(RemoteControlClientPreConfigureTests));

		try
		{
			client.Processors.Should().Contain(x => x is ClientHotReloadProcessor, "EnableHotReloadProcessor=true should register the hot reload processor");
		}
		finally
		{
			await client.DisposeAsync();
		}
	}

	[TestMethod]
	public async Task PreConfigureNextInstance_ShouldBeConsumedOnce()
	{
		using var pair = FrameTransportPair.Create();

		RemoteControlClient.PreConfigureNextInstance(new RemoteControlClientOptions
		{
			SetAsDefaultInstance = true,
			EnableHotReloadProcessor = true,
			RegisterDiagnosticView = false,
			AutoRegisterAppIdentity = false,
			ConnectionTransportOverride = pair.Peer1,
		});

		// First Initialize should consume the pending options.
		var client1 = RemoteControlClient.Initialize(typeof(RemoteControlClientPreConfigureTests));

		try
		{
			RemoteControlClient.Instance.Should().BeSameAs(client1);

			// Second Initialize should use default behavior (no pending options).
			var client2 = RemoteControlClient.Initialize(typeof(RemoteControlClientPreConfigureTests));

			try
			{
				// client2 is also a default client, so it replaces Instance.
				RemoteControlClient.Instance.Should().BeSameAs(client2);
			}
			finally
			{
				await client2.DisposeAsync();
			}
		}
		finally
		{
			await client1.DisposeAsync();
		}
	}

	[TestMethod]
	public async Task PreConfigureNextInstance_WithSetAsDefaultFalse_ShouldNotSetInstance()
	{
		// First create a default instance so we can verify it's not replaced.
		var defaultClient = RemoteControlClient.Initialize(typeof(object), endpoints: null);

		try
		{
			using var pair = FrameTransportPair.Create();

			RemoteControlClient.PreConfigureNextInstance(new RemoteControlClientOptions
			{
				SetAsDefaultInstance = false,
				EnableHotReloadProcessor = true,
				RegisterDiagnosticView = false,
				AutoRegisterAppIdentity = false,
				ConnectionTransportOverride = pair.Peer1,
			});

			var nestedClient = RemoteControlClient.Initialize(typeof(RemoteControlClientPreConfigureTests));

			try
			{
				RemoteControlClient.Instance.Should().BeSameAs(defaultClient, "SetAsDefaultInstance=false should not replace Instance");
			}
			finally
			{
				await nestedClient.DisposeAsync();
			}
		}
		finally
		{
			await defaultClient.DisposeAsync();
		}
	}

	[TestMethod]
	public async Task PreConfigureNextInstance_ShouldAppearInActiveClients()
	{
		using var pair = FrameTransportPair.Create();

		RemoteControlClient.PreConfigureNextInstance(new RemoteControlClientOptions
		{
			SetAsDefaultInstance = true,
			EnableHotReloadProcessor = true,
			RegisterDiagnosticView = false,
			AutoRegisterAppIdentity = false,
			ConnectionTransportOverride = pair.Peer1,
		});

		var client = RemoteControlClient.Initialize(typeof(RemoteControlClientPreConfigureTests));

		try
		{
			RemoteControlClient.ActiveClients.Should().Contain(client);
		}
		finally
		{
			await client.DisposeAsync();
		}
	}

	private static async Task<Frame?> ReceiveUntilAsync(
		IFrameTransport transport,
		Func<Frame, bool> predicate,
		CancellationToken ct)
	{
		while (!ct.IsCancellationRequested)
		{
			var frame = await transport.ReceiveAsync(ct);
			if (frame is null)
			{
				return null;
			}

			if (predicate(frame))
			{
				return frame;
			}
		}

		return null;
	}
}
