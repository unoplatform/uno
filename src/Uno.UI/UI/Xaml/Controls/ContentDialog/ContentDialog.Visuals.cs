#nullable enable

//// Copyright (c) Microsoft Corporation. All rights reserved.
//// Licensed under the MIT License. See LICENSE in the project root for license information.
using System;
using Uno.Extensions;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

namespace Microsoft.UI.Xaml.Controls
{
	partial class ContentDialog
	{
		const double ContentDialog_SIP_Bottom_Margin = 12.0;

		#region .h
		enum PlacementMode
		{
			Undetermined,
			EntireControlInPopup,
			TransplantedRootInPopup,
			InPlace
		};

		PlacementMode m_placementMode = PlacementMode.Undetermined;
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
		bool m_hideInProgress;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value

		bool m_hasPreparedContent;
		bool m_isShowing;
		Storyboard? m_layoutAdjustmentsForInputPaneStoryboard;

		double m_dialogMinHeight;
		#endregion

		private protected override void ChangeVisualState(bool useTransitions)
		{
			base.ChangeVisualState(useTransitions);

			bool fullSizeDesired = FullSizeDesired;

			{
				// ButtonsVisibilityStates
				{
					var primaryText = PrimaryButtonText;

					var secondaryText = SecondaryButtonText;

					var closeText = CloseButtonText;

					bool hasPrimary = !primaryText.IsNullOrEmpty();
					bool hasSecondary = !secondaryText.IsNullOrEmpty();
					bool hasClose = !closeText.IsNullOrEmpty();

					var buttonVisibilityState = "NoneVisible";
					if (hasPrimary && hasSecondary && hasClose)
					{
						buttonVisibilityState = "AllVisible";
					}
					else if (hasPrimary && hasSecondary)
					{
						buttonVisibilityState = "PrimaryAndSecondaryVisible";
					}
					else if (hasPrimary && hasClose)
					{
						buttonVisibilityState = "PrimaryAndCloseVisible";
					}
					else if (hasSecondary && hasClose)
					{
						buttonVisibilityState = "SecondaryAndCloseVisible";
					}
					else if (hasPrimary)
					{
						buttonVisibilityState = "PrimaryVisible";
					}
					else if (hasSecondary)
					{
						buttonVisibilityState = "SecondaryVisible";
					}
					else if (hasClose)
					{
						buttonVisibilityState = "CloseVisible";
					}

					GoToState(useTransitions, buttonVisibilityState);
				}

				// DefaultButtonStates
				{
					var defaultButtonState = "NoDefaultButton";

					var defaultButton = DefaultButton;

					if (defaultButton != ContentDialogButton.None)
					{
						// UNO TODO
						//ctl::ComPtr<DependencyObject> focusedElement;
						//IFC_RETURN(GetFocusedElement(&focusedElement));

						//BOOLEAN isFocusInCommandArea = FALSE;
						//IFC_RETURN(m_tpCommandSpacePart.Cast<Grid>()->IsAncestorOf(focusedElement.Get(), &isFocusInCommandArea));

						// If focus is not in the command area, set the default button visualization just based on the property value.
						// If focus is in the command area, set the default button visualization only if it has focus.
						if (defaultButton == ContentDialogButton.Primary)
						{
							//if (!isFocusInCommandArea || ctl::are_equal(m_tpPrimaryButtonPart.Get(), focusedElement.Get()))
							{
								defaultButtonState = "PrimaryAsDefaultButton";
							}
						}
						else if (defaultButton == ContentDialogButton.Secondary)
						{
							//if (!isFocusInCommandArea || ctl::are_equal(m_tpSecondaryButtonPart.Get(), focusedElement.Get()))
							{
								defaultButtonState = "SecondaryAsDefaultButton";
							}
						}
						else if (defaultButton == ContentDialogButton.Close)
						{
							//if (!isFocusInCommandArea || ctl::are_equal(m_tpCloseButtonPart.Get(), focusedElement.Get()))
							{
								defaultButtonState = "CloseAsDefaultButton";
							}
						}
					}

					GoToState(useTransitions, defaultButtonState);
				}
			}

			{
				// DialogShowingStates
				if (m_placementMode == PlacementMode.InPlace)
				{
					GoToState(true, m_isShowing && !m_hideInProgress ? "DialogShowing" : "DialogHidden");
				}
				else if (m_placementMode != PlacementMode.Undetermined)
				{
					// For ContentDialog's shown in the popup, set the state to always showing since the opened
					// state of the popup effectively controls whether its showing it not.
					GoToState(false, "DialogShowingWithoutSmokeLayer");
				}

				// DialogSizingStates
				{
					GoToState(useTransitions, fullSizeDesired ? "FullDialogSizing" : "DefaultDialogSizing");
				}

				// DialogBorderStates
				{
					GoToState(useTransitions, "NoBorder");
				}
			}

			// On PhoneBlue, the dialog did not move out of the way of the input pane.
			if (true)
			{
				AdjustVisualStateForInputPane();
			}
		}

#if false
		private void ResetAndPrepareContent()
		{
			// Uno TODO: understand when this is applicable (seemingly while dialog is open?)
			//if (HasValidAppliedTemplate() && m_tpCurrentAsyncOperation && m_hasPreparedContent)
			//{
			//	m_hasPreparedContent = false;
			//	PrepareContent();
			//}
		}
#endif

