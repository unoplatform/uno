using System;
using Windows.System;
using Windows.UI.Xaml;
using WUX = Windows.UI.Xaml;
using Uno.WinUI.Runtime.Skia.LinuxFB;
using Windows.UI.Core;
using Uno.Foundation.Extensibility;
using System.ComponentModel;
using Uno.UI.Xaml.Core;

namespace Uno.UI.Runtime.Skia
{
	public class FrameBufferHost : ISkiaHost
	{
		[ThreadStatic]
		private static bool _isDispatcherThread = false;

		private Func<Application> _appBuilder;
		private readonly EventLoop _eventLoop;
		private Renderer? _renderer;
		private DisplayInformationExtension? _displayInformationExtension;

		/// <summary>
		/// Creates a host for a Uno Skia FrameBuffer application.
		/// </summary>
		/// <param name="appBuilder">App builder.</param>
		/// <param name="args">Deprecated, value ignored.</param>		
		/// <remarks>
		/// Args are obsolete and will be removed in the future. Environment.CommandLine is used instead
		/// to fill LaunchEventArgs.Arguments.
		/// </remarks>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public FrameBufferHost(Func<WUX.Application> appBuilder, string[] args) : this(appBuilder)
		{
		}

		public FrameBufferHost(Func<WUX.Application> appBuilder)
		{
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

			void Dispatch(System.Action d)
				=> _eventLoop.Schedule(() => d());

			void CreateApp(ApplicationInitializationCallbackParams _)
			{
				var app = _appBuilder();
				app.Host = this;
			}

			Windows.UI.Core.CoreDispatcher.DispatchOverride = Dispatch;
			Windows.UI.Core.CoreDispatcher.HasThreadAccessOverride = () => _isDispatcherThread;

			_renderer = new Renderer();
			_displayInformationExtension!.Renderer = _renderer;

			CoreServices.Instance.ContentRootCoordinator.CoreWindowContentRootSet += OnCoreWindowContentRootSet;

			WUX.Application.StartWithArguments(CreateApp);
		}

		private void OnCoreWindowContentRootSet(object? sender, object e)
		{
			var xamlRoot = CoreServices.Instance
				.ContentRootCoordinator
				.CoreWindowContentRoot?
				.GetOrCreateXamlRoot();

			if (xamlRoot is null)
			{
				throw new InvalidOperationException("XamlRoot was not properly initialized");
			}

			xamlRoot.InvalidateRender += _renderer!.InvalidateRender;

			CoreServices.Instance.ContentRootCoordinator.CoreWindowContentRootSet -= OnCoreWindowContentRootSet;
		}
	}
}
