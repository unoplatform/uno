#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics.Display;
using Uno.Extensions.ApplicationModel.Core;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Uno.Helpers;
using Uno.UI;
using Uno.UI.Dispatching;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Skia.Headless.UI;
using Uno.UI.Xaml.Controls;
using Uno.WinUI.Runtime.Skia.Headless.UI;
using WUX = Microsoft.UI.Xaml;

namespace Uno.UI.Runtime.Skia.Headless;

public class HeadlessHost : SkiaHost, ISkiaApplicationHost, IDisposable
{
	[ThreadStatic]
	private static bool _isDispatcherThread;

	private readonly EventLoop _eventLoop;
	private readonly ManualResetEvent _terminationGate = new(false);
	private readonly CoreApplicationExtension _coreApplicationExtension;
	private readonly HeadlessHostBuilder _hostBuilder;
	private readonly Func<WUX.Application> _appBuilder;

	private NativeWindowFactoryExtension? _windowFactory;

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

		// Stop all render threads before returning so they never outlive their target buffers.
		_windowFactory?.DisposeWindows();

		return Task.CompletedTask;
	}

	private void InnerInitialize()
	{
		_isDispatcherThread = true;

		// If no window can ever have a render buffer, skip the paint walk globally to save CPU.
		// The flag is global (per app), so it can only be enabled when we know every window is bufferless.
		if (_hostBuilder.KnownBufferless)
		{
			FeatureConfiguration.Rendering.SkipVisualTreePainting = true;
		}

		_windowFactory = new NativeWindowFactoryExtension(_hostBuilder);
		ApiExtensibility.Register(typeof(INativeWindowFactoryExtension), o => _windowFactory);
		ApiExtensibility.Register(typeof(Uno.ApplicationModel.Core.ICoreApplicationExtension), o => _coreApplicationExtension);
		ApiExtensibility.Register<DisplayInformation>(typeof(IDisplayInformationExtension), ResolveDisplayInformation);

		void Dispatch(System.Action d, NativeDispatcherPriority p)
			=> _eventLoop.Schedule(d);

		void CreateApp(ApplicationInitializationCallbackParams _)
		{
			var app = _appBuilder();
			app.Host = this;
		}

		Windows.UI.Core.CoreDispatcher.DispatchOverride = Dispatch;
		Windows.UI.Core.CoreDispatcher.HasThreadAccessOverride = () => _isDispatcherThread;

		WUX.Application.Start(CreateApp);
	}

	/// <summary>
	/// Resolves the per-window <see cref="IDisplayInformationExtension"/> (the window's own wrapper)
	/// from the <see cref="DisplayInformation"/>'s window id.
	/// </summary>
	private static IDisplayInformationExtension ResolveDisplayInformation(DisplayInformation displayInformation)
	{
		var appWindow = AppWindow.GetFromWindowId(displayInformation.WindowId);
		var window = Window.GetFromAppWindow(appWindow);
		var rootElement = window.RootElement ?? throw new InvalidOperationException($"The window's {nameof(window.RootElement)} is not initialized.");
		var xamlRoot = rootElement.XamlRoot ?? throw new InvalidOperationException($"The window's {nameof(window.RootElement)} doesn't have a {nameof(XamlRoot)}.");
		return XamlRootMap.GetHostForRoot(xamlRoot) as HeadlessWindowWrapper
			?? throw new InvalidOperationException($"The {nameof(XamlRoot)} is not associated with a {nameof(HeadlessWindowWrapper)} instance.");
	}

	public void Dispose() => _terminationGate.Set();
}
