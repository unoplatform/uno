#nullable enable

using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SkiaSharp;
using Uno.ApplicationModel.DataTransfer;
using Uno.Disposables;
using Uno.Extensions.ApplicationModel.Core;
using Uno.Extensions.ApplicationModel.DataTransfer;
using Uno.Extensions.Networking.Connectivity;
using Uno.Extensions.Storage.Pickers;
using Uno.Extensions.System;
using Uno.Extensions.System.Profile;
using Uno.Extensions.UI.Core.Preview;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Uno.Helpers.Theming;
using Uno.UI.Core.Preview;
using Uno.UI.Runtime.Skia.Wpf;
using Uno.UI.Runtime.Skia.Wpf.Extensions.UI.Xaml.Controls;
using Uno.UI.Runtime.Skia.Wpf.Extensions.UI.Xaml.Input;
using Uno.UI.Runtime.Skia.Wpf.Rendering;
using Uno.UI.Runtime.Skia.Wpf.WPF.Extensions.Helpers.Theming;
using Uno.UI.Xaml;
using Uno.UI.Xaml.Controls.Extensions;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Input;
using Uno.UI.XamlHost.Skia.Wpf;
using Uno.UI.XamlHost.Skia.Wpf.Hosting;
using Windows.Graphics.Display;
using Windows.Networking.Connectivity;
using Windows.Storage.Pickers;
using Windows.System.Profile.Internal;
using Windows.UI.Core.Preview;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using UnoApplication = Windows.UI.Xaml.Application;
using WinUI = Windows.UI.Xaml;
using WpfApplication = System.Windows.Application;
using WpfCanvas = System.Windows.Controls.Canvas;
using WpfControl = System.Windows.Controls.Control;

namespace Uno.UI.Skia.Platform
{
	public class WpfHost : IWpfApplicationHost
	{
		private readonly Func<UnoApplication> _appBuilder;
		private CompositeDisposable _registrations = new();

		[ThreadStatic] private static WpfHost? _current;

		private bool ignorePixelScaling;
		private FocusManager? _focusManager;
		private bool _isVisible = true;

		static WpfHost()
		{
			RegisterExtensions();
		}

		private static bool _extensionsRegistered;
		private IWpfRenderer? _renderer;

		internal static void RegisterExtensions()
		{
			if (_extensionsRegistered)
			{
				return;
			}

			ApiExtensibility.Register(typeof(Uno.ApplicationModel.Core.ICoreApplicationExtension), o => new CoreApplicationExtension(o));
			ApiExtensibility.Register(typeof(Windows.UI.Core.IUnoCorePointerInputSource), o => new WpfCorePointerInputSource((IWpfHost)o));
			ApiExtensibility.Register(typeof(Windows.UI.Core.ICoreWindowExtension), o => new WpfCoreWindowExtension(o));
			ApiExtensibility.Register(typeof(Windows.UI.ViewManagement.IApplicationViewExtension), o => new WpfApplicationViewExtension(o));
			ApiExtensibility.Register(typeof(ISystemThemeHelperExtension), o => new WpfSystemThemeHelperExtension(o));
			ApiExtensibility.Register(typeof(IDisplayInformationExtension), o => new WpfDisplayInformationExtension(o));
			ApiExtensibility.Register(typeof(Windows.ApplicationModel.DataTransfer.DragDrop.Core.IDragDropExtension), o => new WpfDragDropExtension(o));
			ApiExtensibility.Register(typeof(IFileOpenPickerExtension), o => new FileOpenPickerExtension(o));
			ApiExtensibility.Register<FolderPicker>(typeof(IFolderPickerExtension), o => new FolderPickerExtension(o));
			ApiExtensibility.Register(typeof(IFileSavePickerExtension), o => new FileSavePickerExtension(o));
			ApiExtensibility.Register(typeof(IConnectionProfileExtension), o => new WindowsConnectionProfileExtension(o));
			ApiExtensibility.Register<TextBoxView>(typeof(IOverlayTextBoxViewExtension), o => new TextBoxViewExtension(o));
			ApiExtensibility.Register(typeof(ILauncherExtension), o => new LauncherExtension(o));
			ApiExtensibility.Register(typeof(IClipboardExtension), o => new ClipboardExtensions(o));
			ApiExtensibility.Register(typeof(IAnalyticsInfoExtension), o => new AnalyticsInfoExtension());
			ApiExtensibility.Register(typeof(ISystemNavigationManagerPreviewExtension), o => new SystemNavigationManagerPreviewExtension());

			_extensionsRegistered = true;
		}

		public bool IsIsland => false;

		public Windows.UI.Xaml.UIElement? RootElement => null;

		public static WpfHost? Current => _current;

