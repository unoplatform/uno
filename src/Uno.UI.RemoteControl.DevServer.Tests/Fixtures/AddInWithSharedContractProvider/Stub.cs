using System;
using Microsoft.Extensions.DependencyInjection;
using Uno.Licensing.TestContracts;
using Uno.Utils.DependencyInjection;

// Registers the ILicensingTestContract implementation in the shared DI container.
// The [ServiceCollectionExtension] attribute causes the DevServer host to call the
// ServicesRegistration ctor during add-in activation, proving that
// Uno.Licensing.TestContracts was resolved (via Default ALC bridge from the fix) and
// that the DI registration succeeded.
[assembly: ServiceCollectionExtension(typeof(ServicesRegistration))]

namespace Uno.Utils.DependencyInjection;

public sealed class ServicesRegistration
{
	public ServicesRegistration(IServiceCollection services)
	{
		ArgumentNullException.ThrowIfNull(services);

		// Register the contract implementation so consumers can resolve ILicensingTestContract.
		// This line also forces Uno.Licensing.TestContracts to be resolved at JIT time,
		// reproducing the FileNotFoundException that occurs without the eager-load fix.
		services.AddSingleton<ILicensingTestContract, DefaultLicensingTestContract>();
	}
}

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
internal sealed class ServiceCollectionExtensionAttribute(Type type) : Attribute
{
	public Type Type { get; } = type;
}
