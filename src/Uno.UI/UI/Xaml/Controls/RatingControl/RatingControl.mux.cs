// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference RatingControl.cpp, tag winui3/release/1.7.3, commit 65718e2813a90f

using System;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;
using Windows.System;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using RatingControlAutomationPeer = Microsoft.UI.Xaml.Automation.Peers.RatingControlAutomationPeer;
using Windows.Foundation.Metadata;
using System.Numerics;
using Windows.UI.ViewManagement;

using Microsoft.UI.Input;

namespace Microsoft.UI.Xaml.Controls;

partial class RatingControl
{
#if __SKIA__ // TODO Uno: Expression animation is only supported on Skia
	private const float c_scaleAnimationCenterPointXValue = 16.0f;
	private const float c_scaleAnimationCenterPointYValue = 16.0f;
#endif

	private const int c_captionSpacing = 12;

	private const float c_mouseOverScale = 0.8f;
	// private const float c_touchOverScale = 1.0f; // Unused even in WinUI code
	private const float c_noPointerOverMagicNumber = -100;

	private const int c_noValueSetSentinel = -1;

	private const string c_fontSizeForRenderingKey = "RatingControlFontSizeForRendering";
	private const string c_itemSpacingKey = "RatingControlItemSpacing";
	private const string c_captionTopMarginKey = "RatingControlCaptionTopMargin";

	/// <summary>
	/// Initializes a new instance of the RatingControl class.
	/// </summary>
	public RatingControl()
	{
		// Uno specific: Needs to be initialized in constructor, as "this" is not available in readonly field initializer
		m_dispatcherHelper = new DispatcherHelper(this);

		//__RP_Marker_ClassById(RuntimeProfiler.ProfId_RatingControl);

		this.SetDefaultStyleKey();

#if HAS_UNO
		// Ensure events are detached on Unload and reattached on Load
		Loaded += RatingControl_Loaded;
		Unloaded += RatingControl_Unloaded;
#endif
	}


#if HAS_UNO
	private void RatingControl_Loaded(object sender, RoutedEventArgs e)
	{
		RecycleEvents(true /* useSafeGet */);
		AttachEvents();
	}

	private void AttachEvents()
	{
		if (m_backgroundStackPanel != null)
		{
			m_backgroundStackPanel.PointerCanceled += OnPointerCancelledBackgroundStackPanel;
			m_backgroundStackPanel.PointerCaptureLost += OnPointerCaptureLostBackgroundStackPanel;
			m_backgroundStackPanel.PointerMoved += OnPointerMovedOverBackgroundStackPanel;
			m_backgroundStackPanel.PointerEntered += OnPointerEnteredBackgroundStackPanel;
			m_backgroundStackPanel.PointerExited += OnPointerExitedBackgroundStackPanel;
			m_backgroundStackPanel.PointerPressed += OnPointerPressedBackgroundStackPanel;
			m_backgroundStackPanel.PointerReleased += OnPointerReleasedBackgroundStackPanel;
		}

		if (m_captionTextBlock != null)
		{
			m_captionTextBlock.SizeChanged += OnCaptionSizeChanged;
		}

		m_fontFamilyChangedToken = RegisterPropertyChangedCallback(Control.FontFamilyProperty, OnFontFamilyChanged);

		IsEnabledChanged += OnIsEnabledChanged;
	}

	// Uno specific: Unsubscribing events here instead of the destructor to ensure
	// it happens on the UI thread.
	private void RatingControl_Unloaded(object sender, RoutedEventArgs e)
	{
		RecycleEvents(true /* useSafeGet */);
	}
#endif

	//~RatingControl()
	//{
	// We only need to use safe_get in the deruction loop
	// We don't need to unload events
	// RecycleEvents(true /* useSafeGet */);
	//}

	private double RenderingRatingFontSize()
	{
		// MSFT #10030063 Replacing with Rating size DPs
		return m_scaledFontSizeForRendering;
	}

	private double ActualRatingFontSize()
	{
		return RenderingRatingFontSize() / 2;
	}

	// TODO MSFT #10030063: Convert to itemspacing DP
	private double ItemSpacing()
	{
		EnsureResourcesLoaded();

		return m_itemSpacing;
	}

