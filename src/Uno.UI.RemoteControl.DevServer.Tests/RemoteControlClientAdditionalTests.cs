using Uno.UI.RemoteControl.HotReload;
using Uno.UI.RemoteControl.HotReload.Messages;
using Uno.UI.RemoteControl.Messages;
using Uno.UI.RemoteControl.Messaging;

namespace Uno.UI.RemoteControl.DevServer.Tests;

[TestClass]
[DoNotParallelize]
public sealed class RemoteControlClientAdditionalTests
{
	[TestMethod]
	public async Task CreateAdditional_ShouldNotReplaceDefaultInstance_AndShouldDisableHotReloadByDefaultAsync()
	{
		var defaultClient = RemoteControlClient.Initialize(typeof(object), endpoints: null);

		try
		{
			using var pair = FrameTransportPair.Create();
			await using var additionalClient = RemoteControlClient.CreateAdditional(typeof(object), pair.Peer1);

			RemoteControlClient.Instance.Should().BeSameAs(defaultClient);
			additionalClient.Processors.Should().NotContain(x => x is ClientHotReloadProcessor);
			RemoteControlClientOptions.AdditionalClient.EnableHotReloadProcessor.Should().BeFalse();
			RemoteControlClientOptions.AdditionalClient.RegisterDiagnosticView.Should().BeFalse();
			RemoteControlClientOptions.AdditionalClient.AutoRegisterAppIdentity.Should().BeFalse();
		}
		finally
		{
			await defaultClient.DisposeAsync();
		}
	}

	[TestMethod]
	public async Task CreateAdditional_ShouldUseProvidedTransportAsync()
	{
		using var pair = FrameTransportPair.Create();
		await using var additionalClient = RemoteControlClient.CreateAdditional(typeof(object), pair.Peer1);
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

		var message = new ProcessorsDiscovery("D:/tests/path");
		await additionalClient.SendMessage(message);

		var frame = await ReceiveUntilAsync(
			pair.Peer2,
			static f => f.Scope == WellKnownScopes.DevServerChannel && f.Name == ProcessorsDiscovery.Name,
			cts.Token);

		frame.Should().NotBeNull();
		frame!.TryGetContent<ProcessorsDiscovery>(out var received).Should().BeTrue();
		received!.BasePath.Should().Be("D:/tests/path");
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
