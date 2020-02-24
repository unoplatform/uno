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

			OnStretchUpdatedPartial();
		}

		protected void InitCommonShapeProperties() // Should be called from base class constructor
		{
			// Initialize
			OnFillUpdatedPartial();
			OnStrokeUpdatedPartial();
			OnStrokeThicknessUpdatedPartial();
		}

		protected abstract SvgElement GetMainSvgElement();

		partial void OnFillUpdatedPartial()
		{
			UpdateHitTest();

			var svgElement = GetMainSvgElement();

			switch (Fill)
			{
				case SolidColorBrush scb:
					svgElement.SetStyle("fill", scb.Color.ToCssString());
					_fillBrushSubscription.Disposable = null;
					break;
				case ImageBrush ib:
					_fillBrushSubscription.Disposable = null;
					break;
				case LinearGradientBrush lgb:
					var linearGradient = lgb.ToSvgElement();
					var gradientId = linearGradient.HtmlId;
					GetDefs().Add(linearGradient);
					svgElement.SetStyle("fill", $"url(#{gradientId})");
					_fillBrushSubscription.Disposable = new DisposableAction(
						() => GetDefs().Remove(linearGradient)
					);
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
					svgElement.SetStyle("stroke", scb.Color.ToCssString());
					_strokeBrushSubscription.Disposable = null;
					break;
				case LinearGradientBrush lgb:
					var linearGradient = lgb.ToSvgElement();
					var gradientId = linearGradient.HtmlId;
					GetDefs().Add(linearGradient);
					svgElement.SetStyle("stroke", $"url(#{gradientId})");
					_strokeBrushSubscription.Disposable = new DisposableAction(
						() => GetDefs().Remove(linearGradient)
					);
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

			if (Stroke == null)
			{
				svgElement.ResetStyle("stroke-width");
			}
			else
			{
				svgElement.SetStyle("stroke-width", $"{StrokeThickness}px");
			}
		}

		partial void OnStrokeDashArrayUpdatedPartial()
		{
			var svgElement = GetMainSvgElement();

			if (Stroke == null)
			{
				svgElement.ResetStyle("stroke-dasharray");
			}
			else
			{
				var str = string.Join(",", StrokeDashArray.Select(d => $"{d}px"));
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

		private protected override void OnHitTestVisibilityChanged(HitTestVisibility oldValue, HitTestVisibility newValue)
		{
			// We don't invoke the base, so we stay at the default "pointer-events: none" defined in Uno.UI.css in class svg.uno-uielement.
			// This is required to avoid this SVG element (which is actually only a collection) to stoll pointer events.
		}
	}
}
