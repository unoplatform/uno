using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Uno.UI.RemoteControl.Host.Extensibility;

namespace Uno.UI.RemoteControl.DevServer.Tests;

/// <summary>
/// Unit tests for <see cref="AddInHostedServiceQuarantine"/> and
/// <see cref="QuarantinedHostedService"/>.
/// </summary>
[TestClass]
public class Given_AddInHostedServiceQuarantine
{
	[TestMethod]
	[Description("A hosted service whose constructor throws (the uno-private#1968 crash shape) must not fail IEnumerable<IHostedService> materialization nor StartAsync; services registered after it must still start.")]
	public async Task Apply_ContainsCtorFailure_AndStartsRemainingServices()
	{
		var services = new ServiceCollection();
		services.AddSingleton<State>();
		services.AddSingleton<IHostedService, ThrowingCtorService>();
		services.AddSingleton<IHostedService, HealthyService>();

		AddInHostedServiceQuarantine.Apply(services, fromIndex: 0);

		await using var provider = services.BuildServiceProvider();

		// Mirrors Host.StartAsync: materializes every hosted service, then starts each.
		var hostedServices = provider.GetServices<IHostedService>().ToList();
		hostedServices.Should().HaveCount(2, "both descriptors must materialize as quarantine wrappers");

		foreach (var service in hostedServices)
		{
			await service.StartAsync(CancellationToken.None);
		}

		provider.GetRequiredService<State>().Events.Should().Contain("healthy-start",
			"the healthy service registered after the failing one must still be started");
	}

	[TestMethod]
	[Description("A hosted service whose StartAsync throws must be contained, and the quarantine callback must receive the service description and the original exception.")]
	public async Task Apply_ReportsQuarantine_ThroughCallback()
	{
		var services = new ServiceCollection();
		services.AddSingleton<IHostedService, ThrowingStartService>();

		var quarantined = new List<(string Service, Exception Error)>();
		AddInHostedServiceQuarantine.Apply(
			services, fromIndex: 0, (service, error) => quarantined.Add((service, error)));

		await using var provider = services.BuildServiceProvider();

		foreach (var service in provider.GetServices<IHostedService>())
		{
			await service.StartAsync(CancellationToken.None);
		}

		quarantined.Should().HaveCount(1);
		quarantined[0].Service.Should().Contain(nameof(ThrowingStartService));
		quarantined[0].Error.Should().BeOfType<InvalidOperationException>();
	}

	[TestMethod]
	[Description("Descriptors registered before fromIndex (the host's own hosted services) must keep their fail-fast behavior — quarantine only covers the add-in range.")]
	public void Apply_LeavesHostServicesBeforeIndexUntouched()
	{
		var services = new ServiceCollection();
		services.AddSingleton<State>();
		services.AddSingleton<IHostedService, ThrowingCtorService>();
		var hostOwnDescriptor = services[^1];
		var addInStart = services.Count;
		services.AddSingleton<IHostedService, HealthyService>();

		AddInHostedServiceQuarantine.Apply(services, addInStart);

		services[addInStart - 1].Should().BeSameAs(hostOwnDescriptor,
			"the host's own descriptor must not be rewritten");

		using var provider = services.BuildServiceProvider();
		var act = () => provider.GetServices<IHostedService>().ToList();

		act.Should().Throw<FileNotFoundException>(
			"the unquarantined host-own service must still fail eagerly and loudly");
	}

	[TestMethod]
	[Description("StopAsync must forward to services that started, and must be a no-op (not a throw) for quarantined ones that never got constructed.")]
	public async Task StopAsync_ForwardsOnlyWhenStarted()
	{
		var services = new ServiceCollection();
		services.AddSingleton<State>();
		services.AddSingleton<IHostedService, ThrowingCtorService>();
		services.AddSingleton<IHostedService, HealthyService>();

		AddInHostedServiceQuarantine.Apply(services, fromIndex: 0);

		await using var provider = services.BuildServiceProvider();
		var hostedServices = provider.GetServices<IHostedService>().ToList();

		foreach (var service in hostedServices)
		{
			await service.StartAsync(CancellationToken.None);
		}

		foreach (var service in hostedServices)
		{
			await service.StopAsync(CancellationToken.None);
		}

		var state = provider.GetRequiredService<State>();
		state.Events.Should().Contain("healthy-stop", "started services must be stopped normally");
	}

	[TestMethod]
	[Description("Cancellation is not a quarantine case: when the startup token is cancelled, the wrapper must propagate OperationCanceledException so host shutdown semantics stay intact.")]
	public async Task StartAsync_PropagatesCancellation_WhenTokenCancelled()
	{
		var services = new ServiceCollection();
		services.AddSingleton<IHostedService, CancellationHonoringService>();

		AddInHostedServiceQuarantine.Apply(services, fromIndex: 0);

		await using var provider = services.BuildServiceProvider();
		var hostedService = provider.GetServices<IHostedService>().Single();

		using var cts = new CancellationTokenSource();
		await cts.CancelAsync();

		var act = () => hostedService.StartAsync(cts.Token);

		await act.Should().ThrowAsync<OperationCanceledException>(
			"a cancelled startup is host shutdown, not an add-in failure");
	}

	// ------------------------------------------------------------------ test services

	private sealed class State
	{
		public List<string> Events { get; } = [];
	}

	private sealed class ThrowingCtorService : IHostedService
	{
		public ThrowingCtorService()
			=> throw new FileNotFoundException("Simulated missing dependency (mimics uno-private#1968).");

		public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

		public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
	}

	private sealed class ThrowingStartService : IHostedService
	{
		public Task StartAsync(CancellationToken cancellationToken)
			=> throw new InvalidOperationException("Simulated start failure.");

		public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
	}

	private sealed class HealthyService(State state) : IHostedService
	{
		public Task StartAsync(CancellationToken cancellationToken)
		{
			state.Events.Add("healthy-start");
			return Task.CompletedTask;
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			state.Events.Add("healthy-stop");
			return Task.CompletedTask;
		}
	}

	private sealed class CancellationHonoringService : IHostedService
	{
		public Task StartAsync(CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return Task.CompletedTask;
		}

		public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
	}
}
