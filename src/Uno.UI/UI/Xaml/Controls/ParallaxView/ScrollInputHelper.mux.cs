// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ScrollInputHelper.cpp, commit 5f9e85113

using System;
using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

using FxScrollViewer = Microsoft.UI.Xaml.Controls.ScrollViewer;
using FxZoomMode = Microsoft.UI.Xaml.Controls.ZoomMode;

using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Controls;

partial class ScrollInputHelper
{
	public ScrollInputHelper(Action<bool, bool> infoChangedFunction)
	{
		m_infoChangedFunction = infoChangedFunction;
	}

	// TODO Uno: Original C++ destructor cleanup. Uno does not support cleanup via finalizers.
	// The owning ParallaxView invokes Dispose() from its Loaded/Unloaded lifecycle to avoid leaks.
	public void Dispose()
	{
#if HAS_UNO
		UnhookScrollPresenterPropertyChanged();
		UnhookScrollPresenterContentPropertyChanged();
		UnhookScrollViewerPropertyChanged();
		UnhookScrollViewerContentPropertyChanged();
		UnhookScrollViewerDirectManipulationStarted();
		UnhookScrollViewerDirectManipulationCompleted();
		UnhookRichEditBoxTextChanged();
		UnhookSourceControlTemplateChanged();
		UnhookSourceElementLoaded();
		UnhookTargetElementLoaded();
		UnhookCompositionTargetRendering();
#endif
	}

	public UIElement TargetElement() => m_targetElement;

	public CompositionPropertySet SourcePropertySet() => m_sourcePropertySet;

	public bool IsTargetElementInSource() => m_isTargetElementInSource;

	public string GetSourceOffsetPropertyName(Orientation orientation)
	{
		return (orientation == Orientation.Horizontal) ? s_horizontalOffsetPropertyName : s_verticalOffsetPropertyName;
	}

	public string GetSourceScalePropertyName() => s_scalePropertyName;

	// Returns the offset of the scrolled element in relation to its owning source.
	public double GetOffsetFromScrollContentElement(UIElement element, Orientation orientation)
	{
		UIElement scrollContentElement = GetScrollContentElement();

		if (scrollContentElement is null)
		{
			return 0.0;
		}

		GeneralTransform gt = element.TransformToVisual(scrollContentElement);
		Point elementOffset = gt.TransformPoint(new Point(0, 0));

		return orientation == Orientation.Horizontal ? elementOffset.X : elementOffset.Y;
	}

	public double GetMaxUnderpanOffset(Orientation orientation)
	{
		return (orientation == Orientation.Horizontal) ? m_outOfBoundsPanSize.Width : m_outOfBoundsPanSize.Height;
	}

	public double GetMaxOverpanOffset(Orientation orientation)
	{
		return (orientation == Orientation.Horizontal) ? m_outOfBoundsPanSize.Width : m_outOfBoundsPanSize.Height;
	}

	public double GetContentSize(Orientation orientation)
	{
		return (orientation == Orientation.Horizontal) ? m_contentSize.Width : m_contentSize.Height;
	}

	public double GetViewportSize(Orientation orientation)
	{
		return (orientation == Orientation.Horizontal) ? m_viewportSize.Width : m_viewportSize.Height;
	}

	public void SetSourceElement(UIElement sourceElement)
	{
		if (m_sourceElement != sourceElement)
		{
			UnhookSourceControlTemplateChanged();

			m_sourceElement = sourceElement;
			OnSourceElementChanged(true /*allowSourceElementLoadedHookup*/);
		}
	}

	public void SetTargetElement(UIElement targetElement)
	{
		if (m_targetElement != targetElement)
		{
			UnhookTargetElementLoaded();
			m_targetElement = targetElement;
			OnTargetElementChanged();
		}
	}

	private void SetScrollViewer(FxScrollViewer scrollViewer)
	{
		if (scrollViewer != m_scrollViewer)
		{
			if (m_scrollViewer is not null)
			{
				UnhookScrollViewerPropertyChanged();
				UnhookScrollViewerDirectManipulationStarted();
				UnhookScrollViewerDirectManipulationCompleted();
				UnhookRichEditBoxTextChanged();

				m_richEditBox = null;
				m_scrollViewerPropertySet = null;
				m_isScrollViewerInDirectManipulation = false;
			}

			m_scrollViewer = scrollViewer;

			if (scrollViewer is not null)
			{
				// Check if the ScrollViewer belongs to a RichEditBox so content size changes can be detected
				// via its TextChanged event.
				m_richEditBox = ScrollInputHelper.GetRichEditBoxParent(scrollViewer);

				HookScrollViewerPropertyChanged();
				HookScrollViewerDirectManipulationStarted();
				HookScrollViewerDirectManipulationCompleted();
				HookRichEditBoxTextChanged();

#if !HAS_UNO
				m_scrollViewerPropertySet = ElementCompositionPreview.GetScrollViewerManipulationPropertySet(scrollViewer);
#else
				// TODO Uno specific: ElementCompositionPreview.GetScrollViewerManipulationPropertySet is not implemented in Uno.
				// The parallax expression animations rely on the ScrollViewer's manipulation property set, so ParallaxView will
				// not produce any visual shift when its Source is a ScrollViewer until this API is implemented.
				m_scrollViewerPropertySet = null;
#endif
			}

			ProcessScrollViewerContentChange();
		}
	}

	private void SetScrollPresenter(ScrollPresenter scrollPresenter)
	{
		if (scrollPresenter != m_scrollPresenter)
		{
			if (m_scrollPresenter is not null)
			{
				UnhookScrollPresenterPropertyChanged();
			}

			m_scrollPresenter = scrollPresenter;

			if (scrollPresenter is not null)
			{
				HookScrollPresenterPropertyChanged();
			}

			ProcessScrollPresenterContentChange();
		}
	}

	// Returns the parent RichEditBox if any.
	private static RichEditBox GetRichEditBoxParent(DependencyObject childElement)
	{
		if (childElement is not null)
		{
			DependencyObject parent = VisualTreeHelper.GetParent(childElement);
			if (parent is not null)
			{
				RichEditBox richEditBoxParent = parent as RichEditBox;
				if (richEditBoxParent is not null)
				{
					return richEditBoxParent;
				}
				return ScrollInputHelper.GetRichEditBoxParent(parent);
			}
		}

		return null;
	}

	// Returns the inner ScrollViewer or ScrollPresenter if any.
	private static void GetChildScrollPresenterOrScrollViewer(
		DependencyObject rootElement,
		out ScrollPresenter scrollPresenter,
		out FxScrollViewer scrollViewer)
	{
		scrollPresenter = null;
		scrollViewer = null;

		if (rootElement is not null)
		{
			int childCount = VisualTreeHelper.GetChildrenCount(rootElement);
			for (int i = 0; i < childCount; i++)
			{
				DependencyObject current = VisualTreeHelper.GetChild(rootElement, i);
				scrollViewer = current as FxScrollViewer;
				if (scrollViewer is not null)
				{
					return;
				}
				scrollPresenter = current as ScrollPresenter;
				if (scrollPresenter is not null)
				{
					return;
				}
			}

			for (int i = 0; i < childCount; i++)
			{
				DependencyObject current = VisualTreeHelper.GetChild(rootElement, i);
				ScrollInputHelper.GetChildScrollPresenterOrScrollViewer(
					current,
					out scrollPresenter,
					out scrollViewer);
				if (scrollPresenter is not null)
				{
					return;
				}
				if (scrollViewer is not null)
				{
					return;
				}
			}
		}
	}

	// Returns the ScrollViewer content as a UIElement, if any. Or ScrollPresenter.Content, if any.
	private UIElement GetScrollContentElement()
	{
		if (m_scrollViewer is not null)
		{
			ContentControl scrollViewerAsContentControl = m_scrollViewer as ContentControl;

			object content = scrollViewerAsContentControl.Content;
			if (content is not null)
			{
				return content as UIElement;
			}
		}
		else if (m_scrollPresenter is ScrollPresenter scrollPresenter)
		{
			return scrollPresenter.Content;
		}
		return null;
	}

