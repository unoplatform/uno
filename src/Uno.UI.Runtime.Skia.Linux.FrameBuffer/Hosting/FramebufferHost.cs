﻿using System;
using System.ComponentModel;
using System.Threading;
using Uno.Extensions.ApplicationModel.Core;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Uno.UI.Dispatching;
using Uno.UI.Hosting;
using Uno.UI.Xaml.Controls;
using Uno.UI.Xaml.Core;
using Uno.WinUI.Runtime.Skia.Linux.FrameBuffer;
using Uno.WinUI.Runtime.Skia.Linux.FrameBuffer.UI;
using Windows.Graphics.Display;
using Microsoft.UI.Xaml;
using Uno.Helpers;
using WUX = Microsoft.UI.Xaml;
using System.Threading.Tasks;
using Uno.UI.Runtime.Skia.Linux.FrameBuffer.UI;
using Microsoft.UI.Xaml.Media;

namespace Uno.UI.Runtime.Skia.Linux.FrameBuffer
{
	public class FrameBufferHost : SkiaHost, ISkiaApplicationHost, IXamlRootHost, IDisposable
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

			ApiExtensibility.Register(typeof(INativeWindowFactoryExtension), o => new NativeWindowFactoryExtension(this));
			ApiExtensibility.Register(typeof(Uno.ApplicationModel.Core.ICoreApplicationExtension), o => _coreApplicationExtension!);
			ApiExtensibility.Register<IXamlRootHost>(typeof(Windows.UI.Core.IUnoCorePointerInputSource), o => { FrameBufferPointerInputSource.Instance.SetHost(o); return FrameBufferPointerInputSource.Instance; });
			ApiExtensibility.Register<IXamlRootHost>(typeof(Windows.UI.Core.IUnoKeyboardInputSource), o => { FrameBufferKeyboardInputSource.Instance.SetHost(o); return FrameBufferKeyboardInputSource.Instance; });
			ApiExtensibility.Register(typeof(Windows.UI.ViewManagement.IApplicationViewExtension), o => new ApplicationViewExtension(o));
			ApiExtensibility.Register(typeof(Windows.Graphics.Display.IDisplayInformationExtension), o => _displayInformationExtension ??= new DisplayInformationExtension(o, DisplayScale));

			void Dispatch(System.Action d, NativeDispatcherPriority p)
				=> _eventLoop.Schedule(d);

			void CreateApp(ApplicationInitializationCallbackParams _)
			{
				var app = _appBuilder();
				app.Host = this;

				// Force intialization of the DisplayInformation
				var displayInformation = DisplayInformation.GetForCurrentViewSafe();

				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"Display Information: " +
						$"{_renderer?.FrameBufferDevice.ScreenSize.Width}x{_renderer?.FrameBufferDevice.ScreenSize.Height} " +
						$"({_renderer?.FrameBufferDevice.ScreenPhysicalDimensions} mm), " +
						$"PixelFormat: {_renderer?.FrameBufferDevice.PixelFormat}, " +
						$"ResolutionScale: {displayInformation.ResolutionScale}, " +
						$"LogicalDpi: {displayInformation.LogicalDpi}, " +
						$"RawPixelsPerViewPixel: {displayInformation.RawPixelsPerViewPixel}, " +
						$"DiagonalSizeInInches: {displayInformation.DiagonalSizeInInches}, " +
						$"ScreenInRawPixels: {displayInformation.ScreenWidthInRawPixels}x{displayInformation.ScreenHeightInRawPixels}");
				}

				if (_displayInformationExtension is null)
				{
					throw new InvalidOperationException("DisplayInformation is not yet initialized");
				}

				_displayInformationExtension.Renderer = _renderer;

				// Force the first render once the app has been setup
				Dispatch(() => _renderer?.InvalidateRender(), NativeDispatcherPriority.High);
			}

			Windows.UI.Core.CoreDispatcher.DispatchOverride = Dispatch;
			Windows.UI.Core.CoreDispatcher.HasThreadAccessOverride = () => _isDispatcherThread;

			// We do not have a display timer on this target, we can use
			// a constant timer.
			CompositionTargetTimer.Start();

			FrameBufferInputProvider.Instance.Initialize();

			_renderer = new Renderer(this);

			WUX.Application.Start(CreateApp);
		}

		void IXamlRootHost.InvalidateRender() => _renderer?.InvalidateRender();

		WUX.UIElement? IXamlRootHost.RootElement => FrameBufferWindowWrapper.Instance.Window?.RootElement;

		public void Dispose()
		{
		}
	}
}
