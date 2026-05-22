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
// The contracts DLL is physically present in the provider's directory but absent from this
// consumer's directory and its .deps.json. Resolving the dependency therefore requires that
// both add-ins observe a single ILicensingTestContract Type identity — which happens
// naturally when all add-ins load into a shared (Default) ALC.
[assembly: ServiceCollectionExtension(typeof(ServicesRegistration))]

namespace Uno.Utils.DependencyInjection;

public sealed class ServicesRegistration
{
	public ServicesRegistration(IServiceCollection services)
	{
		ArgumentNullException.ThrowIfNull(services);

		// Referencing ConsumerHostedService forces JIT to resolve Uno.Licensing.TestContracts.
		// If the contracts DLL is not reachable from this add-in's load context, this throws
		// FileNotFoundException at activation time.
		services.AddHostedService<ConsumerHostedService>();
	}
}

/// <summary>
/// Hosted service that receives <see cref="ILicensingTestContract"/> from DI.
/// The contract instance is registered by the provider add-in; when both add-ins share
/// the same <see cref="System.Type"/> identity, DI resolves it successfully and the
/// sentinel file is written with the resolved type's <c>AssemblyQualifiedName</c>.
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