	// Returns the effective horizontal alignment of the ScrollViewer content.
	private HorizontalAlignment GetEffectiveHorizontalAlignment()
	{
		if (m_isScrollViewerInDirectManipulation)
		{
			return m_manipulationHorizontalAlignment;
		}
		else
		{
			return ComputeHorizontalContentAlignment();
		}
	}

	// Returns the effective vertical alignment of the ScrollViewer content.
	private VerticalAlignment GetEffectiveVerticalAlignment()
	{
		if (m_isScrollViewerInDirectManipulation)
		{
			return m_manipulationVerticalAlignment;
		}
		else
		{
			return ComputeVerticalContentAlignment();
		}
	}

	// Returns the effective zoom mode of the ScrollViewer.
	private FxZoomMode GetEffectiveZoomMode()
	{
		if (m_isScrollViewerInDirectManipulation)
		{
			return m_manipulationZoomMode;
		}
		else
		{
			return ComputeZoomMode();
		}
	}

	// Updates the m_outOfBoundsPanSize field based on the viewport size and zoom mode.
	private void UpdateOutOfBoundsPanSize()
	{
		if (m_scrollPresenter is not null || m_scrollViewer is not null)
		{
			double viewportWith = GetViewportSize(Orientation.Horizontal);
			double viewportHeight = GetViewportSize(Orientation.Vertical);

			if (m_scrollViewer is not null && GetEffectiveZoomMode() == FxZoomMode.Disabled)
			{
				// A ScrollViewer can under/overpan up to 10% of its viewport size
				m_outOfBoundsPanSize.Width = (float)(0.1 * viewportWith);
				m_outOfBoundsPanSize.Height = (float)(0.1 * viewportHeight);
			}
			else
			{
				// When zooming is allowed for a ScrollViewer, or in general for the ScrollPresenter,
				// the content can be pushed all the way to the edge of the screen with two fingers,
				// but we limit the offset to one viewport size.
				// Note that if in the future, the ScrollPresenter's underpan & overpan limits become customizable,
				// its ExpressionAnimationSources property set will have to expose those custom limits.
				// The values would then be consumed here for a more accurate ParallaxView behavior.
				m_outOfBoundsPanSize.Width = (float)viewportWith;
				m_outOfBoundsPanSize.Height = (float)viewportHeight;
			}
		}
		else
		{
			m_outOfBoundsPanSize.Width = m_outOfBoundsPanSize.Height = 0.0f;
		}
	}

	// Updates the m_contentSize field.
	private void UpdateContentSize()
	{
		m_contentSize.Width = m_contentSize.Height = 0.0f;
		var scrollPresenter = m_scrollPresenter;

		if (scrollPresenter is null && m_scrollViewer is null)
		{
			return;
		}

		if (scrollPresenter is not null)
		{
			CompositionPropertySet scrollPresenterPropertySet = scrollPresenter.ExpressionAnimationSources;
			Vector2 extent = default;

			CompositionGetValueStatus status = scrollPresenterPropertySet.TryGetVector2("Extent", out extent);
			if (status == CompositionGetValueStatus.Succeeded)
			{
				m_contentSize.Width = extent.X;
				m_contentSize.Height = extent.Y;
			}
			return;
		}
		var scrollViewer = m_scrollViewer;
		float extentWidth = (float)scrollViewer.ExtentWidth;
		float extentHeight = (float)scrollViewer.ExtentHeight;

		UIElement scrollContentElement = GetScrollContentElement();
		ItemsPresenter itemsPresenter = scrollContentElement is not null ? (scrollContentElement as ItemsPresenter) : null;

		if (scrollContentElement is null || itemsPresenter is null)
		{
			m_contentSize.Width = extentWidth;
			m_contentSize.Height = extentHeight;
			return;
		}

		int childrenCount = VisualTreeHelper.GetChildrenCount(itemsPresenter);

		if (childrenCount > 0)
		{
			DependencyObject child = VisualTreeHelper.GetChild(itemsPresenter, childrenCount == 1 ? 0 : 1);

			if (child is not null)
			{
				VirtualizingStackPanel virtualizingStackPanel = child as VirtualizingStackPanel;

				if (virtualizingStackPanel is not null)
				{
					// VirtualizingStackPanel are handled specially because the ScrollViewer.ExtentWidth/ExtentHeight is unit-based instead
					// of pixel-based in the virtualized dimension. The computed size accounts for the potential margins, header and footer.
					double virtualizingSize = 0.0;
					Thickness vspMargin = virtualizingStackPanel.Margin;
					Thickness itMargin = itemsPresenter.Margin;

					if (virtualizingStackPanel.Orientation == Orientation.Horizontal)
					{
						virtualizingSize = virtualizingStackPanel.ExtentWidth * virtualizingStackPanel.ActualWidth / virtualizingStackPanel.ViewportWidth +
							vspMargin.Left + vspMargin.Right + itMargin.Left + itMargin.Right;
					}
					else
					{
						virtualizingSize = virtualizingStackPanel.ExtentHeight * virtualizingStackPanel.ActualHeight / virtualizingStackPanel.ViewportHeight +
							vspMargin.Top + vspMargin.Bottom + itMargin.Top + itMargin.Bottom;
					}

					if (childrenCount > 1)
					{
						child = VisualTreeHelper.GetChild(itemsPresenter, 0);

						if (child is not null)
						{
							FrameworkElement headerChild = child as FrameworkElement;

							if (headerChild is not null)
							{
								virtualizingSize += virtualizingStackPanel.Orientation == Orientation.Horizontal ? headerChild.ActualWidth : headerChild.ActualHeight;
							}
						}
					}

					if (childrenCount > 2)
					{
						child = VisualTreeHelper.GetChild(itemsPresenter, 2);

						if (child is not null)
						{
							FrameworkElement footerChild = child as FrameworkElement;

							if (footerChild is not null)
							{
								virtualizingSize += virtualizingStackPanel.Orientation == Orientation.Horizontal ? footerChild.ActualWidth : footerChild.ActualHeight;
							}
						}
					}

					if (virtualizingStackPanel.Orientation == Orientation.Horizontal)
					{
						m_contentSize.Width = (float)virtualizingSize;
						m_contentSize.Height = extentHeight;
					}
					else
					{
						m_contentSize.Width = extentWidth;
						m_contentSize.Height = (float)virtualizingSize;
					}

					return;
				}
			}
		}

		m_contentSize.Width = extentWidth;
		m_contentSize.Height = extentHeight;
	}

	// Updates the m_viewportSize field.
	private void UpdateViewportSize()
	{
		var scrollPresenter = m_scrollPresenter;
		if (scrollPresenter is not null)
		{
			CompositionPropertySet scrollPresenterPropertySet = scrollPresenter.ExpressionAnimationSources;
			Vector2 viewport = default;

			CompositionGetValueStatus status = scrollPresenterPropertySet.TryGetVector2("Viewport", out viewport);
			if (status == CompositionGetValueStatus.Succeeded)
			{
				m_viewportSize.Width = viewport.X;
				m_viewportSize.Height = viewport.Y;
			}
			return;
		}

		if (m_scrollViewer is not null)
		{
			var scrollViewer = m_scrollViewer;
			UIElement scrollContentElement = GetScrollContentElement();

			if (scrollContentElement is not null)
			{
				ItemsPresenter itemsPresenter = scrollContentElement as ItemsPresenter;

				if (itemsPresenter is not null)
				{
					int childrenCount = VisualTreeHelper.GetChildrenCount(itemsPresenter);

					if (childrenCount > 0)
					{
						DependencyObject child = VisualTreeHelper.GetChild(itemsPresenter, childrenCount == 1 ? 0 : 1);

						if (child is not null)
						{
							VirtualizingStackPanel virtualizingStackPanel = child as VirtualizingStackPanel;

							if (virtualizingStackPanel is not null)
							{
								// VirtualizingStackPanel are handled specially because the ScrollViewer.ViewportWidth/ViewportHeight is unit-based instead
								// of pixel-based in the virtualized dimension. The computed size accounts for the potential margins.
								Thickness itMargin = itemsPresenter.Margin;

								if (virtualizingStackPanel.Orientation == Orientation.Horizontal)
								{
									m_viewportSize.Width = (float)(itemsPresenter.ActualWidth + itMargin.Left + itMargin.Right);
									m_viewportSize.Height = (float)scrollViewer.ViewportHeight;
								}
								else
								{
									m_viewportSize.Width = (float)scrollViewer.ViewportWidth;
									m_viewportSize.Height = (float)(itemsPresenter.ActualHeight + itMargin.Top + itMargin.Bottom);
								}
								return;
							}
						}
					}
				}
			}
			m_viewportSize.Width = (float)scrollViewer.ViewportWidth;
			m_viewportSize.Height = (float)scrollViewer.ViewportHeight;
		}
		else
		{
			m_viewportSize.Width = m_viewportSize.Height = 0.0f;
		}
	}

