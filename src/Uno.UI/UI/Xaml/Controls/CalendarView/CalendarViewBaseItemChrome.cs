// CopyRight (c) Microsoft Corporation. All Rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.


// Work around disruptive max/min macros
#undef max
#undef min

using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Media;
using DirectUI;
using CCalendarViewBaseItemChrome = Windows.UI.Xaml.Controls.CalendarViewBaseItem;
using DateTime = Windows.Foundation.WindowsFoundationDateTime;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Core.Scaling;

namespace Windows.UI.Xaml.Controls
{
	partial class CalendarViewBaseItem
	{
		//private const float InScopeDensityBarOpacity = 0.35f;
		//private const float OutOfScopeDensityBarOpacity = 0.10f;
		//private const float MaxDensityBarHeight = 5.0f;
		private const float TodayBlackouTopacity = 0.40f;
		//private const float FocusBorderThickness = 2.0f;
		//private const float TodaySelectedInnerBorderThickness = 2.0f;

		private interface IContentRenderer
		{
			void RenderFocusRectangle(CalendarViewBaseItem calendarViewBaseItem, FocusRectangleOptions focusOptions);

			void GeneralImageRenderContent(Rect bounds, Rect rect, Brush pBrush, BrushParams emptyBrushParams, CalendarViewBaseItem calendarViewBaseItem, object o, object o1, bool b);
		}

		private protected struct TextBlockFontProperties
		{
			public float fontSize;
			public FontStyle fontStyle;
			public FontWeight fontWeight;
			public FontFamily pFontFamilyNoRef;
		}

		private protected struct TextBlockAlignments
		{
			public HorizontalAlignment horizontalAlignment;
			public VerticalAlignment verticalAlignment;
		}

		private TextBlock m_pMainTextBlock;
		private TextBlock m_pLabelTextBlock;

		private WeakReference<CalendarView> m_wrOwner;

		private const int s_maxNumberOfDensityBars = 10;

		private Color[] m_densityBarColors = new Color[s_maxNumberOfDensityBars];
		private uint m_numberOfDensityBar;

		private protected bool m_isToday;
		private protected bool m_isKeyboardFocused;
		private protected bool m_isSelected;
		private protected bool m_isBlackout;
		private protected bool m_isHovered;
		private protected bool m_isPressed;
		private protected bool m_isOutOfScope;
		private protected bool m_hasLabel;

		private void Initialize_CalendarViewBaseItemChrome()
		{
			m_pMainTextBlock = null;
			m_pLabelTextBlock = null;
			m_isToday = false;
			m_isKeyboardFocused = false;
			m_isSelected = false;
			m_isBlackout = false;
			m_isHovered = false;
			m_isPressed = false;
			m_isOutOfScope = false;
			m_numberOfDensityBar = 0;
			m_hasLabel = false;
		}

		private bool HasTemplateChild()
		{
			return GetFirstChildNoAddRef() != null;
		}

		// TODO UNO
		//private void AddTemplateChild(UIElement pUI)
		//{
		//	global::System.Diagnostics.Debug.Assert(GetFirstChildNoAddRef() == null);

		//	return InsertChild(0, pUI);
		//}

#if false
		private void RemoveTemplateChild()
		{
			UIElement pTemplateChild = GetFirstChildNoAddRef();

			if (pTemplateChild != null)
			{
				RemoveChild(pTemplateChild);
			}
		}
#endif

		private UIElement GetFirstChildNoAddRef() => GetFirstChild();

		private UIElement GetFirstChild()
		{
			UIElement spFirstChild;
			// added in UIElement.GetFirstChild()
			spFirstChild = VisualTreeHelper.GetChild(this, 0) as UIElement;

			// We overrode HasTemplateChild and AddTemplateChild to make sure
			// the template child (if exists) will be always at index 0.
			// So here if the first child is our internal textblocks, it means
			// we don't have a template child.
			if (spFirstChild != m_pMainTextBlock &&
				spFirstChild != m_pLabelTextBlock)
			{
				// keep added, it's the caller's responsibility to release ref.
				return spFirstChild;
			}
			else
			{
				return null;
			}
		}

		internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
		{
			if (args.Property == Control.TemplateProperty)
			{
				InvalidateRender();
			}

			// Uno only
			if (args.Property == Control.BackgroundProperty)
			{
				InvalidateRender();
			}

			base.OnPropertyChanged2(args);
		}

		// TODO UNO
		//private void HitTestLocalInternal(
		//	Point target,
		//	out bool pHit)
		//{
		//	if (GetContext().InvisibleHitTestMode())
		//	{
		//		// Normally we only get here if we passed all bounds checks, but this is not the case if InvisibleHitTestMode is
		//		// enabled. Here we're hit testing invisible elements, which skips all bounds checks. We don't want to blindly
		//		// return true here, because that would mean that this element will always be hit under InvisibleHitTestMode,
		//		// regardless of the incoming point. Instead, make an explicit check against this element's layout size.
		//		//
		//		// InvisibleHitTestMode is used by FindElementsInHostCoordinates with includeAllElements set to true, which the
		//		// VS designer uses to hit test elements that are collapsed or have no background.
		//		Rect layoutSize = new Rect(0, 0, GetActualWidth(), GetActualHeight());
		//		pHit = layoutSize.Contains(target);
		//	}
		//	else
		//	{
		//		// All hits within the hit test bounds count as a hit.
		//		pHit = true;
		//	}

