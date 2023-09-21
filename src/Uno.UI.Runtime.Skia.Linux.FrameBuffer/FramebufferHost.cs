using System;
using System.ComponentModel;
using System.Threading;
using Uno.Extensions.ApplicationModel.Core;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Skia.Gtk.Extensions.UI.Xaml.Controls;
using Uno.UI.Xaml.Controls;
using Uno.UI.Xaml.Core;
using Uno.WinUI.Runtime.Skia.Linux.FrameBuffer;
using Uno.WinUI.Runtime.Skia.Linux.FrameBuffer.UI;
using Uno.WinUI.Runtime.Skia.LinuxFB;
using Windows.Graphics.Display;
using Microsoft.UI.Xaml;
using WUX = Microsoft.UI.Xaml;

namespace Uno.UI.Runtime.Skia.Linux.FrameBuffer
{
	public class FrameBufferHost : ISkiaApplicationHost, IXamlRootHost
	{
		[ThreadStatic]
		private static bool _isDispatcherThread = false;

		private readonly EventLoop _eventLoop;
		private readonly CoreApplicationExtension? _coreApplicationExtension;

		private Func<Application> _appBuilder;
		private Renderer? _renderer;
		private DisplayInformationExtension? _displayInformationExtension;
		private Thread? _consoleInterceptionThread;
		private ManualResetEvent _terminationGate = new(false);

		/// <summary>
		/// Creates a host for a Uno Skia FrameBuffer application.
		/// </summary>
		/// <param name="appBuilder">App builder.</param>
		/// <remarks>
		/// Environment.CommandLine is used to fill LaunchEventArgs.Arguments.
		/// </remarks>
		public FrameBufferHost(Func<WUX.Application> appBuilder)
		{
			_appBuilder = appBuilder;

			_eventLoop = new EventLoop();
			_coreApplicationExtension = new CoreApplicationExtension(_terminationGate);
		}

		/// <summary>
		/// Provides a display scale to override framebuffer default scale
		/// </summary>
		/// <remarks>This value can be overriden by the UNO_DISPLAY_SCALE_OVERRIDE environment variable</remarks>
		public float? DisplayScale { get; set; }

		public void Run()
		{
			StartConsoleInterception();

			_eventLoop.Schedule(Initialize);

			_terminationGate.WaitOne();

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"Application is exiting");
			}
		}

		private void StartConsoleInterception()
		{
			_consoleInterceptionThread = new(() =>
			{

				// Loop until Application.Current.Exit() is invoked
				while (!_coreApplicationExtension!.ExitRequested)
				{
					// Read the console keys without showing them on screen.
					// The keyboard input is handled by libinput.
					Console.ReadKey(true);
				}

				// The process asked to exit
				_terminationGate.Set();
			});

			// The thread must not block the process from exiting
			_consoleInterceptionThread.IsBackground = true;

			_consoleInterceptionThread.Start();
		}

		private void Initialize()
		{
			_isDispatcherThread = true;

			ApiExtensibility.Register(typeof(INativeWindowFactoryExtension), o => new NativeWindowFactoryExtension(this));
			ApiExtensibility.Register(typeof(Uno.ApplicationModel.Core.ICoreApplicationExtension), o => _coreApplicationExtension!);
			ApiExtensibility.Register<IXamlRootHost>(typeof(Windows.UI.Core.IUnoCorePointerInputSource), o => { FrameBufferPointerInputSource.Instance.SetHost(o); return FrameBufferPointerInputSource.Instance; });
			ApiExtensibility.Register<IXamlRootHost>(typeof(Windows.UI.Core.IUnoKeyboardInputSource), o => { FrameBufferKeyboardInputSource.Instance.SetHost(o); return FrameBufferKeyboardInputSource.Instance; });
			ApiExtensibility.Register(typeof(Windows.UI.Core.ICoreWindowExtension), o => new CoreWindowExtension());
			ApiExtensibility.Register(typeof(Windows.UI.ViewManagement.IApplicationViewExtension), o => new ApplicationViewExtension(o));
			ApiExtensibility.Register(typeof(Windows.Graphics.Display.IDisplayInformationExtension), o => _displayInformationExtension ??= new DisplayInformationExtension(o, DisplayScale));

			void Dispatch(System.Action d)
				=> _eventLoop.Schedule(() => d());

			void CreateApp(ApplicationInitializationCallbackParams _)
			{
				var app = _appBuilder();
				app.Host = this;

				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"Display Information: " +
						$"{_renderer?.FrameBufferDevice.ScreenSize.Width}x{_renderer?.FrameBufferDevice.ScreenSize.Height} " +
						$"({_renderer?.FrameBufferDevice.ScreenPhysicalDimensions} mm), " +
						$"PixelFormat: {_renderer?.FrameBufferDevice.PixelFormat}, " +
						$"ResolutionScale: {DisplayInformation.GetForCurrentView().ResolutionScale}, " +
						$"LogicalDpi: {DisplayInformation.GetForCurrentView().LogicalDpi}, " +
						$"RawPixelsPerViewPixel: {DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel}, " +
						$"DiagonalSizeInInches: {DisplayInformation.GetForCurrentView().DiagonalSizeInInches}, " +
						$"ScreenInRawPixels: {DisplayInformation.GetForCurrentView().ScreenWidthInRawPixels}x{DisplayInformation.GetForCurrentView().ScreenHeightInRawPixels}");
				}

				// Force intialization of the DisplayInformation
				DisplayInformation.GetForCurrentView();

				if (_displayInformationExtension is null)
				{
					throw new InvalidOperationException("DisplayInformation is not yet initialized");
				}

				_displayInformationExtension.Renderer = _renderer;
			}

			Windows.UI.Core.CoreDispatcher.DispatchOverride = Dispatch;
			Windows.UI.Core.CoreDispatcher.HasThreadAccessOverride = () => _isDispatcherThread;

			FrameBufferInputProvider.Instance.Initialize();

			_renderer = new Renderer(this);

			CoreServices.Instance.ContentRootCoordinator.CoreWindowContentRootSet += OnCoreWindowContentRootSet;

			WUX.Application.StartWithArguments(CreateApp);
		}

		private void OnCoreWindowContentRootSet(object? sender, object e)
		{
			var contentRoot = CoreServices.Instance
				.ContentRootCoordinator
				.CoreWindowContentRoot;
			var xamlRoot = contentRoot?.GetOrCreateXamlRoot();

			if (xamlRoot is null)
			{
				throw new InvalidOperationException("XamlRoot was not properly initialized");
			}

			contentRoot!.SetHost(this);
			FrameBufferManager.XamlRootMap.Register(xamlRoot, this);

			CoreServices.Instance.ContentRootCoordinator.CoreWindowContentRootSet -= OnCoreWindowContentRootSet;
		}

		void IXamlRootHost.InvalidateRender() => _renderer?.InvalidateRender();

		WUX.UIElement? IXamlRootHost.RootElement => FrameBufferWindowWrapper.Instance.Window?.RootElement;
	}
}
