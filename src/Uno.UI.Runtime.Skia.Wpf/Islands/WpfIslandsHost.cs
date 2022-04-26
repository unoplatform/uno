#nullable enable

using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SkiaSharp;
using Uno.ApplicationModel.DataTransfer;
using Uno.Extensions.ApplicationModel.DataTransfer;
using Uno.Extensions.Networking.Connectivity;
using Uno.Extensions.Storage.Pickers;
using Uno.Extensions.System;
using Uno.Extensions.System.Profile;
using Uno.Extensions.UI.Core.Preview;
using Uno.Foundation.Extensibility;
using Uno.Helpers.Theming;
using Uno.UI.Core.Preview;
using Uno.UI.Runtime.Skia.Wpf;
using Uno.UI.Runtime.Skia.Wpf.WPF.Extensions.Helper.Theming;
using Uno.UI.Runtime.Skia.WPF.Extensions.UI.Xaml.Controls;
using Uno.UI.Xaml;
using Uno.UI.Xaml.Controls.Extensions;
using Uno.UI.Xaml.Core;
using Uno.UI.XamlHost.Skia.Wpf;
using Windows.Devices.Input;
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
using WpfFrameworkPropertyMetadata = System.Windows.FrameworkPropertyMetadata;

namespace Uno.UI.Skia.Platform
{
	[TemplatePart(Name = NativeOverlayLayerPart, Type = typeof(WpfCanvas))]
	public class WpfIslandsHost : WpfControl, WinUI.ISkiaHost, IWpfHost
	{
		private const string NativeOverlayLayerPart = "NativeOverlayLayer";

		private readonly bool designMode;

		[ThreadStatic] private static WpfIslandsHost _current;

		private WpfCanvas? _nativeOverlayLayer = null;
		private WriteableBitmap bitmap;
		private bool ignorePixelScaling;
		private FocusManager? _focusManager;
		private bool _isVisible = true;

		private DisplayInformation _displayInformation;

		static WpfIslandsHost()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(WpfIslandsHost), new WpfFrameworkPropertyMetadata(typeof(WpfIslandsHost)));

			WpfHost.RegisterExtensions();
		}

		public static WpfIslandsHost Current => _current;

		internal WpfCanvas? NativeOverlayLayer => _nativeOverlayLayer;

		/// <summary>
		/// Creates a WpfHost element to host a Uno-Skia into a WPF application.
		/// </summary>
		/// <remarks>
		/// If args are omitted, those from Environment.GetCommandLineArgs() will be used.
		/// </remarks>
		public WpfIslandsHost(global::System.Windows.Threading.Dispatcher dispatcher, Func<WinUI.Application> appBuilder, string[] args = null)
		{
			_current = this;

			void CreateApp(WinUI.ApplicationInitializationCallbackParams _)
			{
				var app = appBuilder();
				app.Host = this;
			}

			Windows.UI.Core.CoreDispatcher.DispatchOverride = d => dispatcher.BeginInvoke(d);
			Windows.UI.Core.CoreDispatcher.HasThreadAccessOverride = dispatcher.CheckAccess;

			WinUI.Application.Start(CreateApp);

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
		}

		private void MainWindow_Closing(object sender, CancelEventArgs e)
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

		public SKSize CanvasSize => bitmap == null ? SKSize.Empty : new SKSize(bitmap.PixelWidth, bitmap.PixelHeight);

		public bool IgnorePixelScaling
		{
			get => ignorePixelScaling;
			set
			{
				ignorePixelScaling = value;
				InvalidateVisual();
			}
		}

		WinUI.XamlRoot? IWpfHost.XamlRoot => throw new NotImplementedException();

		protected override void OnRender(DrawingContext drawingContext)
		{
			base.OnRender(drawingContext);

			if (designMode)
			{
				return;
			}

			if (ActualWidth == 0
				|| ActualHeight == 0
				|| double.IsNaN(ActualWidth)
				|| double.IsNaN(ActualHeight)
				|| double.IsInfinity(ActualWidth)
				|| double.IsInfinity(ActualHeight)
				|| Visibility != Visibility.Visible)
			{
				return;
			}


			int width, height;

			if (_displayInformation == null)
			{
				_displayInformation = DisplayInformation.GetForCurrentView();
			}

			var dpi = _displayInformation.RawPixelsPerViewPixel;
			double dpiScaleX = dpi;
			double dpiScaleY = dpi;
			if (IgnorePixelScaling)
			{
				width = (int)ActualWidth;
				height = (int)ActualHeight;
			}
			else
			{
				var matrix = PresentationSource.FromVisual(this).CompositionTarget.TransformToDevice;
				dpiScaleX = matrix.M11;
				dpiScaleY = matrix.M22;
				width = (int)(ActualWidth * dpiScaleX);
				height = (int)(ActualHeight * dpiScaleY);
			}

			var info = new SKImageInfo(width, height, SKImageInfo.PlatformColorType, SKAlphaType.Premul);

			// reset the bitmap if the size has changed
			if (bitmap == null || info.Width != bitmap.PixelWidth || info.Height != bitmap.PixelHeight)
			{
				bitmap = new WriteableBitmap(width, height, 96 * dpiScaleX, 96 * dpiScaleY, PixelFormats.Pbgra32, null);
			}

			// draw on the bitmap
			bitmap.Lock();
			using (var surface = SKSurface.Create(info, bitmap.BackBuffer, bitmap.BackBufferStride))
			{
				surface.Canvas.Clear(SKColors.White);
				surface.Canvas.SetMatrix(SKMatrix.CreateScale((float)dpiScaleX, (float)dpiScaleY));
				WinUI.Window.Current.Compositor.Render(surface);
			}

			// draw the bitmap to the screen
			bitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
			bitmap.Unlock();
			drawingContext.DrawImage(bitmap, new Rect(0, 0, ActualWidth, ActualHeight));
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

		void IWpfHost.ReleasePointerCapture(PointerIdentifier pointer) => CaptureMouse(); //TODO:MZ:This should capture the correct type of pointer (stylus/mouse/touch)

		void IWpfHost.SetPointerCapture(PointerIdentifier pointer) => ReleaseMouseCapture();
	}
}
