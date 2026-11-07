using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Uno.Utils.DependencyInjection;

// The host's attribute-scanner matches this by full type name, not Type identity,
// so the add-in declares its own copy under the same namespace+name without
// taking a direct dependency on the host assembly.
[assembly: ServiceCollectionExtension(typeof(HangingServicesRegistration))]

namespace Uno.Utils.DependencyInjection;

// Activated by the host's attribute scanner via Activator.CreateInstance.
public sealed class HangingServicesRegistration
{
	public HangingServicesRegistration(IServiceCollection services)
	{
		ArgumentNullException.ThrowIfNull(services);

		services.AddHostedService<HangOnStopHostedService>();
	}
}

// Ignores the stop token and never completes, so the host's graceful shutdown hangs.
public sealed class HangOnStopHostedService : IHostedService
{
	public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

	public Task StopAsync(CancellationToken cancellationToken) => Task.Delay(Timeout.Infinite, CancellationToken.None);
}

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
internal sealed class ServiceCollectionExtensionAttribute(Type type) : Attribute
{
	public Type Type { get; } = type;
}
