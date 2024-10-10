using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Uno.Extensions;

namespace Uno.Utils.DependencyInjection;

public static class ServiceCollectionServiceExtensions
{
	/// <summary>
	/// Register services configured with the <see cref="ServiceAttribute"/> attribute from all loaded assemblies.
	/// </summary>
	/// <param name="svc">The service collection on which services should be registered.</param>
	/// <returns>The service collection for fluent usage.</returns>
	public static IServiceCollection AddFromAttribute(this IServiceCollection svc)
	{
		var attribute = typeof(ServiceAttribute);
		var services = AppDomain
			.CurrentDomain
			.GetAssemblies()
			.SelectMany(assembly => assembly.GetCustomAttributesData())
			.Select(attrData => attrData.TryCreate(attribute) as ServiceAttribute)
			.Where(attr => attr is not null)
			.ToImmutableList();

		foreach (var service in services)
		{
			svc.Add(new ServiceDescriptor(service!.Contract, service.Key, service.Implementation, service.LifeTime));
		}
		svc.AddHostedService(s => new AutoInitService(s, services!));

		return svc;
	}

	private class AutoInitService(IServiceProvider services, IImmutableList<ServiceAttribute> types) : BackgroundService, IHostedService
	{
		/// <inheritdoc />
		protected override Task ExecuteAsync(CancellationToken stoppingToken)
		{
			foreach (var attr in types.Where(attr => attr.IsAutoInit))
			{
				try
				{
					var svc = services.GetService(attr.Contract);

					if (this.Log().IsEnabled(LogLevel.Information))
					{
						this.Log().Log(LogLevel.Information, $"Successfully created an instance of {attr.Contract} for auto-init (impl: {svc?.GetType()})");
					}
				}
				catch (Exception error)
				{
					if (this.Log().IsEnabled(LogLevel.Error))
					{
						this.Log().Log(LogLevel.Error, error, $"Failed to create an instance of {attr.Contract} for auto-init.");
					}
				}
			}

			return Task.CompletedTask;
		}
	}
}