		//	return;
		//}

		//private void HitTestLocalInternal(
		//	HitTestPolygon target,
		//	out bool pHit)
		//{
		//	if (GetContext().InvisibleHitTestMode())
		//	{
		//		// Normally we only get here if we passed all bounds checks, but this is not the case if InvisibleHitTestMode is
		//		// enabled. Here we're hit testing invisible elements, which skips all bounds checks. We don't want to blindly
		//		// return true here, because that would mean that this element will always be hit under InvisibleHitTestMode,
		//		// regardless of the incoming point. Instead, make an explicit check against this element's layout size.
		//		//
		//		// InvisibleHitTestMode is used by FindElementsInHostCoordinates with includeAllElements set to true, which the
		//		// VS designer uses to hit test elements that are collapsed or have no background.
		//		Rect layoutSize = new Rect(0, 0, GetActualWidth(), GetActualHeight());
		//		pHit = target.IntersectsRect(layoutSize);
		//	}
		//	else
		//	{
		//		// All hits within the hit test bounds count as a hit.
		//		pHit = true;
		//	}

		//	return;
		//}

#if false
		private void GenerateContentBounds(
			out Rect pBounds)
		{
			pBounds = default;
			pBounds.X = 0.0f;
			pBounds.Y = 0.0f;
			pBounds.Width = GetActualWidth();
			pBounds.Height = GetActualHeight();

			return;
		}
#endif

		protected override Size MeasureOverride(
			Size availableSize)
		{
			Size desiredSize = default;
			Thickness borderThickness = GetItemBorderThickness();
			Thickness padding = Padding;

			if (ShouldUseLayoutRounding())
			{
				borderThickness.Left = LayoutRound(borderThickness.Left);
				borderThickness.Right = LayoutRound(borderThickness.Right);
				borderThickness.Top = LayoutRound(borderThickness.Top);
				borderThickness.Bottom = LayoutRound(borderThickness.Bottom);

				padding.Left = LayoutRound(padding.Left);
				padding.Right = LayoutRound(padding.Right);
				padding.Top = LayoutRound(padding.Top);
				padding.Bottom = LayoutRound(padding.Bottom);
			}

			CSizeUtil.Deflate(ref availableSize, borderThickness);
			CSizeUtil.Deflate(ref availableSize, padding);

			// Uno workaround
			Uno_MeasureChrome(availableSize);

			// TODO UNO
			//if (IsRoundedCalendarViewBaseItemChromeEnabled())
			//{
			//	if (m_outerBorder)
			//	{
			//		IFC_RETURN(m_outerBorder->Measure(availableSize));
			//	}

			//	if (m_innerBorder)
			//	{
			//		IFC_RETURN(m_innerBorder->Measure(availableSize));
			//	}

			//	if (m_strikethroughLine)
			//	{
			//		IFC_RETURN(m_strikethroughLine->Measure(availableSize));
			//	}
			//}

			if (m_pMainTextBlock is { })
			{
				m_pMainTextBlock.Measure(availableSize);
				desiredSize = m_pMainTextBlock.DesiredSize;
			}

			if (m_pLabelTextBlock is { })
			{
				m_pLabelTextBlock.Measure(availableSize);
			}

			// Get the child to measure it - if any.
			UIElement pChildNoRef = GetFirstChildNoAddRef();

			//If we have a child
			if (pChildNoRef is { })
			{
				pChildNoRef.Measure(availableSize);
				// TODO UNO
				//pChildNoRef.EnsureLayoutStorage();

				desiredSize.Width = Math.Max(pChildNoRef.DesiredSize.Width, desiredSize.Width);
				desiredSize.Height = Math.Max(pChildNoRef.DesiredSize.Height, desiredSize.Height);
			}

			CSizeUtil.Inflate(ref desiredSize, borderThickness);
			CSizeUtil.Inflate(ref desiredSize, padding);

			return desiredSize;
		}

		protected override Size ArrangeOverride(
			Size finalSize)
		{
			Rect finalBounds = new Rect(0.0f, 0.0f, finalSize.Width, finalSize.Height);

			// TODO UNO
			//if (m_outerBorder)
			//{
			//	ASSERT(IsRoundedCalendarViewBaseItemChromeEnabled());

			//	IFC_RETURN(m_outerBorder->Arrange(finalBounds));
			//}

			//if (m_innerBorder)
			//{
			//	ASSERT(IsRoundedCalendarViewBaseItemChromeEnabled());

			//	IFC_RETURN(m_innerBorder->Arrange(finalBounds));
			//}

			//if (m_strikethroughLine)
			//{
			//	ASSERT(IsRoundedCalendarViewBaseItemChromeEnabled());

			//	IFC_RETURN(m_strikethroughLine->Arrange(finalBounds));
			//}

			Thickness borderThickness = GetItemBorderThickness();
			Thickness padding = Padding;

			CSizeUtil.Deflate(ref finalBounds, borderThickness);
			CSizeUtil.Deflate(ref finalBounds, padding);

			if (ShouldUseLayoutRounding())
			{
				borderThickness.Left = LayoutRound(borderThickness.Left);
				borderThickness.Right = LayoutRound(borderThickness.Right);
				borderThickness.Top = LayoutRound(borderThickness.Top);
				borderThickness.Bottom = LayoutRound(borderThickness.Bottom);

				padding.Left = LayoutRound(padding.Left);
				padding.Right = LayoutRound(padding.Right);
				padding.Top = LayoutRound(padding.Top);
				padding.Bottom = LayoutRound(padding.Bottom);
			}

			// Uno workaround
			Uno_ArrangeChrome(finalBounds);

			if (m_pMainTextBlock is { })
			{
				m_pMainTextBlock.Arrange(finalBounds);
			}

			if (m_pLabelTextBlock is { })
			{
				m_pLabelTextBlock.Arrange(finalBounds);
			}

			UIElement pChildNoRef = GetFirstChildNoAddRef();

			if (pChildNoRef is { })
			{
				pChildNoRef.Arrange(finalBounds);
			}

			return finalSize;
		}

