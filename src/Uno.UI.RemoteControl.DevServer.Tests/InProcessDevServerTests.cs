extern alias RemoteServerCore;

using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using Uno.UI.RemoteControl.Host;
using Uno.UI.RemoteControl.HotReload.Messages;
using Uno.UI.RemoteControl.Messaging;
using Uno.UI.RemoteControl.Messaging.IdeChannel;

using RemoteServerCore::DevServerCore;
using RemoteServerCore::Uno.UI.RemoteControl.ServerCore;

using DiscoveredProcessor = RemoteServerCore::Uno.UI.RemoteControl.Messages.DiscoveredProcessor;
using ProcessorsDiscovery = RemoteServerCore::Uno.UI.RemoteControl.Messages.ProcessorsDiscovery;
using ProcessorsDiscoveryResponse = RemoteServerCore::Uno.UI.RemoteControl.Messages.ProcessorsDiscoveryResponse;

namespace Uno.UI.RemoteControl.DevServer.Tests;

[TestClass]
public sealed class InProcessDevServerTests
{
	private const short ProtocolVersion = 1;

	private static readonly RemoteControlConnectionDescriptor _descriptor = new(
		"rainbow-transport",
		"denim-loopback-42",
		new Dictionary<string, string>
		{
			["shadock"] = "ga",
			["preferredHue"] = "rainbow-denim"
		});

	[TestMethod]
	public async Task ConnectApplication_ShouldRespondToDiscoveryFrames()
	{
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
		var processorFactory = new RecordingProcessorFactory();

		await using var devserver = InProcessDevServer.Create(options =>
		{
			options.ProcessorFactoryFactory = _ => processorFactory;
			options.ConfigurationValues["Telemetry:Rainbow"] = "42";
		});

		using var transport = devserver.ConnectApplication(_descriptor, cts.Token);

		var assemblyPath = Path.Combine(Path.GetTempPath(), "rainbow", "42-ga-bu-zo-meu");
		var discovery = new ProcessorsDiscovery(assemblyPath, "Super42");

		await transport.SendAsync(
			Frame.Create(ProtocolVersion, WellKnownScopes.DevServerChannel, ProcessorsDiscovery.Name, discovery),
			cts.Token);

		var responseFrame = await WaitForTransportFrameAsync(
			transport,
			static frame => frame is { Scope: WellKnownScopes.DevServerChannel, Name: ProcessorsDiscoveryResponse.Name },
			cts.Token);

		var response = JsonConvert.DeserializeObject<ProcessorsDiscoveryResponse>(responseFrame.Content);
		response.Should().NotBeNull("devserver must answer discovery requests");
		response!.Assemblies.Should().Contain(assemblyPath);
		response.Processors.Should().ContainSingle(p => p.Type.Contains(nameof(InProcessInternalTestProcessor)));

		processorFactory.Discoveries
			.Should()
			.ContainSingle(d => d.BasePath == assemblyPath && d.AppInstanceId == "Super42");
	}

	[TestMethod]
	public async Task ConnectApplication_ShouldRouteRuntimeFramesToProcessors()
	{
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
		var processorFactory = new RecordingProcessorFactory();

		await using var devserver = InProcessDevServer.Create(options =>
		{
			options.ProcessorFactoryFactory = _ => processorFactory;
		});

		using var transport = devserver.ConnectApplication(_descriptor, cts.Token);

		var discoPath = Path.Combine(Path.GetTempPath(), "rainbow", "denim-zo-42");
		var discoAppInstance = "rainbow-zo-42";
		await transport.SendAsync(
			Frame.Create(ProtocolVersion, WellKnownScopes.DevServerChannel, ProcessorsDiscovery.Name, new ProcessorsDiscovery(discoPath, discoAppInstance)),
			cts.Token);

		await WaitForTransportFrameAsync(
			transport,
			static frame => frame is { Scope: WellKnownScopes.DevServerChannel, Name: ProcessorsDiscoveryResponse.Name },
			cts.Token);

		var denimProcessor = processorFactory.LastDenimProcessor;
		denimProcessor.Should().NotBeNull("discovery should materialize a processor instance");

		var payload = new
		{
			bib = 42,
			shade = "denim",
			flag = "rainbow"
		};

		await transport.SendAsync(
			Frame.Create(ProtocolVersion, denimProcessor!.Scope, "RainbowBibPing", payload),
			cts.Token);

		var processedFrame = await denimProcessor.WaitForFrameAsync(cts.Token);
		processedFrame.Name.Should().Be("RainbowBibPing");
		processedFrame.Content.Should().Contain("denim");
	}

	private static async Task<Frame> WaitForTransportFrameAsync(
		IFrameTransport transport,
		Func<Frame, bool> predicate,
		CancellationToken ct)
	{
		while (true)
		{
			var frame = await transport.ReceiveAsync(ct).ConfigureAwait(false);
			if (frame is null)
			{
				throw new InvalidOperationException("Transport closed before receiving the expected frame.");
			}

			if (predicate(frame))
			{
				return frame;
			}
		}
	}

	private sealed class RecordingProcessorFactory : IRemoteControlProcessorFactory
	{
		private readonly ConcurrentQueue<ProcessorsDiscovery> _discoveries = new();
		private readonly ConcurrentQueue<IServerProcessor> _processors = new();

		public IReadOnlyList<ProcessorsDiscovery> Discoveries => [.. _discoveries];

		public InProcessInternalTestProcessor? LastDenimProcessor => _processors.ToArray().OfType<InProcessInternalTestProcessor>().LastOrDefault();

		public ValueTask<RemoteControlProcessorDiscoveryResult> DiscoverProcessorsAsync(ProcessorsDiscovery discovery, CancellationToken ct)
		{
			ct.ThrowIfCancellationRequested();
			_discoveries.Enqueue(discovery);

			var processor = new InProcessInternalTestProcessor();
			_processors.Enqueue(processor);

			var assemblies = ImmutableArray.Create(discovery.BasePath);
			var processors = ImmutableArray.Create(new DiscoveredProcessor(discovery.BasePath, processor.GetType().FullName ?? nameof(InProcessInternalTestProcessor), "42.0.0-rainbow", true));
			var result = new RemoteControlProcessorDiscoveryResult(assemblies, processors, [processor]);
			return ValueTask.FromResult(result);
		}
	}

	private sealed class InProcessInternalTestProcessor : IServerProcessor
	{
		private readonly ConcurrentQueue<Frame> _frames = new();
		private readonly SemaphoreSlim _frameSignal = new(0);

		public string Scope => nameof(InProcessInternalTestProcessor) + "RainbowDenimScope";

		public Task ProcessFrame(Frame frame)
		{
			_frames.Enqueue(frame);
			_frameSignal.Release();
			return Task.CompletedTask;
		}

		public Task ProcessIdeMessage(IdeMessage message, CancellationToken ct)
			=> Task.CompletedTask;

		public async Task<Frame> WaitForFrameAsync(CancellationToken ct)
		{
			await _frameSignal.WaitAsync(ct).ConfigureAwait(false);
			if (_frames.TryDequeue(out var frame))
			{
				return frame;
			}

			throw new InvalidOperationException("Frame signal triggered without payload.");
		}

		public void Dispose()
			=> _frameSignal.Dispose();
	}
}
