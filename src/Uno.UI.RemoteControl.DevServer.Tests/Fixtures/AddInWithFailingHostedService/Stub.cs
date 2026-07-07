using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Uno.Utils.DependencyInjection;

// The host's attribute-scanner matches this by full type name, not Type identity,
// so the add-in declares its own copy under the same namespace+name without
// taking a direct dependency on the host assembly.
[assembly: ServiceCollectionExtension(typeof(FailingServicesRegistration))]

namespace Uno.Utils.DependencyInjection;

// Activated by the host's attribute scanner via Activator.CreateInstance.
// Registers a hosted service whose constructor throws (the uno-private#1968
// crash shape) followed by a healthy one, proving the host quarantines the
// failure and still starts subsequent services.
public sealed class FailingServicesRegistration
{
	public FailingServicesRegistration(IServiceCollection services)
	{
		ArgumentNullException.ThrowIfNull(services);

		services.AddHostedService<ThrowingCtorHostedService>();
		services.AddHostedService<HealthySentinelHostedService>();
	}
}

// Mimics the production failure: DI constructing the hosted service throws a
// missing-assembly exception (LicensingClient → Kiota → System.Text.Encodings.Web).
public sealed class ThrowingCtorHostedService : IHostedService
{
	public ThrowingCtorHostedService()
		=> throw new FileNotFoundException(
			"Simulated missing dependency: 'Some.Missing.Assembly, Version=8.0.0.0' (mimics uno-private#1968).");

	public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

	public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

// Registered after the throwing service; writes a sentinel when started so the
// test can prove startup continued past the quarantined failure.
public sealed class HealthySentinelHostedService : IHostedService
{
	public Task StartAsync(CancellationToken cancellationToken)
	{
		var sentinel = Environment.GetEnvironmentVariable(
			"UNO_DEVSERVER_TEST_QUARANTINE_HEALTHY_SENTINEL");
		if (!string.IsNullOrEmpty(sentinel))
		{
			File.WriteAllText(sentinel, "healthy-started");
		}

		return Task.CompletedTask;
	}

	public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
internal sealed class ServiceCollectionExtensionAttribute(Type type) : Attribute
{
	public Type Type { get; } = type;
}
