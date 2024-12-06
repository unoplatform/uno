using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Wasm;
using Uno;
using Uno.Collections;
using Uno.Disposables;
using Uno.Extensions;
using Uno.UI;
using Uno.UI.Xaml;

using RadialGradientBrush = Microsoft/* UWP don't rename */.UI.Xaml.Media.RadialGradientBrush;

namespace Microsoft.UI.Xaml.Shapes
{
	partial class Shape
	{
		private static readonly LruCache<string, Rect> _bboxCache = new(FeatureConfiguration.Shape.WasmDefaultBBoxCacheSize);

		internal static int BBoxCacheSize
		{
			get => _bboxCache.Capacity;
			set => _bboxCache.Capacity = value;
		}
	}

	partial class Shape
	{
		private protected string _bboxCacheKey;

		private readonly SerialDisposable _fillBrushSubscription = new SerialDisposable();
		private readonly SerialDisposable _strokeBrushSubscription = new SerialDisposable();

		private DefsSvgElement _defs;
		private protected readonly SvgElement _mainSvgElement;
		private protected bool _shouldUpdateNative = !FeatureConfiguration.Shape.WasmDelayUpdateUntilFirstArrange;

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
			// We can delay the setting of these property until first arrange pass,
			// so to prevent double-updates from both OnPropertyChanged & ArrangeOverride.
			// These updates are a little costly as we need to cross the cs-js bridge.
			_shouldUpdateNative = true;

			// Regarding to caching of GetPathBoundingBox(js: getBBox) result:
			// On shapes that depends on it, so: Line, Path, Polygon, Polyline
			// all the properties below (at the time of written) has no effect on getBBox:
			// > Note that the values of the opacity, visibility, fill, fill-opacity, fill-rule, stroke-dasharray and stroke-dashoffset properties on an element have no effect on the bounding box of an element.
			// > -- https://svgwg.org/svg2-draft/coords.html#BoundingBoxes
			// while not mentioned, stroke-width doesnt affect getBBox neither (for the 4 classes of shape mentioned above).

			// StrokeThickness can alter getBBox on Ellipse and Rectangle, but we dont use getBBox in these two.

			OnFillBrushChanged(); // fill
			OnStrokeBrushChanged(); // stroke
			UpdateStrokeThickness(); // stroke-width
			UpdateStrokeDashArray(); // stroke-dasharray
		}

		private void OnFillBrushChanged()
		{
			if (!_shouldUpdateNative) return;

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
			if (!_shouldUpdateNative) return;

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
			if (!_shouldUpdateNative) return;

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
			if (!_shouldUpdateNative) return;

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
			if (FeatureConfiguration.Shape.WasmCacheBBoxCalculationResult)
			{
				var key = shape.GetBBoxCacheKey();
				if (!string.IsNullOrEmpty(key))
				{
					if (!_bboxCache.TryGetValue(key, out var rect))
					{
						_bboxCache[key] = rect = shape._mainSvgElement.GetBBox();
					}

					return rect;
				}
			}

			var result = shape._mainSvgElement.GetBBox();
			return result;
		}

		private protected void Render(Shape shape, Size? size = null, double scaleX = 1d, double scaleY = 1d, double renderOriginX = 0d, double renderOriginY = 0d)
		{
			Debug.Assert(shape == this);
			var scale = Matrix3x2.CreateScale((float)scaleX, (float)scaleY);
			var translate = Matrix3x2.CreateTranslation((float)renderOriginX, (float)renderOriginY);
			var matrix = scale * translate;
			_mainSvgElement.SetNativeTransform(matrix);
		}

		internal override bool HitTest(Point relativePosition)
		{
			var considerFill = Fill != null;

			// TODO: Verify if this should also consider StrokeThickness (likely it should)
			var considerStroke = Stroke != null;

			return (considerFill || considerStroke) &&
				WindowManagerInterop.ContainsPoint(_mainSvgElement.HtmlId, relativePosition.X, relativePosition.Y, considerFill, considerStroke);
		}

		// lazy impl, and _cacheKey can be invalidated by setting to null
		private string GetBBoxCacheKey() => _bboxCacheKey ?? (_bboxCacheKey = GetBBoxCacheKeyImpl());

		// note: perf is of concern here. avoid $"string interpolation" and current-culture .ToString, and use string.concat and ToStringInvariant
		private protected abstract string GetBBoxCacheKeyImpl();
	}
}
