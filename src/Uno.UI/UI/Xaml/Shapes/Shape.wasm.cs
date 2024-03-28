using System;
using System.Linq;
using Uno;
using Uno.Disposables;
using Uno.Extensions;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Wasm;

using RadialGradientBrush = Microsoft/* UWP don't rename */.UI.Xaml.Media.RadialGradientBrush;
using System.Numerics;
using System.Diagnostics;

namespace Windows.UI.Xaml.Shapes
{
	partial class Shape
	{
		private readonly SerialDisposable _fillBrushSubscription = new SerialDisposable();
		private readonly SerialDisposable _strokeBrushSubscription = new SerialDisposable();

		private DefsSvgElement _defs;
		private protected readonly SvgElement _mainSvgElement;

		protected Shape() : base("svg", isSvg: true)
		{
			// This constructor shouldn't be used. It exists to match WinUI API surface.
			throw new InvalidOperationException("This constructor shouldn't be used.");
		}

		private protected Shape(string mainSvgElementTag) : base("svg", isSvg: true)
		{
			_mainSvgElement = new SvgElement(mainSvgElementTag);
			AddChild(_mainSvgElement);
		}

		private protected void UpdateRender()
		{
			OnFillBrushChanged();
			OnStrokeBrushChanged();
			UpdateStrokeThickness();
			UpdateStrokeDashArray();
		}


		private protected override void OnHitTestVisibilityChanged(HitTestability oldValue, HitTestability newValue)
		{
			// We don't invoke the base, so we stay at the default "pointer-events: none" defined in Uno.UI.css in class svg.uno-uielement.
			// This is required to avoid this SVG element (which is actually only a collection) to stoll pointer events.
		}

		private void OnFillBrushChanged()
		{
			// We don't request an update of the HitTest (UpdateHitTest()) since this element is never expected to be hit testable.
			// Note: We also enforce that the default hit test == false is not altered in the OnHitTestVisibilityChanged.

			// Instead we explicitly set the IsHitTestVisible on each child SvgElement
			var fill = Fill;

			// Known issue: The hit test is only linked to the Fill, but should also take in consideration the Stroke and the StrokeThickness.
			// Note: _mainSvgElement and _defs are internal elements, so it's legit to alter the IsHitTestVisible here.
			_mainSvgElement.IsHitTestVisible = fill != null;
			if (_defs is not null)
			{
				_defs.IsHitTestVisible = fill != null;
			}

			var svgElement = _mainSvgElement;
			switch (fill)
			{
				case SolidColorBrush scb:
					Uno.UI.Xaml.WindowManagerInterop.SetElementFill(svgElement.HtmlId, scb.ColorWithOpacity);
					_fillBrushSubscription.Disposable = null;
					break;
				case ImageBrush ib:
					var (imageFill, subscription) = ib.ToSvgElement(this);
					var imageFillId = imageFill.HtmlId;
					GetDefs().Add(imageFill);
					svgElement.SetStyle("fill", $"url(#{imageFillId})");
					var removeDef = new DisposableAction(() => GetDefs().Remove(imageFill));
					_fillBrushSubscription.Disposable = new CompositeDisposable(removeDef, subscription);
					break;
				case GradientBrush gb:
					var gradient = gb.ToSvgElement();
					var gradientId = gradient.HtmlId;
					GetDefs().Add(gradient);
					svgElement.SetStyle("fill", $"url(#{gradientId})");
					_fillBrushSubscription.Disposable = new DisposableAction(
						() => GetDefs().Remove(gradient)
					);
					break;
				case RadialGradientBrush rgb:
					var radialGradient = rgb.ToSvgElement();
					var radialGradientId = radialGradient.HtmlId;
					GetDefs().Add(radialGradient);
					svgElement.SetStyle("fill", $"url(#{radialGradientId})");
					_fillBrushSubscription.Disposable = new DisposableAction(
						() => GetDefs().Remove(radialGradient)
					);
					break;
				case AcrylicBrush ab:
					svgElement.SetStyle("fill", ab.FallbackColorWithOpacity.ToHexString());
					_fillBrushSubscription.Disposable = null;
					break;
				case null:
					// The default is black if the style is not set in Web's' SVG. So if the Fill property is not set,
					// we explicitly set the style to transparent in order to match the UWP behavior.
					svgElement.SetStyle("fill", "transparent");
					_fillBrushSubscription.Disposable = null;
					break;
				default:
					svgElement.ResetStyle("fill");
					_fillBrushSubscription.Disposable = null;
					break;
			}
		}

