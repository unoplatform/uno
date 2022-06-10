#nullable enable

using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SkiaSharp;
using Uno.UI.XamlHost.Skia.Wpf;
using Windows.Devices.Input;
using Windows.Graphics.Display;
using WinUI = Windows.UI.Xaml;
using WpfControl = global::System.Windows.Controls.Control;
using WpfCanvas = global::System.Windows.Controls.Canvas;
using Uno.UI.Controls;
using Uno.UI.Skia.Platform;
using Uno.UI.Runtime.Skia.Wpf.Rendering;
using Uno.UI.Runtime.Skia.Wpf;

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

		public Windows.UI.Xaml.UIElement? RootElement => ChildInternal;

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
			// TODO: These three lines are required here for initialization, but should be refactored later #8978
			WpfHost.RegisterExtensions();
			Windows.UI.Core.CoreDispatcher.DispatchOverride = d => Dispatcher.BeginInvoke(d);
			Windows.UI.Core.CoreDispatcher.HasThreadAccessOverride = Dispatcher.CheckAccess;

			_designMode = DesignerProperties.GetIsInDesignMode(this);

			_renderer = new UnoWpfRenderer(this);
			_hostPointerHandler = new HostPointerHandler(this);
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

		void IWpfHost.ReleasePointerCapture() => CaptureMouse(); //TODO: This should capture the correct type of pointer (stylus/mouse/touch) #8978[capture]

		void IWpfHost.SetPointerCapture() => ReleaseMouseCapture();

		WinUI.XamlRoot? IWpfHost.XamlRoot => ChildInternal?.XamlRoot;

		System.Windows.Controls.Canvas? IWpfHost.NativeOverlayLayer => _nativeOverlayLayer;
	}
}
