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
	private readonly EventLoop _eventLoop;
	private readonly ManualResetEvent _terminationGate = new(false);
	private readonly CoreApplicationExtension _coreApplicationExtension;
	private readonly HeadlessHostBuilder _hostBuilder;
	private readonly Func<WUX.Application> _appBuilder;

	private NativeWindowFactoryExtension? _windowFactory;
	private bool _previousSkipVisualTreePainting;
	private int _dispatcherThreadId;

	/// <summary>
	/// Creates a host for a Uno Skia headless (offscreen) application. Use <c>UseHeadless()</c> on the
	/// <see cref="Uno.UI.Hosting.IUnoPlatformHostBuilder"/> rather than constructing this directly.
	/// </summary>
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

		// Tear down every window (stop its render thread, unregister from XamlRootMap) before returning.
		_windowFactory?.TearDownWindows();

		// SkipVisualTreePainting is a process-wide flag; restore what it was before this host ran.
		FeatureConfiguration.Rendering.SkipVisualTreePainting = _previousSkipVisualTreePainting;

		return Task.CompletedTask;
	}

	private void InnerInitialize()
	{
		// InnerInitialize runs on the EventLoop's dedicated thread — the dispatcher thread. Capture its id
		// so HasThreadAccessOverride can answer without any thread-static state.
		_dispatcherThreadId = Environment.CurrentManagedThreadId;

		// Headless windows produce no pixel output, so skip the paint walk globally to save CPU. The
		// render cycle still ticks (keeping scheduling/animations alive), and RenderTargetBitmap does its
		// own paint, so on-demand capture is unaffected. The flag is process-wide, so remember the prior
		// value and restore it on shutdown (see RunLoop).
		_previousSkipVisualTreePainting = FeatureConfiguration.Rendering.SkipVisualTreePainting;
		FeatureConfiguration.Rendering.SkipVisualTreePainting = true;

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
		Windows.UI.Core.CoreDispatcher.HasThreadAccessOverride = () => Environment.CurrentManagedThreadId == _dispatcherThreadId;

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
