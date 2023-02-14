#nullable enable

using System;
using System.IO;
using System.ComponentModel;
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
using Uno.UI.Runtime.Skia.Wpf.Extensions.UI.Xaml.Input;
using Uno.UI.Runtime.Skia.Wpf.Rendering;
using Uno.UI.Runtime.Skia.Wpf.WPF.Extensions.Helper.Theming;
using Uno.UI.Runtime.Skia.WPF.Extensions.UI.Xaml.Controls;
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
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using UnoApplication = Microsoft.UI.Xaml.Application;
using WinUI = Microsoft.UI.Xaml;
using WpfApplication = System.Windows.Application;
using WpfCanvas = System.Windows.Controls.Canvas;
using WpfControl = System.Windows.Controls.Control;
using WpfFrameworkPropertyMetadata = System.Windows.FrameworkPropertyMetadata;

namespace Uno.UI.Skia.Platform
{
	[TemplatePart(Name = NativeOverlayLayerPart, Type = typeof(WpfCanvas))]
	public class WpfHost : WpfControl, WinUI.ISkiaHost, IWpfHost
	{
		private const string NativeOverlayLayerPart = "NativeOverlayLayer";

		private readonly Func<UnoApplication> _appBuilder;
		private CompositeDisposable _registrations = new();

		[ThreadStatic] private static WpfHost? _current;

		private WpfCanvas? _nativeOverlayLayer;
		private bool ignorePixelScaling;
		private FocusManager? _focusManager;
		private bool _isVisible = true;

		static WpfHost()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(WpfHost), new WpfFrameworkPropertyMetadata(typeof(WpfHost)));