		/// <summary>
		/// Gets or sets the current Skia Render surface type.
		/// </summary>
		/// <remarks>If <c>null</c>, the host will try to determine the most compatible mode.</remarks>
		public RenderSurfaceType? RenderSurfaceType { get; set; }


		public WpfHost(global::System.Windows.Threading.Dispatcher dispatcher, Func<WinUI.Application> appBuilder)
		{
			_current = this;
			_appBuilder = appBuilder;

			Windows.UI.Core.CoreDispatcher.DispatchOverride = d => dispatcher.BeginInvoke(d);
			Windows.UI.Core.CoreDispatcher.HasThreadAccessOverride = dispatcher.CheckAccess;

			WpfApplication.Current.Activated += Current_Activated;
			WpfApplication.Current.Deactivated += Current_Deactivated;
			WpfApplication.Current.MainWindow.StateChanged += MainWindow_StateChanged;
			WpfApplication.Current.MainWindow.Closing += MainWindow_Closing;
		}

		private void InitializeRenderer()
		{
			if (RenderSurfaceType is null)
			{
				RenderSurfaceType = Skia.RenderSurfaceType.OpenGL;
			}

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"Using {RenderSurfaceType} rendering");
			}

			_renderer = RenderSurfaceType switch
			{
				Skia.RenderSurfaceType.Software => new SoftwareWpfRenderer(this),
				Skia.RenderSurfaceType.OpenGL => new OpenGLWpfRenderer(this),
				_ => throw new InvalidOperationException($"Render Surface type {RenderSurfaceType} is not supported")
			};
		}

		void IWpfApplicationHost.InvalidateRender()
		{
			InvalidateOverlays();
			InvalidateVisual();
		}

		private void MainWindow_Closing(object? sender, CancelEventArgs e)
		{
			var manager = SystemNavigationManagerPreview.GetForCurrentView();
			if (!manager.HasConfirmedClose)
			{
				if (!manager.RequestAppClose())
				{
					e.Cancel = true;
					return;
				}
			}

			// Closing should continue, perform suspension.
			UnoApplication.Current.RaiseSuspending();
		}

		private void StartApp()
		{
			void CreateApp(WinUI.ApplicationInitializationCallbackParams _)
			{
				var app = _appBuilder();
				app.Host = this;
			}

			WinUI.Application.StartWithArguments(CreateApp);

			UpdateWindowPropertiesFromPackage();
		}

		private void MainWindow_StateChanged(object? sender, EventArgs e)
		{
			var wpfWindow = WpfApplication.Current.MainWindow;
			var winUIWindow = WinUI.Window.Current;
			var application = WinUI.Application.Current;
			var wasVisible = _isVisible;

			_isVisible = wpfWindow.WindowState != WindowState.Minimized;

			if (wasVisible && !_isVisible)
			{
				winUIWindow.OnVisibilityChanged(false);
				application?.RaiseEnteredBackground(null);
			}
			else if (!wasVisible && _isVisible)
			{
				application?.RaiseLeavingBackground(() => winUIWindow?.OnVisibilityChanged(true));
			}
		}

		private void Current_Deactivated(object? sender, EventArgs e)
		{
			var winUIWindow = WinUI.Window.Current;
			winUIWindow?.OnActivated(Windows.UI.Core.CoreWindowActivationState.Deactivated);
		}

		private void Current_Activated(object? sender, EventArgs e)
		{
			var winUIWindow = WinUI.Window.Current;
			winUIWindow?.OnActivated(Windows.UI.Core.CoreWindowActivationState.CodeActivated);
		}

		public bool IgnorePixelScaling
		{
			get => ignorePixelScaling;
			set
			{
				ignorePixelScaling = value;
				InvalidateVisual();
			}
		}

		protected override void OnRender(DrawingContext drawingContext)
		{
			base.OnRender(drawingContext);

			_renderer?.Render(drawingContext);
		}

		private void InvalidateOverlays()
		{
			_focusManager ??= VisualTree.GetFocusManagerForElement(Windows.UI.Xaml.Window.Current?.RootElement);
			_focusManager?.FocusRectManager?.RedrawFocusVisual();
			if (_focusManager?.FocusedElement is TextBox textBox)
			{
				textBox.TextBoxView?.Extension?.InvalidateLayout();
			}
		}

		//TODO: This will need to be adjusted when multi-window support is added. https://github.com/unoplatform/uno/issues/8978[windows]
		WinUI.XamlRoot? IWpfApplicationHost.XamlRoot => WinUI.Window.Current?.RootElement?.XamlRoot;

		WpfCanvas? IWpfApplicationHost.NativeOverlayLayer => NativeOverlayLayer;
	}
}