	// Updates all the fields dependent on the ScrollViewer source. Stops/starts
	// the internal composition animations.
	private void UpdateSource(bool allowSourceElementLoadedHookup)
	{
		ScrollPresenter scrollPresenter = null;
		FxScrollViewer scrollViewer = null;
		var sourceElement = m_sourceElement;
		if (sourceElement is not null)
		{
			scrollPresenter = sourceElement as ScrollPresenter;
			scrollViewer = sourceElement as FxScrollViewer;
		}

		if (scrollPresenter is not null || scrollViewer is not null)
		{
			SetScrollPresenter(scrollPresenter);
			SetScrollViewer(scrollViewer);
		}
		else if (sourceElement is not null)
		{
			ScrollInputHelper.GetChildScrollPresenterOrScrollViewer(
				sourceElement,
				out scrollPresenter,
				out scrollViewer);
			SetScrollPresenter(scrollPresenter);
			SetScrollViewer(scrollViewer);
		}
		else
		{
			SetScrollPresenter(null);
			SetScrollViewer(null);
		}

		if (allowSourceElementLoadedHookup &&
			scrollPresenter is null &&
			m_scrollViewer is null)
		{
			HookSourceElementLoaded();
		}

		if (scrollPresenter is null && m_scrollViewer is null)
		{
			StopInternalExpressionAnimations();
			m_sourcePropertySet = null;
		}
		else if (m_targetElement is not null)
		{
			EnsureInternalSourcePropertySetAndExpressionAnimations();
			m_sourcePropertySet = m_internalSourcePropertySet;
			StartInternalExpressionAnimations(m_scrollViewer is not null ? m_scrollViewerPropertySet : scrollPresenter.ExpressionAnimationSources);
		}

		UpdateIsTargetElementInSource();
		UpdateContentSize();
		UpdateViewportSize();
		UpdateOutOfBoundsPanSize();
	}

	// Updates the m_isTargetElementInSource field.
	private void UpdateIsTargetElementInSource()
	{
		var targetElement = m_targetElement;

		if (targetElement is not null)
		{
			bool sourceIsScrollViewer = m_scrollViewer is not null;
			bool sourceIsScrollPresenter = m_scrollPresenter is not null;

			if (sourceIsScrollViewer || sourceIsScrollPresenter)
			{
				DependencyObject parent = targetElement;
				do
				{
					parent = VisualTreeHelper.GetParent(parent);
					if (parent is not null)
					{
						if (sourceIsScrollViewer)
						{
							FxScrollViewer parentAsScrollViewer = parent as FxScrollViewer;

							if (parentAsScrollViewer == m_scrollViewer)
							{
								m_isTargetElementInSource = true;
								return;
							}
						}
						else
						{
							ScrollPresenter parentAsScrollPresenter = parent as ScrollPresenter;

							if (parentAsScrollPresenter == m_scrollPresenter)
							{
								m_isTargetElementInSource = true;
								return;
							}
						}
					}
				} while (parent is not null);
			}
		}

		m_isTargetElementInSource = false;
	}

	// Updates the m_manipulationZoomMode field.
	private void UpdateManipulationZoomMode()
	{
		if (m_targetElement is not null)
		{
			m_manipulationZoomMode = ComputeZoomMode();
		}
	}

	// Updates the m_manipulationHorizontalAlignment/m_manipulationVerticalAlignment fields.
	private void UpdateManipulationAlignments()
	{
		if (m_targetElement is not null)
		{
			m_manipulationHorizontalAlignment = ComputeHorizontalContentAlignment();
			m_manipulationVerticalAlignment = ComputeVerticalContentAlignment();
		}
	}

	// Updates the internal composition animations that account for the alignment portions in the ScrollViewer's manipulation property set (m_scrollViewerPropertySet).
	// The offsets exposed by m_sourcePropertySet exclude those alignment portions.
	private void UpdateInternalExpressionAnimations(bool horizontalInfoChanged, bool verticalInfoChanged, bool zoomInfoChanged)
	{
		bool restartAnimations = false;

		if (m_scrollViewer is not null)
		{
			if (horizontalInfoChanged && m_internalTranslationXExpressionAnimation is not null)
			{
				switch (GetEffectiveHorizontalAlignment())
				{
					case HorizontalAlignment.Left:
						m_internalTranslationXExpressionAnimation.Expression = "source.Translation.X";
						break;

					case HorizontalAlignment.Stretch:
					case HorizontalAlignment.Center:
						m_internalTranslationXExpressionAnimation.Expression =
							"source.Translation.X + ((contentWidth * source.Scale.X - viewportWidth) < 0.0f ? (contentWidth * source.Scale.X - viewportWidth) / 2.0f : 0.0f)";
						m_internalTranslationXExpressionAnimation.SetScalarParameter("contentWidth", (float)GetContentSize(Orientation.Horizontal));
						m_internalTranslationXExpressionAnimation.SetScalarParameter("viewportWidth", (float)GetViewportSize(Orientation.Horizontal));
						break;

					case HorizontalAlignment.Right:
						m_internalTranslationXExpressionAnimation.Expression = "source.Translation.X + ((contentWidth * source.Scale.X - viewportWidth) < 0.0f ? (contentWidth * source.Scale.X - viewportWidth) : 0.0f)";
						m_internalTranslationXExpressionAnimation.SetScalarParameter("contentWidth", (float)GetContentSize(Orientation.Horizontal));
						m_internalTranslationXExpressionAnimation.SetScalarParameter("viewportWidth", (float)GetViewportSize(Orientation.Horizontal));
						break;
				}
				restartAnimations = true;
			}

			if (verticalInfoChanged && m_internalTranslationYExpressionAnimation is not null)
			{
				switch (GetEffectiveVerticalAlignment())
				{
					case VerticalAlignment.Top:
						m_internalTranslationYExpressionAnimation.Expression = "source.Translation.Y";
						break;

					case VerticalAlignment.Stretch:
					case VerticalAlignment.Center:
						m_internalTranslationYExpressionAnimation.Expression =
							"source.Translation.Y + ((contentWidth * source.Scale.Y - viewportWidth) < 0.0f ? (contentWidth * source.Scale.Y - viewportWidth) / 2.0f : 0.0f)";
						m_internalTranslationYExpressionAnimation.SetScalarParameter("contentWidth", (float)GetContentSize(Orientation.Vertical));
						m_internalTranslationYExpressionAnimation.SetScalarParameter("viewportWidth", (float)GetViewportSize(Orientation.Vertical));
						break;

					case VerticalAlignment.Bottom:
						m_internalTranslationYExpressionAnimation.Expression =
							"source.Translation.Y + ((contentWidth * source.Scale.Y - viewportWidth) < 0.0f ? (contentWidth * source.Scale.Y - viewportWidth) : 0.0f)";
						m_internalTranslationYExpressionAnimation.SetScalarParameter("contentWidth", (float)GetContentSize(Orientation.Vertical));
						m_internalTranslationYExpressionAnimation.SetScalarParameter("viewportWidth", (float)GetViewportSize(Orientation.Vertical));
						break;
				}
				restartAnimations = true;
			}

			if (zoomInfoChanged && m_internalScaleExpressionAnimation is not null)
			{
				m_internalScaleExpressionAnimation.Expression = "source.Scale.X";
				restartAnimations = true;
			}

			if (restartAnimations && m_targetElement is not null)
			{
				StartInternalExpressionAnimations(m_scrollViewerPropertySet);
			}
		}
		else if (m_scrollPresenter is ScrollPresenter scrollPresenter)
		{
			if (horizontalInfoChanged && m_internalTranslationXExpressionAnimation is not null)
			{
				m_internalTranslationXExpressionAnimation.Expression = "source.MinPosition.X - source.Position.X";
				restartAnimations = true;
			}
			if (verticalInfoChanged && m_internalTranslationYExpressionAnimation is not null)
			{
				m_internalTranslationYExpressionAnimation.Expression = "source.MinPosition.Y - source.Position.Y";
				restartAnimations = true;
			}

			if (zoomInfoChanged && m_internalScaleExpressionAnimation is not null)
			{
				m_internalScaleExpressionAnimation.Expression = "source.ZoomFactor";
				restartAnimations = true;
			}

			if (restartAnimations && m_targetElement is not null)
			{
				StartInternalExpressionAnimations(scrollPresenter.ExpressionAnimationSources);
			}
		}
	}