		private void PrepareContent()
		{
			global::System.Diagnostics.Debug.Assert(HasValidAppliedTemplate());

			if (!m_hasPreparedContent)
			{
				// Uno note: removed unused code (PlacementMode::TransplantedRootInPopup isn't used by most recent template)

				UpdateVisualState();
				UpdateTitleSpaceVisibility();

				if (m_placementMode != PlacementMode.InPlace)
				{
					SizeAndPositionContentInPopup();
				}

				// Uno TODO (for now this would have to be applied in-app using ElevatedView from Uno.UI.Toolkit)
				//// Cast a shadow
				//(ApplyElevationEffect(m_tpBackgroundElementPart.AsOrNull<UIElement>()));

				m_hasPreparedContent = true;
			}
		}

		private void SizeAndPositionContentInPopup()
		{
			var m_tpPopup = _popup;

			double xOffset = 0;
			double yOffset = 0;

			if (XamlRoot is null)
			{
				throw new InvalidOperationException("Can't size and position content unless loaded");
			}

			var xamlRootSize = XamlRoot.Size;

			var flowDirection = FlowDirection;

			FrameworkElement spBackgroundAsFE = m_tpBackgroundElementPart;

			// Uno note: we're using latest template version (Redstone3). For now we're not supporting legacy templates.

			if (m_placementMode == PlacementMode.EntireControlInPopup)
			{
				Height = xamlRootSize.Height;
				Width = xamlRootSize.Width;
			}
			else if (m_tpLayoutRootPart != null)
			{
				m_tpLayoutRootPart.Height = xamlRootSize.Height;
				m_tpLayoutRootPart.Width = xamlRootSize.Width;
			}

			// Uno - omitted code related to display regions, which aren't currently supported

			// When the ContentDialog is in the visual tree, the popup offset has added
			// to it the top-left point of where layout measured and arranged it to.
			// Since we want ContentDialog to be an overlay, we need to subtract off that
			// point in order to ensure the ContentDialog is always being displayed in
			// window coordinates instead of local coordinates.
			if (m_placementMode == PlacementMode.TransplantedRootInPopup)
			{
				GeneralTransform transformToRoot = m_tpPopup.TransformToVisual(null);
				var offsetFromRoot = (transformToRoot.TransformPoint(default));

				{
					xOffset =
						flowDirection == FlowDirection.LeftToRight ?
						(xOffset - offsetFromRoot.X) :
						(xOffset - offsetFromRoot.X) * -1;
				}

				yOffset -= offsetFromRoot.Y;
			}

			// Set the ContentDialog left and top position.
			m_tpPopup.HorizontalOffset = xOffset;
			m_tpPopup.VerticalOffset = yOffset;
		}

		private void UpdateTitleSpaceVisibility()
		{
			if (m_tpTitlePart != null)
			{
				var hasTitle = Title != null || TitleTemplate != null;
				m_tpTitlePart.Visibility = hasTitle ? Visibility.Visible : Visibility.Collapsed;
			}
		}

		private void AdjustVisualStateForInputPane()
		{
#if __ANDROID__
			if (_popup.UseNativePopup)
			{
				// Skip managed adjustment since the popup itself will adjust to the soft keyboard.
				return;
			}
#endif
			Rect inputPaneRect = InputPane.GetForCurrentView().OccludedRect;

			if (m_isShowing && inputPaneRect.Height > 0)
			{
				// UNO TODO (but in most cases if we're showing a software input pane, the app won't be windowed)
				//// The rect we get is in screen coordinates, so translate it into client
				//// coordinates by subtracting our window's origin point (itself translated
				//// into screen coords) from it.
				//{
				//	wf.Point point = { 0, 0 };
				//	DXamlCore.GetCurrent().ClientToScreen(&point);

				//	inputPaneRect.X -= point.X;
				//	inputPaneRect.Y -= point.Y;
				//}

				Rect getElementBounds(FrameworkElement element)

				{
					GeneralTransform transform = element.TransformToVisual(null);
					var width = element.ActualWidth;
					var height = element.ActualHeight;

					return transform.TransformBounds(new Rect(0, 0, width, height));
				};

				Rect layoutRootBounds = getElementBounds(m_tpLayoutRootPart);

				Rect dialogBounds = getElementBounds(m_tpBackgroundElementPart);

				// If the input pane overlaps the dialog (including a 12px bottom margin), the dialog will get translated
				// up so that is not occluded, while also preserving a 12px margin between the bottom of the dialog
				// and the top of the input pane (see redlines).
				// We achieve this by aligning the dialog to the bottom of its parent panel, if not full-size, and
				// then setting a bottom padding on the parent panel creating a reserved area that corresponds to the
				// intersection of the parent panel's bounds and the input pane's bounds.
				if (inputPaneRect.Y < (dialogBounds.Y + dialogBounds.Height + ContentDialog_SIP_Bottom_Margin))
				{
					var contentVerticalScrollBarVisibility = ScrollBarVisibility.Auto;
					bool setDialogVisibility = false;
					var dialogVerticalAlignment = VerticalAlignment.Center;

					var layoutRootPadding = new Thickness(0, 0, 0, layoutRootBounds.Height - Math.Max(inputPaneRect.Y - layoutRootBounds.Y, (float)(m_dialogMinHeight)) + ContentDialog_SIP_Bottom_Margin);

					bool fullSizeDesired = FullSizeDesired;
					if (!fullSizeDesired)
					{
						dialogVerticalAlignment = VerticalAlignment.Bottom;
						setDialogVisibility = true;
					}

					// Apply our layout adjustments using a storyboard so that we don't stomp over template or user
					// provided values.  When we stop the storyboard, it will restore the previous values.
					var storyboard = CreateStoryboardForLayoutAdjustmentsForInputPane(layoutRootPadding, contentVerticalScrollBarVisibility, setDialogVisibility, dialogVerticalAlignment);

					storyboard.Begin();
					storyboard.SkipToFill();
					m_layoutAdjustmentsForInputPaneStoryboard = storyboard;
				}
			}
			else if (m_layoutAdjustmentsForInputPaneStoryboard != null)
			{
				m_layoutAdjustmentsForInputPaneStoryboard.Stop();
				m_layoutAdjustmentsForInputPaneStoryboard = null;
			}
		}

