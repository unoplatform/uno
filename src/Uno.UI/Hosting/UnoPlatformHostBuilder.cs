#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Uno.Foundation.Logging;

namespace Uno.UI.Hosting;

public class UnoPlatformHostBuilder : IUnoPlatformHostBuilder
{
	private List<Func<IPlatformHostBuilder>> _hostBuilders = new();
	private Func<Application>? _appBuilder;
	private Action? _afterInit;

	private UnoPlatformHostBuilder() { }

	Func<Application>? IUnoPlatformHostBuilder.AppBuilder
	{
		get => _appBuilder;
		set => _appBuilder = value;
	}

	Action? IUnoPlatformHostBuilder.AfterInit
	{
		get => _afterInit;
		set => _afterInit = value;
	}

	public static UnoPlatformHostBuilder Create()
		=> new();

	public UnoPlatformHost Build()
	{
		if (_appBuilder is null)
		{
			throw new InvalidOperationException($"No app builder delegate was provided");
		}

		foreach (var hostBuilderFunc in _hostBuilders)
		{
			var hostBuilder = hostBuilderFunc();

			if (hostBuilder.IsSupported)
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"Using host builder {hostBuilder.GetType()}");
				}

				var host = hostBuilder.Create(_appBuilder);

				host.AfterInit = _afterInit;

				return host;
			}
			else
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"Host builder {hostBuilder.GetType()} is not supported");
				}
			}
		}

		throw new InvalidOperationException($"No platform host could be selected");
	}

	void IUnoPlatformHostBuilder.AddHostBuilder(Func<IPlatformHostBuilder> platformHostBuilder)
		=> _hostBuilders.Add(platformHostBuilder);
}
