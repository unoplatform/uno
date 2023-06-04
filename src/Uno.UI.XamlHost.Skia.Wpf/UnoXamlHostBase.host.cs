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
using Uno.UI.Xaml.Hosting;
using Uno.UI.Controls;
using Uno.UI.Skia.Platform;
using Uno.UI.Runtime.Skia.Wpf.Rendering;
using Uno.UI.XamlHost.Extensions;
using System;
using Uno.Foundation.Logging;
using Windows.Foundation.Metadata;
using Uno.UI.Runtime.Skia.Wpf.Hosting;
using Uno.UI.Runtime.Skia.Wpf.Extensions;

namespace Uno.UI.XamlHost.Skia.Wpf
{
	/// <summary>
	/// UnoXamlHost control hosts UWP XAML content inside the Windows Presentation Foundation
	/// </summary>
	partial class UnoXamlHostBase : IWpfXamlRootHost
	{
		private bool _designMode;
		private bool _ignorePixelScaling;
		private WpfCanvas _nativeOverlayLayer;
		private IWpfRenderer _renderer;
		private Windows.UI.Xaml.UIElement? _rootElement;

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
			WpfExtensionsRegistrar.Register();

			_designMode = DesignerProperties.GetIsInDesignMode(this);

			SetupRenderer();

			Loaded += UnoXamlHostBase_Loaded;
			Unloaded += UnoXamlHostBase_Unloaded;
		}

		private void UnoXamlHostBase_Unloaded(object sender, RoutedEventArgs e)
		{
		}

		private void UnoXamlHostBase_Loaded(object sender, RoutedEventArgs e)
		{
			if (!(_renderer?.Initialize() ?? false))
			{
				RenderSurfaceType = Uno.UI.Skia.RenderSurfaceType.Software;
				SetupRenderer();
				_renderer?.Initialize();
			}
		}

		private void SetupRenderer()
		{
			RenderSurfaceType ??= Uno.UI.Skia.RenderSurfaceType.OpenGL;

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

		void IXamlRootHost.ReleasePointerCapture() => ReleaseMouseCapture(); //TODO: This should capture the correct type of pointer (stylus/mouse/touch) https://github.com/unoplatform/uno/issues/8978[capture]

		void IXamlRootHost.SetPointerCapture() => CaptureMouse();

		void IXamlRootHost.InvalidateRender()
		{
			//InvalidateOverlays();
			InvalidateVisual();
		}

		bool IXamlRootHost.IsIsland => true;

		WinUI.UIElement? IXamlRootHost.RootElement => _rootElement ??= _xamlSource?.GetVisualTreeRoot();

		WpfCanvas? IWpfXamlRootHost.NativeOverlayLayer => _nativeOverlayLayer;

		WinUI.XamlRoot? IXamlRootHost.XamlRoot => ChildInternal?.XamlRoot;

		bool IWpfXamlRootHost.IgnorePixelScaling => IgnorePixelScaling;
	}
}