		private void CreateTextBlock(
			ref TextBlock spTextBlock)
		{
			object value;

			spTextBlock = new TextBlock();

			spTextBlock.SetValue(UIElement.IsHitTestVisibleProperty, false);

			value = TextWrapping.NoWrap;
			spTextBlock.SetValue(TextBlock.TextWrappingProperty, value);

			value = AccessibilityView.Raw;
			spTextBlock.SetValue(AutomationProperties.AccessibilityViewProperty, value);

			value = Visibility.Visible;
			spTextBlock.SetValue(UIElement.VisibilityProperty, value);

		}

		// Sets up and adds the main and label block to the tree.
		private void EnsureTextBlock(ref TextBlock spTextBlock)
		{
			//UIElementCollection pChildrenCollectionNoRef = null;

			if (spTextBlock is null)
			{
				//object value;

				// TODO UNO
				//value = GetValue(UIElement.ChildrenInternalProperty);
				//DoPointerCast(pChildrenCollectionNoRef, value);

				CreateTextBlock(ref spTextBlock);
				AddChild(spTextBlock);
				// TODO UNO
				//pChildrenCollectionNoRef.Append((spTextBlock));

				UpdateTextBlockForeground(spTextBlock);
				UpdateTextBlockForegroundOpacity(spTextBlock);
				UpdateTextBlockFontProperties(spTextBlock);
				UpdateTextBlockAlignments(spTextBlock);
			}
		}

		private bool IsLabel(TextBlock pTextBlock)
		{
			global::System.Diagnostics.Debug.Assert(pTextBlock is { } && (pTextBlock == m_pLabelTextBlock || pTextBlock == m_pMainTextBlock), "the textblock should be main textblock or label textblock.");

			return pTextBlock == m_pLabelTextBlock;
		}

#if false
		private void RenderChrome(
			IContentRenderer pContentRenderer,
			CalendarViewBaseItemChromeLayerPosition layer
		)
		{
			Rect bounds = new Rect(0.0f, 0.0f, GetActualWidth(), GetActualHeight());

			if (ShouldUseLayoutRounding())
			{
				bounds.Width = LayoutRound(bounds.Width);
				bounds.Height = LayoutRound(bounds.Height);
			}

			// we draw backgrounds in Pre phase
			if (layer == CalendarViewBaseItemChromeLayerPosition.Pre)
			{
				// those background properties on CalendarView, which apply to all calendarviewbaseitems.
				// e.g. CalendarViewTodayBackground, CalendarViewOutOfScopeBackground...
				DrawBackground(pContentRenderer, bounds);

				// the Control Background on CalendarViewBaseItem, which applys to this item only.
				// this is drawn on Top of above Background so developer could customize a "SelectedBackground"
				// (which we don't support yet)
				DrawControlBackground(pContentRenderer, bounds);
			}

			// to make sure densitybars layer appears after template child (if any) and before
			// the main and label textblocks, we draw densitybars in TemplateChild_Post phase,
			// if there is no template child, then draw densitybars in Pre phase
			if (layer == CalendarViewBaseItemChromeLayerPosition.TemplateChild_Post ||
				(layer == CalendarViewBaseItemChromeLayerPosition.Pre && GetFirstChildNoAddRef() == null))
			{
				DrawDensityBar(pContentRenderer, bounds);
			}

			// we draw borders in Post phase.
			if (layer == CalendarViewBaseItemChromeLayerPosition.Post)
			{
				bool shouldDrawDottedLines = false;
				shouldDrawDottedLines = ShouldDrawDottedLinesFocusVisual();
				if (m_isKeyboardFocused && shouldDrawDottedLines)
				{
					DrawFocusBorder(pContentRenderer, bounds);
				}
				else
				{
					DrawBorder(pContentRenderer, bounds);
				}

				// only for Today+Selected state, we draw an additional inner border.
				if (m_isToday && m_isSelected)
				{
					var innerBorder = bounds;
					CSizeUtil.Deflate(ref innerBorder, GetItemBorderThickness());
					DrawInnerBorder(pContentRenderer, innerBorder);
				}
			}

		}

		private void RenderDensityBars(
			IContentRenderer pContentRenderer
		)
		{
			// potential bug if we create an LTE to the template child, the density bars will
			// be rendered on the LTE as well.
			RenderChrome(
				pContentRenderer,
				CalendarViewBaseItemChromeLayerPosition.TemplateChild_Post
			);

			return;
		}
#endif

