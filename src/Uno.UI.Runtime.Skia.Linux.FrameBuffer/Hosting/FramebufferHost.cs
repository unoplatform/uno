using System;
using System.Threading;
using Uno.Extensions.ApplicationModel.Core;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Uno.UI.Dispatching;
using Uno.UI.Hosting;
using Uno.UI.Xaml.Controls;
using Uno.WinUI.Runtime.Skia.Linux.FrameBuffer;
using Uno.WinUI.Runtime.Skia.Linux.FrameBuffer.UI;
using Microsoft.UI.Xaml;
using Uno.Helpers;
using WUX = Microsoft.UI.Xaml;
using System.Threading.Tasks;
using Uno.UI.Runtime.Skia.Linux.FrameBuffer.UI;

namespace Uno.UI.Runtime.Skia.Linux.FrameBuffer
{
	public class FrameBufferHost : SkiaHost, ISkiaApplicationHost, IXamlRootHost, IDisposable
	{
		[ThreadStatic]
		private static bool _isDispatcherThread = false;

		private readonly EventLoop _eventLoop;
		private readonly CoreApplicationExtension? _coreApplicationExtension;

		private Func<Application> _appBuilder;
		private FrameBufferRenderer? _renderer;
		private Thread? _consoleInterceptionThread;
		private ManualResetEvent _terminationGate = new(false);
		private readonly FramebufferHostBuilder _hostBuilder;

		/// <summary>
		/// Creates a host for a Uno Skia FrameBuffer application.
		/// </summary>
		/// <param name="appBuilder">App builder.</param>
		/// <remarks>
		/// Environment.CommandLine is used to fill LaunchEventArgs.Arguments.
		/// </remarks>
		public FrameBufferHost(Func<WUX.Application> appBuilder, FramebufferHostBuilder builder)
		{
			_appBuilder = appBuilder;
			_hostBuilder = builder;

			_eventLoop = new EventLoop();
			_coreApplicationExtension = new CoreApplicationExtension(_terminationGate);
		}

		/// <summary>
		/// Provides a display scale to override framebuffer default scale
		/// </summary>
		/// <remarks>This value can be overriden by the UNO_DISPLAY_SCALE_OVERRIDE environment variable</remarks>
		public float? DisplayScale { get; set; }

		protected override void Initialize()
		{
			StartConsoleInterception();

			_eventLoop.Schedule(InnerInitialize);
		}

		protected override Task RunLoop()
		{
			_terminationGate.WaitOne();

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"Application is exiting");
			}

			return Task.CompletedTask;
		}

		private void StartConsoleInterception()
		{
			// ANSI escape sequence to hide the blinking caret
			Console.WriteLine("\u001b[?25l");

			// Only use the keyboard interception if the input is not redirected, to support
			// starting the app without a pty.
			if (!Console.IsInputRedirected && Console.KeyAvailable)
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
			else
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"Console input is redirected, skipping input interception");
				}
			}
		}

		private void InnerInitialize()
		{
			_isDispatcherThread = true;
			FrameBufferWindowWrapper.Init(_hostBuilder.DisplayOrientation);

			ApiExtensibility.Register(typeof(INativeWindowFactoryExtension), o => new NativeWindowFactoryExtension(this));
			ApiExtensibility.Register(typeof(Uno.ApplicationModel.Core.ICoreApplicationExtension), o => _coreApplicationExtension!);
			ApiExtensibility.Register<IXamlRootHost>(typeof(Windows.UI.Core.IUnoCorePointerInputSource), o => { FrameBufferPointerInputSource.Instance.SetHost(o); return FrameBufferPointerInputSource.Instance; });
			ApiExtensibility.Register<IXamlRootHost>(typeof(Windows.UI.Core.IUnoKeyboardInputSource), o => { FrameBufferKeyboardInputSource.Instance.SetHost(o); return FrameBufferKeyboardInputSource.Instance; });
			ApiExtensibility.Register(typeof(Windows.UI.ViewManagement.IApplicationViewExtension), o => new ApplicationViewExtension(o));
			ApiExtensibility.Register(typeof(Windows.Graphics.Display.IDisplayInformationExtension), o => new DisplayInformationExtension(o, DisplayScale));

			void Dispatch(System.Action d, NativeDispatcherPriority p)
				=> _eventLoop.Schedule(d);

			void CreateApp(ApplicationInitializationCallbackParams _)
			{
				var app = _appBuilder();
				app.Host = this;

				// Force the first render once the app has been setup
				Dispatch(() => _renderer.InvalidateRender(), NativeDispatcherPriority.High);
			}

			Windows.UI.Core.CoreDispatcher.DispatchOverride = Dispatch;
			Windows.UI.Core.CoreDispatcher.HasThreadAccessOverride = () => _isDispatcherThread;

			FrameBufferInputProvider.Instance.Initialize();

			if (_hostBuilder.UseDRM ?? false)
			{
				_renderer = new DRMRenderer(this, _hostBuilder.DRMCardPath, _hostBuilder.DRMConnectorChooser, _hostBuilder.GBMSurfaceColorFormat, _hostBuilder.ShowMouseCursor, _hostBuilder.MouseCursorRadius, _hostBuilder.MouseCursorColor);
			}
			else if (_hostBuilder.UseDRM is null)
			{
				try
				{
					_renderer = new DRMRenderer(this, _hostBuilder.DRMCardPath, _hostBuilder.DRMConnectorChooser, _hostBuilder.GBMSurfaceColorFormat, _hostBuilder.ShowMouseCursor, _hostBuilder.MouseCursorRadius, _hostBuilder.MouseCursorColor);
				}
				catch (Exception e)
				{
					this.LogError()?.Error($"Failed to create an OpenGLES context with error '{e.Message}', falling back to software rendering");
					_renderer = new SoftwareRenderer(this, _hostBuilder.ShowMouseCursor, _hostBuilder.MouseCursorRadius, _hostBuilder.MouseCursorColor);
				}
			}
			else
			{
				_renderer = new SoftwareRenderer(this, _hostBuilder.ShowMouseCursor, _hostBuilder.MouseCursorRadius, _hostBuilder.MouseCursorColor);
			}

			WUX.Application.Start(CreateApp);
		}

		void IXamlRootHost.InvalidateRender() => _renderer?.InvalidateRender();

		WUX.UIElement? IXamlRootHost.RootElement => FrameBufferWindowWrapper.Instance.Window?.RootElement;

		public void Dispose()
		{
		}
	}
}
