extern alias RemoteServerCore;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Uno.Extensions;
using Uno.UI.RemoteControl;
using Uno.UI.RemoteControl.Host;
using Uno.UI.RemoteControl.HotReload.Messages;
using Uno.UI.RemoteControl.Messages;
using Uno.UI.RemoteControl.Messaging;
using Uno.UI.RemoteControl.Messaging.IdeChannel;
using Uno.UI.RemoteControl.Server.AppLaunch;
using Uno.UI.RemoteControl.Services;
using ServerCoreRemoteControlServer = RemoteServerCore::Uno.UI.RemoteControl.Server.RemoteControlServer;
using ServerCoreRemoteControlServerHost = RemoteServerCore::Uno.UI.RemoteControl.ServerCore.RemoteControlServerHost;
using ServerCoreLaunchMonitor = RemoteServerCore::Uno.UI.RemoteControl.Server.AppLaunch.IApplicationLaunchMonitor;
using ServerCoreTelemetry = RemoteServerCore::Uno.UI.RemoteControl.Server.Telemetry.ITelemetry;
using ServerCoreConfiguration = RemoteServerCore::Uno.UI.RemoteControl.ServerCore.Configuration.IRemoteControlConfiguration;
using ServerCoreProcessorFactory = RemoteServerCore::Uno.UI.RemoteControl.ServerCore.IRemoteControlProcessorFactory;
using ServerCoreProcessorResult = RemoteServerCore::Uno.UI.RemoteControl.ServerCore.RemoteControlProcessorDiscoveryResult;
using ServerCoreProcessorsDiscovery = RemoteServerCore::Uno.UI.RemoteControl.Messages.ProcessorsDiscovery;
using ServerCoreDiscoveredProcessor = RemoteServerCore::Uno.UI.RemoteControl.Messages.DiscoveredProcessor;

[assembly: Uno.UI.RemoteControl.Host.ServerProcessorAttribute(typeof(Uno.UI.RemoteControl.DevServer.Tests.RemoteControlServerBehaviorTests.DiagnosticsAwareProcessor))]
[assembly: Uno.UI.RemoteControl.Host.ServerProcessorAttribute(typeof(Uno.UI.RemoteControl.DevServer.Tests.RemoteControlServerBehaviorTests.IdeRoutingProcessor))]

namespace Uno.UI.RemoteControl.DevServer.Tests;

[TestClass]
public class RemoteControlServerBehaviorTests
{
	private const short ProtocolVersion = 1;

	static RemoteControlServerBehaviorTests()
	{
		LogExtensionPoint.AmbientLoggerFactory ??= LoggerFactory.Create(builder => builder.AddDebug());
	}

	// Ensures the diagnostics sink flowing through DevServerDiagnostics is the instance RemoteControlServer stages
	// before invoking processors, guaranteeing invalid-frame reporting uses the right plumbing.
	[TestMethod]
	public async Task DiagnosticsSink_ShouldBeAssignedDuringProcessorExecution()
	{
		using var context = RemoteControlServerTestContext.Create();
		var appInstanceId = Guid.NewGuid().ToString("N");
		var transport = await context.RunAsync(new[]
		{
			context.CreateDiscoveryFrame(appInstanceId),
			Frame.Create(ProtocolVersion, DiagnosticsAwareProcessor.ScopeName, "probe", new { denim = 42 })
		});

		var expectedSink = typeof(ServerCoreRemoteControlServer)
			.GetNestedType("DiagnosticsSink", BindingFlags.NonPublic);

		expectedSink.Should().NotBeNull();
		var payload = DeserializeSentMessage<DiagnosticsReportMessage>(transport, DiagnosticsReportMessage.MessageName);
		payload.SinkTypeName.Should().Be(expectedSink!.FullName);
	}