		private bool ShouldUseLayoutRounding()
		{
			// Similar to what Borders do, but we don't care about corner radius (ours is always 0).
			var scale = RootScale.GetRasterizationScaleForElement(this);
			return (scale != 1.0f) && GetUseLayoutRounding();
		}

#if false
		private uint GetIntValueOfColor(Color color)
		{
			return ((uint)color.A << 24) | ((uint)color.R << 16) | ((uint)color.G << 8) | (uint)color.B;
		}

		private void DrawDensityBar(
			IContentRenderer pContentRenderer,
			Rect bounds
		)
		{
			// TODO UNO
			//var owner = GetOwner();
			//if (m_numberOfDensityBar > 0 && owner is {})
			//{
			//	HWRenderParams rp = *(pContentRenderer.GetRenderParams());
			//	rp = HWRenderParamsOverride hwrpOverride(pContentRenderer);
			//	rp.opacityToCompNode *= m_isOutOfScope ? OutOfScopeDensityBarOpacity : InScopeDensityBarOpacity;

			//	// density bar bounds:
			//	//   Height: itemHeight / 10 - 1, max 5px
			//	//   Width: itemWidth - 2, centered
			//	// 1 px between bars.

			//	Rect densityBarBounds = bounds;
			//	densityBarBounds.Height = densityBarBounds.Height / s_maxNumberOfDensityBars - 1;
			//	densityBarBounds.Height = Math.Min(densityBarBounds.Height, MaxDensityBarHeight);
			//	densityBarBounds.Width = bounds.Width - 2;
			//	densityBarBounds.X = 1;

			//	densityBarBounds.Y = bounds.Height - densityBarBounds.Height;

			//	for (uint i = 0; i < m_numberOfDensityBar; ++i)
			//	{
			//		Brush pBrush = null;

			//		pBrush = owner.GetBrushNoRef(GetIntValueOfColor(m_densityBarColors[i]));

			//		BrushParams emptyBrushParams = default;
			//		pContentRenderer.GeneralImageRenderContent(
			//			densityBarBounds,
			//			densityBarBounds,
			//			pBrush,
			//			emptyBrushParams,
			//			this,
			//			null, // pNinegrid
			//			null, // pShapeHwTexture
			//			false); // fIsHollow

			//		densityBarBounds.Y -= densityBarBounds.Height + 1;
			//	}
			//}

			return;
		}

		private void DrawBorder(
			IContentRenderer pContentRenderer,
			Brush pBrush,
			Rect bounds,
			Thickness pNinegrid,
			bool isHollow

		)
		{
			BrushParams emptyBrushParams = default;
			pContentRenderer.GeneralImageRenderContent(
				bounds,
				bounds,
				pBrush,
				emptyBrushParams,
				this,
				pNinegrid, // pNinegrid
				null, // pShapeHwTexture
				isHollow); // fIsHollow

			return;
		}

		private void DrawBorder(
			IContentRenderer pContentRenderer,
			Rect bounds
		)
		{
			Thickness thickness = GetItemBorderThickness();
			Brush pBrush = GetItemBorderBrush(false /* forFocus */);

			if (pBrush is { } && thickness.Bottom != 0 && thickness.Top != 0 && thickness.Left != 0 && thickness.Right != 0)
			{
				DrawBorder(pContentRenderer, pBrush, bounds, thickness /* ninegrid */, true /* isHollow */);
			}

			return;
		}

		// for Selected+Today inner border.
		private void DrawInnerBorder(
			IContentRenderer pContentRenderer,
			Rect bounds
		)
		{
			// TODO UNO
			//Thickness thickness = new Thickness(
			//	TodaySelectedInnerBorderThickness, TodaySelectedInnerBorderThickness, TodaySelectedInnerBorderThickness, TodaySelectedInnerBorderThickness
			//);
			//Brush pBrush = GetItemInnerBorderBrush();
			//if (pBrush is {})
			//{
			//	CMILMatrix brushToElement(true);
			//	HWRenderParams & rp = *(pContentRenderer.GetRenderParams());
			//	HWRenderParams localRP = rp;
			//	localRP = HWRenderParamsOverride hwrpOverride(pContentRenderer);
			//	TransformAndClipStack transformsAndClips;
			//	Rect localBounds = new Rect(0.0f, 0.0f, bounds.Width, bounds.Height);

			//	if (bounds.X != 0.0f ||
			//		bounds.Y != 0.0f)
			//	{
			//		// Borders are ninegrids which need to have (X, Y) = (0, 0).
			//		// Apply transform in case if it is not aligned at the origin.
			//		transformsAndClips.Set(rp.pTransformsAndClipsToCompNode);
			//		brushToElement.AppendTranslation(bounds.X, bounds.Y);
			//		transformsAndClips.PrependTransform(brushToElement);
			//		localRP.pTransformsAndClipsToCompNode = &transformsAndClips;
			//	}

			//	DrawBorder(pContentRenderer, pBrush, localBounds, thickness /* ninegrid */, true /* isHollow */);
			//}

			return;
		}

