using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Uno.UI.RemoteControl.Host.Extensibility;

/// <summary>
/// Isolates add-in hosted services from the host's startup path: a broken add-in
/// (failing constructor, missing dependency, throwing StartAsync) must degrade that
/// add-in only, never prevent the DevServer from becoming ready — Hot Reload keeps
/// working even when an optional add-in cannot start.
/// </summary>
internal static class AddInHostedServiceQuarantine
{
	/// <summary>
	/// Rewrites every non-keyed <see cref="IHostedService"/> descriptor registered at or
	/// after <paramref name="fromIndex"/> so its construction is deferred to
	/// <see cref="IHostedService.StartAsync"/> and any failure is contained by
	/// <see cref="QuarantinedHostedService"/>. Descriptors before <paramref name="fromIndex"/>
	/// (the host's own services) keep their fail-fast behavior.
	/// </summary>
	/// <param name="onQuarantined">
	/// Optional callback invoked with a description of the failed service and the error,
	/// so the caller can emit telemetry without this class depending on the telemetry API.
	/// </param>
	internal static void Apply(IServiceCollection services, int fromIndex, Action<string, Exception>? onQuarantined = null)
	{
		for (var i = Math.Max(fromIndex, 0); i < services.Count; i++)
		{
			var descriptor = services[i];
			if (descriptor.ServiceType != typeof(IHostedService) || descriptor.IsKeyedService)
			{
				continue;
			}

			services[i] = ServiceDescriptor.Describe(
				typeof(IHostedService),
				sp => new QuarantinedHostedService(sp, descriptor, onQuarantined),
				descriptor.Lifetime);
		}
	}
}

/// <summary>
/// Wraps an add-in <see cref="IHostedService"/> registration: the inner service is
/// created lazily in <see cref="StartAsync"/> (not during the host's eager
/// <c>IEnumerable&lt;IHostedService&gt;</c> materialization), and construction or start
/// failures are logged and reported instead of propagating to
/// <c>Host.StartAsync</c>, which would kill the whole DevServer process.
/// </summary>
internal sealed class QuarantinedHostedService(
	IServiceProvider services,
	ServiceDescriptor inner,
	Action<string, Exception>? onQuarantined) : IHostedService, IAsyncDisposable, IDisposable
{
	private IHostedService? _created;
	private bool _started;

	internal string InnerDescription =>
		_created?.GetType().FullName
		?? inner.ImplementationType?.FullName
		?? inner.ImplementationInstance?.GetType().FullName
		?? $"factory-registered {nameof(IHostedService)}";

	public async Task StartAsync(CancellationToken cancellationToken)
	{
		try
		{
			var service = inner switch
			{
				{ ImplementationInstance: IHostedService instance } => instance,
				{ ImplementationFactory: { } factory } => (IHostedService)factory(services),
				{ ImplementationType: { } type } => (IHostedService)ActivatorUtilities.CreateInstance(services, type),
				_ => throw new InvalidOperationException($"Unsupported service descriptor shape for '{InnerDescription}'."),
			};
			_created = service;

			await service.StartAsync(cancellationToken);
			_started = true;

			ObserveBackgroundExecution(service);
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			throw;
		}
		catch (Exception error)
		{
			Quarantine(error);
		}
	}

	public async Task StopAsync(CancellationToken cancellationToken)
	{
		if (_started && _created is { } service)
		{
			try
			{
				await service.StopAsync(cancellationToken);
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
			{
				throw;
			}
			catch (Exception error)
			{
				Log()?.LogWarning(error, "Add-in hosted service '{Service}' failed to stop.", InnerDescription);
			}
		}
	}

	/// <summary>
	/// The host applies <c>BackgroundServiceExceptionBehavior</c> (default: stop the host)
	/// only to services it sees as <see cref="BackgroundService"/>; this wrapper hides the
	/// inner one, so observe its execution here to keep add-in crashes visible — contained
	/// and logged — without stopping the DevServer.
	/// </summary>
	private void ObserveBackgroundExecution(IHostedService service)
	{
		if (service is BackgroundService { ExecuteTask: { } task })
		{
			_ = task.ContinueWith(
				t => Quarantine(t.Exception!.GetBaseException()),
				CancellationToken.None,
				TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
				TaskScheduler.Default);
		}
	}

	private void Quarantine(Exception error)
	{
		var message =
			"Add-in hosted service '{Service}' failed and was quarantined; the DevServer continues without it. " +
			"Features contributed by this service are unavailable until the underlying issue is fixed " +
			"(typically a dependency-version misalignment in the add-in — see previous resolution messages).";

		if (Log() is { } logger)
		{
			logger.LogError(error, message, InnerDescription);
		}
		else
		{
			Console.Error.WriteLine(
				$"Uno.UI.RemoteControl.Host: {message.Replace("{Service}", InnerDescription)} ({error.GetType().Name}: {error.Message})");
		}

		try
		{
			onQuarantined?.Invoke(InnerDescription, error);
		}
		catch (Exception)
		{
			// The quarantine callback must never take down the host either.
		}
	}

	private ILogger? Log()
		=> services.GetService<ILoggerFactory>()?.CreateLogger<QuarantinedHostedService>();

	public void Dispose()
	{
		// Only dispose instances this wrapper created; externally-provided
		// ImplementationInstance registrations follow the container's convention
		// of not disposing caller-owned singletons.
		if (inner.ImplementationInstance is null && _created is IDisposable disposable)
		{
			try
			{
				disposable.Dispose();
			}
			catch (Exception error)
			{
				Log()?.LogWarning(error, "Add-in hosted service '{Service}' failed to dispose.", InnerDescription);
			}
		}
	}

	public async ValueTask DisposeAsync()
	{
		if (inner.ImplementationInstance is null && _created is IAsyncDisposable asyncDisposable)
		{
			try
			{
				await asyncDisposable.DisposeAsync();
			}
			catch (Exception error)
			{
				Log()?.LogWarning(error, "Add-in hosted service '{Service}' failed to dispose.", InnerDescription);
			}
		}
		else
		{
			Dispose();
		}
	}
}