	// Validates that app launch frames drive ApplicationLaunchMonitor registration first, then connection success.
	[TestMethod]
	public async Task AppLaunchFrames_ShouldDriveLaunchMonitorLifecycle()
	{
		using var context = RemoteControlServerTestContext.Create();
		var mvid = Guid.NewGuid();

		await context.RunAsync(new[]
		{
			Frame.Create(ProtocolVersion, WellKnownScopes.DevServerChannel, AppLaunchMessage.Name, new AppLaunchMessage
			{
				Mvid = mvid,
				Platform = "Wasm",
				IsDebug = true,
				Ide = "UnitTestIDE",
				Plugin = "unit",
				Step = AppLaunchStep.Launched
			}),
			Frame.Create(ProtocolVersion, WellKnownScopes.DevServerChannel, AppLaunchMessage.Name, new AppLaunchMessage
			{
				Mvid = mvid,
				Platform = "Wasm",
				IsDebug = true,
				Step = AppLaunchStep.Connected
			})
		});

		context.LaunchMonitor.Registrations.Should().ContainSingle();
		context.LaunchMonitor.Connections.Should().ContainSingle();
		context.LaunchMonitor.Registrations[0].Ide.Should().Be("UnitTestIDE");
		context.LaunchMonitor.Connections[0].Mvid.Should().Be(mvid);
	}

	// Confirms a ping frame is answered with the original sequence id while keeping server version data intact.
	[TestMethod]
	public async Task KeepAliveFrames_ShouldEchoSequenceAndPreserveVersion()
	{
		using var context = RemoteControlServerTestContext.Create();
		var sequence = 42UL;
		var transport = await context.RunAsync(new[]
		{
			Frame.Create(ProtocolVersion, WellKnownScopes.DevServerChannel, KeepAliveMessage.Name, new KeepAliveMessage
			{
				SequenceId = sequence,
				AssemblyVersion = "denim-0.1.0"
			})
		});

		transport.SentFrames.Should().ContainSingle();
		var pong = JsonConvert.DeserializeObject<KeepAliveMessage>(transport.SentFrames[0].Content);
		pong.Should().NotBeNull();
		pong!.SequenceId.Should().Be(sequence);
		pong.AssemblyVersion.Should().NotBeNull();
	}

	// Guards that discovery failures raise a telemetry event so IDE diagnostics can tell why processors were missing.
	[TestMethod]
	public async Task ProcessorDiscoveryFailures_ShouldEmitTelemetryError()
	{
		using var context = RemoteControlServerTestContext.Create();
		var bogusPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing");
		var appInstanceId = Guid.NewGuid().ToString("N");

		await context.RunAsync(new[]
		{
			Frame.Create(ProtocolVersion, WellKnownScopes.DevServerChannel, ProcessorsDiscovery.Name, new ProcessorsDiscovery(bogusPath, appInstanceId))
		});

		context.Telemetry.Events.Should().Contain(e => e.Name == "processor-discovery-error");
	}

	// Exercises the IDE→processor path to prove messages are delivered via IRemoteControlServer even post-discovery.
	[TestMethod]
	public async Task IdeMessages_ShouldRouteToRegisteredProcessor()
	{
		using var context = RemoteControlServerTestContext.Create();
		var appInstanceId = Guid.NewGuid().ToString("N");

		var transport = await context.RunAsync(new[] { context.CreateDiscoveryFrame(appInstanceId) });

		context.IdeChannel.Emit(new TestIdeCommand());

		var payload = DeserializeSentMessage<IdeRouteNotification>(transport, IdeRouteNotification.MessageName);
		payload.Count.Should().BeGreaterThanOrEqualTo(1);
	}

	// Ensures RemoteControlServerHost creates independent DI scopes per connection and disposes transports deterministically.
	[TestMethod]
	public async Task RemoteControlServerHost_ShouldIsolateScopesAndDisposeTransport()
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddSingleton<RecordingStore>();
		services.AddScoped<ScopedStamp>();
		services.AddScoped<IRemoteControlServerConnection, RecordingConnectionHandler>();

		await using var serviceProvider = services.BuildServiceProvider();
		await using var host = CreateHost(serviceProvider, disposeProvider: false);

		var firstTransport = new TrackingTransport();
		await host.RunConnectionAsync((sp, ct) => new ValueTask<IFrameTransport>(firstTransport));

		var secondTransport = new TrackingTransport();
		await host.RunConnectionAsync((sp, ct) => new ValueTask<IFrameTransport>(secondTransport));