	protected override void OnApplyTemplate()
	{
		RecycleEvents();

		// Retrieve pointers to stable controls 
		//IControlProtected thisAsControlProtected = this;

		if (GetTemplateChild<StackPanel>("CaptionStackPanel") is StackPanel captionStackPanel)
		{
			m_captionStackPanel = captionStackPanel;
		}

		var captionTextBlock = (TextBlock)GetTemplateChild("Caption");
		if (captionTextBlock != null)
		{
			m_captionTextBlock = captionTextBlock;
			captionTextBlock.SizeChanged += OnCaptionSizeChanged;
		}

		var backgroundStackPanel = (StackPanel)GetTemplateChild("RatingBackgroundStackPanel");
		if (backgroundStackPanel != null)
		{
			m_backgroundStackPanel = backgroundStackPanel;

			backgroundStackPanel.PointerCanceled += OnPointerCancelledBackgroundStackPanel;
			backgroundStackPanel.PointerCaptureLost += OnPointerCaptureLostBackgroundStackPanel;
			backgroundStackPanel.PointerMoved += OnPointerMovedOverBackgroundStackPanel;
			backgroundStackPanel.PointerEntered += OnPointerEnteredBackgroundStackPanel;
			backgroundStackPanel.PointerExited += OnPointerExitedBackgroundStackPanel;
			backgroundStackPanel.PointerPressed += OnPointerPressedBackgroundStackPanel;
			backgroundStackPanel.PointerReleased += OnPointerReleasedBackgroundStackPanel;
		}

		m_foregroundStackPanel = (StackPanel)GetTemplateChild("RatingForegroundStackPanel");

		m_backgroundStackPanelTranslateTransform = GetTemplateChild<TranslateTransform>("RatingBackgroundStackPanelTranslateTransform");
		m_foregroundStackPanelTranslateTransform = GetTemplateChild<TranslateTransform>("RatingForegroundStackPanelTranslateTransform");

		// FUTURE: Ideally these would be in template overrides:

		// IsFocusEngagementEnabled means the control has to be "engaged" with 
		// using the A button before it actually receives key input from gamepad.
		FocusEngaged += OnFocusEngaged;
		FocusDisengaged += OnFocusDisengaged;

		IsEnabledChanged += OnIsEnabledChanged;
		m_fontFamilyChangedToken = RegisterPropertyChangedCallback(Control.FontFamilyProperty, OnFontFamilyChanged);

		Visual visual = ElementCompositionPreview.GetElementVisual(this);
		Compositor comp = visual.Compositor;

		m_sharedPointerPropertySet = comp.CreatePropertySet();

		m_sharedPointerPropertySet.InsertScalar("starsScaleFocalPoint", c_noPointerOverMagicNumber);
		m_sharedPointerPropertySet.InsertScalar("pointerScalar", c_mouseOverScale);

		StampOutRatingItems();
	}

	private double CoerceValueBetweenMinAndMax(double value)
	{
		if (value < 0.0) // Force all negative values to the sentinel "unset" value.
		{
			value = c_noValueSetSentinel;
		}
		else if (value <= 1.0)
		{
			value = 1.0;
		}
		else if (value > MaxRating)
		{
			value = MaxRating;
		}

		return value;
	}

	// UIElement / UIElementOverridesHelper
	protected override AutomationPeer OnCreateAutomationPeer()
	{
		return new RatingControlAutomationPeer(this);
	}

	// private methods 

	// TODO: MSFT: call me when font size changes, and stuff like that, glyph, etc
	private void StampOutRatingItems()
	{
		if (m_backgroundStackPanel == null || m_foregroundStackPanel == null)
		{
			// OnApplyTemplate() hasn't executed yet, this is being called 
			// from a property value changed handler for markup set values.

			return;
		}


		// Before anything else, we need to retrieve the scaled font size.
		// Note that this is NOT the font size multiplied by the font scale factor,
		// as large font sizes are scaled less than small font sizes.
		// There isn't an API to retrieve this, so in lieu of that, we'll create a TextBlock
		// with the desired properties, measure it at infinity, and see what its desired width is.

		if (IsItemInfoPresentAndFontInfo())
		{
			EnsureResourcesLoaded();

			var textBlock = new TextBlock();
			textBlock.FontFamily = FontFamily;
			textBlock.Text = GetAppropriateGlyph(RatingControlStates.Set);
			textBlock.FontSize = m_fontSizeForRendering;
			textBlock.Measure(new(double.PositiveInfinity, double.PositiveInfinity));
			m_scaledFontSizeForRendering = textBlock.DesiredSize.Width;
		}
		else if (IsItemInfoPresentAndImageInfo())
		{
			// If we're using images rather than glyphs, then there's no text scaling
			// that will be happening.
			m_scaledFontSizeForRendering = m_fontSizeForRendering;
		}

		// Background initialization:

		m_backgroundStackPanel.Children.Clear();

		if (IsItemInfoPresentAndFontInfo())
		{
			PopulateStackPanelWithItems("BackgroundGlyphDefaultTemplate", m_backgroundStackPanel, RatingControlStates.Unset);
		}
		else if (IsItemInfoPresentAndImageInfo())
		{
			PopulateStackPanelWithItems("BackgroundImageDefaultTemplate", m_backgroundStackPanel, RatingControlStates.Unset);
		}

		// Foreground initialization:
		m_foregroundStackPanel.Children.Clear();
		if (IsItemInfoPresentAndFontInfo())
		{
			PopulateStackPanelWithItems("ForegroundGlyphDefaultTemplate", m_foregroundStackPanel, RatingControlStates.Set);
		}
		else if (IsItemInfoPresentAndImageInfo())
		{
			PopulateStackPanelWithItems("ForegroundImageDefaultTemplate", m_foregroundStackPanel, RatingControlStates.Set);
		}

		// The scale transform and margin cause the stars to be positioned at the top of the RatingControl.
		// We want them in the middle, so to achieve that, we'll additionally apply a y-transform that will
		// put the center of the stars in the center of the RatingControl.
		var yTranslation = (ActualHeight - ActualRatingFontSize()) / 2;

		if (m_backgroundStackPanelTranslateTransform is { } backgroundStackPanelTranslateTransform)
		{
			backgroundStackPanelTranslateTransform.Y = yTranslation;
		}

		if (m_foregroundStackPanelTranslateTransform is { } foregroundStackPanelTranslateTransform)
		{
			foregroundStackPanelTranslateTransform.Y = yTranslation;
		}

		// If we have at least one item, we'll use the first item of the foreground stack panel as a representative element to determine some values.
		if (MaxRating >= 1)
		{
			var firstItem = (UIElement)m_foregroundStackPanel.Children[0];
			firstItem.Measure(new(double.PositiveInfinity, double.PositiveInfinity));
			var defaultItemSpacing = firstItem.DesiredSize.Width - ActualRatingFontSize();
			var netItemSpacing = ItemSpacing() - defaultItemSpacing;

			// We want the caption to be a set distance away from the right-hand side of the last item,
			// so we'll give it a left margin that accounts for the built-in item spacing.
			if (m_captionTextBlock is { } captionTextBlock)
			{
				var margin = captionTextBlock.Margin;
				margin.Left = c_captionSpacing - defaultItemSpacing;
				captionTextBlock.Margin(margin);
			}

			// If we have at least two items, we'll need to apply the item spacing.
			// We'll calculate the default item spacing using the first item, and then
			// subtract it from the desired item spacing to get the Spacing property
			// to apply to the stack panels.
			if (MaxRating >= 2)
			{
				m_backgroundStackPanel.Spacing = netItemSpacing;
				m_foregroundStackPanel.Spacing = netItemSpacing;
			}
		}


		UpdateRatingItemsAppearance();
	}

