using System;
using Windows.System;
using Windows.UI.Xaml;
using WUX = Windows.UI.Xaml;
using Uno.WinUI.Runtime.Skia.LinuxFB;
using Windows.UI.Core;
using Uno.Foundation.Extensibility;

namespace Uno.UI.Runtime.Skia
{
	public class FrameBufferHost : ISkiaHost
	{
		[ThreadStatic]
		private static bool _isDispatcherThread = false;

		private string[] _args;
		private Func<Application> _appBuilder;
		private readonly EventLoop _eventLoop;
		private Renderer? _renderer;
		private DisplayInformationExtension? _displayInformationExtension;

		public FrameBufferHost(Func<WUX.Application> appBuilder, string[] args)
		{
			_args = args;
			_appBuilder = appBuilder;

			_eventLoop = new EventLoop();
		}

		public void Run()
		{
			_eventLoop.Schedule(Initialize);

			System.Console.ReadLine();
		}

		private void Initialize()
		{
			ApiExtensibility.Register(typeof(Windows.UI.Core.ICoreWindowExtension), o => new CoreWindowExtension(o));
			ApiExtensibility.Register(typeof(Windows.UI.ViewManagement.IApplicationViewExtension), o => new ApplicationViewExtension(o));
			ApiExtensibility.Register(typeof(Windows.Graphics.Display.IDisplayInformationExtension), o => _displayInformationExtension ??= new DisplayInformationExtension(o));

			_renderer = new Renderer();
			_displayInformationExtension!.Renderer = _renderer;

			bool EnqueueNative(DispatcherQueuePriority priority, DispatcherQueueHandler callback)
			{
				_eventLoop.Schedule(() => callback());

				return true;
			}

			void Dispatch(System.Action d)
			{
				_eventLoop.Schedule(() => d());
			}

			Windows.System.DispatcherQueue.EnqueueNativeOverride = EnqueueNative;
			Windows.UI.Core.CoreDispatcher.DispatchOverride = Dispatch;
			Windows.UI.Core.CoreDispatcher.HasThreadAccessOverride = () => _isDispatcherThread;

			void CreateApp(ApplicationInitializationCallbackParams _)
			{
				var app = _appBuilder();
				app.Host = this;
			}

			WUX.Application.Start(CreateApp, _args);
		}
	}
}
