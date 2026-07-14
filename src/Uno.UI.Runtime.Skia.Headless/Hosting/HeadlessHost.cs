#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Uno.Extensions.ApplicationModel.Core;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Uno.Helpers;
using Uno.UI.Dispatching;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Skia.Headless.UI;
using Uno.UI.Xaml.Controls;
using Uno.WinUI.Runtime.Skia.Headless.UI;
using WUX = Microsoft.UI.Xaml;

namespace Uno.UI.Runtime.Skia.Headless;

public class HeadlessHost : SkiaHost, ISkiaApplicationHost, IXamlRootHost, IDisposable
{
	[ThreadStatic]
	private static bool _isDispatcherThread;

	private readonly EventLoop _eventLoop;
	private readonly ManualResetEvent _terminationGate = new(false);
	private readonly CoreApplicationExtension _coreApplicationExtension;
	private readonly HeadlessHostBuilder _hostBuilder;
	private readonly Func<WUX.Application> _appBuilder;

	private HeadlessRenderer? _renderer;

	/// <summary>
	/// Creates a host for a Uno Skia headless (offscreen) application.
	/// </summary>
	/// <param name="appBuilder">App builder.</param>
	/// <remarks>
	/// Environment.CommandLine is used to fill LaunchEventArgs.Arguments.
	/// </remarks>
	public HeadlessHost(Func<WUX.Application> appBuilder) : this(appBuilder, new HeadlessHostBuilder())
	{
	}

	internal HeadlessHost(Func<WUX.Application> appBuilder, HeadlessHostBuilder builder)
	{
		_appBuilder = appBuilder;
		_hostBuilder = builder;

		_eventLoop = new EventLoop();
		_coreApplicationExtension = new CoreApplicationExtension(_terminationGate);
	}

	protected override void Initialize()
	{
		_eventLoop.Schedule(InnerInitialize);
	}

	protected override Task RunLoop()
	{
		_terminationGate.WaitOne();

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"Application is exiting");
		}

		_renderer?.Dispose();

		return Task.CompletedTask;
	}

	private void InnerInitialize()
	{
		_isDispatcherThread = true;

		var scale = ResolveScale(_hostBuilder.Scale);
		HeadlessWindowWrapper.Init(_hostBuilder.Width, _hostBuilder.Height, scale);

		ApiExtensibility.Register(typeof(INativeWindowFactoryExtension), o => new NativeWindowFactoryExtension(this));
		ApiExtensibility.Register(typeof(Uno.ApplicationModel.Core.ICoreApplicationExtension), o => _coreApplicationExtension);
		ApiExtensibility.Register(typeof(Windows.Graphics.Display.IDisplayInformationExtension), o => new DisplayInformationExtension(o, _hostBuilder.Orientation));

		void Dispatch(System.Action d, NativeDispatcherPriority p)
			=> _eventLoop.Schedule(d);

		void CreateApp(ApplicationInitializationCallbackParams _)
		{
			var app = _appBuilder();
			app.Host = this;

			// Force the first render once the app has been setup.
			Dispatch(() => _renderer?.InvalidateRender(), NativeDispatcherPriority.High);
		}

		Windows.UI.Core.CoreDispatcher.DispatchOverride = Dispatch;
		Windows.UI.Core.CoreDispatcher.HasThreadAccessOverride = () => _isDispatcherThread;

		_renderer = new HeadlessRenderer(
			this,
			_hostBuilder.RenderBuffer,
			_hostBuilder.RenderRowBytes,
			_hostBuilder.RenderColorType,
			_hostBuilder.OnFrameRendered);

		WUX.Application.Start(CreateApp);
	}

	/// <summary>
	/// The configured scale can be overridden globally via the UNO_DISPLAY_SCALE_OVERRIDE environment
	/// variable, keeping a single source of truth shared by the window bounds and DisplayInformation.
	/// </summary>
	private static float ResolveScale(float configuredScale)
		=> float.TryParse(
			Environment.GetEnvironmentVariable("UNO_DISPLAY_SCALE_OVERRIDE"),
			System.Globalization.NumberStyles.Any,
			System.Globalization.CultureInfo.InvariantCulture,
			out var envScale) && envScale > 0
			? envScale
			: configuredScale;

	void IXamlRootHost.InvalidateRender() => _renderer?.InvalidateRender();

	WUX.UIElement? IXamlRootHost.RootElement => HeadlessWindowWrapper.Instance.Window?.RootElement;

	public void Dispose() => _terminationGate.Set();
}