	private void ReRenderCaption()
	{
		var captionTextBlock = m_captionTextBlock;
		if (captionTextBlock != null)
		{
			ResetControlSize();
		}
	}

	private void UpdateRatingItemsAppearance()
	{
		if (m_foregroundStackPanel != null)
		{
			// TODO: MSFT 11521414 - complete disabled state functionality

			double placeholderValue = PlaceholderValue;
			double ratingValue = Value;
			double value = 0.0;

			if (m_isPointerOver)
			{
				value = Math.Ceiling(m_mousePercentage * MaxRating);
				if (ratingValue == c_noValueSetSentinel)
				{
					if (placeholderValue == -1)
					{
						VisualStateManager.GoToState(this, "PointerOverPlaceholder", false);
						CustomizeStackPanel(m_foregroundStackPanel, RatingControlStates.PointerOverPlaceholder);
					}
					else
					{
						VisualStateManager.GoToState(this, "PointerOverUnselected", false);
						// The API is locked, so we can't change this part to be consistent any more:
						CustomizeStackPanel(m_foregroundStackPanel, RatingControlStates.PointerOverPlaceholder);
					}
				}
				else
				{
					VisualStateManager.GoToState(this, "PointerOverSet", false);
					CustomizeStackPanel(m_foregroundStackPanel, RatingControlStates.PointerOverSet);
				}
			}
			else if (ratingValue > c_noValueSetSentinel)
			{
				value = ratingValue;
				VisualStateManager.GoToState(this, "Set", false);
				CustomizeStackPanel(m_foregroundStackPanel, RatingControlStates.Set);
			}
			else if (placeholderValue > c_noValueSetSentinel)
			{
				value = placeholderValue;
				VisualStateManager.GoToState(this, "Placeholder", false);
				CustomizeStackPanel(m_foregroundStackPanel, RatingControlStates.Placeholder);
			} // there's no "unset" state because the foreground items are simply cropped out

			if (!IsEnabled)
			{
				// TODO: MSFT 11521414 - complete disabled state functionality [merge this code block with ifs above]
				VisualStateManager.GoToState(this, "Disabled", false);
				CustomizeStackPanel(m_foregroundStackPanel, RatingControlStates.Disabled);
			}

			uint i = 0;
			foreach (var uiElement in m_foregroundStackPanel.Children)
			{
				// Handle clips on stars
				var width = RenderingRatingFontSize();
				if ((double)i + 1 > value)
				{
					if (i < value)
					{
						// partial stars
						width *= (float)(value - Math.Floor(value));
					}
					else
					{
						// empty stars
						width = 0.0f;
					}
				}

				Rect rect = new Rect();
				rect.X = 0;
				rect.Y = 0;
				rect.Height = RenderingRatingFontSize();
				rect.Width = width;

				RectangleGeometry rg = new RectangleGeometry();
				rg.Rect = rect;
				uiElement.Clip = rg;

				i++;
			}

			ResetControlSize();
		}
	}

	private void ApplyScaleExpressionAnimation(UIElement uiElement, int starIndex)
	{
#if !__SKIA__ // TODO Uno: Expression animation is only supported on Skia
		var scaleTransform = new ScaleTransform()
		{
			ScaleX = 0.5,
			ScaleY = 0.5
		};
		uiElement.RenderTransform = scaleTransform;
		uiElement.RenderTransformOrigin = new Point(0.5, 0.5);
#else
		Visual uiElementVisual = ElementCompositionPreview.GetElementVisual(uiElement);
		Compositor comp = uiElementVisual.Compositor;

		// starsScaleFocalPoint is updated in OnPointerMovedOverBackgroundStackPanel.
		// This expression uses the horizontal delta between pointer position and star center to calculate the star scale.
		// Star gets larger when pointer is closer to its center, and gets smaller when pointer moves further away.
		ExpressionAnimation ea = comp.CreateExpressionAnimation(
		   "max( (-0.0005 * sharedPropertySet.pointerScalar * ((starCenterX - sharedPropertySet.starsScaleFocalPoint)*(starCenterX - sharedPropertySet.starsScaleFocalPoint))) + 1.0*sharedPropertySet.pointerScalar, 0.5)"
		);
		var starCenter = (float)(CalculateStarCenter(starIndex));
		ea.SetScalarParameter("starCenterX", starCenter);
		ea.SetReferenceParameter("sharedPropertySet", m_sharedPointerPropertySet);

		uiElementVisual.StartAnimation("Scale.X", ea);
		uiElementVisual.StartAnimation("Scale.Y", ea);

		EnsureResourcesLoaded();

		uiElementVisual.CenterPoint = new(c_scaleAnimationCenterPointXValue, c_scaleAnimationCenterPointYValue, 0.0f);
#endif
	}