	// Returns the ScrollViewer's content horizontal alignment.
	private HorizontalAlignment ComputeHorizontalContentAlignment()
	{
		// Panels that implement XAML's internal IScrollInfo interface: OrientedVirtualizingPanel, CarouselPanel, TextBoxView for TextBox, RichTextBox and PasswordBox.

		HorizontalAlignment horizontalAlignment = HorizontalAlignment.Stretch;

		// First access the ScrollViewer's HorizontalContentAlignment
		if (m_scrollViewer is not null)
		{
			horizontalAlignment = m_scrollViewer.HorizontalContentAlignment;

			// Determine whether the ScrollContentPresenter is the IScrollInfo implementer or not
			if (IsScrollContentPresenterIScrollInfoProvider())
			{
				// When the ScrollContentPresenter is the IScrollInfo implementer,
				// use the horizontal alignment of the manipulated element by default.
				UIElement scrollContentElement = GetScrollContentElement();

				if (scrollContentElement is not null)
				{
					FrameworkElement contentAsFrameworkElement = scrollContentElement as FrameworkElement;

					if (contentAsFrameworkElement is not null)
					{
						horizontalAlignment = contentAsFrameworkElement.HorizontalAlignment;
					}
				}
			}
		}

		return horizontalAlignment;
	}

	// Returns the ScrollViewer's content vertical alignment.
	private VerticalAlignment ComputeVerticalContentAlignment()
	{
		// Panels that implement XAML's internal IScrollInfo interface: OrientedVirtualizingPanel, CarouselPanel, TextBoxView for TextBox, RichTextBox and PasswordBox.

		VerticalAlignment verticalAlignment = VerticalAlignment.Stretch;

		// First access the ScrollViewer's VerticalContentAlignment
		if (m_scrollViewer is not null)
		{
			verticalAlignment = m_scrollViewer.VerticalContentAlignment;

			// Determine whether the ScrollContentPresenter is the IScrollInfo implementer or not
			if (IsScrollContentPresenterIScrollInfoProvider())
			{
				// When the ScrollContentPresenter is the IScrollInfo implementer,
				// use the vertical alignment of the manipulated element by default.
				UIElement scrollContentElement = GetScrollContentElement();

				if (scrollContentElement is not null)
				{
					FrameworkElement contentAsFrameworkElement = scrollContentElement as FrameworkElement;

					if (contentAsFrameworkElement is not null)
					{
						verticalAlignment = contentAsFrameworkElement.VerticalAlignment;
					}
				}
			}
		}

		return verticalAlignment;
	}

	private FxZoomMode ComputeZoomMode()
	{
		return m_scrollViewer is not null ? m_scrollViewer.ZoomMode : FxZoomMode.Disabled;
	}

	// Determines whether the ScrollViewer's ScrollContentPresenter is the IScrollInfo implementer used by the ScrollViewer.
	private bool IsScrollContentPresenterIScrollInfoProvider()
	{
		if (m_scrollViewer is not null)
		{
			UIElement scrollContentElement = GetScrollContentElement();

			if (scrollContentElement is not null)
			{
				ItemsPresenter itemsPresenter = scrollContentElement as ItemsPresenter;

				if (itemsPresenter is not null)
				{
					int childrenCount = VisualTreeHelper.GetChildrenCount(itemsPresenter);

					if (childrenCount > 0)
					{
						DependencyObject child = VisualTreeHelper.GetChild(itemsPresenter, childrenCount == 1 ? 0 : 1);

						if (child is not null)
						{
#if !HAS_UNO
							OrientedVirtualizingPanel itemsPanelAsOrientedVirtualizingPanel = child as OrientedVirtualizingPanel;
							CarouselPanel itemsPanelAsCarouselPanel = child as CarouselPanel;

							if (itemsPanelAsOrientedVirtualizingPanel is not null || itemsPanelAsCarouselPanel is not null)
							{
								return false;
							}
#else
							// TODO Uno: OrientedVirtualizingPanel and CarouselPanel are XAML-internal panels that are not exposed in Uno.
							// VirtualizingStackPanel is however a concrete IScrollInfo implementer in Uno as well.
							if (child is VirtualizingStackPanel)
							{
								return false;
							}
#endif
						}
					}
				}
			}
			return true;
		}
		return false;
	}

	// Creates the internal composition property set, m_internalSourcePropertySet, that filters out the alignment portions of the ScrollViewer manipulation property set.
	private void EnsureInternalSourcePropertySetAndExpressionAnimations()
	{
		var targetElement = m_targetElement;
		if (m_internalSourcePropertySet is null && targetElement is not null)
		{
			Visual visual = ElementCompositionPreview.GetElementVisual(targetElement);
			Compositor compositor = visual.Compositor;

			m_internalSourcePropertySet = compositor.CreatePropertySet();
			m_internalSourcePropertySet.InsertScalar(s_horizontalOffsetPropertyName, 0.0f);
			m_internalSourcePropertySet.InsertScalar(s_verticalOffsetPropertyName, 0.0f);
			m_internalSourcePropertySet.InsertScalar(s_scalePropertyName, 1.0f);

			var scrollViewer = m_scrollViewer;
			m_internalTranslationXExpressionAnimation = compositor.CreateExpressionAnimation(scrollViewer is not null ? "source.Translation.X" : "source.MinPosition.X - source.Position.X");
			m_internalTranslationYExpressionAnimation = compositor.CreateExpressionAnimation(scrollViewer is not null ? "source.Translation.Y" : "source.MinPosition.Y - source.Position.Y");
			m_internalScaleExpressionAnimation = compositor.CreateExpressionAnimation(scrollViewer is not null ? "source.Scale.X" : "source.ZoomFactor");
		}
	}

