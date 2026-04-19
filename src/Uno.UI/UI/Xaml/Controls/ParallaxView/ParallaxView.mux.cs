// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ParallaxView.cpp, commit 5f9e85113

using System;
using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

partial class ParallaxView
{
	public ParallaxView()
	{
		// __RP_Marker_ClassById(RuntimeProfiler::ProfId_ParallaxView);

		ScrollInputHelper scrollInputHelper =
			new ScrollInputHelper(
				(bool horizontalInfoChanged, bool verticalInfoChanged) =>
				{
					OnScrollInputHelperInfoChanged(horizontalInfoChanged, verticalInfoChanged);
				});
		m_scrollInputHelper = scrollInputHelper;

		HookLoaded();
		HookSizeChanged();

#if HAS_UNO
		// Uno specific: Without C++ destructors we dispose of the ScrollInputHelper and unhook child
		// property-changed callbacks when the control is unloaded to avoid leaking event subscriptions
		// onto long-lived Sources or Children.
		Unloaded += ParallaxView_Unloaded;
#endif
	}

#if HAS_UNO
	private void ParallaxView_Unloaded(object sender, RoutedEventArgs e)
	{
		// TODO Uno: Original C++ destructor cleanup. Uno does not support cleanup via finalizers.
		// Move this logic into Loaded/Unloaded event handlers or other lifecycle methods to avoid leaks.

		// Original destructor logic (not executed):
		// UnhookChildPropertyChanged(true /* isInDestructor */);

		UnhookChildPropertyChanged(false /* isInDestructor */);
		m_scrollInputHelper?.Dispose();
	}
#endif

	/// <summary>
	/// Forces the automatically computed horizontal offsets to be recomputed.
	/// </summary>
	public void RefreshAutomaticHorizontalOffsets()
	{
		// This method is meant to be invoked when the ParallaxView is parallaxing horizontally, is
		// placed within a scrollPresenter content and its horizontal offset within that content has changed.
		if (HorizontalSourceOffsetKind == ParallaxSourceOffsetKind.Relative && HorizontalShift != 0.0)
		{
			UpdateStartOffsetExpression(Orientation.Horizontal);
			UpdateEndOffsetExpression(Orientation.Horizontal);
		}
	}

	/// <summary>
	/// Forces the automatically computed vertical offsets to be recomputed.
	/// </summary>
	public void RefreshAutomaticVerticalOffsets()
	{
		// This method is meant to be invoked when the ParallaxView is parallaxing vertically, is
		// placed within a scrollPresenter content and its vertical offset within that content has changed.
		if (VerticalSourceOffsetKind == ParallaxSourceOffsetKind.Relative && VerticalShift != 0.0)
		{
			UpdateStartOffsetExpression(Orientation.Vertical);
			UpdateEndOffsetExpression(Orientation.Vertical);
		}
	}
	// #pragma endregion

