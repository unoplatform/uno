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

		protected Shape() : base("svg", isSvg: true)
		{
			_svgChildren = new UIElementCollection(this);

			OnStretchUpdatedPartial();
		}

		private void OnSvgChildrenChanged(object sender, NotifyCollectionChangedEventArgs e)
			=> OnChildrenChanged();

		protected override void OnLoaded()
		{
			base.OnLoaded();
			_svgChildren.CollectionChanged += OnSvgChildrenChanged;
		}

		protected override void OnUnloaded()
		{
			base.OnUnloaded();
			_svgChildren.CollectionChanged -= OnSvgChildrenChanged;
		}

		protected void InitCommonShapeProperties() // Should be called from base class constructor
		{
			// Initialize
			OnFillUpdatedPartial();
			OnStrokeUpdatedPartial();
			OnStrokeThicknessUpdatedPartial();
		}

		private readonly UIElementCollection _svgChildren;

		public UIElementCollection SvgChildren => _svgChildren;

		protected abstract SvgElement GetMainSvgElement();

		protected virtual void OnChildrenChanged()
		{
		}

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
					SvgChildren.Add(linearGradient);
					svgElement.SetStyle("fill", $"url(#{gradientId})");
					_fillBrushSubscription.Disposable = new DisposableAction(
						() => SvgChildren.Remove(linearGradient)
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
					break;
				default:
					svgElement.ResetStyle("stroke");
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
	}
}