	// Starts the animations targeting the properties inside m_internalSourcePropertySet.
	private void StartInternalExpressionAnimations(CompositionPropertySet source)
	{
		if (m_internalSourcePropertySet is not null && source is not null)
		{
			m_internalTranslationXExpressionAnimation.SetReferenceParameter("source", source);
			m_internalTranslationYExpressionAnimation.SetReferenceParameter("source", source);
			m_internalScaleExpressionAnimation.SetReferenceParameter("source", source);

			m_internalSourcePropertySet.StopAnimation(s_horizontalOffsetPropertyName);
			m_internalSourcePropertySet.StopAnimation(s_verticalOffsetPropertyName);
			m_internalSourcePropertySet.StopAnimation(s_scalePropertyName);

			m_internalSourcePropertySet.StartAnimation(s_horizontalOffsetPropertyName, m_internalTranslationXExpressionAnimation);
			m_internalSourcePropertySet.StartAnimation(s_verticalOffsetPropertyName, m_internalTranslationYExpressionAnimation);
			m_internalSourcePropertySet.StartAnimation(s_scalePropertyName, m_internalScaleExpressionAnimation);
		}
	}

	// Stops the animations targeting the properties inside m_internalSourcePropertySet.
	private void StopInternalExpressionAnimations()
	{
		if (m_internalSourcePropertySet is not null)
		{
			m_internalSourcePropertySet.StopAnimation(s_horizontalOffsetPropertyName);
			m_internalSourcePropertySet.StopAnimation(s_verticalOffsetPropertyName);
			m_internalSourcePropertySet.StopAnimation(s_scalePropertyName);

			m_internalSourcePropertySet.InsertScalar(s_horizontalOffsetPropertyName, 0.0f);
			m_internalSourcePropertySet.InsertScalar(s_verticalOffsetPropertyName, 0.0f);
			m_internalSourcePropertySet.InsertScalar(s_scalePropertyName, 1.0f);
		}
	}

	private void ProcessSourceElementChange(bool allowSourceElementLoadedHookup)
	{
		CompositionPropertySet oldSourcePropertySet = m_sourcePropertySet;
		bool oldIsTargetElementInSource = m_isTargetElementInSource;
		double oldViewportWidth = GetViewportSize(Orientation.Horizontal);
		double oldViewportHeight = GetViewportSize(Orientation.Vertical);
		double oldContentWidth = GetContentSize(Orientation.Horizontal);
		double oldContentHeight = GetContentSize(Orientation.Vertical);
		double oldUnderpanWidth = GetMaxUnderpanOffset(Orientation.Horizontal);
		double oldUnderpanHeight = GetMaxUnderpanOffset(Orientation.Vertical);
		double oldOverpanWidth = GetMaxOverpanOffset(Orientation.Horizontal);
		double oldOverpanHeight = GetMaxOverpanOffset(Orientation.Vertical);

		UnhookSourceElementLoaded();

		UpdateSource(allowSourceElementLoadedHookup);

		if (m_sourcePropertySet != oldSourcePropertySet ||
			m_isTargetElementInSource != oldIsTargetElementInSource)
		{
			OnSourceInfoChanged(true /*horizontalInfoChanged*/, true /*verticalInfoChanged*/, true /*zoomInfoChanged*/);
		}
		else
		{
			bool horizontalInfoChanged =
				oldViewportWidth != GetViewportSize(Orientation.Horizontal) ||
				oldContentWidth != GetContentSize(Orientation.Horizontal) ||
				oldUnderpanWidth != GetMaxUnderpanOffset(Orientation.Horizontal) ||
				oldOverpanWidth != GetMaxOverpanOffset(Orientation.Horizontal);

			bool verticalInfoChanged =
				oldViewportHeight != GetViewportSize(Orientation.Vertical) ||
				oldContentHeight != GetContentSize(Orientation.Vertical) ||
				oldUnderpanHeight != GetMaxUnderpanOffset(Orientation.Vertical) ||
				oldOverpanHeight != GetMaxOverpanOffset(Orientation.Vertical);

			if (horizontalInfoChanged || verticalInfoChanged)
			{
				OnSourceInfoChanged(horizontalInfoChanged, verticalInfoChanged, true /*zoomInfoChanged*/);
			}
		}
	}

	private void ProcessTargetElementChange()
	{
		bool oldIsTargetElementInSource = m_isTargetElementInSource;

		UpdateIsTargetElementInSource();

		if (m_isTargetElementInSource != oldIsTargetElementInSource)
		{
			OnSourceInfoChanged(true /*horizontalInfoChanged*/, true /*verticalInfoChanged*/, false /*zoomInfoChanged*/);
		}
		else if (m_targetElement is not null)
		{
			UpdateInternalExpressionAnimations(true /*horizontalInfoChanged*/, true /*verticalInfoChanged*/, false /*zoomInfoChanged*/);
		}
	}

	// Invoked when the ScrollViewer.Content or ScrollPresenter.Content size changed.
	private void ProcessContentSizeChange()
	{
		double oldContentWidth = GetContentSize(Orientation.Horizontal);
		double oldContentHeight = GetContentSize(Orientation.Vertical);

		UpdateContentSize();

		double newContentWidth = GetContentSize(Orientation.Horizontal);
		double newContentHeight = GetContentSize(Orientation.Vertical);

		if (oldContentWidth != newContentWidth || oldContentHeight != newContentHeight)
		{
			OnSourceInfoChanged(oldContentWidth != newContentWidth, oldContentHeight != newContentHeight, false /*zoomInfoChanged*/);
		}
	}

	// Invoked when the source element has changed.
	private void OnSourceElementChanged(bool allowSourceElementLoadedHookup)
	{
		var sourceElement = m_sourceElement;
		if (sourceElement is not null)
		{
			Control sourceAsControl = sourceElement as Control;
			FxScrollViewer sourceAsScrollViewer = sourceElement as FxScrollViewer;

			if (sourceAsControl is not null && sourceAsScrollViewer is null)
			{
				HookSourceControlTemplateChanged();
			}
		}
		else
		{
			// No need to find the inner ScrollViewer at the next UI thread tick.
			UnhookCompositionTargetRendering();
		}

		ProcessSourceElementChange(allowSourceElementLoadedHookup);
	}

	// Invoked when the target element has changed.
	private void OnTargetElementChanged()
	{
		var scrollViewer = m_scrollViewer;
		var targetElement = m_targetElement;
		var scrollPresenter = m_scrollPresenter;
		if (targetElement is not null)
		{
			DependencyObject parent = VisualTreeHelper.GetParent(targetElement);
			if (parent is null)
			{
				HookTargetElementLoaded();
			}
		}

		if (scrollPresenter is not null || scrollViewer is not null)
		{
			EnsureInternalSourcePropertySetAndExpressionAnimations();
			m_sourcePropertySet = m_internalSourcePropertySet;
			if (targetElement is not null)
			{
				StartInternalExpressionAnimations(scrollViewer is not null ? m_scrollViewerPropertySet : scrollPresenter.ExpressionAnimationSources);
				if (m_isScrollViewerInDirectManipulation)
				{
					UpdateManipulationAlignments();
					UpdateManipulationZoomMode();
				}
			}

			ProcessTargetElementChange();
		}
	}

	// Invoked when the source is a Control other than a ScrollViewer, and its Template property changed.
	private void ProcessSourceControlTemplateChange()
	{
		// Wait for one UI thread tick so the new control template gets applied and the potential inner ScrollViewer can be set.
		HookCompositionTargetRendering();
	}

	// Invoked when a source characteristic influencing the composition animations changed.
	private void OnSourceInfoChanged(bool horizontalInfoChanged, bool verticalInfoChanged, bool zoomInfoChanged)
	{
		MUX_ASSERT(horizontalInfoChanged || verticalInfoChanged);

		if ((m_scrollPresenter is not null || m_scrollViewer is not null) && m_targetElement is not null)
		{
			UpdateInternalExpressionAnimations(horizontalInfoChanged, verticalInfoChanged, zoomInfoChanged);
		}

		if (m_infoChangedFunction is not null)
		{
			// Let the ScrollInputHelper consumer know about the characteristic change too.
			m_infoChangedFunction(horizontalInfoChanged, verticalInfoChanged);
		}
	}

	private void OnTargetElementLoaded(object sender, RoutedEventArgs args)
	{
		UnhookTargetElementLoaded();
		ProcessTargetElementChange();
	}

