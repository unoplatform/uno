extern alias RemoteServerCore;

using RemoteServerCore::DevServerCore;
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

	[TestMethod]
	public async Task CreateAdditional_ShouldNotContaminateDefaultStatusAsync()
	{
		var defaultClient = RemoteControlClient.Initialize(typeof(object), endpoints: null);
		var defaultStatusChanged = 0;
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

		void OnStatusChanged(object? _, RemoteControlStatus __)
			=> Interlocked.Increment(ref defaultStatusChanged);

		defaultClient.StatusChanged += OnStatusChanged;

		try
		{
			using var pair = FrameTransportPair.Create();
			await using var additionalClient = RemoteControlClient.CreateAdditional(typeof(object), pair.Peer1);

			await additionalClient.SendMessage(new ProcessorsDiscovery("D:/tests/path-2"));
			await ReceiveUntilAsync(
				pair.Peer2,
				static f => f.Scope == WellKnownScopes.DevServerChannel && f.Name == ProcessorsDiscovery.Name,
				cts.Token);

			defaultStatusChanged.Should().Be(0, "additional client lifecycle should not trigger status changes on the default client");
		}
		finally
		{
			defaultClient.StatusChanged -= OnStatusChanged;
			await defaultClient.DisposeAsync();
		}
	}

	[TestMethod]
	public async Task CreateAdditional_Dispose_ShouldKeepDefaultInstanceAsync()
	{
		var defaultClient = RemoteControlClient.Initialize(typeof(object), endpoints: null);

		try
		{
			using var pair = FrameTransportPair.Create();
			var additionalClient = RemoteControlClient.CreateAdditional(typeof(object), pair.Peer1);
			await additionalClient.DisposeAsync();

			RemoteControlClient.Instance.Should().BeSameAs(defaultClient);
		}
		finally
		{
			await defaultClient.DisposeAsync();
		}
	}

	[TestMethod]
	public async Task CreateAdditional_WithInProcessServer_ShouldRemainIndependentFromDefaultAsync()
	{
		var defaultClient = RemoteControlClient.Initialize(typeof(object), endpoints: null);
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
		await using var devserver = InProcessDevServer.Create();

		try
		{
			var transport = devserver.ConnectApplication(ct: cts.Token);
			await using var additionalClient = RemoteControlClient.CreateAdditional(typeof(RemoteControlClientAdditionalTests), transport);

			RemoteControlClient.Instance.Should().BeSameAs(defaultClient);
			additionalClient.Processors.Should().NotContain(x => x is ClientHotReloadProcessor);
		}
		finally
		{
			await defaultClient.DisposeAsync();
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