	// #pragma region IFrameworkElementOverridesHelper
	protected override Size MeasureOverride(Size availableSize)
	{
		Size childDesiredSize = new Size(0.0f, 0.0f);
		UIElement child = Child;

		if (child is not null)
		{
			// Include the HorizontalShift/VerticalShift amounts in the available size so the desired child size
			// accounts for the parallaxing effect.
			Size childAvailableSize = new Size(
				availableSize.Width + Math.Abs(HorizontalShift),
				availableSize.Height + Math.Abs(VerticalShift));
#if !HAS_UNO
			child.Measure(childAvailableSize);
			childDesiredSize = child.DesiredSize;
#else
			// Uno specific: Use MeasureElement instead of child.Measure() because ParallaxView is a
			// FrameworkElement (not a Panel) and hosts its child via UIElement.AddChild; the direct
			// child.Measure() call does not route through Uno's layouter bookkeeping on non-reference
			// targets. This mirrors the pattern used by Border.MeasureOverride.
			childDesiredSize = MeasureElement(child, childAvailableSize);
#endif
		}

		return new Size(
			double.IsInfinity(availableSize.Width) ? childDesiredSize.Width : availableSize.Width,
			double.IsInfinity(availableSize.Height) ? childDesiredSize.Height : availableSize.Height);
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		UIElement child = Child;

		if (child is not null)
		{
			FrameworkElement childAsFE = child as FrameworkElement;
			Rect finalRect = new Rect(0.0f, 0.0f, child.DesiredSize.Width, child.DesiredSize.Height);

			if (HorizontalShift != 0.0 && finalRect.Width < finalSize.Width + Math.Abs(HorizontalShift))
			{
				// The child is parallaxing horizontally. Ensure that its arrange width exceeds the ParallaxView's arrange width by at least HorizontalShift.
				// Expand its height by the same ratio if it's stretched vertically.
				float stretchRatio = finalRect.Width > 0.0f ? (float)((finalSize.Width + Math.Abs(HorizontalShift)) / finalRect.Width) : 0.0f;
				finalRect.Width = finalSize.Width + Math.Abs(HorizontalShift);
				if (stretchRatio != 0.0f && childAsFE is not null && double.IsNaN(childAsFE.Height) && childAsFE.VerticalAlignment == VerticalAlignment.Stretch)
				{
					finalRect.Height *= stretchRatio;
				}
			}
			if (VerticalShift != 0.0 && finalRect.Height < finalSize.Height + Math.Abs(VerticalShift))
			{
				// The child is parallaxing vertically. Ensure that its arrange height exceeds the ParallaxView's arrange height by at least VerticalShift.
				// Expand its width by the same ratio if it's stretched horizontally.
				float stretchRatio = finalRect.Height > 0.0f ? (float)((finalSize.Height + Math.Abs(VerticalShift)) / finalRect.Height) : 0.0f;
				finalRect.Height = finalSize.Height + Math.Abs(VerticalShift);
				if (stretchRatio != 0.0f && childAsFE is not null && double.IsNaN(childAsFE.Width) && childAsFE.HorizontalAlignment == HorizontalAlignment.Stretch)
				{
					finalRect.Width *= stretchRatio;
				}
			}

			if (childAsFE is not null)
			{
				// Ensure the child is properly aligned within the ParallaxView based on its
				// alignment properties. This alignment behavior is identical to the Border's behavior.
				double offset = 0.0;

				switch (childAsFE.HorizontalAlignment)
				{
					case HorizontalAlignment.Center:
						offset = (finalSize.Width - finalRect.Width) / 2.0;
						break;
					case HorizontalAlignment.Right:
						offset = finalSize.Width - finalRect.Width;
						break;
					case HorizontalAlignment.Stretch:
						if (finalRect.Width < finalSize.Width)
						{
							offset = (finalSize.Width - finalRect.Width) / 2.0;
						}
						break;
				}
				finalRect.X = offset;

				offset = 0.0;

				switch (childAsFE.VerticalAlignment)
				{
					case VerticalAlignment.Center:
						offset = (finalSize.Height - finalRect.Height) / 2.0;
						break;
					case VerticalAlignment.Bottom:
						offset = finalSize.Height - finalRect.Height;
						break;
					case VerticalAlignment.Stretch:
						if (finalRect.Height < finalSize.Height)
						{
							offset = (finalSize.Height - finalRect.Height) / 2.0;
						}
						break;
				}
				finalRect.Y = offset;
			}

#if !HAS_UNO
			child.Arrange(finalRect);
#else
			// Uno specific: Use ArrangeElement instead of child.Arrange() because ParallaxView is a
			// FrameworkElement (not a Panel) and hosts its child via UIElement.AddChild; the direct
			// child.Arrange() call does not route through Uno's layouter bookkeeping on non-reference
			// targets. This mirrors the pattern used by Border.ArrangeOverride.
			ArrangeElement(child, finalRect);
#endif

			// Set a rectangular clip on this ParallaxView the same size as the arrange
			// rectangle so the child does not render beyond it.
			RectangleGeometry rectangleGeometry = Clip as RectangleGeometry;

			if (rectangleGeometry is null)
			{
				// Ensure that this ParallaxView has a rectangular clip.
				RectangleGeometry newRectangleGeometry = new RectangleGeometry();
				Clip = newRectangleGeometry;

				rectangleGeometry = newRectangleGeometry;
			}

			Rect currentClipRect = rectangleGeometry.Rect;

			if (currentClipRect.X != 0.0 || currentClipRect.Width != finalSize.Width ||
				currentClipRect.Y != 0.0 || currentClipRect.Height != finalSize.Height)
			{
				Rect newClipRect = new Rect(0.0, 0.0, finalSize.Width, finalSize.Height);
				rectangleGeometry.Rect = newClipRect;
			}
		}

		return finalSize;
	}
	// #pragma endregion

	// Returns the target property path, according to the availability of the ElementCompositionPreview::SetIsTranslationEnabled method.
	private static string GetVisualTargetedPropertyName(Orientation orientation)
	{
		return orientation == Orientation.Horizontal ? s_translationXPropertyName : s_translationYPropertyName;
	}


	// Invoked when a dependency property of this ParallaxView has changed.
	private void OnPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		DependencyProperty dependencyProperty = args.Property;