		private void OnStrokeBrushChanged()
		{
			var svgElement = _mainSvgElement;
			var stroke = Stroke;

			switch (stroke)
			{
				case SolidColorBrush scb:
					svgElement.SetStyle("stroke", scb.ColorWithOpacity.ToHexString());
					_strokeBrushSubscription.Disposable = null;
					break;
				case ImageBrush ib:
					var (imageFill, subscription) = ib.ToSvgElement(this);
					var imageFillId = imageFill.HtmlId;
					GetDefs().Add(imageFill);
					svgElement.SetStyle("stroke", $"url(#{imageFillId})");
					var removeDef = new DisposableAction(() => GetDefs().Remove(imageFill));
					_fillBrushSubscription.Disposable = new CompositeDisposable(removeDef, subscription);
					break;
				case GradientBrush gb:
					var gradient = gb.ToSvgElement();
					var gradientId = gradient.HtmlId;
					GetDefs().Add(gradient);
					svgElement.SetStyle("stroke", $"url(#{gradientId})");
					_strokeBrushSubscription.Disposable = new DisposableAction(
						() => GetDefs().Remove(gradient)
					);
					break;
				case RadialGradientBrush rgb:
					var radialGradient = rgb.ToSvgElement();
					var radialGradientId = radialGradient.HtmlId;
					GetDefs().Add(radialGradient);
					svgElement.SetStyle("stroke", $"url(#{radialGradientId})");
					_strokeBrushSubscription.Disposable = new DisposableAction(
						() => GetDefs().Remove(radialGradient)
					);
					break;
				case AcrylicBrush ab:
					svgElement.SetStyle("stroke", ab.FallbackColorWithOpacity.ToHexString());
					_strokeBrushSubscription.Disposable = null;
					break;
				default:
					svgElement.ResetStyle("stroke");
					_strokeBrushSubscription.Disposable = null;
					break;
			}
		}

		private void UpdateStrokeThickness()
		{
			var svgElement = _mainSvgElement;
			var strokeThickness = ActualStrokeThickness;

			if (strokeThickness != 1.0d)
			{
				svgElement.SetStyle("stroke-width", $"{strokeThickness}px");
			}
			else
			{
				svgElement.ResetStyle("stroke-width");
			}
		}

		private void UpdateStrokeDashArray()
		{
			var svgElement = _mainSvgElement;

			if (StrokeDashArray is not { } strokeDashArray)
			{
				svgElement.ResetStyle("stroke-dasharray");
			}
			else
			{
				var str = string.Join(",", strokeDashArray.Select(d => $"{d.ToStringInvariant()}px"));
				svgElement.SetStyle("stroke-dasharray", str);
			}
		}

		/// <summary>
		/// Gets host for non-visual elements
		/// </summary>
		private UIElementCollection GetDefs()
		{
			if (_defs == null)
			{
				_defs = new DefsSvgElement();
				AddChild(_defs);
			}

			return _defs.Defs;
		}

		private static Rect GetPathBoundingBox(Shape shape)
		{
			return shape._mainSvgElement.GetBBox();
		}

		private protected void Render(Shape shape, Size? size = null, double scaleX = 1d, double scaleY = 1d, double renderOriginX = 0d, double renderOriginY = 0d)
		{
			Debug.Assert(shape == this);
			var scale = Matrix3x2.CreateScale((float)scaleX, (float)scaleY);
			var translate = Matrix3x2.CreateTranslation((float)renderOriginX, (float)renderOriginY);
			var matrix = scale * translate;
			_mainSvgElement.SetNativeTransform(matrix);
		}

	}
}