		private void DrawFocusBorder(
			IContentRenderer pContentRenderer,
			Rect bounds
		)
		{
			FocusRectangleOptions focusOptions = default;
			Brush pFocusBrush = GetItemBorderBrush(true /* forFocus */);
			Brush pFocusAltBrush = GetItemFocusAltBorderBrush();

			focusOptions.firstThickness = new Thickness(FocusBorderThickness);

			focusOptions.drawFirst = true;
			focusOptions.firstBrush = (SolidColorBrush)(pFocusBrush);
			focusOptions.drawSecond = true; // always use the default CalendarItemBackground even if the item has another background (e.g. TodayBackground).
			focusOptions.secondBrush = (SolidColorBrush)(pFocusAltBrush);

			pContentRenderer.RenderFocusRectangle(
				this,
				focusOptions
			);

			return;
		}

		private void DrawBackground(
			IContentRenderer pContentRenderer,
			Rect bounds
		)
		{
			Brush pBrush = GetItemBackgroundBrush();
			if (pBrush is { })
			{
				BrushParams emptyBrushParams = default;
				pContentRenderer.GeneralImageRenderContent(
					bounds,
					bounds,
					pBrush,
					emptyBrushParams,
					this,
					null, // pNinegrid
					null, // pShapeHwTexture
					false); // fIsHollow
			}

			return;
		}

		private void DrawControlBackground(
			IContentRenderer pContentRenderer,
			Rect bounds
		)
		{
			object cVal;

			cVal = GetValue(Control.BackgroundProperty);

			//global::System.Diagnostics.Debug.Assert(cVal.GetType() == ValueType.valueObject);
			var pBrush = (Brush)(cVal);

			if (pBrush is { })
			{
				BrushParams emptyBrushParams = default;
				pContentRenderer.GeneralImageRenderContent(
					bounds,
					bounds,
					pBrush,
					emptyBrushParams,
					this,
					null, // pNinegrid
					null, // pShapeHwTexture
					false); // fIsHollow
			}

			return;
		}
#endif

		private void SetOwner(CalendarView pOwner)
		{
			global::System.Diagnostics.Debug.Assert(!(m_wrOwner?.TryGetTarget(out _) ?? false));
			m_wrOwner = new WeakReference<CalendarView>(pOwner);
		}

		private protected CalendarView GetOwner()
		{
			m_wrOwner.TryGetTarget(out var target);
			return target;
		}

		internal void SetDensityColors(
			IIterable<Color> pColors)
		{
			if (pColors is { })
			{
				IIterator<Color> spIterator;
				uint index = 0;
				bool hasCurrent = false;

				spIterator = pColors.GetIterator();

				hasCurrent = spIterator.HasCurrent;

				while (hasCurrent && index < s_maxNumberOfDensityBars)
				{
					m_densityBarColors[index] = spIterator.Current;
					hasCurrent = spIterator.MoveNext();
					++index;
				}

				m_numberOfDensityBar = index;
			}
			else
			{
				m_numberOfDensityBar = 0;
			}

			InvalidateRender();

		}

		// Today state affects text foreground, opacity, borderbrush and FontWeight
		internal void SetIsToday(bool state)
		{
			if (m_isToday != state)
			{
				m_isToday = state;

				UpdateTextBlocksForeground();
				UpdateTextBlocksForegroundOpacity();
				UpdateTextBlocksFontProperties();

				InvalidateRender();
			}

		}

		// Focus state affects background and border brush.
		internal void SetIsKeyboardFocused(bool state)
		{
			if (m_isKeyboardFocused != state)
			{
				m_isKeyboardFocused = state;
				InvalidateRender();
			}

			return;
		}

		// Selection state affects background, foreground and forground.
		internal void SetIsSelected(bool state)
		{
			if (m_isSelected != state)
			{
				m_isSelected = state;
				UpdateTextBlocksForeground();
				UpdateTextBlocksForegroundOpacity();
				InvalidateRender();
			}

		}

		// Blackout state affects background, foreground and forground.
		internal void SetIsBlackout(bool state)
		{
			if (m_isBlackout != state)
			{
				m_isBlackout = state;
				UpdateTextBlocksForeground();
				UpdateTextBlocksForegroundOpacity();
				InvalidateRender();
			}

		}

		// Hover state affects border brush and background.
		private void SetIsHovered(bool state)
		{

			if (m_isHovered != state)
			{
				m_isHovered = state;

				InvalidateRender();
			}

			return;
		}

		// Pressed state affects background, forground and border Brush.
		private void SetIsPressed(bool state)
		{
			if (m_isPressed != state)
			{
				m_isPressed = state;
				UpdateTextBlocksForeground();

				InvalidateRender();
			}

		}

		// Scope state affects foreground and background.
		internal void SetIsOutOfScope(bool state)
		{
			if (m_isOutOfScope != state)
			{
				m_isOutOfScope = state;
				UpdateTextBlocksForeground();

				InvalidateRender();
			}

		}

		private void UpdateTextBlocksForeground()
		{
			if (m_pMainTextBlock is { })
			{
				UpdateTextBlockForeground(m_pMainTextBlock);
			}

			if (m_pLabelTextBlock is { })
			{
				UpdateTextBlockForeground(m_pLabelTextBlock);
			}

			return;
		}

