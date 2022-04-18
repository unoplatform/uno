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
using WpfApplication = global::System.Windows.Application;
using WpfCanvas = global::System.Windows.Controls.Canvas;
using WpfControl = global::System.Windows.Controls.Control;
using WpfFrameworkPropertyMetadata = System.Windows.FrameworkPropertyMetadata;

namespace Uno.UI.XamlHost.Skia.Wpf
{
	/// <summary>
	/// UnoXamlHost control hosts UWP XAML content inside the Windows Presentation Foundation
	/// </summary>
	abstract partial class UnoXamlHostBase : WpfControl, WinUI.ISkiaHost
	{
		private bool _designMode;
		private DisplayInformation _displayInformation;
		private bool _ignorePixelScaling;
		private WriteableBitmap _bitmap;

		public bool IgnorePixelScaling
		{
			get => _ignorePixelScaling;
			set
			{
				_ignorePixelScaling = value;
				InvalidateVisual();
			}
		}

		private void InitializeHost()
		{
			_designMode = DesignerProperties.GetIsInDesignMode(this);
		}

		protected override void OnRender(DrawingContext drawingContext)
		{			
			if (!IsXamlContentLoaded())
			{
				return;
			}

			if (_designMode)
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
			if (_bitmap == null || info.Width != _bitmap.PixelWidth || info.Height != _bitmap.PixelHeight)
			{
				_bitmap = new WriteableBitmap(width, height, 96 * dpiScaleX, 96 * dpiScaleY, PixelFormats.Pbgra32, null);
			}

			// draw on the bitmap
			_bitmap.Lock();
			using (var surface = SKSurface.Create(info, _bitmap.BackBuffer, _bitmap.BackBufferStride))
			{
				surface.Canvas.Clear(SKColors.White);
				surface.Canvas.SetMatrix(SKMatrix.CreateScale((float)dpiScaleX, (float)dpiScaleY));
				if (ChildInternal?.Visual != null)
				{
					WinUI.Window.Current.Compositor.RenderVisual(surface, ChildInternal.Visual);
				}
				surface.Canvas.Flush();
			}

			// draw the bitmap to the screen
			_bitmap.AddDirtyRect(new Int32Rect(0, 0, _bitmap.PixelWidth, _bitmap.PixelHeight));			
			_bitmap.Unlock();
			

			drawingContext.DrawImage(_bitmap, new Rect(0, 0, ActualWidth, ActualHeight));
			drawingContext.DrawEllipse(Brushes.Blue, null, new Point(10, 10), 10, 10);
		}

		private Random _randomizer = new Random();
	}
}