	private void PopulateStackPanelWithItems(string templateName, StackPanel stackPanel, RatingControlStates state)
	{
		object lookup = Application.Current.Resources.Lookup(templateName);
		var dt = lookup as DataTemplate;

		EnsureResourcesLoaded();

		for (int i = 0; i < MaxRating; i++)
		{
			var ui = dt.LoadContent() as UIElement;
			if (ui != null)
			{
				CustomizeRatingItem(ui, state);
				stackPanel.Children.Add(ui);
				ApplyScaleExpressionAnimation(ui, i);
			}
		}
	}

	private void CustomizeRatingItem(UIElement ui, RatingControlStates type)
	{
		if (IsItemInfoPresentAndFontInfo())
		{
			var textBlock = ui as TextBlock;
			if (textBlock != null)
			{
				textBlock.FontFamily = FontFamily;
				textBlock.Text = GetAppropriateGlyph(type);
				textBlock.FontSize = m_fontSizeForRendering;
			}
		}
		else if (IsItemInfoPresentAndImageInfo())
		{
			var image = ui as Image;
			if (image != null)
			{
				image.Source = GetAppropriateImageSource(type);
				image.Width = m_fontSizeForRendering; // 
				image.Height = m_fontSizeForRendering; // MSFT #10030063 Replacing with Rating size DPs
			}
		}
		else
		{
			throw new InvalidOperationException("ItemInfo property is null");
		}

	}

	private void CustomizeStackPanel(StackPanel stackPanel, RatingControlStates state)
	{
		foreach (UIElement child in stackPanel.Children)
		{
			CustomizeRatingItem(child, state);
		}
	}

	private string GetAppropriateGlyph(RatingControlStates type)
	{
		if (!IsItemInfoPresentAndFontInfo())
		{
			throw new InvalidOperationException("Runtime error, tried to retrieve a glyph when the ItemInfo is not a RatingItemGlyphInfo");
		}

		RatingItemFontInfo rifi = ItemInfo as RatingItemFontInfo;

		switch (type)
		{
			case RatingControlStates.Disabled:
				return GetNextGlyphIfNull(rifi.DisabledGlyph, RatingControlStates.Set);
			case RatingControlStates.PointerOverSet:
				return GetNextGlyphIfNull(rifi.PointerOverGlyph, RatingControlStates.Set);
			case RatingControlStates.PointerOverPlaceholder:
				return GetNextGlyphIfNull(rifi.PointerOverPlaceholderGlyph, RatingControlStates.Placeholder);
			case RatingControlStates.Placeholder:
				return GetNextGlyphIfNull(rifi.PlaceholderGlyph, RatingControlStates.Set);
			case RatingControlStates.Unset:
				return GetNextGlyphIfNull(rifi.UnsetGlyph, RatingControlStates.Set);
			case RatingControlStates.Null:
				return null;
			default:
				return rifi.Glyph; // "Set" state
		}
	}

	private string GetNextGlyphIfNull(string glyph, RatingControlStates fallbackType)
	{
		if (string.IsNullOrEmpty(glyph))
		{
			if (fallbackType == RatingControlStates.Null)
			{
				return null;
			}
			return GetAppropriateGlyph(fallbackType);
		}
		return glyph;
	}

	private ImageSource GetAppropriateImageSource(RatingControlStates type)
	{
		if (!IsItemInfoPresentAndImageInfo())
		{
			throw new InvalidOperationException("Runtime error, tried to retrieve an image when the ItemInfo is not a RatingItemImageInfo");
		}

		RatingItemImageInfo imageInfo = ItemInfo as RatingItemImageInfo;

		switch (type)
		{
			case RatingControlStates.Disabled:
				return GetNextImageIfNull(imageInfo.DisabledImage, RatingControlStates.Set);
			case RatingControlStates.PointerOverSet:
				return GetNextImageIfNull(imageInfo.PointerOverImage, RatingControlStates.Set);
			case RatingControlStates.PointerOverPlaceholder:
				return GetNextImageIfNull(imageInfo.PointerOverPlaceholderImage, RatingControlStates.Placeholder);
			case RatingControlStates.Placeholder:
				return GetNextImageIfNull(imageInfo.PlaceholderImage, RatingControlStates.Set);
			case RatingControlStates.Unset:
				return GetNextImageIfNull(imageInfo.UnsetImage, RatingControlStates.Set);
			case RatingControlStates.Null:
				return null;
			default:
				return imageInfo.Image; // "Set" state
		}
	}

