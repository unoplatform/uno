#nullable enable

using SkiaSharp;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Uno.Extensions.Storage.Pickers;
using System.Windows.Threading;
using Uno.Foundation.Extensibility;
using Uno.Helpers.Theming;
using Uno.UI.Runtime.Skia.Wpf.WPF.Extensions.Helper.Theming;
using Windows.Graphics.Display;
using Windows.System;
using WinUI = Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.UI.Xaml.Controls.Extensions;
using Uno.UI.Runtime.Skia.WPF.Extensions.UI.Xaml.Controls;
using WpfApplication = System.Windows.Application;
using WpfCanvas = System.Windows.Controls.Canvas;
using WpfControl = System.Windows.Controls.Control;
using WpfFrameworkPropertyMetadata = System.Windows.FrameworkPropertyMetadata;
using Uno.UI.Xaml;
using Uno.UI.Runtime.Skia.Wpf;

namespace Uno.UI.Skia.Platform
{
	[TemplatePart(Name = NativeOverlayLayerPart, Type = typeof(WpfCanvas))]
	public class WpfHost : WpfControl, WinUI.ISkiaHost
	{
		private const string NativeOverlayLayerPart = "NativeOverlayLayer";

		private readonly bool designMode;

		[ThreadStatic] private static WpfHost _current;

		private WpfCanvas? _nativeOverlayLayer = null;
		private WriteableBitmap bitmap;
		private bool ignorePixelScaling;

		static WpfHost()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(WpfHost), new WpfFrameworkPropertyMetadata(typeof(WpfHost)));

			ApiExtensibility.Register(typeof(Windows.UI.Core.ICoreWindowExtension), o => new WpfCoreWindowExtension(o));
			ApiExtensibility.Register<Windows.UI.Xaml.Application>(typeof(IApplicationExtension), o => new WpfApplicationExtension(o));
			ApiExtensibility.Register(typeof(Windows.UI.ViewManagement.IApplicationViewExtension), o => new WpfApplicationViewExtension(o));
			ApiExtensibility.Register(typeof(ISystemThemeHelperExtension), o => new WpfSystemThemeHelperExtension(o));
			ApiExtensibility.Register(typeof(IDisplayInformationExtension), o => new WpfDisplayInformationExtension(o));
			ApiExtensibility.Register(typeof(Windows.ApplicationModel.DataTransfer.DragDrop.Core.IDragDropExtension), o => new WpfDragDropExtension(o));
			ApiExtensibility.Register(typeof(IFileOpenPickerExtension), o => new FileOpenPickerExtension(o));
			ApiExtensibility.Register(typeof(IFileSavePickerExtension), o => new FileSavePickerExtension(o));
			ApiExtensibility.Register<TextBoxView>(typeof(ITextBoxViewExtension), o => new TextBoxViewExtension(o));
		}

		public static WpfHost Current => _current;

		internal WpfCanvas? NativeOverlayLayer => _nativeOverlayLayer;

		/// <summary>
		/// Creates a WpfHost element to host a Uno-Skia into a WPF application.
		/// </summary>
		/// <remarks>
		/// If args are omitted, those from Environment.GetCommandLineArgs() will be used.
		/// </remarks>
		public WpfHost(global::System.Windows.Threading.Dispatcher dispatcher, Func<WinUI.Application> appBuilder, string[] args = null)
		{
			_current = this;

			args ??= Environment
				.GetCommandLineArgs()
				.Skip(1)
				.ToArray();

			designMode = DesignerProperties.GetIsInDesignMode(this);

			void CreateApp(WinUI.ApplicationInitializationCallbackParams _)
			{
				var app = appBuilder();
				app.Host = this;
			}

			bool EnqueueNative(DispatcherQueuePriority priority, DispatcherQueueHandler callback)
			{
				if (priority == DispatcherQueuePriority.Normal)
				{
					dispatcher.BeginInvoke(callback);
				}
				else
				{
					var p = priority switch
					{
						DispatcherQueuePriority.Low => DispatcherPriority.Background,
						DispatcherQueuePriority.High => DispatcherPriority.Send, // This one is higher than normal
						_ => DispatcherPriority.Normal
					};
					dispatcher.BeginInvoke(p, callback);
				}

				return true;
			}

			Windows.System.DispatcherQueue.EnqueueNativeOverride = EnqueueNative;

			Windows.UI.Core.CoreDispatcher.DispatchOverride = d => dispatcher.BeginInvoke(d);
			Windows.UI.Core.CoreDispatcher.HasThreadAccessOverride = dispatcher.CheckAccess;

			WinUI.Application.Start(CreateApp, args);

			WinUI.Window.InvalidateRender += () => InvalidateVisual();

			WpfApplication.Current.Activated += Current_Activated;
			WpfApplication.Current.Deactivated += Current_Deactivated;
			WpfApplication.Current.MainWindow.StateChanged += MainWindow_StateChanged;

			SizeChanged += WpfHost_SizeChanged;
			Loaded += WpfHost_Loaded;
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
			var isVisible = wpfWindow.WindowState != WindowState.Minimized;
			winUIWindow.OnVisibilityChanged(isVisible);
		}

		private void Current_Deactivated(object? sender, EventArgs e)
		{
			var winUIWindow = WinUI.Window.Current;
			winUIWindow?.OnActivated(Windows.UI.Core.CoreWindowActivationState.Deactivated);

			var application = WinUI.Application.Current;
			application?.OnEnteredBackground();
		}

		private void Current_Activated(object? sender, EventArgs e)
		{
			var application = WinUI.Application.Current;
			application?.OnLeavingBackground();

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
			var dpi = VisualTreeHelper.GetDpi(WpfApplication.Current.MainWindow);
			double dpiScaleX = dpi.DpiScaleX;
			double dpiScaleY = dpi.DpiScaleY;
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
				WinUI.Window.Current.Compositor.Render(surface, info);
			}

			// draw the bitmap to the screen
			bitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
			bitmap.Unlock();
			drawingContext.DrawImage(bitmap, new Rect(0, 0, ActualWidth, ActualHeight));
		}
	}
}