			RegisterExtensions();
		}

		private static bool _extensionsRegistered;
		private UnoWpfRenderer _renderer;
		private HostPointerHandler? _hostPointerHandler;

		internal static void RegisterExtensions()
		{
			if (_extensionsRegistered)
			{
				return;
			}

			ApiExtensibility.Register(typeof(Uno.ApplicationModel.Core.ICoreApplicationExtension), o => new CoreApplicationExtension(o));
			ApiExtensibility.Register(typeof(Windows.UI.Core.ICoreWindowExtension), o => new WpfCoreWindowExtension(o));
			ApiExtensibility.Register<Microsoft.UI.Xaml.Application>(typeof(IApplicationExtension), o => new WpfApplicationExtension(o));
			ApiExtensibility.Register(typeof(Windows.UI.ViewManagement.IApplicationViewExtension), o => new WpfApplicationViewExtension(o));
			ApiExtensibility.Register(typeof(ISystemThemeHelperExtension), o => new WpfSystemThemeHelperExtension(o));
			ApiExtensibility.Register(typeof(IDisplayInformationExtension), o => new WpfDisplayInformationExtension(o));
			ApiExtensibility.Register(typeof(Windows.ApplicationModel.DataTransfer.DragDrop.Core.IDragDropExtension), o => new WpfDragDropExtension(o));
			ApiExtensibility.Register(typeof(IFileOpenPickerExtension), o => new FileOpenPickerExtension(o));
			ApiExtensibility.Register<FolderPicker>(typeof(IFolderPickerExtension), o => new FolderPickerExtension(o));
			ApiExtensibility.Register(typeof(IFileSavePickerExtension), o => new FileSavePickerExtension(o));
			ApiExtensibility.Register(typeof(IConnectionProfileExtension), o => new WindowsConnectionProfileExtension(o));
			ApiExtensibility.Register<TextBoxView>(typeof(ITextBoxViewExtension), o => new TextBoxViewExtension(o));
			ApiExtensibility.Register(typeof(ILauncherExtension), o => new LauncherExtension(o));
			ApiExtensibility.Register(typeof(IClipboardExtension), o => new ClipboardExtensions(o));
			ApiExtensibility.Register(typeof(IAnalyticsInfoExtension), o => new AnalyticsInfoExtension());
			ApiExtensibility.Register(typeof(ISystemNavigationManagerPreviewExtension), o => new SystemNavigationManagerPreviewExtension());
			ApiExtensibility.Register(typeof(IPointerExtension), o => new PointerExtension());

			_extensionsRegistered = true;
		}

		public bool IsIsland => false;

		public Microsoft.UI.Xaml.UIElement? RootElement => null;

		public static WpfHost? Current => _current;

		internal WpfCanvas? NativeOverlayLayer => _nativeOverlayLayer;

		/// <summary>
		/// Creates a WpfHost element to host a Uno-Skia into a WPF application.
		/// </summary>
		/// <param name="appBuilder">App builder.</param>
		/// <param name="args">Deprecated, value ignored.</param>		
		/// <remarks>
		/// Args are obsolete and will be removed in the future. Environment.CommandLine is used instead
		/// to fill LaunchEventArgs.Arguments.
		/// </remarks>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public WpfHost(global::System.Windows.Threading.Dispatcher dispatcher, Func<WinUI.Application> appBuilder, string[]? args = null) : this(dispatcher, appBuilder)
		{
		}

		public WpfHost(global::System.Windows.Threading.Dispatcher dispatcher, Func<WinUI.Application> appBuilder)
		{
			FocusVisualStyle = null;

			_current = this;
			_appBuilder = appBuilder;

			Windows.UI.Core.CoreDispatcher.DispatchOverride = d => dispatcher.BeginInvoke(d);
			Windows.UI.Core.CoreDispatcher.HasThreadAccessOverride = dispatcher.CheckAccess;
			_renderer = new UnoWpfRenderer(this);

			WpfApplication.Current.Activated += Current_Activated;
			WpfApplication.Current.Deactivated += Current_Deactivated;
			WpfApplication.Current.MainWindow.StateChanged += MainWindow_StateChanged;
			WpfApplication.Current.MainWindow.Closing += MainWindow_Closing;

			Windows.Foundation.Size preferredWindowSize = ApplicationView.PreferredLaunchViewSize;
			if (preferredWindowSize != Windows.Foundation.Size.Empty)
			{
				WpfApplication.Current.MainWindow.Width = (int)preferredWindowSize.Width;
				WpfApplication.Current.MainWindow.Height = (int)preferredWindowSize.Height;
			}

			SizeChanged += WpfHost_SizeChanged;
			Loaded += WpfHost_Loaded;

			CoreServices.Instance.ContentRootCoordinator.CoreWindowContentRootSet += OnCoreWindowContentRootSet;
			RegisterForBackgroundColor();
		}

		private void UpdateWindowPropertiesFromPackage()
		{
			if (Windows.ApplicationModel.Package.Current.Logo is Uri uri)
			{
				var basePath = uri.OriginalString.Replace('\\', Path.DirectorySeparatorChar);
				var iconPath = Path.Combine(Windows.ApplicationModel.Package.Current.InstalledPath, basePath);

				if (File.Exists(iconPath))
				{
					if (this.Log().IsEnabled(LogLevel.Information))
					{
						this.Log().Info($"Loading icon file [{iconPath}] from Package.appxmanifest file");
					}

					WpfApplication.Current.MainWindow.Icon = new BitmapImage(new Uri(iconPath));
				}
				else if (Microsoft.UI.Xaml.Media.Imaging.BitmapImage.GetScaledPath(basePath) is { } scaledPath && File.Exists(scaledPath))
				{
					if (this.Log().IsEnabled(LogLevel.Information))
					{
						this.Log().Info($"Loading icon file [{scaledPath}] scaled logo from Package.appxmanifest file");
					}

					WpfApplication.Current.MainWindow.Icon = new BitmapImage(new Uri(scaledPath));
				}
				else
				{
					if (this.Log().IsEnabled(LogLevel.Warning))
					{
						this.Log().Warn($"Unable to find icon file [{iconPath}] specified in the Package.appxmanifest file.");
					}
				}
			}

			Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().Title = Windows.ApplicationModel.Package.Current.DisplayName;
		}

		private void RegisterForBackgroundColor()
		{
			void Update()
			{
				if (WinUI.Window.Current.Background is WinUI.Media.SolidColorBrush brush)
				{
					_renderer.BackgroundColor = brush.Color;
				}
				else
				{
					if (this.Log().IsEnabled(LogLevel.Warning))
					{
						this.Log().Warn($"This platform only supports SolidColorBrush for the Window background");
					}
				}
			}

			Update();

			_registrations.Add(WinUI.Window.Current.RegisterBackgroundChangedEvent((s, e) => Update()));
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

			XamlRootMap.Register(xamlRoot, this);

			CoreServices.Instance.ContentRootCoordinator.CoreWindowContentRootSet -= OnCoreWindowContentRootSet;
		}

		void IWpfHost.InvalidateRender()
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

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_nativeOverlayLayer = GetTemplateChild(NativeOverlayLayerPart) as WpfCanvas;

			// App needs to be created after the native overlay layer is properly initialized
			// otherwise the initially focused input element would cause exception.
			StartApp();
		}

		private void StartApp()
		{
			void CreateApp(WinUI.ApplicationInitializationCallbackParams _)
			{
				var app = _appBuilder();
				app.Host = this;
			}

			WinUI.Application.StartWithArguments(CreateApp);
			_hostPointerHandler = new HostPointerHandler(this);

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

		private void WpfHost_Loaded(object sender, RoutedEventArgs e)
		{
			WinUI.Window.Current.OnNativeSizeChanged(new Windows.Foundation.Size(ActualWidth, ActualHeight));

			// Avoid dotted border on focus.
			if (Parent is WpfControl control)
			{
				control.FocusVisualStyle = null;
			}
		}

		private void WpfHost_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			WinUI.Window.Current.OnNativeSizeChanged(
				new Windows.Foundation.Size(
					e.NewSize.Width,
					e.NewSize.Height
				)
			);
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

		[Obsolete("It will be removed in the next major release.")]
		public SKSize CanvasSize => _renderer.CanvasSize;

		protected override void OnRender(DrawingContext drawingContext)
		{
			base.OnRender(drawingContext);

			_renderer.Render(drawingContext);
		}

		private void InvalidateOverlays()
		{
			_focusManager ??= VisualTree.GetFocusManagerForElement(Microsoft.UI.Xaml.Window.Current?.RootElement);
			_focusManager?.FocusRectManager?.RedrawFocusVisual();
			if (_focusManager?.FocusedElement is TextBox textBox)
			{
				textBox.TextBoxView?.Extension?.InvalidateLayout();
			}
		}

		void IWpfHost.ReleasePointerCapture() => ReleaseMouseCapture(); //TODO: This should capture the correct type of pointer (stylus/mouse/touch) https://github.com/unoplatform/uno/issues/8978[capture]

		void IWpfHost.SetPointerCapture() => CaptureMouse();

		//TODO: This will need to be adjusted when multi-window support is added. https://github.com/unoplatform/uno/issues/8978[windows]
		WinUI.XamlRoot? IWpfHost.XamlRoot => WinUI.Window.Current?.RootElement?.XamlRoot;

		WpfCanvas? IWpfHost.NativeOverlayLayer => NativeOverlayLayer;
	}
}