	private ImageSource GetNextImageIfNull(ImageSource image, RatingControlStates fallbackType)
	{
		if (image == null)
		{
			if (fallbackType == RatingControlStates.Null)
			{
				return null;
			}
			return GetAppropriateImageSource(fallbackType);
		}
		return image;
	}

	private void ResetControlSize()
	{
		Width = CalculateTotalRatingControlWidth();
		Height = m_fontSizeForRendering;
	}

	private void ChangeRatingBy(double change, bool originatedFromMouse)
	{
		if (change != 0.0)
		{
			double ratingValue = 0.0;
			double oldRatingValue = Value;
			if (oldRatingValue != c_noValueSetSentinel)
			{
				// If the Value was programmatically set to a fraction, drop that fraction before we modify it
				if ((int)Value != Value)
				{
					if (change == -1)
					{
						ratingValue = (int)Value;
					}
					else
					{
						ratingValue = (int)Value + change;
					}
				}
				else
				{
					oldRatingValue = ratingValue = oldRatingValue;
					ratingValue += change;
				}
			}
			else
			{
				ratingValue = InitialSetValue;
			}

			SetRatingTo(ratingValue, originatedFromMouse);
		}
	}

	private void SetRatingTo(double newRating, bool originatedFromMouse)
	{
		double ratingValue = 0.0;
		double oldRatingValue = Value;

		ratingValue = Math.Min(newRating, (double)(MaxRating));
		ratingValue = Math.Max(ratingValue, 0.0);

		// The base case, and the you have no rating, and you pressed left case [wherein nothing should happen]
		if (oldRatingValue > c_noValueSetSentinel || ratingValue != 0.0)
		{
			if (!IsClearEnabled && ratingValue <= 0.0)
			{
				Value = 1.0;
			}
			else if (ratingValue == oldRatingValue && IsClearEnabled && (ratingValue != MaxRating || originatedFromMouse))
			{
				// If you increase the Rating via the keyboard/gamepad when it's maxed, the value should stay stable.
				// But if you click a star that represents the current Rating value, it should clear the rating.

				Value = c_noValueSetSentinel;
			}
			else if (ratingValue > 0.0)
			{
				Value = ratingValue;
			}
			else
			{
				Value = c_noValueSetSentinel;
			}

			// Notify that the Value has changed
			ValueChanged?.Invoke(this, null);
		}
	}

	private void OnPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		var property = args.Property;
		// Do coercion first.
		if (property == MaxRatingProperty)
		{
			// Enforce minimum MaxRating
			var value = (int)args.NewValue;
			var coercedValue = Math.Max(1, value);

			if (Value > coercedValue)
			{
				Value = coercedValue;
			}

			if (PlaceholderValue > coercedValue)
			{
				PlaceholderValue = coercedValue;
			}

			if (coercedValue != value)
			{
				SetValue(property, coercedValue);
				return;
			}
		}
		else if (property == PlaceholderValueProperty || property == ValueProperty)
		{
			var value = (double)args.NewValue;
			var coercedValue = CoerceValueBetweenMinAndMax(value);
			if (value != coercedValue)
			{
				SetValue(property, coercedValue);
				// early return, we'll come back to handle the change to the corced value.
				return;
			}
		}

