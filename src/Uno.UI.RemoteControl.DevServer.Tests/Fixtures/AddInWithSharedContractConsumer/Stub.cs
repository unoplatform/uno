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
// The contracts DLL is physically present in the provider's directory but absent from
// the consumer's directory and its .deps.json. Without the cross-add-in fix (step 4
// file-system probe inside AddInLoadContext.Load), JIT resolution of ILicensingTestContract
// throws FileNotFoundException; with the fix, both add-ins share a single Type identity
// and DI resolves the registered service successfully.
[assembly: ServiceCollectionExtension(typeof(ServicesRegistration))]

namespace Uno.Utils.DependencyInjection;

public sealed class ServicesRegistration
{
	public ServicesRegistration(IServiceCollection services)
	{
		ArgumentNullException.ThrowIfNull(services);

		// The ConsumerHostedService constructor takes ILicensingTestContract.
		// Referencing the type here forces JIT to resolve Uno.Licensing.TestContracts
		// from this add-in's ALC context. Without the fix this fails with
		// FileNotFoundException because the contracts DLL is not in this add-in's
		// output directory and not in any deps.json.
		services.AddHostedService<ConsumerHostedService>();
	}
}

/// <summary>
/// Hosted service that receives <see cref="ILicensingTestContract"/> from DI.
/// The contract instance is registered by the provider add-in; when both add-ins share
/// the same Type identity (because <c>AddInLoadContext.Load</c>'s file-system probe loads
/// the contracts DLL once into the shared add-in context), DI resolves it successfully.
/// </summary>
public sealed class ConsumerHostedService(ILicensingTestContract contract) : IHostedService
{
	public Task StartAsync(CancellationToken cancellationToken)
	{
		var sentinel = Environment.GetEnvironmentVariable(
			"UNO_DEVSERVER_TEST_SHARED_CONTRACT_SENTINEL");
		if (!string.IsNullOrEmpty(sentinel))
		{
			File.WriteAllText(sentinel, contract.GetType().AssemblyQualifiedName ?? "resolved");
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