	private void OnSourceElementLoaded(object sender, RoutedEventArgs args)
	{
		UnhookSourceElementLoaded();
		ProcessSourceElementChange(false /*allowSourceElementLoadedHookup*/);
	}

	private void OnSourceElementPropertyChanged(DependencyObject sender, DependencyProperty args)
	{
		if (args == Control.TemplateProperty)
		{
			ProcessSourceControlTemplateChange();
		}
	}

	private void ProcessScrollViewerContentChange()
	{
		UnhookScrollViewerContentPropertyChanged();

		m_sourceContent = null;

		var scrollViewer = m_scrollViewer;
		if (scrollViewer is not null)
		{
			object newContent = scrollViewer.Content;

			if (newContent is not null)
			{
				m_sourceContent = newContent as FrameworkElement;

				HookScrollViewerContentPropertyChanged();
			}
		}

		OnSourceInfoChanged(true /*horizontalInfoChanged*/, true /*verticalInfoChanged*/, true /*zoomInfoChanged*/);
	}

	private void ProcessScrollPresenterContentChange()
	{
		var scrollPresenter = m_scrollPresenter;
		UnhookScrollPresenterContentPropertyChanged();

		m_sourceContent = null;

		if (scrollPresenter is not null)
		{
			UIElement newContent = scrollPresenter.Content;

			if (newContent is not null)
			{
				m_sourceContent = newContent as FrameworkElement;

				HookScrollPresenterContentPropertyChanged();
			}
		}

		OnSourceInfoChanged(true /*horizontalInfoChanged*/, true /*verticalInfoChanged*/, true /*zoomInfoChanged*/);
	}

	private void ProcessScrollViewerZoomModeChange()
	{
		UpdateOutOfBoundsPanSize();
		OnSourceInfoChanged(true /*horizontalInfoChanged*/, true /*verticalInfoChanged*/, false /*zoomInfoChanged*/);
	}

	private void OnSourceSizeChanged(object sender, SizeChangedEventArgs args)
	{
		double oldViewportWidth = GetViewportSize(Orientation.Horizontal);
		double oldViewportHeight = GetViewportSize(Orientation.Vertical);
		double oldUnderpanWidth = GetMaxUnderpanOffset(Orientation.Horizontal);
		double oldUnderpanHeight = GetMaxUnderpanOffset(Orientation.Vertical);
		double oldOverpanWidth = GetMaxOverpanOffset(Orientation.Horizontal);
		double oldOverpanHeight = GetMaxOverpanOffset(Orientation.Vertical);

		UpdateViewportSize();
		UpdateOutOfBoundsPanSize();

		bool horizontalInfoChanged =
			oldViewportWidth != GetViewportSize(Orientation.Horizontal) ||
			oldUnderpanWidth != GetMaxUnderpanOffset(Orientation.Horizontal) ||
			oldOverpanWidth != GetMaxOverpanOffset(Orientation.Horizontal);

		bool verticalInfoChanged =
			oldViewportHeight != GetViewportSize(Orientation.Vertical) ||
			oldUnderpanHeight != GetMaxUnderpanOffset(Orientation.Vertical) ||
			oldOverpanHeight != GetMaxOverpanOffset(Orientation.Vertical);

		if (horizontalInfoChanged || verticalInfoChanged)
		{
			OnSourceInfoChanged(horizontalInfoChanged, verticalInfoChanged, false /*zoomInfoChanged*/);
		}
	}

	private void OnScrollViewerDirectManipulationStarted(object sender, object args)
	{
		// Alignment and zoom mode changes during a manipulation are ignored until the end of that manipulation.

		m_isScrollViewerInDirectManipulation = true;

		UpdateManipulationAlignments();
		UpdateManipulationZoomMode();
	}

	private void OnScrollViewerDirectManipulationCompleted(object sender, object args)
	{
		// Alignment and zoom mode changes that occurred during this completed manipulation are now taken into account.

		HorizontalAlignment oldEffectiveHorizontalAlignment = HorizontalAlignment.Left;
		VerticalAlignment oldEffectiveVerticalAlignment = VerticalAlignment.Top;
		FxZoomMode oldZoomMode = FxZoomMode.Disabled;

		if (m_targetElement is not null)
		{
			oldEffectiveHorizontalAlignment = GetEffectiveHorizontalAlignment();
			oldEffectiveVerticalAlignment = GetEffectiveVerticalAlignment();
			oldZoomMode = GetEffectiveZoomMode();
		}

		m_isScrollViewerInDirectManipulation = false;

		if (m_targetElement is not null)
		{
			FxZoomMode newZoomMode = GetEffectiveZoomMode();

			if (oldZoomMode != newZoomMode)
			{
				ProcessScrollViewerZoomModeChange();
			}

			HorizontalAlignment newEffectiveHorizontalAlignment = GetEffectiveHorizontalAlignment();
			VerticalAlignment newEffectiveVerticalAlignment = GetEffectiveVerticalAlignment();

			if (oldEffectiveHorizontalAlignment != newEffectiveHorizontalAlignment || oldEffectiveVerticalAlignment != newEffectiveVerticalAlignment)
			{
				UpdateInternalExpressionAnimations(
					oldEffectiveHorizontalAlignment != newEffectiveHorizontalAlignment,
					oldEffectiveVerticalAlignment != newEffectiveVerticalAlignment,
					false /*zoomInfoChanged*/);
			}
		}
	}

	private void OnRichEditBoxTextChanged(object sender, RoutedEventArgs args)
	{
		ProcessContentSizeChange();
	}

	private void OnCompositionTargetRendering(object sender, object args)
	{
		// Unhook the Rendering event handler and attempt to find the potential new inner ScrollViewer.
		UnhookCompositionTargetRendering();
		ProcessSourceElementChange(false /*allowSourceElementLoadedHookup*/);
	}

	private void OnSourceContentSizeChanged(object sender, SizeChangedEventArgs args)
	{
		ProcessContentSizeChange();
	}

	// Invoked when a tracked dependency property changes for the ScrollViewer dependency object.
	private void OnScrollViewerPropertyChanged(DependencyObject sender, DependencyProperty args)
	{
		if (args == ContentControl.ContentProperty)
		{
			ProcessScrollViewerContentChange();
		}
		else if (args == FxScrollViewer.ZoomModeProperty)
		{
			if (!m_isScrollViewerInDirectManipulation)
			{
				ProcessScrollViewerZoomModeChange();
			}
		}
		else if (args == Control.HorizontalContentAlignmentProperty)
		{
			if (!m_isScrollViewerInDirectManipulation)
			{
				UpdateInternalExpressionAnimations(true /*horizontalInfoChanged*/, false /*verticalInfoChanged*/, false /*zoomInfoChanged*/);
			}
		}
		else if (args == Control.VerticalContentAlignmentProperty)
		{
			if (!m_isScrollViewerInDirectManipulation)
			{
				UpdateInternalExpressionAnimations(false /*horizontalInfoChanged*/, true /*verticalInfoChanged*/, false /*zoomInfoChanged*/);
			}
		}
	}

	// Invoked when a tracked dependency property changes for the ScrollPresenter dependency object.
	private void OnScrollPresenterPropertyChanged(DependencyObject sender, DependencyProperty args)
	{
		if (args == ScrollPresenter.ContentProperty)
		{
			ProcessScrollPresenterContentChange();
		}
	}

	// Invoked when a tracked dependency property changes for the ScrollViewer.Content dependency object.
	private void OnScrollViewerContentPropertyChanged(DependencyObject sender, DependencyProperty args)
	{
		if (args == FrameworkElement.HorizontalAlignmentProperty)
		{
			if (!m_isScrollViewerInDirectManipulation)
			{
				UpdateInternalExpressionAnimations(true /*horizontalInfoChanged*/, false /*verticalInfoChanged*/, false /*zoomInfoChanged*/);
			}
		}
		else if (args == FrameworkElement.VerticalAlignmentProperty)
		{
			if (!m_isScrollViewerInDirectManipulation)
			{
				UpdateInternalExpressionAnimations(false /*horizontalInfoChanged*/, true /*verticalInfoChanged*/, false /*zoomInfoChanged*/);
			}
		}
	}

