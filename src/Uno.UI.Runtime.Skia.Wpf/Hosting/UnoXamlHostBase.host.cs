#nullable enable

using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SkiaSharp;
using Uno.UI.Runtime.Skia.Wpf;
using Windows.Devices.Input;
using Windows.Graphics.Display;
using WinUI = Windows.UI.Xaml;
using WpfControl = global::System.Windows.Controls.Control;
using WpfCanvas = global::System.Windows.Controls.Canvas;
using Uno.UI.Runtime.Skia.Wpf.Rendering;
using Uno.UI.Controls;

namespace Uno.UI.XamlHost.Skia.Wpf
{
	/// <summary>
	/// UnoXamlHost control hosts UWP XAML content inside the Windows Presentation Foundation
	/// </summary>
	partial class UnoXamlHostBase
	{
		private bool _designMode;
		private DisplayInformation _displayInformation;
		private bool _ignorePixelScaling;
		private WriteableBitmap _bitmap;
		private HostPointerHandler _hostPointerHandler;
		private WpfCanvas _nativeOverlayLayer;
		private UnoWpfRenderer _renderer;

		public bool IsIsland => true;

		public Windows.UI.Composition.Visual? Visual => ChildInternal?.Visual;

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

			_hostPointerHandler = new HostPointerHandler(this);
			_renderer = new UnoWpfRenderer(this);
		}

		protected override void OnRender(DrawingContext drawingContext)
		{
			base.OnRender(drawingContext);
			
			if (!IsXamlContentLoaded())
			{
				return;
			}

			_renderer?.Render(drawingContext);
		}

		void IWpfHost.ReleasePointerCapture(PointerIdentifier pointer) => CaptureMouse(); //TODO: This should capture the correct type of pointer (stylus/mouse/touch) #8978[capture]

		void IWpfHost.SetPointerCapture(PointerIdentifier pointer) => ReleaseMouseCapture();

		WinUI.XamlRoot? IWpfHost.XamlRoot => ChildInternal?.XamlRoot;

		System.Windows.Controls.Canvas? IWpfHost.NativeOverlayLayer => _nativeOverlayLayer;
	}
}
