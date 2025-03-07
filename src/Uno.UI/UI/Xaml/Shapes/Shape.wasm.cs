#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Windows.Foundation;
using Windows.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Wasm;
using Uno;
using Uno.Collections;
using Uno.Disposables;
using Uno.Extensions;
using Uno.UI;
using Uno.UI.Xaml;

using RadialGradientBrush = Microsoft/* UWP don't rename */.UI.Xaml.Media.RadialGradientBrush;
using BrushDef = (Microsoft.UI.Xaml.UIElement Def, System.IDisposable? InnerSubscription);

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

		private record UpdateRenderPropertiesHashes(int? Fill, int? Stroke, int? StrokeWidth, int? StrokeDashArray);
	}

	partial class Shape
	{
		private protected string? _bboxCacheKey;

		private readonly SerialDisposable _fillBrushSubscription = new SerialDisposable();
		private readonly SerialDisposable _strokeBrushSubscription = new SerialDisposable();

		private DefsSvgElement? _defs;
		private protected readonly SvgElement _mainSvgElement;
		private protected bool _shouldUpdateNative = !FeatureConfiguration.Shape.WasmDelayUpdateUntilFirstArrange;

		private UpdateRenderPropertiesHashes _lastRenderHashes = new(null, null, StrokeWidth: 0d.GetHashCode(), null);

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

			// nested subscriptions of SolidColorBrush::{ Color, Opacity } for Fill and Stroke
			// are done in OnFillChanged/OnStrokeChanged which in turns calls OnFillBrushChanged/OnStrokeBrushChanged
			// on brush changes and on nested properties changes.

			var hashes = new UpdateRenderPropertiesHashes
			(
				Fill: GetHashOfInterestFor(GetActualFill()),
				Stroke: GetHashOfInterestFor(Stroke),
				StrokeWidth: ActualStrokeThickness.GetHashCode(),
				StrokeDashArray: GetHashOfInterestFor(StrokeDashArray)
			);

			switch (
				_lastRenderHashes.Fill != hashes.Fill,
				_lastRenderHashes.Stroke != hashes.Stroke,
				_lastRenderHashes.StrokeWidth != hashes.StrokeWidth,
				_lastRenderHashes.StrokeDashArray != hashes.StrokeDashArray
			)
			{
				case (true, false, false, false): UpdateSvgFill(); break;
				case (false, true, false, false): UpdateSvgStroke(); break;
				case (false, false, true, false): UpdateSvgStrokeWidth(); break;
				case (false, false, false, true): UpdateSvgStrokeDashArray(); break;
				case (true, true, false, false): UpdateSvgFillAndStroke(); break;

				case (false, false, false, false): return;
				default: UpdateSvgEverything(); break;
			}

			_lastRenderHashes = hashes;

			// todo@xy: we need to ensure dp-of-interests guarantees an arrange call if changed
		}
		private void OnFillBrushChanged()
		{
			if (!_shouldUpdateNative) return;

			var hash = GetHashOfInterestFor(GetActualFill());
			if (hash != _lastRenderHashes.Fill)
			{
				UpdateSvgFill();
				_lastRenderHashes = _lastRenderHashes with { Fill = hash };
			}
		}
		private void OnStrokeBrushChanged()
		{
			if (!_shouldUpdateNative) return;

			var hash = GetHashOfInterestFor(Stroke);
			if (hash != _lastRenderHashes.Stroke)
			{
				UpdateSvgStroke();
				_lastRenderHashes = _lastRenderHashes with { Stroke = hash };
			}
		}

		private void UpdateSvgFill()
		{
			if (!_shouldUpdateNative) return;

			UpdateHitTestVisibility();

			var (color, def) = GetBrushImpl(GetActualFill());

			_fillBrushSubscription.Disposable = TryAppendBrushDef(def);
			WindowManagerInterop.SetShapeFillStyle(_mainSvgElement.HtmlId, color?.ToCssIntegerAsInt(), def?.Def.HtmlId);
		}
		private void UpdateSvgStroke()
		{
			if (!_shouldUpdateNative) return;

			var (color, def) = GetBrushImpl(Stroke);

			_strokeBrushSubscription.Disposable = TryAppendBrushDef(def);
			WindowManagerInterop.SetShapeStrokeStyle(_mainSvgElement.HtmlId, color?.ToCssIntegerAsInt(), def?.Def.HtmlId);
		}
		private void UpdateSvgStrokeWidth()
		{
			if (!_shouldUpdateNative) return;

			WindowManagerInterop.SetShapeStrokeWidthStyle(_mainSvgElement.HtmlId, ActualStrokeThickness);
		}
		private void UpdateSvgStrokeDashArray()
		{
			if (!_shouldUpdateNative) return;

			WindowManagerInterop.SetShapeStrokeDashArrayStyle(_mainSvgElement.HtmlId, StrokeDashArray?.ToArray() ?? Array.Empty<double>());
		}
		private void UpdateSvgFillAndStroke()
		{
			if (!_shouldUpdateNative) return;

			var fillImpl = GetBrushImpl(GetActualFill());
			var strokeImpl = GetBrushImpl(Stroke);

			_fillBrushSubscription.Disposable = TryAppendBrushDef(fillImpl.Def);
			_strokeBrushSubscription.Disposable = TryAppendBrushDef(strokeImpl.Def);
			WindowManagerInterop.SetShapeStylesFast1(
				_mainSvgElement.HtmlId,
				fillImpl.Color?.ToCssIntegerAsInt(), fillImpl.Def?.Def.HtmlId,
				strokeImpl.Color?.ToCssIntegerAsInt(), strokeImpl.Def?.Def.HtmlId
			);
		}
		private void UpdateSvgEverything()
		{
			if (!_shouldUpdateNative) return;

			var fillImpl = GetBrushImpl(GetActualFill());
			var strokeImpl = GetBrushImpl(Stroke);

			_fillBrushSubscription.Disposable = TryAppendBrushDef(fillImpl.Def);
			_strokeBrushSubscription.Disposable = TryAppendBrushDef(strokeImpl.Def);
			WindowManagerInterop.SetShapeStylesFast2(
				_mainSvgElement.HtmlId,
				fillImpl.Color?.ToCssIntegerAsInt(), fillImpl.Def?.Def.HtmlId,
				strokeImpl.Color?.ToCssIntegerAsInt(), strokeImpl.Def?.Def.HtmlId, ActualStrokeThickness, StrokeDashArray?.ToArray() ?? Array.Empty<double>()
			);
		}


		private void UpdateHitTestVisibility()
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

		private protected void Render(Shape? shape, Size? size = null, double scaleX = 1d, double scaleY = 1d, double renderOriginX = 0d, double renderOriginY = 0d)
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
		private string? GetBBoxCacheKey() => _bboxCacheKey ?? (_bboxCacheKey = GetBBoxCacheKeyImpl());

		// note: perf is of concern here. avoid $"string interpolation" and current-culture .ToString, and use string.concat and ToStringInvariant
		private protected abstract string? GetBBoxCacheKeyImpl();

		private Brush GetActualFill()
		{
			// The default is black if the style is not set in Web's' SVG. So if the Fill property is not set,
			// we explicitly set the style to transparent in order to match the UWP behavior.

			return Fill ?? SolidColorBrushHelper.Transparent;
		}

		private (Color? Color, BrushDef? Def) GetBrushImpl(Brush brush) => brush switch // todo@xy: fix the name...
		{
			SolidColorBrush scb => (scb.ColorWithOpacity, null),
			ImageBrush ib => (null, ib.ToSvgElement(this)),
			AcrylicBrush ab => (ab.FallbackColorWithOpacity, null),
			LinearGradientBrush lgb => (null, (lgb.ToSvgElement(), null)),
			RadialGradientBrush rgb => (null, (rgb.ToSvgElement(), null)),
			// The default is black if the style is not set in Web's' SVG. So if the Fill property is not set,
			// we explicitly set the style to transparent in order to match the UWP behavior.
			null => (null, null),

			_ => default,
		};
		private IDisposable? TryAppendBrushDef(BrushDef? def)
		{
			if (def is not { } d) return null;

			GetDefs().Add(d.Def);
			return new DisposableAction(() =>
			{
				GetDefs().Remove(d.Def);
				d.InnerSubscription?.Dispose();
			});
		}

		private static int? GetHashOfInterestFor(Brush brush)
		{
			int GetLGBHash(LinearGradientBrush lgb)
			{
				var hash = new HashCode();
				hash.Add(lgb.StartPoint);
				hash.Add(lgb.EndPoint);
				if (lgb.GradientStops is { Count: > 0 })
				{
					foreach (var stop in lgb.GradientStops)
					{
						hash.Add(stop);
					}
				}

				return hash.ToHashCode();
			}
			int GetRGBHash(RadialGradientBrush rgb)
			{
				var hash = new HashCode();
				hash.Add(rgb.Center);
				hash.Add(rgb.RadiusX);
				hash.Add(rgb.RadiusX);
				if (rgb.GradientStops is { Count: > 0 })
				{
					foreach (var stop in rgb.GradientStops)
					{
						hash.Add(stop);
					}
				}

				return hash.ToHashCode();
			}

			return brush switch
			{
				SolidColorBrush scb => scb.ColorWithOpacity.GetHashCode(),
				// We don't care about the nested properties of ImageBrush,
				// because their changes will be updated through ImageBrush::ToSvgElement subscriptions.
				// So an object's reference hash is good here.
				ImageBrush ib => ib.GetHashCode(),
				LinearGradientBrush lgb => GetLGBHash(lgb),
				RadialGradientBrush rgb => GetRGBHash(rgb),
				AcrylicBrush ab => ab.FallbackColorWithOpacity.GetHashCode(),

				_ => null,
			};
		}
		private static int? GetHashOfInterestFor(DoubleCollection doubles)
		{
			if (doubles is not { Count: > 0 })
			{
				return null;
			}

			var hash = new HashCode();
			foreach (var item in doubles)
			{
				hash.Add(item);
			}

			return hash.ToHashCode();
		}
	}
}
