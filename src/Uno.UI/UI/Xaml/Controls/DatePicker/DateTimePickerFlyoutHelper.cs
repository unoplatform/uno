// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls
{
	internal static class DateTimePickerFlyoutHelper
	{
		internal static Point CalculatePlacementPosition(
			FrameworkElement pTargetElement,
			Control pFlyoutPresenter)
		{
			//wrl.ComPtr<xaml.FrameworkElement>
			//spTargetAsFE(pTargetElement);
			//wrl.ComPtr<xaml_controls.IControl>
			//spFlyoutPresenterAsControl(pFlyoutPresenter);
			//wrl.ComPtr<UIElement> spTargetAsUIE;
			//UIElement spFlyoutPresenterAsUIE;
			//Control spFlyoutPresenterAsControlProtected;
			//wrl.ComPtr<xaml.IDependencyObject> spHighlightRectangleAsDo;
			//wrl.ComPtr<UIElement> spHighlightRectangleAsUIE;
			//wrl.ComPtr<xaml.FrameworkElement> spHighlightRectangleAsFE;
			GeneralTransform spTransformFromPresenterToHighlight;
			GeneralTransform spTransformFromTargetToWindow;
			double width = 0.0;
			double height = 0.0;
			Point targetPoint = default;
			FlowDirection flowDirection = FlowDirection.LeftToRight;

			if (pTargetElement == null) throw new ArgumentNullException(nameof(pTargetElement));
			if (pFlyoutPresenter == null) throw new ArgumentNullException(nameof(pFlyoutPresenter));

			//spTargetAsFE.As(spTargetAsUIE);
			//spFlyoutPresenterAsControl.As(spFlyoutPresenterAsUIE);
			//spFlyoutPresenterAsControl.As(spFlyoutPresenterAsControlProtected);
			//Calculate the correct targetPoint to show the DatePickerFlyout.
			//We want to choose a point such that the HighlightRect template part is centered over the target element.

			var spHighlightRectangleAsDo = pFlyoutPresenter.GetTemplateChild("HighlightRect");
			//if (spHighlightRectangleAsDo != null)
			//{
			//	IGNOREHR(spHighlightRectangleAsDo.As(spHighlightRectangleAsUIE));
			//	IGNOREHR(spHighlightRectangleAsDo.As(spHighlightRectangleAsFE));
			//}

			if (spHighlightRectangleAsDo is FrameworkElement spHighlightRectangleAsFE)
			{
				spTransformFromPresenterToHighlight = pFlyoutPresenter.TransformToVisual(spHighlightRectangleAsFE);
				targetPoint = spTransformFromPresenterToHighlight.TransformPoint(targetPoint);
				width = spHighlightRectangleAsFE.ActualWidth;
				height = spHighlightRectangleAsFE.ActualHeight;
				targetPoint.X -= (float)(width / 2);
				targetPoint.Y -= (float)(height / 2);
			}

			spTransformFromTargetToWindow = pTargetElement.TransformToVisual(null);
			targetPoint = spTransformFromTargetToWindow.TransformPoint(targetPoint);
			width = pTargetElement.ActualWidth;
			height = pTargetElement.ActualHeight;
			flowDirection = pTargetElement.FlowDirection;
			targetPoint.X = (flowDirection == FlowDirection.LeftToRight)
				? targetPoint.X + (float)(width / 2)
				: targetPoint.X - (float)(width / 2);
			targetPoint.Y += (float)(height / 2);

			return targetPoint;
		}

		internal static bool ShouldInvertKeyDirection(FrameworkElement contentPanel)
		{
			FlowDirection flowDirection = FlowDirection.LeftToRight;
			if (contentPanel != null)
			{
				flowDirection = contentPanel.FlowDirection;
			}

			var invert = flowDirection == FlowDirection.RightToLeft;
			return invert;
		}

		internal static bool ShouldFirstToThirdDirection(VirtualKey key, bool invert)
		{
			return (key == VirtualKey.Left && !invert) || (key == VirtualKey.Right && invert);
		}

		internal static bool ShouldThirdToFirstDirection(VirtualKey key, bool invert)
		{
			return (key == VirtualKey.Left && invert) || (key == VirtualKey.Right && !invert);
		}

		internal static void OnKeyDownImpl(
			KeyRoutedEventArgs pEventArgs,
			Control tpFirstPickerAsControl,
			Control tpSecondPickerAsControl,
			Control tpThirdPickerAsControl,
			FrameworkElement tpContentPanel)
		{
			bool handled = false;
			VirtualKey key = VirtualKey.None;

			handled = pEventArgs.Handled;
			if (handled)
			{
				return;
			}

			key = pEventArgs.Key;

			if (key == VirtualKey.Left || key == VirtualKey.Right)
			{
				FocusState firstPickerFocusState = FocusState.Unfocused;
				FocusState secondPickerFocusState = FocusState.Unfocused;
				FocusState thirdPickerFocusState = FocusState.Unfocused;
				bool focusChanged = false;

				if (tpFirstPickerAsControl != null)
				{
					//UIElement tpFirstPickerAsElement;
					//tpFirstPickerAsControl.QueryInterface(IID_PPV_ARGS(tpFirstPickerAsElement));
					firstPickerFocusState = tpFirstPickerAsControl.FocusState;
				}

				if (tpSecondPickerAsControl != null)
				{
					//UIElement tpSecondPickerAsElement;
					//tpSecondPickerAsControl.QueryInterface(IID_PPV_ARGS(tpSecondPickerAsElement));
					secondPickerFocusState = tpSecondPickerAsControl.FocusState;
				}

				if (tpThirdPickerAsControl != null)
				{
					//UIElement tpThirdPickerAsElement;
					//tpThirdPickerAsControl.QueryInterface(IID_PPV_ARGS(tpThirdPickerAsElement));
					thirdPickerFocusState = tpThirdPickerAsControl.FocusState;
				}

				// in RTL, Grid 0 is on the right side. so visual effect from left to right is thirdPicker-secondPicker-firstPicker. 
				// So we need to invert the key direction before it's handled: if left key is pressed, it's FirstToThirdDirection; if right key is pressed, it's ThirdToFirstDirection

				bool invert = false;
				invert = ShouldInvertKeyDirection(tpContentPanel);

				bool shouldFirstToThirdDirection = ShouldFirstToThirdDirection(key, invert);
				bool shouldThirdToFirstDirection = ShouldThirdToFirstDirection(key, invert);

				if (shouldFirstToThirdDirection)
				{
					if (secondPickerFocusState != FocusState.Unfocused)
					{
						if (tpFirstPickerAsControl != null)
						{
							focusChanged = tpFirstPickerAsControl.Focus(FocusState.Keyboard);
						}
					}
					else if (thirdPickerFocusState != FocusState.Unfocused)
					{
						if (tpSecondPickerAsControl != null)
						{
							focusChanged = tpSecondPickerAsControl.Focus(FocusState.Keyboard);
						}
						else if (tpFirstPickerAsControl != null)
						{
							focusChanged = tpFirstPickerAsControl.Focus(FocusState.Keyboard);
						}
					}
				}
				else if (shouldThirdToFirstDirection)
				{
					if (firstPickerFocusState != FocusState.Unfocused)
					{
						if (tpSecondPickerAsControl != null)
						{
							focusChanged = tpSecondPickerAsControl.Focus(FocusState.Keyboard);
						}
						else if (tpThirdPickerAsControl != null)
						{
							focusChanged = tpThirdPickerAsControl.Focus(FocusState.Keyboard);
						}
					}
					else if (secondPickerFocusState != FocusState.Unfocused)
					{
						if (tpThirdPickerAsControl != null)
						{
							focusChanged = tpThirdPickerAsControl.Focus(FocusState.Keyboard);
						}
					}
				}

				pEventArgs.Handled = focusChanged;
			}

			return;
		}
	}
}
