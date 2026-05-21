using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Uno.Licensing.TestContracts;
using Uno.Utils.DependencyInjection;

// Registers a hosted service that depends on ILicensingTestContract from DI.
// ILicensingTestContract is provided by the companion AddInWithSharedContractProvider add-in.
// Without the cross-add-in eager-load fix, the type identity of ILicensingTestContract
// differs between add-ins (or the contracts assembly cannot be loaded at all), causing
// System.InvalidOperationException: Unable to resolve service for type ILicensingTestContract.
[assembly: ServiceCollectionExtension(typeof(ServicesRegistration))]

namespace Uno.Utils.DependencyInjection;

public sealed class ServicesRegistration
{
	public ServicesRegistration(IServiceCollection services)
	{
		ArgumentNullException.ThrowIfNull(services);

		// The ConsumerHostedService constructor takes ILicensingTestContract.
		// Referencing the type here forces JIT to resolve Uno.Licensing.TestContracts
		// from this add-in's ALC context. Without the fix, this fails with
		// FileNotFoundException because the contracts DLL is not in this add-in's
		// output directory and not in Default ALC.
		services.AddHostedService<ConsumerHostedService>();
	}
}

/// <summary>
/// Hosted service that receives <see cref="ILicensingTestContract"/> from DI.
/// The contract instance is registered by the provider add-in; if the two add-ins
/// share the same type identity (via Default ALC bridging), DI resolves it successfully.
/// </summary>
public sealed class ConsumerHostedService(ILicensingTestContract _contract) : IHostedService
{
	public Task StartAsync(CancellationToken cancellationToken)
	{
		_ = _contract;

		var sentinel = Environment.GetEnvironmentVariable(
			"UNO_DEVSERVER_TEST_SHARED_CONTRACT_SENTINEL");
		if (!string.IsNullOrEmpty(sentinel))
		{
			File.WriteAllText(sentinel, _contract.GetType().AssemblyQualifiedName ?? "resolved");
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