		private Storyboard CreateStoryboardForLayoutAdjustmentsForInputPane(
		   Thickness layoutRootPadding,
		   ScrollBarVisibility contentVerticalScrollBarVisiblity,
		   bool setDialogVerticalAlignment,
		   VerticalAlignment dialogVerticalAlignment)
		{
			Storyboard storyboardLocal = new Storyboard();

			var storyboardChildren = storyboardLocal.Children;

			// LayoutRoot Padding
			{
				ObjectAnimationUsingKeyFrames objectAnimation = new ObjectAnimationUsingKeyFrames();

				Storyboard.SetTarget(objectAnimation, m_tpBackgroundElementPart);
				Storyboard.SetTargetProperty(objectAnimation, "Margin");

				var objectKeyFrames = objectAnimation.KeyFrames;

				DiscreteObjectKeyFrame discreteObjectKeyFrame = new DiscreteObjectKeyFrame();

				KeyTime keyTime = KeyTime.FromTimeSpan(TimeSpan.Zero);

				discreteObjectKeyFrame.KeyTime = keyTime;
				discreteObjectKeyFrame.Value = layoutRootPadding;

				objectKeyFrames.Add(discreteObjectKeyFrame as DiscreteObjectKeyFrame);
				storyboardChildren.Add(objectAnimation as ObjectAnimationUsingKeyFrames);
			}

			// ContentScrollViewer VerticalScrollBarVisibility
			{
				ObjectAnimationUsingKeyFrames objectAnimation = new ObjectAnimationUsingKeyFrames();

				Storyboard.SetTarget((objectAnimation), m_tpContentScrollViewerPart);
				Storyboard.SetTargetProperty(objectAnimation, "VerticalScrollBarVisibility");

				var objectKeyFrames = (objectAnimation.KeyFrames);

				DiscreteObjectKeyFrame discreteObjectKeyFrame = new DiscreteObjectKeyFrame();

				KeyTime keyTime = KeyTime.FromTimeSpan(TimeSpan.Zero);

				discreteObjectKeyFrame.KeyTime = keyTime;
				discreteObjectKeyFrame.Value = contentVerticalScrollBarVisiblity;

				objectKeyFrames.Add(discreteObjectKeyFrame as DiscreteObjectKeyFrame);
				storyboardChildren.Add(objectAnimation as ObjectAnimationUsingKeyFrames);
			}

			// BackgroundElement VerticalAlignment
			if (setDialogVerticalAlignment)
			{
				ObjectAnimationUsingKeyFrames objectAnimation = new ObjectAnimationUsingKeyFrames();

				Storyboard.SetTarget(objectAnimation, m_tpBackgroundElementPart);
				Storyboard.SetTargetProperty(objectAnimation, "VerticalAlignment");

				var objectKeyFrames = (objectAnimation.KeyFrames);

				DiscreteObjectKeyFrame discreteObjectKeyFrame = new DiscreteObjectKeyFrame();

				KeyTime keyTime = KeyTime.FromTimeSpan(TimeSpan.Zero);

				discreteObjectKeyFrame.KeyTime = keyTime;
				discreteObjectKeyFrame.Value = dialogVerticalAlignment;

				objectKeyFrames.Add(discreteObjectKeyFrame as DiscreteObjectKeyFrame);
				storyboardChildren.Add(objectAnimation as ObjectAnimationUsingKeyFrames);
			}

			return storyboardLocal;
		}
	}
}