		private void UpdateTextBlocksForegroundOpacity()
		{
			if (m_pMainTextBlock is { })
			{
				UpdateTextBlockForegroundOpacity(m_pMainTextBlock);
			}

			if (m_pLabelTextBlock is { })
			{
				UpdateTextBlockForegroundOpacity(m_pLabelTextBlock);
			}

			return;
		}


		internal void UpdateTextBlocksFontProperties()
		{
			if (m_pMainTextBlock is { })
			{
				UpdateTextBlockFontProperties(m_pMainTextBlock);
			}

			if (m_pLabelTextBlock is { })
			{
				UpdateTextBlockFontProperties(m_pLabelTextBlock);
			}

			return;
		}

		private void UpdateTextBlocksAlignments()
		{
			if (m_pMainTextBlock is { })
			{
				UpdateTextBlockAlignments(m_pMainTextBlock);
			}

			if (m_pLabelTextBlock is { })
			{
				UpdateTextBlockAlignments(m_pLabelTextBlock);
			}

			return;
		}

		private void UpdateTextBlockForeground(TextBlock pTextBlock)
		{
			Brush pBrush = GetTextBlockForeground();

			global::System.Diagnostics.Debug.Assert(pTextBlock is { });

			pTextBlock.SetValue(TextBlock.ForegroundProperty, pBrush);

			return;
		}

		private void UpdateTextBlockForegroundOpacity(TextBlock pTextBlock)
		{
			double opacity = GetTextBlockForegroundOpacity();

			global::System.Diagnostics.Debug.Assert(pTextBlock is { });

			if (IsLabel(pTextBlock) && !m_hasLabel)
			{
				opacity = 0.0f;
			}

			pTextBlock.SetValue(UIElement.OpacityProperty, opacity);

		}

		internal void UpdateTextBlockFontProperties(TextBlock pTextBlock)
		{
			TextBlockFontProperties properties;

			global::System.Diagnostics.Debug.Assert(pTextBlock is { });

			if (GetTextBlockFontProperties(
				IsLabel(pTextBlock),
				out properties))
			{
				object value;

				pTextBlock.SetValue(TextBlock.FontSizeProperty, (double)properties.fontSize);

				value = properties.fontStyle;
				pTextBlock.SetValue(TextBlock.FontStyleProperty, (FontStyle)value);

				value = properties.fontWeight;
				pTextBlock.SetValue(TextBlock.FontWeightProperty, (FontWeight)value);

				pTextBlock.SetValue(TextBlock.FontFamilyProperty, (FontFamily)properties.pFontFamilyNoRef);

			}

		}

		private void UpdateTextBlockAlignments(TextBlock pTextBlock)
		{
			TextBlockAlignments alignments;

			global::System.Diagnostics.Debug.Assert(pTextBlock is { });

			if (GetTextBlockAlignments(
				IsLabel(pTextBlock),
				out alignments))
			{
				object value;

				value = alignments.horizontalAlignment;
				pTextBlock.SetValue(FrameworkElement.HorizontalAlignmentProperty, value);

				value = alignments.verticalAlignment;
				pTextBlock.SetValue(FrameworkElement.VerticalAlignmentProperty, value);
			}

		}

		internal void UpdateMainText(string mainText)
		{
			object value;
			string strString;

			EnsureTextBlock(ref m_pMainTextBlock);

			strString = mainText;

			value = strString;
			m_pMainTextBlock.SetValue(TextBlock.TextProperty, value);

		}

		internal string GetMainText()
		{
			string pMainText = default;
			if (m_pMainTextBlock is { })
			{
				object value;
				//uint count = 0;

				value = m_pMainTextBlock.GetValue(TextBlock.TextProperty);

				string strString = value as string;
				pMainText = strString;
			}

			return pMainText;
		}

		private bool IsHovered()
		{
			return m_isHovered;
		}

		private bool IsPressed()
		{
			return m_isPressed;
		}

		internal void UpdateLabelText(string labelText)
		{
			object value;
			string strString;

			EnsureTextBlock(ref m_pLabelTextBlock);

			strString = labelText;

			value = strString;
			m_pLabelTextBlock.SetValue(TextBlock.TextProperty, value);

		}

		internal void ShowLabelText(bool showLabel)
		{
			m_hasLabel = showLabel;

			if (m_pLabelTextBlock is { })
			{
				UpdateTextBlockForegroundOpacity(m_pLabelTextBlock);
			}
			else
			{
				global::System.Diagnostics.Debug.Assert(!m_hasLabel);
			}

		}

		internal void InvalidateRender()
		{
			Uno_InvalidateRender();

			// TODO UNO
			//NWSetContentDirty(this, DirtyFlags.Bounds);
			//// if we have template child and density bars, we need to invalidate render on
			//// template child as well because our density bars are appending to the template
			//// child's post render data. We need to make sure the template child's render
			//// data gets invalidated.
			////
			//// if there is no template child, the density bars are just a part of our pre
			//// render data. invalidate render on this calendarviewbaseitem is good enough.
			//if (m_numberOfDensityBar > 0)
			//{
			//	var pTemplateChild = GetFirstChildNoAddRef();
			//	if (pTemplateChild is {})
			//	{
			//		NWSetContentDirty(pTemplateChild, DirtyFlags.Bounds);
			//	}
			//}
		}

