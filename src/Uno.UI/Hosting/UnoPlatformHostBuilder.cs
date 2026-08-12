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
	private readonly List<Action> _drawingRegistrations = new();
	private Func<Application>? _appBuilder;
	private Action? _afterInitAction;
	private Type? _appType;

	internal UnoPlatformHostBuilder() { }

	Func<Application>? IUnoPlatformHostBuilder.AppBuilder
	{
		get => _appBuilder;
		set => _appBuilder = value;
	}

	Action? IUnoPlatformHostBuilder.AfterInitAction
	{
		get => _afterInitAction;
		set => _afterInitAction = value;
	}

	void IUnoPlatformHostBuilder.SetAppType(Type appType)
		=> _appType = appType;

	public static UnoPlatformHostBuilder Create()
		=> new();

	void IUnoPlatformHostBuilder.AddDrawingRegistration(Action apply)
		=> _drawingRegistrations.Add(apply);

	public UnoPlatformHost Build()
	{
		if (_appBuilder is null || _appType is null)
		{
			throw new InvalidOperationException($"No app builder delegate was provided via the .App extension method.");
		}

		// Apply the app-declared drawing-backend registrations before any host runs, so the render/drawing backend
		// + content seams are in place before the host negotiates graphics (GraphicsRegistry.Initialize) and the
		// first frame is recorded.
		foreach (var apply in _drawingRegistrations)
		{
			apply();
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

				var host = hostBuilder.Create(_appBuilder, _appType);

				host.AfterInitAction = _afterInitAction;

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