		// Property value changed handling.
		if (property == CaptionProperty)
		{
			OnCaptionChanged(args);
		}
		else if (property == InitialSetValueProperty)
		{
			OnInitialSetValueChanged(args);
		}
		else if (property == IsClearEnabledProperty)
		{
			OnIsClearEnabledChanged(args);
		}
		else if (property == IsReadOnlyProperty)
		{
			OnIsReadOnlyChanged(args);
		}
		else if (property == ItemInfoProperty)
		{
			OnItemInfoChanged(args);
		}
		else if (property == MaxRatingProperty)
		{
			OnMaxRatingChanged(args);
		}
		else if (property == PlaceholderValueProperty)
		{
			OnPlaceholderValueChanged(args);
		}
		else if (property == ValueProperty)
		{
			OnValueChanged(args);
		}
	}

	private void OnCaptionChanged(DependencyPropertyChangedEventArgs args)
	{
		ReRenderCaption();
	}

	private void OnFontFamilyChanged(DependencyObject sender, DependencyProperty args)
	{
		if (m_backgroundStackPanel != null) // We don't want to do this for the initial property set
		{
			for (int i = 0; i < MaxRating; i++)
			{
				// FUTURE: handle image rating items
				var backgroundTB = m_backgroundStackPanel.Children[i] as TextBlock;
				if (backgroundTB != null)
				{
					CustomizeRatingItem(backgroundTB, RatingControlStates.Unset);
				}

				var foregroundTB = m_foregroundStackPanel.Children[i] as TextBlock;
				if (foregroundTB != null)
				{
					CustomizeRatingItem(foregroundTB, RatingControlStates.Set);
				}
			}
		}

		UpdateRatingItemsAppearance();
	}

	void OnInitialSetValueChanged(DependencyPropertyChangedEventArgs args)
	{

	}

	void OnIsClearEnabledChanged(DependencyPropertyChangedEventArgs args)
	{

	}

	void OnIsReadOnlyChanged(DependencyPropertyChangedEventArgs args)
	{
		// TODO: MSFT Colour changes - see spec
	}

	private void OnItemInfoChanged(DependencyPropertyChangedEventArgs args)
	{
		bool changedType = false;

		if (ItemInfo == null)
		{
			m_infoType = RatingInfoType.None;
		}
		else if (ItemInfo is RatingItemFontInfo ratingItemFontInfo)
		{
			if (m_infoType != RatingInfoType.Font && m_backgroundStackPanel != null /* prevent calling StampOutRatingItems() twice at initialisation */)
			{
				m_infoType = RatingInfoType.Font;
				StampOutRatingItems();
				changedType = true;
			}
		}
		else
		{
			if (m_infoType != RatingInfoType.Image)
			{
				m_infoType = RatingInfoType.Image;
				StampOutRatingItems();
				changedType = true;
			}
		}

		// We don't want to do this for the initial property set
		// Or if we just stamped them out
		if (m_backgroundStackPanel != null && !changedType)
		{
			for (int i = 0; i < MaxRating; i++)
			{
				CustomizeRatingItem(m_backgroundStackPanel.Children[i], RatingControlStates.Unset);
				CustomizeRatingItem(m_foregroundStackPanel.Children[i], RatingControlStates.Set);
			}
		}

		UpdateRatingItemsAppearance();
	}

	private void OnMaxRatingChanged(DependencyPropertyChangedEventArgs args)
	{
		StampOutRatingItems();
	}

	private void OnPlaceholderValueChanged(DependencyPropertyChangedEventArgs args)
	{
		UpdateRatingItemsAppearance();
	}

	private void OnValueChanged(DependencyPropertyChangedEventArgs args)
	{
		// Fire property change for UIA
		AutomationPeer peer = FrameworkElementAutomationPeer.FromElement(this);
		if (peer != null)
		{
			var ratingPeer = peer as RatingControlAutomationPeer;
			ratingPeer.RaisePropertyChangedEvent(Value);
		}

		UpdateRatingItemsAppearance();
	}

	private void OnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs args)
	{
		// MSFT 11521414 TODO: change states (add a state)
		UpdateRatingItemsAppearance();
	}

	private void OnCaptionSizeChanged(object sender, SizeChangedEventArgs args)
	{
		// The caption's size changing means that the text scale factor has been updated and applied.
		// As such, we should re-run sizing and layout when this occurs.
		m_scaledFontSizeForRendering = -1;

		StampOutRatingItems();
		ResetControlSize();
	}

	private void OnPointerCancelledBackgroundStackPanel(object sender, PointerRoutedEventArgs args)
	{
		PointerExitedImpl(args);
	}

	private void OnPointerCaptureLostBackgroundStackPanel(object sender, PointerRoutedEventArgs args)
	{
		// We capture the pointer because we want to support the drag off the
		// left side to clear the rating scenario. However, this means that
		// when we simply click to set values - we get here, but we don't want
		// to reset the scaling on the stars underneath the pointer.
		PointerExitedImpl(args, false /* resetScaleAnimation */);
		m_hasPointerCapture = false;
	}

	private void OnPointerMovedOverBackgroundStackPanel(object sender, PointerRoutedEventArgs args)
	{
		if (!IsReadOnly)
		{
			if (m_backgroundStackPanel is { } backgroundStackPanel)
			{
				var point = args.GetCurrentPoint(backgroundStackPanel);
				var xPosition = point.Position.X;

				m_mousePercentage = (xPosition - m_firstItemOffset) / CalculateActualRatingWidth();

				UpdateRatingItemsAppearance();
				args.Handled = true;
			}
		}
	}

	private void OnPointerEnteredBackgroundStackPanel(object sender, PointerRoutedEventArgs args)
	{
		if (!IsReadOnly)
		{
			m_isPointerOver = true;

			if (m_backgroundStackPanel is { } backgroundStackPanel)
			{
				if (MaxRating >= 1)
				{
					var firstItem = (UIElement)backgroundStackPanel.Children[0];
					var firstItemOffsetPoint = firstItem.TransformToVisual(backgroundStackPanel).TransformPoint(new(0, 0));
					m_firstItemOffset = firstItemOffsetPoint.X;
				}
			}

			args.Handled = true;
		}
	}

	private void OnPointerExitedBackgroundStackPanel(object sender, PointerRoutedEventArgs args)
	{
		PointerExitedImpl(args);
	}

	private void PointerExitedImpl(PointerRoutedEventArgs args, bool resetScaleAnimation = true)
	{
		if (resetScaleAnimation)
		{
			m_isPointerOver = false;
		}

		if (!m_isPointerDown)
		{
			// Only clear pointer capture when not holding pointer down for drag left to clear value scenario.
			if (m_hasPointerCapture)
			{
				m_backgroundStackPanel.ReleasePointerCapture(args.Pointer);
				m_hasPointerCapture = false;
			}

			if (resetScaleAnimation)
			{
				m_sharedPointerPropertySet.InsertScalar("starsScaleFocalPoint", c_noPointerOverMagicNumber);
			}
			UpdateRatingItemsAppearance();
		}

		args.Handled = true;
	}

	private void OnPointerPressedBackgroundStackPanel(object sender, PointerRoutedEventArgs args)
	{
		if (!IsReadOnly)
		{
			m_isPointerDown = true;

			// We capture the pointer on pointer down because we want to support
			// the drag off the left side to clear the rating scenario.
			m_backgroundStackPanel.CapturePointer(args.Pointer);
			m_hasPointerCapture = true;
		}
	}

	private void OnPointerReleasedBackgroundStackPanel(object sender, PointerRoutedEventArgs args)
	{
		if (!IsReadOnly)
		{
			var point = args.GetCurrentPoint(m_backgroundStackPanel);
			var xPosition = point.Position.X;

			double mousePercentage = xPosition / CalculateActualRatingWidth();
			SetRatingTo(Math.Ceiling(mousePercentage * MaxRating), true);

			ElementSoundPlayer.Play(ElementSoundKind.Invoke);
		}

		if (m_isPointerDown)
		{
			m_isPointerDown = false;
			UpdateRatingItemsAppearance();
		}
	}

	private double CalculateTotalRatingControlWidth()
	{
		double totalWidth = CalculateActualRatingWidth();

		// If we have a non-empty caption, we also need to account for both its width and the spacing that comes before it.
		if (m_captionTextBlock is { } captionTextBlock)
		{
			var captionAsWinRT = (string)GetValue(CaptionProperty);

			if (!string.IsNullOrEmpty(captionAsWinRT))
			{
				totalWidth += c_captionSpacing + captionTextBlock.ActualWidth;
			}
		}

		return totalWidth;
	}

