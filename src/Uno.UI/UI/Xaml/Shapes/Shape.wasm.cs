using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Threading;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Wasm;
using Uno.Extensions;
using Uno.Foundation;
using Uno.Disposables;
using Uno;

namespace Windows.UI.Xaml.Shapes
{
	[Markup.ContentProperty(Name = "SvgChildren")]
	partial class Shape
	{
		private readonly SerialDisposable _fillBrushSubscription = new SerialDisposable();
		private readonly SerialDisposable _strokeBrushSubscription = new SerialDisposable();

		private DefsSvgElement _defs;

		public UIElementCollection SvgChildren { get; }

		protected Shape() : base("svg", isSvg: true)
		{
			SvgChildren = new UIElementCollection(this);
			SvgChildren.CollectionChanged += OnSvgChildrenChanged;

			OnStretchUpdatedPartial();
		}

		protected void InitCommonShapeProperties() // Should be called from base class constructor
		{
			// Initialize
			OnFillUpdatedPartial(); // Required to properly update the HitTest
			// Don't OnStrokeUpdatedPartial(); => Stroke is still at its default value in the ctor, and it will always results to ResetStyle("stroke")
			// Don't OnStrokeThicknessUpdatedPartial(); => The default value is set in Uno.UI.css
		}

		protected abstract SvgElement GetMainSvgElement();

		private static readonly NotifyCollectionChangedEventHandler OnSvgChildrenChanged = (object sender, NotifyCollectionChangedEventArgs args) =>
		{
			if (sender is UIElementCollection children && children.Owner is Shape shape)
			{
				shape.OnChildrenChanged();
			}
		};

		protected virtual void OnChildrenChanged()
		{
		}

		private protected override void OnHitTestVisibilityChanged(HitTestability oldValue, HitTestability newValue)
		{
			// We don't invoke the base, so we stay at the default "pointer-events: none" defined in Uno.UI.css in class svg.uno-uielement.
			// This is required to avoid this SVG element (which is actually only a collection) to stoll pointer events.
		}

		partial void OnFillUpdatedPartial()
		{
			// We don't request an update of the HitTest (UpdateHitTest()) since this element is never expected to be hit testable.
			// Note: We also enforce that the default hit test == false is not altered in the OnHitTestVisibilityChanged.

			// Instead we explicitly set the IsHitTestVisible on each child SvgElement
			var fill = Fill;
			foreach (var element in SvgChildren)
			{
				// Known issue: The hit test is only linked to the Fill, but should also take in consideration the Stroke and the StrokeThickness.
				// Note: SvgChildren are internal elements, so it's legit to alter the IsHitTestVisible here.
				element.IsHitTestVisible = fill != null;
			}

			var svgElement = GetMainSvgElement();
			switch (fill)
			{
				case SolidColorBrush scb:
					svgElement.SetStyle("fill", scb.ColorWithOpacity.ToHexString());
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

		partial void OnStrokeUpdatedPartial()
		{
			var svgElement = GetMainSvgElement();

			switch (Stroke)
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
				case AcrylicBrush ab:
					svgElement.SetStyle("stroke", ab.FallbackColorWithOpacity.ToHexString());
					_strokeBrushSubscription.Disposable = null;
					break;
				default:
					svgElement.ResetStyle("stroke");
					_strokeBrushSubscription.Disposable = null;
					break;
			}

			OnStrokeThicknessUpdatedPartial();
		}

		partial void OnStrokeThicknessUpdatedPartial()
		{
			var svgElement = GetMainSvgElement();
			var strokeThickness = ActualStrokeThickness;

			if (Stroke == null)
			{
				svgElement.SetStyle("stroke-width", $"{DefaultStrokeThicknessWhenNoStrokeDefined}px");
			}
			else if (strokeThickness != 1.0d)
			{
				svgElement.SetStyle("stroke-width", $"{strokeThickness}px");
			}
			else
			{
				svgElement.ResetStyle("stroke-width");
			}
		}

		partial void OnStrokeDashArrayUpdatedPartial()
		{
			var svgElement = GetMainSvgElement();

			if (StrokeDashArray == null)
			{
				svgElement.ResetStyle("stroke-dasharray");
			}
			else
			{
				var str = string.Join(",", StrokeDashArray.Select(d => $"{d.ToStringInvariant()}px"));
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
				SvgChildren.Add(_defs);
			}

			return _defs.Defs;
		}
	}
}
