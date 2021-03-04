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
using WpfApplication = System.Windows.Application;

namespace Uno.UI.Skia.Platform
{
	public class WpfHost : FrameworkElement, WinUI.ISkiaHost
	{
		private readonly bool designMode;
		private WriteableBitmap bitmap;
		private bool ignorePixelScaling;		

		static WpfHost()
		{
			ApiExtensibility.Register(typeof(Windows.UI.Core.ICoreWindowExtension), o => new WpfCoreWindowExtension(o));
			ApiExtensibility.Register(typeof(Windows.UI.ViewManagement.IApplicationViewExtension), o => new WpfApplicationViewExtension(o));
			ApiExtensibility.Register(typeof(ISystemThemeHelperExtension), o => new WpfSystemThemeHelperExtension(o));
			ApiExtensibility.Register(typeof(IDisplayInformationExtension), o => new WpfDisplayInformationExtension(o));
			ApiExtensibility.Register(typeof(Windows.ApplicationModel.DataTransfer.DragDrop.Core.IDragDropExtension), o => new WpfDragDropExtension(o));
			ApiExtensibility.Register(typeof(IFileOpenPickerExtension), o => new FileOpenPickerExtension(o));
			ApiExtensibility.Register(typeof(IFileSavePickerExtension), o => new FileSavePickerExtension(o));
		}

		[ThreadStatic] private static WpfHost _current;
		public static WpfHost Current => _current;

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
				if(priority == DispatcherQueuePriority.Normal)
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

			SizeChanged += WpfHost_SizeChanged;
			Loaded += WpfHost_Loaded;
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
