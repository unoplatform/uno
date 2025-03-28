using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Skia;

public class SkiaHostBuilder : ISkiaHostBuilder
{
	private List<Func<IPlatformHostBuilder>> _hostBuilders = new();
	private Func<Application>? _appBuilder;
	private Action? _afterInit;

	private SkiaHostBuilder() { }

	Func<Application>? ISkiaHostBuilder.AppBuilder
	{
		get => _appBuilder;
		set => _appBuilder = value;
	}

	Action? ISkiaHostBuilder.AfterInit
	{
		get => _afterInit;
		set => _afterInit = value;
	}

	public static SkiaHostBuilder Create()
		=> new();

	public SkiaHost Build()
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

	void ISkiaHostBuilder.AddHostBuilder(Func<IPlatformHostBuilder> platformHostBuilder)
	{
		_hostBuilders.Add(platformHostBuilder);
	}
}