		if (dependencyProperty == ChildProperty)
		{
			object oldChild = args.OldValue;
			object newChild = args.NewValue;
			UpdateChild(oldChild as UIElement, newChild as UIElement);
		}
		else if (dependencyProperty == IsHorizontalShiftClampedProperty ||
				 dependencyProperty == HorizontalSourceOffsetKindProperty ||
				 dependencyProperty == HorizontalSourceStartOffsetProperty ||
				 dependencyProperty == HorizontalSourceEndOffsetProperty ||
				 dependencyProperty == MaxHorizontalShiftRatioProperty)
		{
			UpdateExpressionAnimation(Orientation.Horizontal);
		}
		else if (dependencyProperty == HorizontalShiftProperty)
		{
			InvalidateMeasure();
			UpdateExpressionAnimation(Orientation.Horizontal);
		}
		else if (dependencyProperty == SourceProperty)
		{
			if (m_scrollInputHelper is not null)
			{
				object newSource = args.NewValue;
				m_scrollInputHelper.SetSourceElement(newSource as UIElement);
			}
		}
		else if (dependencyProperty == IsVerticalShiftClampedProperty ||
				 dependencyProperty == VerticalSourceOffsetKindProperty ||
				 dependencyProperty == VerticalSourceStartOffsetProperty ||
				 dependencyProperty == VerticalSourceEndOffsetProperty ||
				 dependencyProperty == MaxVerticalShiftRatioProperty)
		{
			UpdateExpressionAnimation(Orientation.Vertical);
		}
		else if (dependencyProperty == VerticalShiftProperty)
		{
			InvalidateMeasure();
			UpdateExpressionAnimation(Orientation.Vertical);
		}
	}

	// Invoked by ScrollInputHelper when a characteristic changes requires a re-evaluation of the parallaxing expression animations.
	internal void OnScrollInputHelperInfoChanged(bool horizontalInfoChanged, bool verticalInfoChanged)
	{
		if (horizontalInfoChanged)
		{
			UpdateExpressionAnimation(Orientation.Horizontal);
		}
		if (verticalInfoChanged)
		{
			UpdateExpressionAnimation(Orientation.Vertical);
		}
	}

	private void OnLoaded(object sender, RoutedEventArgs args)
	{
		// Characteristics influencing the source start and end offsets are ready now.
		if (HorizontalShift != 0.0)
		{
			UpdateStartOffsetExpression(Orientation.Horizontal);
			UpdateEndOffsetExpression(Orientation.Horizontal);
		}

		if (VerticalShift != 0.0)
		{
			UpdateStartOffsetExpression(Orientation.Vertical);
			UpdateEndOffsetExpression(Orientation.Vertical);
		}
	}

	private void OnSizeChanged(object sender, SizeChangedEventArgs args)
	{
		// ParallaxView sizes influence the source end offset.
		if (HorizontalShift != 0.0)
		{
			UpdateEndOffsetExpression(Orientation.Horizontal);
		}

		if (VerticalShift != 0.0)
		{
			UpdateEndOffsetExpression(Orientation.Vertical);
		}
	}

	// Invoked when a tracked dependency property changes for the Child dependency object.
	private void OnChildPropertyChanged(DependencyObject sender, DependencyProperty args)
	{
		FrameworkElement senderAsFrameworkElement = sender as FrameworkElement;

		if (senderAsFrameworkElement is not null &&
			(args == FrameworkElement.HorizontalAlignmentProperty ||
			 args == FrameworkElement.VerticalAlignmentProperty))
		{
			senderAsFrameworkElement.InvalidateArrange();
		}
	}

	private void UpdateChild(UIElement oldChild, UIElement newChild)
	{
		FrameworkElement childAsFrameworkElement = null;

		UnhookChildPropertyChanged(false /* isInDestructor */);

#if !HAS_UNO
		var children = (this as Panel).Children;
		children.Clear();

		if (newChild is not null)
		{
			children.Append(newChild);

			// Detect when a child alignment changes, so it can be re-arranged.
			childAsFrameworkElement = newChild as FrameworkElement;
			HookChildPropertyChanged(childAsFrameworkElement);
		}
#else
		// TODO Uno specific: The WinUI runtime class is publicly a FrameworkElement while its implementation
		// inherits from Panel via DeriveFromPanelHelper_base, so that `try_as<Panel>().Children()` succeeds.
		// Uno's ParallaxView stays a FrameworkElement to match the IDL exactly, and instead manages the single
		// child via UIElement.AddChild/RemoveChild (matching the Border pattern).
		if (oldChild is not null)
		{
			RemoveChild(oldChild);
		}

		if (newChild is not null)
		{
			AddChild(newChild);

			// Detect when a child alignment changes, so it can be re-arranged.
			childAsFrameworkElement = newChild as FrameworkElement;
			HookChildPropertyChanged(childAsFrameworkElement);
		}
#endif

		if (m_scrollInputHelper is not null && m_scrollInputHelper.TargetElement() != newChild)
		{
			m_scrollInputHelper.SetTargetElement(newChild);
			if (HorizontalShift != 0.0)
			{
				UpdateExpressionAnimation(Orientation.Horizontal);
			}
			if (VerticalShift != 0.0)
			{
				UpdateExpressionAnimation(Orientation.Vertical);
			}
		}
	}

	// Sets up the internal composition property set that tracks the animated source start & end offsets.
	private void EnsureAnimatedVariables()
	{
		if (m_animatedVariables is null && m_targetVisual is not null)
		{
			m_animatedVariables = m_targetVisual.Compositor.CreatePropertySet();
			m_animatedVariables.InsertScalar("HorizontalSourceStartOffset", 0.0f);
			m_animatedVariables.InsertScalar("HorizontalSourceEndOffset", 0.0f);
			m_animatedVariables.InsertScalar("VerticalSourceStartOffset", 0.0f);
			m_animatedVariables.InsertScalar("VerticalSourceEndOffset", 0.0f);
		}
	}

	// Updates the composition animation for the source start offset.
	private void UpdateStartOffsetExpression(Orientation orientation)
	{
		if (m_scrollInputHelper is not null && m_scrollInputHelper.SourcePropertySet() is not null && m_animatedVariables is not null &&
			((orientation == Orientation.Horizontal && HorizontalShift != 0.0) ||
			(orientation == Orientation.Vertical && VerticalShift != 0.0)))
		{
			ExpressionAnimation startOffsetExpressionAnimation = null;

			if (orientation == Orientation.Horizontal)
			{
				if (m_horizontalSourceStartOffsetExpression is null)
				{
					m_horizontalSourceStartOffsetExpression = m_targetVisual.Compositor.CreateExpressionAnimation();
				}
				startOffsetExpressionAnimation = m_horizontalSourceStartOffsetExpression;
			}
			else
			{
				if (m_verticalSourceStartOffsetExpression is null)
				{
					m_verticalSourceStartOffsetExpression = m_targetVisual.Compositor.CreateExpressionAnimation();
				}
				startOffsetExpressionAnimation = m_verticalSourceStartOffsetExpression;
			}

			string startOffsetExpression;
			float startOffset = (float)(orientation == Orientation.Horizontal ? HorizontalSourceStartOffset : VerticalSourceStartOffset);

			startOffsetExpressionAnimation.SetScalarParameter("startOffset", startOffset);

			if ((orientation == Orientation.Horizontal && HorizontalSourceOffsetKind == ParallaxSourceOffsetKind.Relative) ||
				(orientation == Orientation.Vertical && VerticalSourceOffsetKind == ParallaxSourceOffsetKind.Relative))
			{
				// Horizontal/VerticalSourceStartOffset is added to automatic value

				float maxUnderpanOffset = (float)m_scrollInputHelper.GetMaxUnderpanOffset(orientation);

				startOffsetExpressionAnimation.SetScalarParameter("maxUnderpanOffset", maxUnderpanOffset);

				if (m_scrollInputHelper.IsTargetElementInSource())
				{
					// Target is inside the scrollPresenter.

					// startOffset = (ParallaxViewOffset + HorizontalSourceStartOffset) * ZoomFactor - ViewportWidth - MaxUnderpanOffset
					float parallaxViewOffset = (float)m_scrollInputHelper.GetOffsetFromScrollContentElement(this, orientation);
					float viewportSize = (float)m_scrollInputHelper.GetViewportSize(orientation);

					startOffsetExpression = "(parallaxViewOffset + startOffset) * source." + m_scrollInputHelper.GetSourceScalePropertyName() + " - viewportSize - maxUnderpanOffset";
					startOffsetExpressionAnimation.SetScalarParameter("parallaxViewOffset", parallaxViewOffset);
					startOffsetExpressionAnimation.SetScalarParameter("viewportSize", viewportSize);
					startOffsetExpressionAnimation.SetReferenceParameter("source", m_scrollInputHelper.SourcePropertySet());
				}
				else
				{
					// Target is outside the scrollPresenter.

					// startOffset = HorizontalSourceStartOffset * ZoomFactor - MaxUnderpanOffset
					startOffsetExpression = "startOffset * source." + m_scrollInputHelper.GetSourceScalePropertyName() + " - maxUnderpanOffset";
					startOffsetExpressionAnimation.SetReferenceParameter("source", m_scrollInputHelper.SourcePropertySet());
				}
			}
			else
			{
				// Horizontal/VerticalSourceStartOffset is an absolute value

				// If HorizontalSourceStartOffset <= 0 Then
				//   startOffset = HorizontalSourceStartOffset
				// Else
				//   startOffset = HorizontalSourceStartOffset * ZoomFactor
				if (startOffset > 0.0f)
				{
					startOffsetExpression = "startOffset * source." + m_scrollInputHelper.GetSourceScalePropertyName();
					startOffsetExpressionAnimation.SetReferenceParameter("source", m_scrollInputHelper.SourcePropertySet());
				}
				else
				{
					startOffsetExpression = "startOffset";
				}
			}

			if (startOffsetExpressionAnimation.Expression != startOffsetExpression)
			{
				startOffsetExpressionAnimation.Expression = startOffsetExpression;
			}

			m_animatedVariables.StopAnimation((orientation == Orientation.Horizontal) ? "HorizontalSourceStartOffset" : "VerticalSourceStartOffset");
			m_animatedVariables.StartAnimation((orientation == Orientation.Horizontal) ? "HorizontalSourceStartOffset" : "VerticalSourceStartOffset", startOffsetExpressionAnimation);
		}
	}

	// Updates the composition animation for the source end offset.
	private void UpdateEndOffsetExpression(Orientation orientation)
	{
		if (m_scrollInputHelper is not null && m_scrollInputHelper.SourcePropertySet() is not null && m_animatedVariables is not null &&
			((orientation == Orientation.Horizontal && HorizontalShift != 0.0) ||
			(orientation == Orientation.Vertical && VerticalShift != 0.0)))
		{
			ExpressionAnimation endOffsetExpressionAnimation = null;

			if (orientation == Orientation.Horizontal)
			{
				if (m_horizontalSourceEndOffsetExpression is null)
				{
					m_horizontalSourceEndOffsetExpression = m_targetVisual.Compositor.CreateExpressionAnimation();
				}
				endOffsetExpressionAnimation = m_horizontalSourceEndOffsetExpression;
			}
			else
			{
				if (m_verticalSourceEndOffsetExpression is null)
				{
					m_verticalSourceEndOffsetExpression = m_targetVisual.Compositor.CreateExpressionAnimation();
				}
				endOffsetExpressionAnimation = m_verticalSourceEndOffsetExpression;
			}

			string endOffsetExpression;
			float endOffset = (float)(orientation == Orientation.Horizontal ? HorizontalSourceEndOffset : VerticalSourceEndOffset);

			endOffsetExpressionAnimation.SetScalarParameter("endOffset", endOffset);
			endOffsetExpressionAnimation.SetReferenceParameter("source", m_scrollInputHelper.SourcePropertySet());

			if ((orientation == Orientation.Horizontal && HorizontalSourceOffsetKind == ParallaxSourceOffsetKind.Relative) ||
				(orientation == Orientation.Vertical && VerticalSourceOffsetKind == ParallaxSourceOffsetKind.Relative))
			{
				// Horizontal/VerticalSourceEndOffset is added to automatic value

				float maxOverpanOffset = (float)m_scrollInputHelper.GetMaxOverpanOffset(orientation);

				endOffsetExpressionAnimation.SetScalarParameter("maxOverpanOffset", maxOverpanOffset);

				if (m_scrollInputHelper.IsTargetElementInSource())
				{
					// Target is inside the scrollPresenter.

					// endOffset = (ParallaxViewOffset + ParallaxViewWidth + HorizontalSourceEndOffset) * ZoomFactor + MaxOverpanOffset
					float parallaxViewOffset = (float)m_scrollInputHelper.GetOffsetFromScrollContentElement(this, orientation);
					float parallaxViewSize = (float)(orientation == Orientation.Horizontal ? ActualWidth : ActualHeight);

					endOffsetExpression = "(parallaxViewOffset + parallaxViewSize + endOffset) * source." + m_scrollInputHelper.GetSourceScalePropertyName() + " + maxOverpanOffset";
					endOffsetExpressionAnimation.SetScalarParameter("parallaxViewOffset", parallaxViewOffset);
					endOffsetExpressionAnimation.SetScalarParameter("parallaxViewSize", parallaxViewSize);
				}
				else
				{
					// Target is outside the scrollPresenter.

					float viewportSize = (float)m_scrollInputHelper.GetViewportSize(orientation);
					float contentSize = (float)m_scrollInputHelper.GetContentSize(orientation);

					// endOffset = Max(0, (ContentWidth + HorizontalSourceEndOffset) * ZoomFactor - ViewportWidth) + MaxOverpanOffset
					endOffsetExpression = "Max(0.0f, (contentSize + endOffset) * source." + m_scrollInputHelper.GetSourceScalePropertyName() + " - viewportSize) + maxOverpanOffset";
					endOffsetExpressionAnimation.SetScalarParameter("viewportSize", viewportSize);
					endOffsetExpressionAnimation.SetScalarParameter("contentSize", contentSize);
				}
			}
			else
			{
				// Horizontal/VerticalSourceEndOffset is an absolute value

				float viewportSize = (float)m_scrollInputHelper.GetViewportSize(orientation);
				float contentSize = (float)m_scrollInputHelper.GetContentSize(orientation);

				// If (ContentWidth > ViewportWidth) Then
				//   If (HorizontalSourceEndOffset <= ContentWidth - ViewportWidth) Then
				//     endOffset = Max(0, HorizontalSourceEndOffset * ZoomFactor)
				//   Else
				//     endOffset = Max(0, (ContentWidth - ViewportWidth) * ZoomFactor) + HorizontalSourceEndOffset - ContentWidth + ViewportWidth
				// Else
				//   If (HorizontalSourceEndOffset <= 0) Then
				//     endOffset = Max(0, (ContentWith + HorizontalSourceEndOffset) * ZoomFactor - ViewportWidth)
				//   Else
				//     endOffset = Max(0, ContentWidth * ZoomFactor - ViewportWidth) + HorizontalSourceEndOffset
				if (contentSize > viewportSize)
				{
					if (endOffset <= contentSize - viewportSize)
					{
						endOffsetExpression = "Max(0.0f, endOffset * source." + m_scrollInputHelper.GetSourceScalePropertyName() + ")";
					}
					else
					{
						endOffsetExpression = "Max(0.0f, (contentSize - viewportSize) * source." + m_scrollInputHelper.GetSourceScalePropertyName() + ") + endOffset - contentSize + viewportSize";
						endOffsetExpressionAnimation.SetScalarParameter("contentSize", contentSize);
						endOffsetExpressionAnimation.SetScalarParameter("viewportSize", viewportSize);
					}
				}
				else
				{
					if (endOffset <= 0.0f)
					{
						endOffsetExpression = "Max(0.0f, (contentSize + endOffset) * source." + m_scrollInputHelper.GetSourceScalePropertyName() + " - viewportSize)";
					}
					else
					{
						endOffsetExpression = "Max(0.0f, contentSize * source." + m_scrollInputHelper.GetSourceScalePropertyName() + " - viewportSize) + endOffset";
					}
					endOffsetExpressionAnimation.SetScalarParameter("contentSize", contentSize);
					endOffsetExpressionAnimation.SetScalarParameter("viewportSize", viewportSize);
				}
			}

			if (endOffsetExpressionAnimation.Expression != endOffsetExpression)
			{
				endOffsetExpressionAnimation.Expression = endOffsetExpression;
			}

			m_animatedVariables.StopAnimation((orientation == Orientation.Horizontal) ? "HorizontalSourceEndOffset" : "VerticalSourceEndOffset");
			m_animatedVariables.StartAnimation((orientation == Orientation.Horizontal) ? "HorizontalSourceEndOffset" : "VerticalSourceEndOffset", endOffsetExpressionAnimation);
		}
	}

	// Updates the parallaxing composition animation.
	private void UpdateExpressionAnimation(Orientation orientation)
	{
		if (m_scrollInputHelper is not null && m_scrollInputHelper.TargetElement() is not null && m_scrollInputHelper.SourcePropertySet() is not null)
		{
			Visual targetVisual = ElementCompositionPreview.GetElementVisual(m_scrollInputHelper.TargetElement());
			if (m_targetVisual != targetVisual)
			{
				m_targetVisual = targetVisual;
				ElementCompositionPreview.SetIsTranslationEnabled(m_scrollInputHelper.TargetElement(), true);
				EnsureAnimatedVariables();
			}
		}
		else if (m_targetVisual is not null)
		{
			// Stop prior parallaxing animations.
			m_targetVisual.StopAnimation(GetVisualTargetedPropertyName(Orientation.Horizontal));
			m_targetVisual.StopAnimation(GetVisualTargetedPropertyName(Orientation.Vertical));
			m_isHorizontalAnimationStarted = m_isVerticalAnimationStarted = false;
			m_targetVisual.Properties.InsertVector3(s_translationPropertyName, new Vector3(0.0f, 0.0f, 0.0f));
			m_targetVisual = null;
		}

		if (m_targetVisual is not null)
		{
			if ((orientation == Orientation.Horizontal && HorizontalShift == 0.0) ||
				(orientation == Orientation.Vertical && VerticalShift == 0.0))
			{
				if (orientation == Orientation.Horizontal)
				{
					if (m_isHorizontalAnimationStarted)
					{
						// Stop prior horizontal parallaxing animation.
						m_targetVisual.StopAnimation(GetVisualTargetedPropertyName(Orientation.Horizontal));
						m_isHorizontalAnimationStarted = false;
						m_targetVisual.Properties.InsertVector3(s_translationPropertyName, new Vector3(0.0f, 0.0f, 0.0f));

						if (m_isVerticalAnimationStarted)
						{
							m_targetVisual.StartAnimation(GetVisualTargetedPropertyName(Orientation.Vertical), m_verticalParallaxExpressionInternal);
						}
					}
				}
				else if (m_isVerticalAnimationStarted)
				{
					// Stop prior vertical parallaxing animation.
					m_targetVisual.StopAnimation(GetVisualTargetedPropertyName(Orientation.Vertical));
					m_isVerticalAnimationStarted = false;
					m_targetVisual.Properties.InsertVector3(s_translationPropertyName, new Vector3(0.0f, 0.0f, 0.0f));

					if (m_isHorizontalAnimationStarted)
					{
						m_targetVisual.StartAnimation(GetVisualTargetedPropertyName(Orientation.Horizontal), m_horizontalParallaxExpressionInternal);
					}
				}
			}
			else
			{
				UpdateStartOffsetExpression(orientation);
				UpdateEndOffsetExpression(orientation);

				ExpressionAnimation parallaxExpressionInternal = (orientation == Orientation.Horizontal) ? m_horizontalParallaxExpressionInternal : m_verticalParallaxExpressionInternal;
				string source = "source." + m_scrollInputHelper.GetSourceOffsetPropertyName(orientation);
				string startOffset = (orientation == Orientation.Horizontal) ? "animatedVariables.HorizontalSourceStartOffset" : "animatedVariables.VerticalSourceStartOffset";
				string endOffset = (orientation == Orientation.Horizontal) ? "animatedVariables.HorizontalSourceEndOffset" : "animatedVariables.VerticalSourceEndOffset";
				string parallaxExpression;
				float shift = (float)(orientation == Orientation.Horizontal ? HorizontalShift : VerticalShift);

				if ((orientation == Orientation.Horizontal && IsHorizontalShiftClamped) ||
					(orientation == Orientation.Vertical && IsVerticalShiftClamped))
				{
					// Clamped parallax offset case.

					if (shift > 0.0)
					{
						// X <= startOffset --> P(X) = 0
						parallaxExpression = "(-" + source + " <= " + startOffset + ") ? 0.0f : ";

						// startOffset < X < endOffset --> P(X) = -Min(MaxRatio, shift / (endOffset - startOffset)) * (X - startOffset)
						parallaxExpression += "((-" + source + " < " + endOffset + ") ? ";
						parallaxExpression += "(-Min(maxRatio, (shift / (" + endOffset + " - " + startOffset + "))) * (-" + source + " - " + startOffset + ")) : ";

						// X >= endOffset --> P(X) = -Min(MaxRatio * Max(0 , endOffset - startOffset), shift)
						parallaxExpression += "-Min(maxRatio * Max(0.0f, " + endOffset + " - " + startOffset + "), shift))";
					}
					else
					{
						// shift < 0.0

						// X <= startOffset --> P(X) = -Min(MaxRatio * Max(0 , endOffset - startOffset), -shift)
						parallaxExpression = "(-" + source + " <= " + startOffset + ") ? -Min(maxRatio * Max(0.0f, " + endOffset + " - " + startOffset + "), -shift) : ";

						// startOffset < X < endOffset --> P(X) = Min(MaxRatio, shift / (startOffset - endOffset)) * (X - endOffset)
						parallaxExpression += "((-" + source + " < " + endOffset + ") ? ";
						parallaxExpression += "(Min(maxRatio, (shift / (" + startOffset + " - " + endOffset + "))) * (-" + source + " - " + endOffset + ")) : ";

						// X >= endOffset --> P(X) = 0
						parallaxExpression += "0.0f)";
					}
				}
				else
				{
					// Unclamped parallax offset case.

					if (shift > 0.0)
					{
						// startOffset != endOffset --> P(X) = -Min(MaxRatio, shift / (endOffset - startOffset)) * (X - startOffset)
						string coreExpression = "-Min(maxRatio, shift / (" + endOffset + " - " + startOffset + ")) * (-" + source + " - " + startOffset + ")";
#if !HAS_UNO
						// startOffset == endOffset --> P(X) = 0
						parallaxExpression = "(" + startOffset + " == " + endOffset + ") ? 0.0f : " + coreExpression;
#else
						// TODO Uno specific: Uno's Composition ExpressionAnimation parser does not support the '==' operator.
						// Rewrite `a == b ? 0 : core` as the semantically equivalent `a >= b ? (b >= a ? 0 : core) : core`.
						parallaxExpression = "(" + startOffset + " >= " + endOffset + ") ? ((" + endOffset + " >= " + startOffset + ") ? 0.0f : " + coreExpression + ") : (" + coreExpression + ")";
#endif
					}
					else
					{
						// startOffset != endOffset --> P(X) = Min(MaxRatio, shift / (startOffset - endOffset)) * (X - endOffset)
						string coreExpression = "Min(maxRatio, shift / (" + startOffset + " - " + endOffset + ")) * (-" + source + " - " + endOffset + ")";
#if !HAS_UNO
						// startOffset == endOffset --> P(X) = 0
						parallaxExpression = "(" + startOffset + " == " + endOffset + ") ? 0.0f : " + coreExpression;
#else
						// TODO Uno specific: Uno's Composition ExpressionAnimation parser does not support the '==' operator.
						// Rewrite `a == b ? 0 : core` as the semantically equivalent `a >= b ? (b >= a ? 0 : core) : core`.
						parallaxExpression = "(" + startOffset + " >= " + endOffset + ") ? ((" + endOffset + " >= " + startOffset + ") ? 0.0f : " + coreExpression + ") : (" + coreExpression + ")";
#endif
					}
				}

				if (parallaxExpressionInternal is null)
				{
					parallaxExpressionInternal = m_targetVisual.Compositor.CreateExpressionAnimation(parallaxExpression);
					if (orientation == Orientation.Horizontal)
					{
						m_horizontalParallaxExpressionInternal = parallaxExpressionInternal;
					}
					else
					{
						m_verticalParallaxExpressionInternal = parallaxExpressionInternal;
					}
				}
				else if (parallaxExpressionInternal.Expression != parallaxExpression)
				{
					parallaxExpressionInternal.Expression = parallaxExpression;
				}

				parallaxExpressionInternal.SetReferenceParameter("source", m_scrollInputHelper.SourcePropertySet());
				parallaxExpressionInternal.SetReferenceParameter("animatedVariables", m_animatedVariables);
				parallaxExpressionInternal.SetScalarParameter("maxRatio", (float)Math.Max(0.0, (orientation == Orientation.Horizontal ? MaxHorizontalShiftRatio : MaxVerticalShiftRatio)));
				parallaxExpressionInternal.SetScalarParameter("shift", shift);

				if (orientation == Orientation.Horizontal)
				{
					if (m_isHorizontalAnimationStarted)
					{
						m_targetVisual.StopAnimation(GetVisualTargetedPropertyName(Orientation.Horizontal));
					}
					m_targetVisual.StartAnimation(GetVisualTargetedPropertyName(Orientation.Horizontal), parallaxExpressionInternal);
					m_isHorizontalAnimationStarted = true;
				}
				else
				{
					if (m_isVerticalAnimationStarted)
					{
						m_targetVisual.StopAnimation(GetVisualTargetedPropertyName(Orientation.Vertical));
					}
					m_targetVisual.StartAnimation(GetVisualTargetedPropertyName(Orientation.Vertical), parallaxExpressionInternal);
					m_isVerticalAnimationStarted = true;
				}
			}
		}
	}

	private void HookLoaded()
	{
		if (m_loadedToken == 0)
		{
			m_loadedHandler = OnLoaded;
			Loaded += m_loadedHandler;
			m_loadedToken = 1;
		}
	}

	private void HookSizeChanged()
	{
		if (m_sizeChangedToken == 0)
		{
			m_sizeChangedHandler = OnSizeChanged;
			SizeChanged += m_sizeChangedHandler;
			m_sizeChangedToken = 1;
		}
	}

	private void HookChildPropertyChanged(FrameworkElement child)
	{
		if (child is not null)
		{
			m_currentListeningChild = child;
			if (m_childHorizontalAlignmentChangedToken == 0)
			{
				m_childHorizontalAlignmentChangedToken = child.RegisterPropertyChangedCallback(
					FrameworkElement.HorizontalAlignmentProperty, OnChildPropertyChanged);
			}
			if (m_childVerticalAlignmentChangedToken == 0)
			{
				m_childVerticalAlignmentChangedToken = child.RegisterPropertyChangedCallback(
					FrameworkElement.VerticalAlignmentProperty, OnChildPropertyChanged);
			}
		}
	}

	private void UnhookChildPropertyChanged(bool isInDestructor)
	{
		// TODO Uno: C++ uses tracker_ref<FrameworkElement>::safe_get(isInDestructor) to obtain the
		// tracked child even while the reference-tracker is tearing down. Uno does not have reference
		// trackers, so we simply read the field. `isInDestructor` is preserved for traceability.
		FrameworkElement child = m_currentListeningChild;
		if (child is not null)
		{
			if (m_childHorizontalAlignmentChangedToken != 0)
			{
				child.UnregisterPropertyChangedCallback(FrameworkElement.HorizontalAlignmentProperty, m_childHorizontalAlignmentChangedToken);
				m_childHorizontalAlignmentChangedToken = 0;
			}
			if (m_childVerticalAlignmentChangedToken != 0)
			{
				child.UnregisterPropertyChangedCallback(FrameworkElement.VerticalAlignmentProperty, m_childVerticalAlignmentChangedToken);
				m_childVerticalAlignmentChangedToken = 0;
			}
			m_currentListeningChild = null;
		}
	}
}