		internal Thickness GetItemBorderThickness()
		{
			var pOwner = GetOwner();
			if (pOwner is { })
			{
				var thickness = pOwner.m_calendarItemBorderThickness;
				if (ShouldUseLayoutRounding())
				{
					thickness.Left = LayoutRound(thickness.Left);
					thickness.Right = LayoutRound(thickness.Right);
					thickness.Top = LayoutRound(thickness.Top);
					thickness.Bottom = LayoutRound(thickness.Bottom);
				}

				return thickness;
			}

			return default(Thickness);
		}

		// when item gets keyboard focus, the first brush is same as unfocused brush, the second brush is
		// same as background. This function will only return the firstbrush for focused state and it will
		// return the exactly same brush for both focused and unfocused. the only exception is if all other
		// states are off, for focused state we use m_pFocusBorderBrush, for unfocused state we use
		// m_pCalendarItemBorderBrush.
		private Brush GetItemBorderBrush(bool forFocus)
		{
			Brush pBrush = null;
			var pOwner = GetOwner();
			if (pOwner is { })
			{
				// Today VS Selected, Today wins
				if (m_isToday)
				{
					// Pressed VS Hovered, Pressed wins
					if (m_isPressed)
					{
						pBrush = pOwner.m_pTodayPressedBorderBrush;
					}
					else if (m_isHovered)
					{
						pBrush = pOwner.m_pTodayHoverBorderBrush;
					}
				}
				else if (m_isSelected && !m_isBlackout)
				{
					// Pressed VS Hovered, Pressed wins
					if (m_isPressed)
					{
						pBrush = pOwner.m_pSelectedPressedBorderBrush;
					}
					else if (m_isHovered)
					{
						pBrush = pOwner.m_pSelectedHoverBorderBrush;
					}
					else
					{
						pBrush = pOwner.m_pSelectedBorderBrush;
					}
				}
				// Pressed VS Hovered, Pressed wins
				else if (m_isPressed)
				{
					pBrush = pOwner.m_pPressedBorderBrush;
				}
				else if (m_isHovered)
				{
					pBrush = pOwner.m_pHoverBorderBrush;
				}
				else
				{
					if (forFocus)
					{
						global::System.Diagnostics.Debug.Assert(m_isKeyboardFocused);
						pBrush = pOwner.m_pFocusBorderBrush;
					}
					else
					{
						pBrush = pOwner.m_pCalendarItemBorderBrush;
					}
				}
			}

			return pBrush;
		}

#if false
		// Focus Alternative border brush is same as the item background.
		private Brush GetItemFocusAltBorderBrush()
		{
			Brush pBrush = null;
			var pOwner = GetOwner();
			if (pOwner is { })
			{
				pBrush = pOwner.m_pCalendarItemBackground;
			}

			return pBrush;
		}
#endif

		// for Selected+Today inner border.
		private Brush GetItemInnerBorderBrush()
		{
			global::System.Diagnostics.Debug.Assert(m_isToday && m_isSelected);

			var pOwner = GetOwner();
			if (pOwner is { })
			{
				return pOwner.m_pTodaySelectedInnerBorderBrush;
			}

			return null;
		}

		internal Brush GetItemBackgroundBrush()
		{
			Brush pBrush = null;

			var pOwner = GetOwner();
			if (pOwner is { })
			{
				if (m_isToday)
				{
					if (m_isBlackout)
					{
						pBrush = pOwner.m_pTodayBlackoutBackground;
					}
					else
					{
						pBrush = pOwner.m_pTodayBackground;
					}
				}
				else if (m_isOutOfScope)
				{
					pBrush = pOwner.m_pOutOfScopeBackground;
				}
				else
				{
					pBrush = pOwner.m_pCalendarItemBackground;
				}
			}

			return pBrush;
		}

		private Brush GetTextBlockForeground()
		{
			Brush pBrush = null;
			var pOwner = GetOwner();
			if (pOwner is { })
			{
				if (!IsEnabled)
				{
					pBrush = pOwner.m_pDisabledForeground;
				}
				else if (m_isToday)
				{
					pBrush = pOwner.m_pTodayForeground;
				}
				else if (m_isBlackout)
				{
					pBrush = pOwner.m_pBlackoutForeground;
				}
				else if (m_isSelected)
				{
					pBrush = pOwner.m_pSelectedForeground;
				}
				else if (m_isPressed)
				{
					pBrush = pOwner.m_pPressedForeground;
				}
				else if (m_isOutOfScope)
				{
					pBrush = pOwner.m_pOutOfScopeForeground;
				}
				else
				{
					pBrush = pOwner.m_pCalendarItemForeground;
				}
			}

			return pBrush;
		}

		private float GetTextBlockForegroundOpacity()
		{
			float opacity = 1.0f;

			if (m_isToday && m_isBlackout)
			{
				opacity = TodayBlackouTopacity;
			}

			return opacity;
		}

#if false
		private void CustomizeFocusRectangle(FocusRectangleOptions options, out bool shouldDrawFocusRect)
		{
			bool shouldDrawDottedLines = false;
			shouldDrawDottedLines = ShouldDrawDottedLinesFocusVisual();
			if (shouldDrawDottedLines)
			{
				shouldDrawFocusRect = false;
			}
			else
			{
				if (m_isSelected || m_isHovered || m_isPressed)
				{
					options.secondBrush = (SolidColorBrush)(GetItemBorderBrush(false /* forFocus */));
				}

				shouldDrawFocusRect = true;
			}

			return;
		}

