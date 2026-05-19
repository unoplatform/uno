using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Uno.Utils.DependencyInjection;

// The host's attribute-scanner matches this by full type name, not Type identity,
// so the add-in declares its own copy under the same namespace+name without
// taking a direct dependency on the host assembly.
[assembly: ServiceCollectionExtension(typeof(ServicesRegistration))]

namespace Uno.Utils.DependencyInjection;

// Activated by the host's attribute scanner via Activator.CreateInstance.
// Exercises IServiceCollection bridge, IHostedService bridge, and cross-ALC
// ServiceDescriptor lookup across the AddInLoadContext boundary.
public sealed class ServicesRegistration
{
	public ServicesRegistration(IServiceCollection services)
	{
		ArgumentNullException.ThrowIfNull(services);

		// Write a sentinel to prove ctor was invoked (IServiceCollection bridge OK).
		var ctorSentinel = Environment.GetEnvironmentVariable(
			"UNO_DEVSERVER_TEST_DI_CTOR_SENTINEL");
		if (!string.IsNullOrEmpty(ctorSentinel))
		{
			File.WriteAllText(ctorSentinel, "ctor-invoked");
		}

		// Register two ITestToken instances and a hosted service.
		services.AddSingleton<ITestToken, TestToken1>();
		services.AddSingleton<ITestToken, TestToken2>();
		services.AddHostedService<TestHostedService>();
	}
}

// Marker interface — defined here in the fixture, not in AddInLoadContext.
public interface ITestToken { }

public sealed class TestToken1 : ITestToken { }
public sealed class TestToken2 : ITestToken { }

// Started by the ASP.NET Core host as part of IHost.StartAsync.
// Writes the resolved ITestToken count to a sentinel file, proving
// IHostedService and IServiceProvider Type-identity across the ALC boundary.
public sealed class TestHostedService(IServiceProvider services) : IHostedService
{
	public Task StartAsync(CancellationToken cancellationToken)
	{
		var sentinel = Environment.GetEnvironmentVariable(
			"UNO_DEVSERVER_TEST_DI_HOSTED_SENTINEL");
		if (!string.IsNullOrEmpty(sentinel))
		{
			var tokenCount = services.GetServices<ITestToken>().Count();
			File.WriteAllText(
				sentinel,
				tokenCount.ToString(CultureInfo.InvariantCulture));
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
