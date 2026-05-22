using System;
using Microsoft.Extensions.DependencyInjection;
using Uno.Licensing.TestContracts;
using Uno.Utils.DependencyInjection;

// Registers DefaultLicensingTestContract as ILicensingTestContract in DI.
// The consumer add-in (AddInWithSharedContractConsumer) takes ILicensingTestContract via
// constructor injection — DI must be able to match the registered Type with what the
// consumer's ctor expects. This works naturally only when both add-ins observe a single
// Type identity for ILicensingTestContract.
[assembly: ServiceCollectionExtension(typeof(ServicesRegistration))]

namespace Uno.Utils.DependencyInjection;

public sealed class ServicesRegistration
{
	public ServicesRegistration(IServiceCollection services)
	{
		ArgumentNullException.ThrowIfNull(services);

		services.AddSingleton<ILicensingTestContract, DefaultLicensingTestContract>();
	}
}

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
internal sealed class ServiceCollectionExtensionAttribute(Type type) : Attribute
{
	public Type Type { get; } = type;
}