		var recorder = serviceProvider.GetRequiredService<RecordingStore>();
		recorder.Calls.Should().HaveCount(2);
		recorder.Calls.Distinct().Should().HaveCount(2);
		firstTransport.CloseCalled.Should().BeTrue();
		firstTransport.Disposed.Should().BeTrue();
		secondTransport.CloseCalled.Should().BeTrue();
		secondTransport.Disposed.Should().BeTrue();
	}

	private sealed class RemoteControlServerTestContext : IDisposable
	{
		private readonly ServiceProvider _serviceProvider;
		private readonly ServerCoreRemoteControlServer _server;

		private RemoteControlServerTestContext(ServiceProvider serviceProvider, ServerCoreRemoteControlServer server, TestIdeChannel ideChannel, FakeLaunchMonitor launchMonitor, FakeTelemetry telemetry)
		{
			_serviceProvider = serviceProvider;
			_server = server;
			IdeChannel = ideChannel;
			LaunchMonitor = launchMonitor;
			Telemetry = telemetry;
			AssemblyPath = typeof(RemoteControlServerBehaviorTests).Assembly.Location;
		}

		public TestIdeChannel IdeChannel { get; }

		public FakeLaunchMonitor LaunchMonitor { get; }

		public FakeTelemetry Telemetry { get; }

		public string AssemblyPath { get; }

		public static RemoteControlServerTestContext Create()
		{
			var services = new ServiceCollection();
			var telemetry = new FakeTelemetry();
			services.AddSingleton(typeof(ServerCoreTelemetry), telemetry);
			var serviceProvider = services.BuildServiceProvider();

			var configuration = new InMemoryRemoteControlConfiguration();
			var ideChannel = new TestIdeChannel();
			var launchMonitor = new FakeLaunchMonitor();
			var processorFactory = new TestProcessorFactory(serviceProvider);
			var server = new ServerCoreRemoteControlServer(configuration, ideChannel, launchMonitor, processorFactory, serviceProvider);
			processorFactory.AttachServer(server);

			return new RemoteControlServerTestContext(serviceProvider, server, ideChannel, launchMonitor, telemetry);
		}

		public Frame CreateDiscoveryFrame(string appInstanceId)
			=> Frame.Create(ProtocolVersion, WellKnownScopes.DevServerChannel, ProcessorsDiscovery.Name, new ProcessorsDiscovery(AssemblyPath, appInstanceId));

		public async Task<ScriptedFrameTransport> RunAsync(Frame[] script)
		{
			var transport = new ScriptedFrameTransport(script);
			await ((IRemoteControlServerConnection)_server).HandleConnectionAsync(transport, CancellationToken.None);
			return transport;
		}

		public void Dispose()
		{
			_server.Dispose();
			_serviceProvider.Dispose();
		}
	}

	private sealed class TestProcessorFactory : ServerCoreProcessorFactory
	{
		private readonly IServiceProvider _services;
		private IRemoteControlServer? _server;

		public TestProcessorFactory(IServiceProvider services)
		{
			_services = services ?? throw new ArgumentNullException(nameof(services));
		}

		public void AttachServer(IRemoteControlServer server)
		{
			_server = server ?? throw new ArgumentNullException(nameof(server));
		}

		ValueTask<ServerCoreProcessorResult> ServerCoreProcessorFactory.DiscoverProcessorsAsync(
			ServerCoreProcessorsDiscovery discovery,
			CancellationToken ct)
		{
			if (!File.Exists(discovery.BasePath))
			{
				throw new FileNotFoundException("Processor assembly not found.", discovery.BasePath);
			}

			var server = _server ?? throw new InvalidOperationException("Server not attached.");
			var assembly = Assembly.LoadFrom(discovery.BasePath);
			var discovered = ImmutableArray.CreateBuilder<ServerCoreDiscoveredProcessor>();
			var instances = new List<IServerProcessor>();

			foreach (var attribute in assembly.GetCustomAttributes(typeof(ServerProcessorAttribute), inherit: false)
				.OfType<ServerProcessorAttribute>())
			{
				var processorName = attribute.ProcessorType.FullName ?? attribute.ProcessorType.Name;
				try
				{
					var instance = ActivatorUtilities.CreateInstance(_services, attribute.ProcessorType, server);
					if (instance is IServerProcessor processor)
					{
						instances.Add(processor);
						discovered.Add(new(discovery.BasePath, processorName, GetVersion(attribute.ProcessorType), true));
					}
					else
					{
						discovered.Add(new(discovery.BasePath, processorName, GetVersion(attribute.ProcessorType), false));
					}
				}
				catch (Exception ex)
				{
					discovered.Add(new(discovery.BasePath, processorName, GetVersion(attribute.ProcessorType), false, ex.ToString()));
				}
			}

			var result = new ServerCoreProcessorResult(
				ImmutableArray.Create(discovery.BasePath),
				discovered.ToImmutable(),
				instances.ToImmutableArray());

			return ValueTask.FromResult(result);
		}

		private static string GetVersion(Type processorType)
			=> processorType.Assembly.GetName().Version?.ToString() ?? "--unknown--";
	}

	private sealed class ScriptedFrameTransport : IFrameTransport
	{
		private readonly Queue<Frame?> _frames;

		public ScriptedFrameTransport(IEnumerable<Frame> frames)
		{
			_frames = new Queue<Frame?>(frames.Append(null));
		}

		public bool IsConnected { get; private set; } = true;

		public List<Frame> SentFrames { get; } = [];

		public Task<Frame?> ReceiveAsync(CancellationToken ct)
		{
			ct.ThrowIfCancellationRequested();

			return Task.FromResult(_frames.Count > 0 ? _frames.Dequeue() : null);
		}

		public Task SendAsync(Frame frame, CancellationToken ct)
		{
			SentFrames.Add(frame);
			return Task.CompletedTask;
		}

		public Task CloseAsync()
		{
			IsConnected = false;
			return Task.CompletedTask;
		}

		public void Dispose()
		{
			IsConnected = false;
		}
	}

	private sealed class FakeLaunchMonitor : ServerCoreLaunchMonitor
	{
		public List<ApplicationLaunchMonitor.LaunchEvent> Registrations { get; } = [];
		public List<(Guid Mvid, string? Platform, bool IsDebug)> Connections { get; } = [];

		public void RegisterLaunch(Guid mvid, string? platform, bool isDebug, string ide, string plugin)
			=> Registrations.Add(new ApplicationLaunchMonitor.LaunchEvent(mvid, platform ?? "Unknown", isDebug, ide, plugin, DateTimeOffset.UtcNow));

		public bool ReportConnection(Guid mvid, string? platform, bool isDebug)
		{
			Connections.Add((mvid, platform, isDebug));
			return true;
		}
	}

	private sealed class FakeTelemetry : ServerCoreTelemetry
	{
		public List<(string Name, IDictionary<string, string>? Properties)> Events { get; } = [];

		public bool Enabled => true;

		public void Dispose()
		{
		}

		public void Flush()
		{
		}

		public Task FlushAsync(CancellationToken ct) => Task.CompletedTask;

		public void ThreadBlockingTrackEvent(string eventName, IDictionary<string, string> properties, IDictionary<string, double> measurements)
			=> TrackEvent(eventName, properties, measurements);

		public void TrackEvent(string eventName, (string key, string value)[]? properties, (string key, double value)[]? measurements)
			=> TrackEvent(eventName, properties?.ToDictionary(p => p.key, p => p.value), measurements?.ToDictionary(m => m.key, m => m.value));

		public void TrackEvent(string eventName, IDictionary<string, string>? properties, IDictionary<string, double>? measurements)
			=> Events.Add((eventName, properties));
	}

	private sealed class TestIdeChannel : IIdeChannel
	{
		public event EventHandler<IdeMessage>? MessageFromIde;

		public List<IdeMessage> SentMessages { get; } = [];

		public void Emit(IdeMessage message)
			=> MessageFromIde?.Invoke(this, message);

		public Task SendToIdeAsync(IdeMessage message, CancellationToken ct)
		{
			SentMessages.Add(message);
			return Task.CompletedTask;
		}

		public Task<bool> TrySendToIdeAsync(IdeMessage message, CancellationToken ct)
		{
			SentMessages.Add(message);
			return Task.FromResult(true);
		}

		public Task<bool> WaitForReady(CancellationToken ct = default)
			=> Task.FromResult(true);
	}

	private static T DeserializeSentMessage<T>(ScriptedFrameTransport transport, string messageName)
	{
		var frame = transport.SentFrames.Single(f => f.Name == messageName);
		var payload = JsonConvert.DeserializeObject<T>(frame.Content);
		payload.Should().NotBeNull();
		return payload!;
	}

	public sealed class DiagnosticsAwareProcessor : IServerProcessor
	{
		public const string ScopeName = "GaDiagnostics";
		private readonly IRemoteControlServer _server;

		public DiagnosticsAwareProcessor(IRemoteControlServer server)
		{
			_server = server;
		}

		public string Scope => ScopeName;

		public void Dispose()
		{
		}

		public Task ProcessFrame(Frame frame)
			=> _server.SendFrame(new DiagnosticsReportMessage
			{
				SinkTypeName = DevServerDiagnostics.Current.GetType().FullName ?? typeof(DevServerDiagnostics).Name
			});

		public Task ProcessIdeMessage(IdeMessage message, CancellationToken ct)
			=> Task.CompletedTask;
	}

	public sealed class IdeRoutingProcessor : IServerProcessor
	{
		public const string ScopeName = "GaIdeScope";
		private int _ideMessageCount;
		private readonly IRemoteControlServer _server;

		public IdeRoutingProcessor(IRemoteControlServer server)
		{
			_server = server;
		}

		public string Scope => ScopeName;

		public void Dispose()
		{
		}

		public Task ProcessFrame(Frame frame)
			=> Task.CompletedTask;

		public Task ProcessIdeMessage(IdeMessage message, CancellationToken ct)
			=> _server.SendFrame(new IdeRouteNotification
			{
				Count = Interlocked.Increment(ref _ideMessageCount)
			});
	}

	private sealed class DiagnosticsReportMessage : IMessage
	{
		public const string MessageName = "ga-diagnostics-report";

		public string SinkTypeName { get; set; } = string.Empty;

		public string Scope => DiagnosticsAwareProcessor.ScopeName;

		public string Name => MessageName;
	}

	private sealed class IdeRouteNotification : IMessage
	{
		public const string MessageName = "ga-ide-route-notification";

		public int Count { get; set; }

		public string Scope => IdeRoutingProcessor.ScopeName;

		public string Name => MessageName;
	}

	private sealed record TestIdeCommand() : IdeMessage(IdeRoutingProcessor.ScopeName);

	private sealed class InMemoryRemoteControlConfiguration : ServerCoreConfiguration
	{
		private readonly Dictionary<string, string> _values = new();

		public string? GetValue(string key)
			=> _values.TryGetValue(key, out var value) ? value : null;
	}

	private sealed class TrackingTransport : IFrameTransport
	{
		public bool CloseCalled { get; private set; }
		public bool Disposed { get; private set; }

		public bool IsConnected => !CloseCalled;

		public Task<Frame?> ReceiveAsync(CancellationToken ct)
			=> Task.FromResult<Frame?>(null);

		public Task SendAsync(Frame frame, CancellationToken ct)
			=> Task.CompletedTask;

		public Task CloseAsync()
		{
			CloseCalled = true;
			return Task.CompletedTask;
		}

		public void Dispose()
		{
			Disposed = true;
		}
	}

	private sealed class RecordingStore
	{
		public List<Guid> Calls { get; } = [];
	}

	private sealed class ScopedStamp
	{
		public Guid Value { get; } = Guid.NewGuid();
	}

	private sealed class RecordingConnectionHandler : IRemoteControlServerConnection
	{
		private readonly RecordingStore _store;
		private readonly ScopedStamp _stamp;

		public RecordingConnectionHandler(RecordingStore store, ScopedStamp stamp)
		{
			_store = store;
			_stamp = stamp;
		}

		public Task HandleConnectionAsync(IFrameTransport transport, CancellationToken ct)
		{
			_store.Calls.Add(_stamp.Value);
			return Task.CompletedTask;
		}
	}

	private static ServerCoreRemoteControlServerHost CreateHost(IServiceProvider serviceProvider, bool disposeProvider)
	{
		var leaseType = typeof(ServerCoreRemoteControlServerHost).Assembly.GetType("Uno.UI.RemoteControl.ServerCore.GlobalServiceProviderLease", throwOnError: true)!;
		var lease = Activator.CreateInstance(leaseType, serviceProvider, disposeProvider)
			?? throw new InvalidOperationException("Failed to create GlobalServiceProviderLease.");

		return (ServerCoreRemoteControlServerHost)Activator.CreateInstance(
			typeof(ServerCoreRemoteControlServerHost),
			BindingFlags.NonPublic | BindingFlags.Instance,
			binder: null,
			args: new[] { lease },
			culture: null)!;
	}
}