		private bool ShouldDrawDottedLinesFocusVisual()
		{
			var shouldDrawDottedLines = false;

			// TODO UNO
			//object useSystemFocusVisuals;
			//var owner = GetOwner();
			//if (owner is {})
			//{
			//	owner.GetValue(
			//		owner.GetPropertyByIndexInline(KnownPropertyIndex.UIElement_UseSystemFocusVisuals),
			//		&useSystemFocusVisuals);

			//	var focusVisualKind = Application.GetFocusVisualKind();

			//	if (useSystemFocusVisuals is bool b && b && focusVisualKind == FocusVisualKind.DottedLine)
			//	{
			//		shouldDrawDottedLines = true;
			//	}
			//}

			return shouldDrawDottedLines;
		}
#endif

		//month year item
		private protected virtual bool GetTextBlockFontProperties(
			bool isLabel,
			out TextBlockFontProperties pProperties)
		{
			pProperties = default;
			var pOwner = GetOwner();

			if (pOwner is { })
			{
				pProperties.fontSize = isLabel ? pOwner.m_firstOfYearDecadeLabelFontSize : pOwner.m_monthYearItemFontSize;
				pProperties.fontStyle = isLabel ? pOwner.m_firstOfYearDecadeLabelFontStyle : pOwner.m_monthYearItemFontStyle;
				if (isLabel)
				{
					pProperties.fontWeight = pOwner.m_firstOfYearDecadeLabelFontWeight;
				}
				else if (m_isToday)
				{
					pProperties.fontWeight = pOwner.m_todayFontWeight;
				}
				else
				{
					pProperties.fontWeight = pOwner.m_monthYearItemFontWeight;
				}

				pProperties.pFontFamilyNoRef = isLabel ? pOwner.m_pFirstOfYearDecadeLabelFontFamily : pOwner.m_pMonthYearItemFontFamily;
			}

			return pOwner != null;
		}

		private protected virtual bool GetTextBlockAlignments(
			bool isLabel,
			out TextBlockAlignments pAlignments)
		{
			pAlignments = default;

			pAlignments.horizontalAlignment = HorizontalAlignment.Center;
			pAlignments.verticalAlignment = isLabel ? VerticalAlignment.Top : VerticalAlignment.Center;

			return GetOwner() != null;
		}

		//Uno only
		#region Uno Specific Logic
		private Brush FindTodaySelectedBackgroundBrush()
		{
			var pOwner = GetOwner();
			if (pOwner is { } && m_isToday && m_isSelected)
			{
				return pOwner.m_pTodaySelectedBackground;
			}

			return null;
		}

		private Brush FindSelectedBackgroundBrush()
		{
			var pOwner = GetOwner();
			if (pOwner is { } && !m_isToday && m_isSelected)
			{
				return pOwner.m_pSelectedBackground;
			}

			return null;
		}

		private protected virtual CornerRadius GetItemCornerRadius()
		{
			var pOwner = GetOwner();
			if (pOwner is { })
			{
				return pOwner.m_calendarItemCornerRadius;
			}

			return CornerRadius.None;
		}
		#endregion
	}

	partial class CalendarViewDayItem
	{
		private protected override bool GetTextBlockFontProperties(
			bool isLabel,
			out TextBlockFontProperties pProperties)
		{
			pProperties = default;
			var pOwner = GetOwner();

			if (pOwner is { })
			{
				pProperties.fontSize = isLabel ? pOwner.m_firstOfMonthLabelFontSize : pOwner.m_dayItemFontSize;
				pProperties.fontStyle = isLabel ? pOwner.m_firstOfMonthLabelFontStyle : pOwner.m_dayItemFontStyle;
				if (isLabel)
				{
					pProperties.fontWeight = pOwner.m_firstOfMonthLabelFontWeight;
				}
				else if (m_isToday)
				{
					pProperties.fontWeight = pOwner.m_todayFontWeight;
				}
				else
				{
					pProperties.fontWeight = pOwner.m_dayItemFontWeight;
				}

				pProperties.pFontFamilyNoRef = isLabel ? pOwner.m_pFirstOfMonthLabelFontFamily : pOwner.m_pDayItemFontFamily;
			}

			return pOwner != null;
		}

		private protected override bool GetTextBlockAlignments(
			bool isLabel,
			out TextBlockAlignments pProperties)
		{
			pProperties = default;
			var pOwner = GetOwner();

			if (pOwner is { })
			{
				pProperties.horizontalAlignment = isLabel ? pOwner.m_horizontalFirstOfMonthLabelAlignment : pOwner.m_horizontalDayItemAlignment;
				pProperties.verticalAlignment = isLabel ? pOwner.m_verticalFirstOfMonthLabelAlignment : pOwner.m_verticalDayItemAlignment;
			}

			return pOwner != null;
		}

		//Uno only
		#region Uno Specific Logic
		private protected override CornerRadius GetItemCornerRadius()
		{
			var pOwner = GetOwner();
			if (pOwner is { })
			{
				return pOwner.m_dayItemCornerRadius;
			}

			return CornerRadius.None;
		}
		#endregion
	}
}