	private void HookSourceElementLoaded()
	{
		var sourceElement = m_sourceElement;
		if (sourceElement is not null && m_sourceElementLoadedToken == 0)
		{
			FrameworkElement sourceElementAsFrameworkElement = sourceElement as FrameworkElement;

			if (sourceElementAsFrameworkElement is not null)
			{
				m_sourceElementLoadedHandler = OnSourceElementLoaded;
				sourceElementAsFrameworkElement.Loaded += m_sourceElementLoadedHandler;
				m_sourceElementLoadedToken = 1;
			}
		}
	}

	private void UnhookSourceElementLoaded()
	{
		var sourceElement = m_sourceElement;
		if (sourceElement is not null && m_sourceElementLoadedToken != 0)
		{
			FrameworkElement sourceElementAsFrameworkElement = sourceElement as FrameworkElement;

			if (sourceElementAsFrameworkElement is not null && m_sourceElementLoadedHandler is not null)
			{
				sourceElementAsFrameworkElement.Loaded -= m_sourceElementLoadedHandler;
				m_sourceElementLoadedHandler = null;
				m_sourceElementLoadedToken = 0;
			}
		}
	}

	private void HookSourceControlTemplateChanged()
	{
		var sourceElement = m_sourceElement;
		if (sourceElement is not null && m_sourceControlTemplateChangedToken == 0)
		{
			m_sourceControlTemplateChangedToken = sourceElement.RegisterPropertyChangedCallback(
				Control.TemplateProperty, OnSourceElementPropertyChanged);
		}
	}

	private void UnhookSourceControlTemplateChanged()
	{
		var sourceElement = m_sourceElement;
		if (sourceElement is not null && m_sourceControlTemplateChangedToken != 0)
		{
			sourceElement.UnregisterPropertyChangedCallback(Control.TemplateProperty, m_sourceControlTemplateChangedToken);
			m_sourceControlTemplateChangedToken = 0;
		}
	}

	private void HookTargetElementLoaded()
	{
		var targetElement = m_targetElement;

		if (targetElement is not null && m_targetElementLoadedToken == 0)
		{
			FrameworkElement targetElementAsFrameworkElement = targetElement as FrameworkElement;

			if (targetElementAsFrameworkElement is not null)
			{
				m_targetElementLoadedHandler = OnTargetElementLoaded;
				targetElementAsFrameworkElement.Loaded += m_targetElementLoadedHandler;
				m_targetElementLoadedToken = 1;
			}
		}
	}

	private void UnhookTargetElementLoaded()
	{
		var targetElement = m_targetElement;
		if (targetElement is not null && m_targetElementLoadedToken != 0)
		{
			FrameworkElement targetElementAsFrameworkElement = targetElement as FrameworkElement;

			if (targetElementAsFrameworkElement is not null && m_targetElementLoadedHandler is not null)
			{
				targetElementAsFrameworkElement.Loaded -= m_targetElementLoadedHandler;
				m_targetElementLoadedHandler = null;
				m_targetElementLoadedToken = 0;
			}
		}
	}

	private void HookScrollViewerPropertyChanged()
	{
		var scrollViewer = m_scrollViewer;
		if (scrollViewer is not null)
		{
			MUX_ASSERT(m_scrollViewerContentChangedToken == 0);
			MUX_ASSERT(m_scrollViewerHorizontalContentAlignmentChangedToken == 0);
			MUX_ASSERT(m_scrollViewerVerticalContentAlignmentChangedToken == 0);
			MUX_ASSERT(m_scrollViewerZoomModeChangedToken == 0);
			MUX_ASSERT(m_sourceSizeChangedToken == 0);

			m_scrollViewerContentChangedToken = scrollViewer.RegisterPropertyChangedCallback(
				ContentControl.ContentProperty, OnScrollViewerPropertyChanged);
			m_scrollViewerHorizontalContentAlignmentChangedToken = scrollViewer.RegisterPropertyChangedCallback(
				Control.HorizontalContentAlignmentProperty, OnScrollViewerPropertyChanged);
			m_scrollViewerVerticalContentAlignmentChangedToken = scrollViewer.RegisterPropertyChangedCallback(
				Control.VerticalContentAlignmentProperty, OnScrollViewerPropertyChanged);
			m_scrollViewerZoomModeChangedToken = scrollViewer.RegisterPropertyChangedCallback(
				FxScrollViewer.ZoomModeProperty, OnScrollViewerPropertyChanged);
			m_sourceSizeChangedHandler = OnSourceSizeChanged;
			scrollViewer.SizeChanged += m_sourceSizeChangedHandler;
			m_sourceSizeChangedToken = 1;
		}
	}

	private void HookScrollPresenterPropertyChanged()
	{
		var scrollPresenter = m_scrollPresenter;

		if (scrollPresenter is not null)
		{
			MUX_ASSERT(m_scrollPresenterContentChangedToken == 0);
			MUX_ASSERT(m_sourceSizeChangedToken == 0);

			m_scrollPresenterContentChangedToken = scrollPresenter.RegisterPropertyChangedCallback(
				ScrollPresenter.ContentProperty, OnScrollPresenterPropertyChanged);
			m_sourceSizeChangedHandler = OnSourceSizeChanged;
			scrollPresenter.SizeChanged += m_sourceSizeChangedHandler;
			m_sourceSizeChangedToken = 1;
		}
	}

	private void UnhookScrollViewerPropertyChanged()
	{
		var scrollViewer = m_scrollViewer;
		if (scrollViewer is not null)
		{
			if (m_scrollViewerContentChangedToken != 0)
			{
				scrollViewer.UnregisterPropertyChangedCallback(ContentControl.ContentProperty, m_scrollViewerContentChangedToken);
				m_scrollViewerContentChangedToken = 0;
			}
			if (m_scrollViewerHorizontalContentAlignmentChangedToken != 0)
			{
				scrollViewer.UnregisterPropertyChangedCallback(Control.HorizontalContentAlignmentProperty, m_scrollViewerHorizontalContentAlignmentChangedToken);
				m_scrollViewerHorizontalContentAlignmentChangedToken = 0;
			}
			if (m_scrollViewerVerticalContentAlignmentChangedToken != 0)
			{
				scrollViewer.UnregisterPropertyChangedCallback(Control.VerticalContentAlignmentProperty, m_scrollViewerVerticalContentAlignmentChangedToken);
				m_scrollViewerVerticalContentAlignmentChangedToken = 0;
			}
			if (m_scrollViewerZoomModeChangedToken != 0)
			{
				scrollViewer.UnregisterPropertyChangedCallback(FxScrollViewer.ZoomModeProperty, m_scrollViewerZoomModeChangedToken);
				m_scrollViewerZoomModeChangedToken = 0;
			}
			if (m_sourceSizeChangedToken != 0 && m_sourceSizeChangedHandler is not null)
			{
				scrollViewer.SizeChanged -= m_sourceSizeChangedHandler;
				m_sourceSizeChangedHandler = null;
				m_sourceSizeChangedToken = 0;
			}
		}
	}

	private void UnhookScrollPresenterPropertyChanged()
	{
		var scrollPresenter = m_scrollPresenter;

		if (scrollPresenter is not null)
		{
			if (m_scrollPresenterContentChangedToken != 0)
			{
				scrollPresenter.UnregisterPropertyChangedCallback(ScrollPresenter.ContentProperty, m_scrollPresenterContentChangedToken);
				m_scrollPresenterContentChangedToken = 0;
			}
			if (m_sourceSizeChangedToken != 0 && m_sourceSizeChangedHandler is not null)
			{
				scrollPresenter.SizeChanged -= m_sourceSizeChangedHandler;
				m_sourceSizeChangedHandler = null;
				m_sourceSizeChangedToken = 0;
			}
		}
	}