#if __SKIA__ // TODO Uno: Expression animation is only supported on Skia
	private double CalculateStarCenter(int starIndex)
	{
		// TODO: MSFT sub in real API DP values
		// MSFT #10030063
		// [real Rating Size * (starIndex + 0.5)] + (starIndex * itemSpacing)
		return (ActualRatingFontSize() * (starIndex + 0.5)) + (starIndex * ItemSpacing());
	}
#endif

	private double CalculateActualRatingWidth()
	{
		// TODO: MSFT replace hardcoding
		// MSFT #10030063
		// (max rating * rating size) + ((max rating - 1) * item spacing)
		return ((double)MaxRating * ActualRatingFontSize()) + (((double)MaxRating - 1) * ItemSpacing());
	}

	// IControlOverrides
	protected override void OnKeyDown(KeyRoutedEventArgs eventArgs)
	{
		if (eventArgs.Handled)
		{
			return;
		}

		if (!IsReadOnly)
		{
			bool handled = false;
			VirtualKey key = eventArgs.Key;

			double flowDirectionReverser = 1.0;

			if (FlowDirection == FlowDirection.RightToLeft)
			{
				flowDirectionReverser *= -1.0;
			}

			var originalKey = eventArgs.OriginalKey;

			// Up down are right/left in keyboard only
			if (originalKey == VirtualKey.Up)
			{
				key = VirtualKey.Right;
				flowDirectionReverser = 1.0;
			}
			else if (originalKey == VirtualKey.Down)
			{
				key = VirtualKey.Left;
				flowDirectionReverser = 1.0;
			}

			if (originalKey == VirtualKey.GamepadDPadLeft || originalKey == VirtualKey.GamepadDPadRight ||
				originalKey == VirtualKey.GamepadLeftThumbstickLeft || originalKey == VirtualKey.GamepadLeftThumbstickRight)
			{
				ElementSoundPlayer.Play(ElementSoundKind.Focus);
			}

			switch (key)
			{
				case VirtualKey.Left:
					ChangeRatingBy(-1.0 * flowDirectionReverser, false);
					handled = true;
					break;
				case VirtualKey.Right:
					ChangeRatingBy(1.0 * flowDirectionReverser, false);
					handled = true;
					break;
				case VirtualKey.Home:
					SetRatingTo(0.0, false);
					handled = true;
					break;
				case VirtualKey.End:
					SetRatingTo((double)(MaxRating), false);
					handled = true;
					break;
				default:
					break;
			}

			eventArgs.Handled = handled;
		}

		base.OnKeyDown(eventArgs);
	}

	// We use the same engagement model as Slider/sorta like ComboBox
	// Pressing GamepadA engages, pressing either GamepadA or GamepadB disengages,
	// where GamepadA commits the new value, and GamepadB discards and restores the old value.

	// The reason we do this in the OnPreviewKey* virtuals is we need
	// to beat the framework to handling this event. Because disengagement
	// happens on key down, and engagement happens on key up...
	// if we disengage on GamepadA, the framework would otherwise
	// automatically reengage us.

	// Order:
	// OnPreviewKey* virtuals
	// PreviewKey subscribed events
	// [regular key events]

	protected override void OnPreviewKeyDown(KeyRoutedEventArgs eventArgs)
	{
		if (eventArgs.Handled)
		{
			return;
		}

		if (!IsReadOnly && IsFocusEngaged && IsFocusEngagementEnabled)
		{
			var originalKey = eventArgs.OriginalKey;
			if (originalKey == VirtualKey.GamepadA)
			{
				m_shouldDiscardValue = false;
				m_preEngagementValue = -1.0;
				RemoveFocusEngagement();
				m_disengagedWithA = true;
				eventArgs.Handled = true;
			}
			else if (originalKey == VirtualKey.GamepadB)
			{
				bool valueChanged = false;
				m_shouldDiscardValue = false;

				if (Value != m_preEngagementValue)
				{
					valueChanged = true;
				}

				Value = m_preEngagementValue;

				if (valueChanged)
				{
					ValueChanged?.Invoke(this, null);
				}

				m_preEngagementValue = -1.0;
				RemoveFocusEngagement();
				eventArgs.Handled = true;
			}
		}
	}

	protected override void OnPreviewKeyUp(KeyRoutedEventArgs eventArgs)
	{
		var originalKey = eventArgs.OriginalKey;

		if (IsFocusEngagementEnabled && originalKey == VirtualKey.GamepadA && m_disengagedWithA)
		{
			// Block the re-engagement
			m_disengagedWithA = false; // We want to do this regardless of handled
			eventArgs.Handled = true;
		}
	}

	private void OnFocusEngaged(Control sender, FocusEngagedEventArgs args)
	{
		if (!IsReadOnly)
		{
			EnterGamepadEngagementMode();
		}
	}

	private void OnFocusDisengaged(Control sender, FocusDisengagedEventArgs args)
	{
		// Revert value:
		// for catching programmatic disengagements, gamepad ones are handled in OnPreviewKeyDown
		if (m_shouldDiscardValue)
		{
			bool valueChanged = false;

			if (Value != m_preEngagementValue)
			{
				valueChanged = true;
			}

			Value = m_preEngagementValue;
			m_preEngagementValue = -1.0f;

			if (valueChanged)
			{
				ValueChanged?.Invoke(this, null);
			}
		}

		ExitGamepadEngagementMode();
	}

	private void EnterGamepadEngagementMode()
	{
		double currentValue = Value;
		m_shouldDiscardValue = true;

		if (currentValue == c_noValueSetSentinel)
		{
			Value = InitialSetValue;
			// Notify that the Value has changed
			ValueChanged?.Invoke(this, null);
			currentValue = InitialSetValue;
			m_preEngagementValue = -1;
		}
		else
		{
			currentValue = Value;
			m_preEngagementValue = currentValue;
		}

		ElementSoundPlayer.Play(ElementSoundKind.Invoke);
	}

	private void ExitGamepadEngagementMode()
	{
		ElementSoundPlayer.Play(ElementSoundKind.GoBack);

		m_sharedPointerPropertySet.InsertScalar("starsScaleFocalPoint", c_noPointerOverMagicNumber);
		m_disengagedWithA = false;
	}

	private void RecycleEvents(bool useSafeGet = false)
	{
		if (m_backgroundStackPanel != null)
		{
			m_backgroundStackPanel.PointerCanceled -= OnPointerCancelledBackgroundStackPanel;
			m_backgroundStackPanel.PointerCaptureLost -= OnPointerCaptureLostBackgroundStackPanel;
			m_backgroundStackPanel.PointerMoved -= OnPointerMovedOverBackgroundStackPanel;
			m_backgroundStackPanel.PointerEntered -= OnPointerEnteredBackgroundStackPanel;
			m_backgroundStackPanel.PointerExited -= OnPointerExitedBackgroundStackPanel;
			m_backgroundStackPanel.PointerPressed -= OnPointerPressedBackgroundStackPanel;
			m_backgroundStackPanel.PointerReleased -= OnPointerReleasedBackgroundStackPanel;
		}

		if (m_captionTextBlock != null)
		{
			m_captionTextBlock.SizeChanged -= OnCaptionSizeChanged;
		}

		UnregisterPropertyChangedCallback(Control.FontFamilyProperty, m_fontFamilyChangedToken);

		IsEnabledChanged -= OnIsEnabledChanged;
	}

	private void EnsureResourcesLoaded()
	{
		if (!m_resourcesLoaded)
		{
			var fontSizeForRenderingKey = c_fontSizeForRenderingKey;
			var itemSpacingKey = c_itemSpacingKey;
			var captionTopMarginKey = c_captionTopMarginKey;

			if (Resources.HasKey(fontSizeForRenderingKey))
			{
				m_fontSizeForRendering = (double)Resources.Lookup(fontSizeForRenderingKey);
			}
			else if (Application.Current.Resources.HasKey(fontSizeForRenderingKey))
			{
				m_fontSizeForRendering = (double)Application.Current.Resources.Lookup(fontSizeForRenderingKey);
			}
			else
			{
				m_fontSizeForRendering = c_defaultFontSizeForRendering;
			}

			if (Resources.HasKey(itemSpacingKey))
			{
				m_itemSpacing = (double)Resources.Lookup(itemSpacingKey);
			}
			else if (Application.Current.Resources.HasKey(itemSpacingKey))
			{
				m_itemSpacing = (double)Application.Current.Resources.Lookup(itemSpacingKey);
			}
			else
			{
				m_itemSpacing = c_defaultItemSpacing;
			}

			if (Resources.HasKey(captionTopMarginKey))
			{
				m_captionTopMargin = (double)Resources.Lookup(captionTopMarginKey);
			}
			else if (Application.Current.Resources.HasKey(captionTopMarginKey))
			{
				m_captionTopMargin = (double)Application.Current.Resources.Lookup(captionTopMarginKey);
			}
			else
			{
				m_captionTopMargin = c_defaultCaptionTopMargin;
			}

			m_resourcesLoaded = true;
		}
	}
}
