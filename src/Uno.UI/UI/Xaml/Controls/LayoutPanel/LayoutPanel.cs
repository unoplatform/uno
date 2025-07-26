// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.


using System;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls
{

	partial class LayoutPanel
	{
		private void OnPropertyChanged(DependencyPropertyChangedEventArgs args)
		{
			var dependencyProperty = args.Property;

			if (dependencyProperty == LayoutProperty)
			{
				OnLayoutChanged(args.OldValue as Layout, args.NewValue as Layout);
			}
			else if (dependencyProperty == BorderBrushProperty)
			{
				var newValue = (Brush)args.NewValue;

				BorderBrushInternal = newValue;
				OnBorderBrushChanged((Brush)args.OldValue, newValue);
			}
			else if (dependencyProperty == BorderThicknessProperty)
			{
				var newValue = (Thickness)args.NewValue;

				BorderThicknessInternal = newValue;
				OnBorderThicknessChanged((Thickness)args.OldValue, newValue);
			}
			else if (dependencyProperty == CornerRadiusProperty)
			{
				var newValue = (CornerRadius)args.NewValue;

				CornerRadiusInternal = newValue;
				OnCornerRadiusChanged((CornerRadius)args.OldValue, newValue);
			}
			else if (dependencyProperty == PaddingProperty)
			{
				var newValue = (Thickness)args.NewValue;

				PaddingInternal = newValue;
				OnPaddingChanged((Thickness)args.OldValue, newValue);

				InvalidateMeasure();
			}
		}


		protected override Size MeasureOverride(Size availableSize)
		{
			Size desiredSize;

			// We adjust availableSize based on our Padding and BorderThickness:
			var padding = Padding;
			var borderThickness = BorderThickness;
			var effectiveHorizontalPadding =
				(padding.Left + padding.Right + borderThickness.Left + borderThickness.Right);
			var effectiveVerticalPadding =
				(padding.Top + padding.Bottom + borderThickness.Top + borderThickness.Bottom);

			var adjustedSize = availableSize;
			adjustedSize.Width -= effectiveHorizontalPadding;
			adjustedSize.Height -= effectiveVerticalPadding;

			adjustedSize.Width = Math.Max(0.0d, adjustedSize.Width);
			adjustedSize.Height = Math.Max(0.0d, adjustedSize.Height);

			var layout = Layout;

			if (layout is { })
			{
				var layoutDesiredSize = layout.Measure(m_layoutContext, adjustedSize);
				layoutDesiredSize.Width += effectiveHorizontalPadding;
				layoutDesiredSize.Height += effectiveVerticalPadding;
				desiredSize = layoutDesiredSize;
			}
			else
			{
				Size desiredSizeUnpadded = new Size();
				foreach (UIElement child in Children)
				{
					child.Measure(adjustedSize);
					var childDesiredSize = child.DesiredSize;
					desiredSizeUnpadded.Width = Math.Max(desiredSizeUnpadded.Width, childDesiredSize.Width);
					desiredSizeUnpadded.Height = Math.Max(desiredSizeUnpadded.Height, childDesiredSize.Height);
				}
				desiredSize = desiredSizeUnpadded;
				desiredSize.Width += effectiveHorizontalPadding;
				desiredSize.Height += effectiveVerticalPadding;
			}
			return desiredSize;
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			Size result = finalSize;

			var padding = Padding;
			var borderThickness = BorderThickness;

			var effectiveHorizontalPadding =
				(float)(padding.Left + padding.Right + borderThickness.Left + borderThickness.Right);
			var effectiveVerticalPadding =
				(float)(padding.Top + padding.Bottom + borderThickness.Top + borderThickness.Bottom);
			var leftAdjustment = (float)(padding.Left + borderThickness.Left);
			var topAdjustment = (float)(padding.Top + borderThickness.Top);

			var adjustedSize = finalSize;
			adjustedSize.Width -= effectiveHorizontalPadding;
			adjustedSize.Height -= effectiveVerticalPadding;

			adjustedSize.Width = Math.Max(0.0f, adjustedSize.Width);
			adjustedSize.Height = Math.Max(0.0f, adjustedSize.Height);

			var layout = Layout;

			if (layout is { })
			{
				var layoutSize = layout.Arrange(m_layoutContext, adjustedSize);
				layoutSize.Width += effectiveHorizontalPadding;
				layoutSize.Height += effectiveVerticalPadding;

				// We need to reposition the child elements if we have top or left padding:
				if (leftAdjustment != 0 || topAdjustment != 0)
				{
					foreach (UIElement child in Children)
					{
						if (child is FrameworkElement childAsFe)
						{
							var layoutSlot = LayoutInformation.GetLayoutSlot(childAsFe);
							layoutSlot.X += leftAdjustment;
							layoutSlot.Y += topAdjustment;
							childAsFe.Arrange(layoutSlot);
						}
					}
				}

				result = layoutSize;
			}
			else
			{
				Rect arrangeRect = new Rect(leftAdjustment, topAdjustment, adjustedSize.Width, adjustedSize.Height);
				foreach (UIElement child in Children)
				{
					child.Arrange(arrangeRect);
				}
			}

			return result;
		}

		void OnLayoutChanged(Layout oldValue, Layout newValue)
		{
			m_layoutContext ??= new LayoutPanelLayoutContext(this);

			if (oldValue is { })
			{
				oldValue.UninitializeForContext(m_layoutContext);
				oldValue.MeasureInvalidated -= InvalidateMeasureForLayout;
				oldValue.ArrangeInvalidated -= InvalidateArrangeForLayout;
			}

			if (newValue is { })
			{
				newValue.InitializeForContext(m_layoutContext);
				newValue.MeasureInvalidated += InvalidateMeasureForLayout;
				newValue.ArrangeInvalidated += InvalidateArrangeForLayout;
			}

			InvalidateMeasure();
		}

		void InvalidateMeasureForLayout(Layout sender, object o) => InvalidateMeasure();

		void InvalidateArrangeForLayout(Layout sender, object o) => InvalidateArrange();
	}
}