	private void HookScrollViewerContentPropertyChanged()
	{
		var sourceContent = m_sourceContent;
		if (sourceContent is not null)
		{
			if (m_scrollViewerContentHorizontalAlignmentChangedToken == 0)
			{
				m_scrollViewerContentHorizontalAlignmentChangedToken = sourceContent.RegisterPropertyChangedCallback(
					FrameworkElement.HorizontalAlignmentProperty, OnScrollViewerContentPropertyChanged);
			}
			if (m_scrollViewerContentVerticalAlignmentChangedToken == 0)
			{
				m_scrollViewerContentVerticalAlignmentChangedToken = sourceContent.RegisterPropertyChangedCallback(
					FrameworkElement.VerticalAlignmentProperty, OnScrollViewerContentPropertyChanged);
			}
			if (m_sourceContentSizeChangedToken == 0)
			{
				m_sourceContentSizeChangedHandler = OnSourceContentSizeChanged;
				sourceContent.SizeChanged += m_sourceContentSizeChangedHandler;
				m_sourceContentSizeChangedToken = 1;
			}
		}
	}

	private void HookScrollPresenterContentPropertyChanged()
	{
		var sourceContent = m_sourceContent;

		if (sourceContent is not null)
		{
			if (m_sourceContentSizeChangedToken == 0)
			{
				m_sourceContentSizeChangedHandler = OnSourceContentSizeChanged;
				sourceContent.SizeChanged += m_sourceContentSizeChangedHandler;
				m_sourceContentSizeChangedToken = 1;
			}
		}
	}

	private void UnhookScrollViewerContentPropertyChanged()
	{
		var sourceContent = m_sourceContent;

		if (sourceContent is not null)
		{
			if (m_scrollViewerContentHorizontalAlignmentChangedToken != 0)
			{
				sourceContent.UnregisterPropertyChangedCallback(FrameworkElement.HorizontalAlignmentProperty, m_scrollViewerContentHorizontalAlignmentChangedToken);
				m_scrollViewerContentHorizontalAlignmentChangedToken = 0;
			}
			if (m_scrollViewerContentVerticalAlignmentChangedToken != 0)
			{
				sourceContent.UnregisterPropertyChangedCallback(FrameworkElement.VerticalAlignmentProperty, m_scrollViewerContentVerticalAlignmentChangedToken);
				m_scrollViewerContentVerticalAlignmentChangedToken = 0;
			}
			if (m_sourceContentSizeChangedToken != 0 && m_sourceContentSizeChangedHandler is not null)
			{
				sourceContent.SizeChanged -= m_sourceContentSizeChangedHandler;
				m_sourceContentSizeChangedHandler = null;
				m_sourceContentSizeChangedToken = 0;
			}
		}
	}

	// Note that if in the future the ScrollPresenter supports a virtual mode where the extent does not
	// correspond to its Content size, the ScrollPresenter will need to raise an event when its virtual extent
	// changes so that the ScrollPresenter.ExpressionAnimationSources's Extent composition property can
	// be read. This should replace hooking up the SizeChanged event on the ScrollPresenter.Content altogether.
	private void UnhookScrollPresenterContentPropertyChanged()
	{
		var sourceContent = m_sourceContent;

		if (sourceContent is not null)
		{
			if (m_sourceContentSizeChangedToken != 0 && m_sourceContentSizeChangedHandler is not null)
			{
				sourceContent.SizeChanged -= m_sourceContentSizeChangedHandler;
				m_sourceContentSizeChangedHandler = null;
				m_sourceContentSizeChangedToken = 0;
			}
		}
	}

	private void HookScrollViewerDirectManipulationStarted()
	{
#if !HAS_UNO
		var scrollViewer = m_scrollViewer;
		if (scrollViewer is not null)
		{
			MUX_ASSERT(m_scrollViewerDirectManipulationStartedToken == 0);

			m_scrollViewerDirectManipulationStartedHandler = OnScrollViewerDirectManipulationStarted;
			scrollViewer.DirectManipulationStarted += m_scrollViewerDirectManipulationStartedHandler;
			m_scrollViewerDirectManipulationStartedToken = 1;
		}
#else
		// TODO Uno specific: ScrollViewer.DirectManipulationStarted is not exposed by Uno's ScrollViewer.
		// Parallax updates driven by a direct-manipulation transition are therefore not re-evaluated at the
		// manipulation boundaries on Uno. Non-manipulation view changes still work via the property-changed
		// callbacks above.
#endif
	}

	private void UnhookScrollViewerDirectManipulationStarted()
	{
#if !HAS_UNO
		var scrollViewer = m_scrollViewer;
		if (scrollViewer is not null && m_scrollViewerDirectManipulationStartedToken != 0 && m_scrollViewerDirectManipulationStartedHandler is not null)
		{
			scrollViewer.DirectManipulationStarted -= m_scrollViewerDirectManipulationStartedHandler;
			m_scrollViewerDirectManipulationStartedHandler = null;
			m_scrollViewerDirectManipulationStartedToken = 0;
		}
#endif
	}

	private void HookScrollViewerDirectManipulationCompleted()
	{
#if !HAS_UNO
		var scrollViewer = m_scrollViewer;
		if (scrollViewer is not null)
		{
			MUX_ASSERT(m_scrollViewerDirectManipulationCompletedToken == 0);

			m_scrollViewerDirectManipulationCompletedHandler = OnScrollViewerDirectManipulationCompleted;
			scrollViewer.DirectManipulationCompleted += m_scrollViewerDirectManipulationCompletedHandler;
			m_scrollViewerDirectManipulationCompletedToken = 1;
		}
#else
		// TODO Uno specific: ScrollViewer.DirectManipulationCompleted is not exposed by Uno's ScrollViewer.
#endif
	}

	private void UnhookScrollViewerDirectManipulationCompleted()
	{
#if !HAS_UNO
		var scrollViewer = m_scrollViewer;
		if (scrollViewer is not null && m_scrollViewerDirectManipulationCompletedToken != 0 && m_scrollViewerDirectManipulationCompletedHandler is not null)
		{
			scrollViewer.DirectManipulationCompleted -= m_scrollViewerDirectManipulationCompletedHandler;
			m_scrollViewerDirectManipulationCompletedHandler = null;
			m_scrollViewerDirectManipulationCompletedToken = 0;
		}
#endif
	}

	private void HookRichEditBoxTextChanged()
	{
		var richEditBox = m_richEditBox;
		if (richEditBox is not null)
		{
			MUX_ASSERT(m_richEditBoxTextChangedToken == 0);

			m_richEditBoxTextChangedHandler = OnRichEditBoxTextChanged;
			richEditBox.TextChanged += m_richEditBoxTextChangedHandler;
			m_richEditBoxTextChangedToken = 1;
		}
	}

	private void UnhookRichEditBoxTextChanged()
	{
		var richEditBox = m_richEditBox;
		if (richEditBox is not null && m_richEditBoxTextChangedToken != 0 && m_richEditBoxTextChangedHandler is not null)
		{
			richEditBox.TextChanged -= m_richEditBoxTextChangedHandler;
			m_richEditBoxTextChangedHandler = null;
			m_richEditBoxTextChangedToken = 0;
		}
	}

	private void HookCompositionTargetRendering()
	{
		if (m_renderingToken == 0)
		{
			m_renderingHandler = OnCompositionTargetRendering;
			Microsoft.UI.Xaml.Media.CompositionTarget.Rendering += m_renderingHandler;
			m_renderingToken = 1;
		}
	}

	private void UnhookCompositionTargetRendering()
	{
		if (m_renderingToken != 0 && m_renderingHandler is not null)
		{
			Microsoft.UI.Xaml.Media.CompositionTarget.Rendering -= m_renderingHandler;
			m_renderingHandler = null;
			m_renderingToken = 0;
		}
	}
}
