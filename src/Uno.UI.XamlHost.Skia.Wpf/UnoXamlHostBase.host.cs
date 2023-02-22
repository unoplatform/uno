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
using Uno.UI.XamlHost.Extensions;
using System;
using Uno.Foundation.Logging;
using Windows.Foundation.Metadata;

namespace Uno.UI.XamlHost.Skia.Wpf
{
	/// <summary>
	/// UnoXamlHost control hosts UWP XAML content inside the Windows Presentation Foundation
	/// </summary>
	partial class UnoXamlHostBase
	{
		private bool _designMode;
		private bool _ignorePixelScaling;
		private HostPointerHandler _hostPointerHandler;
		private WpfCanvas _nativeOverlayLayer;
		private IWpfRenderer _renderer;
		private Windows.UI.Xaml.UIElement? _rootElement;

		public bool IsIsland => true;

		public Windows.UI.Xaml.UIElement? RootElement =>
			_rootElement ??= _xamlSource?.GetVisualTreeRoot();

		/// <summary>
		/// Gets or sets the current Skia Render surface type.
		/// </summary>
		/// <remarks>If <c>null</c>, the host will try to determine the most compatible mode.</remarks>
		public Uno.UI.Skia.RenderSurfaceType? RenderSurfaceType { get; set; }

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
			// TODO: These three lines are required here for initialization, but should be refactored later https://github.com/unoplatform/uno/issues/8978
			WpfHost.RegisterExtensions();

			_designMode = DesignerProperties.GetIsInDesignMode(this);

			InitializeRenderer();
			_hostPointerHandler = new HostPointerHandler(this);
		}

		private void InitializeRenderer()
		{
			if (RenderSurfaceType is null)
			{
				RenderSurfaceType = Uno.UI.Skia.RenderSurfaceType.OpenGL;
			}

			_renderer = RenderSurfaceType switch
			{
				Uno.UI.Skia.RenderSurfaceType.Software => new SoftwareWpfRenderer(this),
				Uno.UI.Skia.RenderSurfaceType.OpenGL => new OpenGLWpfRenderer(this),
				_ => throw new InvalidOperationException($"Render Surface type {RenderSurfaceType} is not supported")
			};
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

		void IWpfHost.ReleasePointerCapture() => ReleaseMouseCapture(); //TODO: This should capture the correct type of pointer (stylus/mouse/touch) https://github.com/unoplatform/uno/issues/8978[capture]

		void IWpfHost.SetPointerCapture() => CaptureMouse();

		WinUI.XamlRoot? IWpfHost.XamlRoot => ChildInternal?.XamlRoot;

		System.Windows.Controls.Canvas? IWpfHost.NativeOverlayLayer => _nativeOverlayLayer;
	}
}
