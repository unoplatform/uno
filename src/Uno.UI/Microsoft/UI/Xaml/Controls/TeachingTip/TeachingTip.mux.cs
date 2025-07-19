// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TeachingTip\TeachingTip.cpp, tag winui3/release/1.5.0, commit c8bd154c0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.UI.Private.Controls;
using Uno.Disposables;
using Uno.Extensions;
using Uno.UI.DataBinding;
using Uno.UI.Helpers.WinUI;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.Graphics.Display;
using Windows.System;
using Windows.UI.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using static Uno.UI.Helpers.WinUI.CppWinRTHelpers;
using CompositionTarget = Microsoft.UI.Xaml.Media.CompositionTarget;
using Microsoft.UI.Composition;

#if !HAS_UNO_WINUI
using Windows.UI.Xaml.Automation.Peers;
#endif

namespace Microsoft.UI.Xaml.Controls;

public partial class TeachingTip : ContentControl
{
	private const string c_TitleTextBlockVisibleStateName = "ShowTitleTextBlock";
	private const string c_TitleTextBlockCollapsedStateName = "CollapseTitleTextBlock";
	private const string c_SubtitleTextBlockVisibleStateName = "ShowSubtitleTextBlock";
	private const string c_SubtitleTextBlockCollapsedStateName = "CollapseSubtitleTextBlock";

#if false // Not used in WinUI
	private const string c_OverlayCornerRadiusName = "OverlayCornerRadius";
#endif

	/// <summary>
	/// Initializes a new instance of the TeachingTip class.
	/// </summary>
	public TeachingTip()
	{
		DefaultStyleKey = typeof(TeachingTip);
		// EnsureProperties()
		Unloaded += ClosePopupOnUnloadEvent;
		m_automationNameChangedRevoker = RegisterPropertyChangedCallback(AutomationProperties.NameProperty, OnAutomationNameChanged);
		m_automationIdChangedRevoker = RegisterPropertyChangedCallback(AutomationProperties.AutomationIdProperty, OnAutomationIdChanged);
		SetValue(TemplateSettingsProperty, new TeachingTipTemplateSettings());
	}

	protected override AutomationPeer OnCreateAutomationPeer() =>
		new TeachingTipAutomationPeer(this);

	protected override void OnApplyTemplate()
	{
		base.OnApplyTemplate();

		m_acceleratorKeyActivatedRevoker.Disposable = null;
		m_previewKeyDownForF6Revoker.Disposable = null;
		m_effectiveViewportChangedRevoker.Disposable = null;
		m_contentSizeChangedRevoker.Disposable = null;
		m_closeButtonClickedRevoker.Disposable = null;
		m_alternateCloseButtonClickedRevoker.Disposable = null;
		m_actionButtonClickedRevoker.Disposable = null;

		m_container = (Border)GetTemplateChild(s_containerName);
		m_rootElement = m_container.Child;

		m_tailOcclusionGrid = (Grid)GetTemplateChild(s_tailOcclusionGridName);
		m_contentRootGrid = (Grid)GetTemplateChild(s_contentRootGridName);
		m_nonHeroContentRootGrid = (Grid)GetTemplateChild(s_nonHeroContentRootGridName);
		m_heroContentBorder = (Border)GetTemplateChild(s_heroContentBorderName);
		m_actionButton = (Button)GetTemplateChild(s_actionButtonName);
		m_alternateCloseButton = (Button)GetTemplateChild(s_alternateCloseButtonName);
		m_closeButton = (Button)GetTemplateChild(s_closeButtonName);
		m_tailEdgeBorder = (Grid)GetTemplateChild(s_tailEdgeBorderName);
		m_tailPolygon = (Polygon)GetTemplateChild(s_tailPolygonName);
		ToggleVisibilityForEmptyContent(c_TitleTextBlockVisibleStateName, c_TitleTextBlockCollapsedStateName, Title);
		ToggleVisibilityForEmptyContent(c_SubtitleTextBlockVisibleStateName, c_SubtitleTextBlockCollapsedStateName, Subtitle);

		var container = m_container;
		if (container is not null)
		{
			container.Child = null;
		}

		IDisposable GetContentSizeChangedRevoker()
		{
			if (m_tailOcclusionGrid is { } tailOcclusionGrid)
			{
				tailOcclusionGrid.SizeChanged += OnContentSizeChanged;
				return Disposable.Create(() => tailOcclusionGrid.SizeChanged -= OnContentSizeChanged);
			}
			return Disposable.Empty;
		}

		m_contentSizeChangedRevoker.Disposable = GetContentSizeChangedRevoker();

		if (m_contentRootGrid is { } contentRootGrid)
		{
			AutomationProperties.SetLocalizedLandmarkType(contentRootGrid, ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_TeachingTipCustomLandmarkName));
		}

		IDisposable GetCloseButtonClickedRevoker()
		{
			if (m_closeButton is { } closeButton)
			{
				closeButton.Click += OnCloseButtonClicked;
				return Disposable.Create(() => closeButton.Click -= OnCloseButtonClicked);
			}
			return Disposable.Empty;
		}

		m_closeButtonClickedRevoker.Disposable = GetCloseButtonClickedRevoker();

		IDisposable GetAlternateCloseButtonClickedRevoker()
		{
			if (m_alternateCloseButton is { } alternateCloseButton)
			{
				AutomationProperties.SetName(alternateCloseButton, ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_TeachingTipAlternateCloseButtonName));
				ToolTip tooltip = new ToolTip();
				tooltip.Content = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_TeachingTipAlternateCloseButtonTooltip);
				ToolTipService.SetToolTip(alternateCloseButton, tooltip);
				alternateCloseButton.Click += OnCloseButtonClicked;
				return Disposable.Create(() => alternateCloseButton.Click -= OnCloseButtonClicked);
			}
			return Disposable.Empty;
		}

		m_alternateCloseButtonClickedRevoker.Disposable = GetAlternateCloseButtonClickedRevoker();

		IDisposable GetActionButtonClickedRevoker()
		{
			if (m_actionButton is { } actionButton)
			{
				actionButton.Click += OnActionButtonClicked;
				return Disposable.Create(() => actionButton.Click -= OnActionButtonClicked);
			}
			return Disposable.Empty;
		}

		m_actionButtonClickedRevoker.Disposable = GetActionButtonClickedRevoker();

		UpdateButtonsState();
		OnIsLightDismissEnabledChanged();
		OnIconSourceChanged();
		OnHeroContentPlacementChanged();

		UpdateButtonAutomationProperties(m_actionButton, ActionButtonContent);
		UpdateButtonAutomationProperties(m_closeButton, CloseButtonContent);

		EstablishShadows();

		m_isTemplateApplied = true;
	}

	private void OnPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		var property = args.Property;

		if (property == IsOpenProperty)
		{
			OnIsOpenChanged();
		}
		else if (property == TargetProperty)
		{
			// Unregister from old target if it exists
			if (args.OldValue is not null)
			{
				m_TargetUnloadedRevoker.Disposable = null;
			}

			// Register to new target if it exists
			if (args.NewValue is { } value)
			{
				var newTarget = (FrameworkElement)value;
				newTarget.Unloaded += ClosePopupOnUnloadEvent;
				m_TargetUnloadedRevoker.Disposable = Disposable.Create(() => newTarget.Unloaded -= ClosePopupOnUnloadEvent);
			}
			OnTargetChanged();
		}
		else if (property == PlacementMarginProperty)
		{
			OnPlacementMarginChanged();
		}
		else if (property == IsLightDismissEnabledProperty)
		{
			OnIsLightDismissEnabledChanged();
		}
		else if (property == ShouldConstrainToRootBoundsProperty)
		{
			OnShouldConstrainToRootBoundsChanged();
		}
		else if (property == TailVisibilityProperty)
		{
			OnTailVisibilityChanged();
		}
		else if (property == PreferredPlacementProperty)
		{
			if (IsOpen)
			{
				PositionPopup();
			}
		}
		else if (property == HeroContentPlacementProperty)
		{
			OnHeroContentPlacementChanged();
		}
		else if (property == IconSourceProperty)
		{
			OnIconSourceChanged();
		}
		else if (property == TitleProperty)
		{
			SetPopupAutomationProperties();
			if (ToggleVisibilityForEmptyContent(c_TitleTextBlockVisibleStateName, c_TitleTextBlockCollapsedStateName, Title))
			{
				TeachingTipTestHooks.NotifyTitleVisibilityChanged(this);
			}
		}
		else if (property == SubtitleProperty)
		{
			if (ToggleVisibilityForEmptyContent(c_SubtitleTextBlockVisibleStateName, c_SubtitleTextBlockCollapsedStateName, Subtitle))
			{
				TeachingTipTestHooks.NotifySubtitleVisibilityChanged(this);
			}
		}
		else if (property == ActionButtonContentProperty)
		{
			UpdateButtonsState();
			var value = args.NewValue;
			UpdateButtonAutomationProperties(m_actionButton, value);
		}
		else if (property == CloseButtonContentProperty)
		{
			UpdateButtonsState();
			var value = args.NewValue;
			UpdateButtonAutomationProperties(m_closeButton, value);
		}
	}

	private void UpdateButtonAutomationProperties(Button button, object content)
	{
		if (button is not null)
		{
			string nameHString = SharedHelpers.TryGetStringRepresentationFromObject(content);
			AutomationProperties.SetName(button, nameHString);
		}
	}

	private bool ToggleVisibilityForEmptyContent(string visibleStateName, string collapsedStateName, string content)
	{
		if (content != "")
		{
			VisualStateManager.GoToState(this, visibleStateName, false);
			return true;
		}
		else
		{
			VisualStateManager.GoToState(this, collapsedStateName, false);
			return true;
		}
	}

#if false // Not used in WinUI
	private void OnOcclusionContentChanged(object oldContent, object newContent)
	{
		if (newContent != null)
		{
			VisualStateManager.GoToState(this, "Content", false);
		}
		else
		{
			VisualStateManager.GoToState(this, "NoContent", false);
		}
	}
#endif

	private void SetPopupAutomationProperties()
	{
		if (m_popup is { } popup)
		{
			var name = AutomationProperties.GetName(this);
			if (string.IsNullOrEmpty(name))
			{
				name = Title;
			}
			AutomationProperties.SetName(popup, name);

			AutomationProperties.SetAutomationId(popup, AutomationProperties.GetAutomationId(this));
		}
	}

	// Playing a closing animation when the Teaching Tip is closed via light dismiss requires this work around.
	// This is because there is no event that occurs when a popup is closing due to light dismiss so we have no way to intercept
	// the close and play our animation first. To work around this we've created a second popup which has no content and sits
	// underneath the teaching tip and is put into light dismiss mode instead of the primary popup. Then when this popup closes
	// due to light dismiss we know we are supposed to close the primary popup as well. To ensure that this popup does not block
	// interaction to the primary popup we need to make sure that the LightDismissIndicatorPopup is always opened first, so that
	// it is Z ordered underneath the primary popup.
	private void CreateLightDismissIndicatorPopup()
	{
		var popup = new Popup();
		popup.XamlRoot = XamlRoot;
		// A Popup needs contents to open, so set a child that doesn't do anything.
		var grid = new Grid();
		popup.Child = grid;

		m_lightDismissIndicatorPopup = popup;
	}

	private bool UpdateTail()
	{
		// An effective placement of var indicates that no tail should be shown.
		var (placement, tipDoesNotFit) = DetermineEffectivePlacement();
		m_currentEffectiveTailPlacementMode = placement;
		var tailVisibility = TailVisibility;
		if (tailVisibility == TeachingTipTailVisibility.Collapsed || (m_target == null && tailVisibility != TeachingTipTailVisibility.Visible))
		{
			m_currentEffectiveTailPlacementMode = TeachingTipPlacementMode.Auto;
		}

		if (placement != m_currentEffectiveTipPlacementMode)
		{
			m_currentEffectiveTipPlacementMode = placement;
			TeachingTipTestHooks.NotifyEffectivePlacementChanged(this);
		}

		var nullableTailOcclusionGrid = m_tailOcclusionGrid;

		var height = nullableTailOcclusionGrid?.ActualHeight ?? 0;
		var width = nullableTailOcclusionGrid?.ActualWidth ?? 0;

		(float firstColumnWidth, float secondColumnWidth, float nextToLastColumnWidth, float lastColumnWidth) GetColumnWidths(Grid nullableTailOcclusionGrid)
		{
			var columnDefinitions = nullableTailOcclusionGrid?.ColumnDefinitions;
			if (columnDefinitions != null)
			{
				var firstColumnWidth = columnDefinitions.Count > 0 ? (float)(columnDefinitions[0].ActualWidth) : 0.0f;
				var secondColumnWidth = columnDefinitions.Count > 1 ? (float)(columnDefinitions[1].ActualWidth) : 0.0f;
				var nextToLastColumnWidth = columnDefinitions.Count > 1 ? (float)(columnDefinitions[columnDefinitions.Count - 2].ActualWidth) : 0.0f;
				var lastColumnWidth = columnDefinitions.Count > 0 ? (float)(columnDefinitions[columnDefinitions.Count - 1].ActualWidth) : 0.0f;

				return (firstColumnWidth, secondColumnWidth, nextToLastColumnWidth, lastColumnWidth);
			}
			return (0.0f, 0.0f, 0.0f, 0.0f);
		}

		var (firstColumnWidth, secondColumnWidth, nextToLastColumnWidth, lastColumnWidth) = GetColumnWidths(nullableTailOcclusionGrid);

		(float firstColumnHeight, float secondColumnHeight, float nextToLastColumnHeight, float lastColumnHeight) GetColumnHeights(Grid nullableTailOcclusionGrid)
		{
			var rowDefinitions = nullableTailOcclusionGrid?.RowDefinitions;
			if (rowDefinitions != null)
			{
				var firstRowHeight = rowDefinitions.Count > 0 ? (float)(rowDefinitions[0].ActualHeight) : 0.0f;
				var secondRowHeight = rowDefinitions.Count > 1 ? (float)(rowDefinitions[1].ActualHeight) : 0.0f;
				var nextToLastRowHeight = rowDefinitions.Count > 1 ? (float)(rowDefinitions[rowDefinitions.Count - 2].ActualHeight) : 0.0f;
				var lastRowHeight = rowDefinitions.Count > 0 ? (float)(rowDefinitions[rowDefinitions.Count - 1].ActualHeight) : 0.0f;

				return (firstRowHeight, secondRowHeight, nextToLastRowHeight, lastRowHeight);
			}
			return (0.0f, 0.0f, 0.0f, 0.0f);
		}
		var (firstRowHeight, secondRowHeight, nextToLastRowHeight, lastRowHeight) = GetColumnHeights(nullableTailOcclusionGrid);

		UpdateSizeBasedTemplateSettings();

		switch (m_currentEffectiveTailPlacementMode)
		{
			// An effective placement of var means the tip should not display a tail.
			case TeachingTipPlacementMode.Auto:
				TrySetCenterPoint(nullableTailOcclusionGrid, new Vector3((float)width / 2, (float)height / 2, 0.0f));
				UpdateDynamicHeroContentPlacementToTop();
				VisualStateManager.GoToState(this, "Untargeted", false);
				break;

			case TeachingTipPlacementMode.Top:
				TrySetCenterPoint(nullableTailOcclusionGrid, new Vector3((float)width / 2, (float)height - lastRowHeight, 0.0f));
				TrySetCenterPoint(m_tailEdgeBorder, new Vector3(((float)width / 2) - firstColumnWidth, 0.0f, 0.0f));
				UpdateDynamicHeroContentPlacementToTop();
				VisualStateManager.GoToState(this, "Top", false);
				break;

			case TeachingTipPlacementMode.Bottom:
				TrySetCenterPoint(nullableTailOcclusionGrid, new Vector3((float)width / 2, firstRowHeight, 0.0f));
				TrySetCenterPoint(m_tailEdgeBorder, new Vector3(((float)width / 2) - firstColumnWidth, 0.0f, 0.0f));
				UpdateDynamicHeroContentPlacementToBottom();
				VisualStateManager.GoToState(this, "Bottom", false);
				break;

			case TeachingTipPlacementMode.Left:
				TrySetCenterPoint(nullableTailOcclusionGrid, new Vector3((float)width - lastColumnWidth, ((float)height / 2), 0.0f));
				TrySetCenterPoint(m_tailEdgeBorder, new Vector3(0.0f, ((float)height / 2) - firstRowHeight, 0.0f));
				UpdateDynamicHeroContentPlacementToTop();
				VisualStateManager.GoToState(this, "Left", false);
				break;

			case TeachingTipPlacementMode.Right:
				TrySetCenterPoint(nullableTailOcclusionGrid, new Vector3(firstColumnWidth, (float)height / 2, 0.0f));
				TrySetCenterPoint(m_tailEdgeBorder, new Vector3(0.0f, ((float)height / 2) - firstRowHeight, 0.0f));
				UpdateDynamicHeroContentPlacementToTop();
				VisualStateManager.GoToState(this, "Right", false);
				break;

			case TeachingTipPlacementMode.TopRight:
				TrySetCenterPoint(nullableTailOcclusionGrid, new Vector3(firstColumnWidth + secondColumnWidth + 1, (float)height - lastRowHeight, 0.0f));
				TrySetCenterPoint(m_tailEdgeBorder, new Vector3(secondColumnWidth, 0.0f, 0.0f));
				UpdateDynamicHeroContentPlacementToTop();
				VisualStateManager.GoToState(this, "TopRight", false);
				break;

			case TeachingTipPlacementMode.TopLeft:
				TrySetCenterPoint(nullableTailOcclusionGrid, new Vector3((float)width - (nextToLastColumnWidth + lastColumnWidth + 1), (float)height - lastRowHeight, 0.0f));
				TrySetCenterPoint(m_tailEdgeBorder, new Vector3((float)width - (nextToLastColumnWidth + firstColumnWidth + lastColumnWidth), 0.0f, 0.0f));
				UpdateDynamicHeroContentPlacementToTop();
				VisualStateManager.GoToState(this, "TopLeft", false);
				break;

			case TeachingTipPlacementMode.BottomRight:
				TrySetCenterPoint(nullableTailOcclusionGrid, new Vector3(firstColumnWidth + secondColumnWidth + 1, firstRowHeight, 0.0f));
				TrySetCenterPoint(m_tailEdgeBorder, new Vector3(secondColumnWidth, 0.0f, 0.0f));
				UpdateDynamicHeroContentPlacementToBottom();
				VisualStateManager.GoToState(this, "BottomRight", false);
				break;

			case TeachingTipPlacementMode.BottomLeft:
				TrySetCenterPoint(nullableTailOcclusionGrid, new Vector3((float)width - (nextToLastColumnWidth + lastColumnWidth + 1), firstRowHeight, 0.0f));
				TrySetCenterPoint(m_tailEdgeBorder, new Vector3((float)width - (nextToLastColumnWidth + firstColumnWidth + lastColumnWidth), 0.0f, 0.0f));
				UpdateDynamicHeroContentPlacementToBottom();
				VisualStateManager.GoToState(this, "BottomLeft", false);
				break;

			case TeachingTipPlacementMode.LeftTop:
				TrySetCenterPoint(nullableTailOcclusionGrid, new Vector3((float)width - lastColumnWidth, (float)height - (nextToLastRowHeight + lastRowHeight + 1), 0.0f));
				TrySetCenterPoint(m_tailEdgeBorder, new Vector3(0.0f, (float)height - (nextToLastRowHeight + firstRowHeight + lastRowHeight), 0.0f));
				UpdateDynamicHeroContentPlacementToTop();
				VisualStateManager.GoToState(this, "LeftTop", false);
				break;

			case TeachingTipPlacementMode.LeftBottom:
				TrySetCenterPoint(nullableTailOcclusionGrid, new Vector3((float)width - lastColumnWidth, (firstRowHeight + secondRowHeight + 1), 0.0f));
				TrySetCenterPoint(m_tailEdgeBorder, new Vector3(0.0f, secondRowHeight, 0.0f));
				UpdateDynamicHeroContentPlacementToBottom();
				VisualStateManager.GoToState(this, "LeftBottom", false);
				break;

			case TeachingTipPlacementMode.RightTop:
				TrySetCenterPoint(nullableTailOcclusionGrid, new Vector3(firstColumnWidth, (float)height - (nextToLastRowHeight + lastRowHeight + 1), 0.0f));
				TrySetCenterPoint(m_tailEdgeBorder, new Vector3(0.0f, (float)height - (nextToLastRowHeight + firstRowHeight + lastRowHeight), 0.0f));
				UpdateDynamicHeroContentPlacementToTop();
				VisualStateManager.GoToState(this, "RightTop", false);
				break;

			case TeachingTipPlacementMode.RightBottom:
				TrySetCenterPoint(nullableTailOcclusionGrid, new Vector3(firstColumnWidth, (firstRowHeight + secondRowHeight + 1), 0.0f));
				TrySetCenterPoint(m_tailEdgeBorder, new Vector3(0.0f, secondRowHeight, 0.0f));
				UpdateDynamicHeroContentPlacementToBottom();
				VisualStateManager.GoToState(this, "RightBottom", false);
				break;

			case TeachingTipPlacementMode.Center:
				TrySetCenterPoint(nullableTailOcclusionGrid, new Vector3((float)width / 2, (float)height - lastRowHeight, 0.0f));
				TrySetCenterPoint(m_tailEdgeBorder, new Vector3(((float)width / 2) - firstColumnWidth, 0.0f, 0.0f));
				UpdateDynamicHeroContentPlacementToTop();
				VisualStateManager.GoToState(this, "Center", false);
				break;

			default:
				break;
		}

		return tipDoesNotFit;
	}

	private void PositionPopup()
	{
		bool tipDoesNotFit;
		if (m_target != null)
		{
			tipDoesNotFit = PositionTargetedPopup();
		}
		else
		{
			tipDoesNotFit = PositionUntargetedPopup();
		}
		if (tipDoesNotFit)
		{
			IsOpen = false;
		}

		TeachingTipTestHooks.NotifyOffsetChanged(this);
	}

	private bool PositionTargetedPopup()
	{
		bool tipDoesNotFit = UpdateTail();
		var offset = PlacementMargin;

		(double tipHeight, double tipWidth) GetTipSize()
		{
			var tailOcclusionGrid = m_tailOcclusionGrid;
			if (tailOcclusionGrid != null)
			{
				var tipHeight = tailOcclusionGrid.ActualHeight;
				var tipWidth = tailOcclusionGrid.ActualWidth;
				return (tipHeight, tipWidth);
			}
			return (0.0, 0.0);
		}

		var (tipHeight, tipWidth) = GetTipSize();


		var popup = m_popup;
		if (popup != null)
		{
			// Depending on the effective placement mode of the tip we use a combination of the tip's size, the target's position within the app, the target's
			// size, and the target offset property to determine the appropriate vertical and horizontal offsets of the popup that the tip is contained in.
			switch (m_currentEffectiveTipPlacementMode)
			{
				case TeachingTipPlacementMode.Top:
					popup.VerticalOffset = m_currentTargetBoundsInCoreWindowSpace.Y - tipHeight - offset.Top;
					popup.HorizontalOffset = ((m_currentTargetBoundsInCoreWindowSpace.X * 2.0f) + m_currentTargetBoundsInCoreWindowSpace.Width - tipWidth) / 2.0f;
					break;

				case TeachingTipPlacementMode.Bottom:
					popup.VerticalOffset = m_currentTargetBoundsInCoreWindowSpace.Y + m_currentTargetBoundsInCoreWindowSpace.Height + (float)(offset.Bottom);
					popup.HorizontalOffset = ((m_currentTargetBoundsInCoreWindowSpace.X * 2.0f) + m_currentTargetBoundsInCoreWindowSpace.Width - tipWidth) / 2.0f;
					break;

				case TeachingTipPlacementMode.Left:
					popup.VerticalOffset = ((m_currentTargetBoundsInCoreWindowSpace.Y * 2.0f) + m_currentTargetBoundsInCoreWindowSpace.Height - tipHeight) / 2.0f;
					popup.HorizontalOffset = m_currentTargetBoundsInCoreWindowSpace.X - tipWidth - offset.Left;
					break;

				case TeachingTipPlacementMode.Right:
					popup.VerticalOffset = ((m_currentTargetBoundsInCoreWindowSpace.Y * 2.0f) + m_currentTargetBoundsInCoreWindowSpace.Height - tipHeight) / 2.0f;
					popup.HorizontalOffset = m_currentTargetBoundsInCoreWindowSpace.X + m_currentTargetBoundsInCoreWindowSpace.Width + (float)(offset.Right);
					break;

				case TeachingTipPlacementMode.TopRight:
					popup.VerticalOffset = m_currentTargetBoundsInCoreWindowSpace.Y - tipHeight - offset.Top;
					popup.HorizontalOffset = (((m_currentTargetBoundsInCoreWindowSpace.X * 2.0f) + m_currentTargetBoundsInCoreWindowSpace.Width) / 2.0f) - MinimumTipEdgeToTailCenter();
					break;

				case TeachingTipPlacementMode.TopLeft:
					popup.VerticalOffset = m_currentTargetBoundsInCoreWindowSpace.Y - tipHeight - offset.Top;
					popup.HorizontalOffset = (((m_currentTargetBoundsInCoreWindowSpace.X * 2.0f) + m_currentTargetBoundsInCoreWindowSpace.Width) / 2.0f) - tipWidth + MinimumTipEdgeToTailCenter();
					break;

				case TeachingTipPlacementMode.BottomRight:
					popup.VerticalOffset = m_currentTargetBoundsInCoreWindowSpace.Y + m_currentTargetBoundsInCoreWindowSpace.Height + (float)(offset.Bottom);
					popup.HorizontalOffset = ((((m_currentTargetBoundsInCoreWindowSpace.X * 2.0f) + m_currentTargetBoundsInCoreWindowSpace.Width) / 2.0f) - MinimumTipEdgeToTailCenter());
					break;

				case TeachingTipPlacementMode.BottomLeft:
					popup.VerticalOffset = m_currentTargetBoundsInCoreWindowSpace.Y + m_currentTargetBoundsInCoreWindowSpace.Height + (float)(offset.Bottom);
					popup.HorizontalOffset = ((((m_currentTargetBoundsInCoreWindowSpace.X * 2.0f) + m_currentTargetBoundsInCoreWindowSpace.Width) / 2.0f) - tipWidth + MinimumTipEdgeToTailCenter());
					break;

				case TeachingTipPlacementMode.LeftTop:
					popup.VerticalOffset = (((m_currentTargetBoundsInCoreWindowSpace.Y * 2.0f) + m_currentTargetBoundsInCoreWindowSpace.Height) / 2.0f) - tipHeight + MinimumTipEdgeToTailCenter();
					popup.HorizontalOffset = m_currentTargetBoundsInCoreWindowSpace.X - tipWidth - offset.Left;
					break;

				case TeachingTipPlacementMode.LeftBottom:
					popup.VerticalOffset = (((m_currentTargetBoundsInCoreWindowSpace.Y * 2.0f) + m_currentTargetBoundsInCoreWindowSpace.Height) / 2.0f) - MinimumTipEdgeToTailCenter();
					popup.HorizontalOffset = m_currentTargetBoundsInCoreWindowSpace.X - tipWidth - offset.Left;
					break;

				case TeachingTipPlacementMode.RightTop:
					popup.VerticalOffset = (((m_currentTargetBoundsInCoreWindowSpace.Y * 2.0f) + m_currentTargetBoundsInCoreWindowSpace.Height) / 2.0f) - tipHeight + MinimumTipEdgeToTailCenter();
					popup.HorizontalOffset = m_currentTargetBoundsInCoreWindowSpace.X + m_currentTargetBoundsInCoreWindowSpace.Width + (float)(offset.Right);
					break;

				case TeachingTipPlacementMode.RightBottom:
					popup.VerticalOffset = (((m_currentTargetBoundsInCoreWindowSpace.Y * 2.0f) + m_currentTargetBoundsInCoreWindowSpace.Height) / 2.0f) - MinimumTipEdgeToTailCenter();
					popup.HorizontalOffset = m_currentTargetBoundsInCoreWindowSpace.X + m_currentTargetBoundsInCoreWindowSpace.Width + (float)(offset.Right);
					break;

				case TeachingTipPlacementMode.Center:
					popup.VerticalOffset = m_currentTargetBoundsInCoreWindowSpace.Y + (m_currentTargetBoundsInCoreWindowSpace.Height / 2.0f) - tipHeight - offset.Top;
					popup.HorizontalOffset = (((m_currentTargetBoundsInCoreWindowSpace.X * 2.0f) + m_currentTargetBoundsInCoreWindowSpace.Width - tipWidth) / 2.0f);
					break;

				default:
					throw new InvalidOperationException("Provided placement is not supported");
			}
		}
		return tipDoesNotFit;
	}

	private bool PositionUntargetedPopup()
	{
		var windowBoundsInCoreWindowSpace = GetEffectiveWindowBoundsInCoreWindowSpace(GetWindowBounds());

		(double finalTipHeight, double finalTipWidth) GetFinalTipSize()
		{
			var tailOcclusionGrid = m_tailOcclusionGrid;
			if (tailOcclusionGrid != null)
			{
				var finalTipHeight = tailOcclusionGrid.ActualHeight;
				var finalTipWidth = tailOcclusionGrid.ActualWidth;
				return (finalTipHeight, finalTipWidth);
			}
			return (0.0, 0.0);
		}

		var (finalTipHeight, finalTipWidth) = GetFinalTipSize();

		bool tipDoesNotFit = UpdateTail();

		var offset = PlacementMargin;

		// Depending on the effective placement mode of the tip we use a combination of the tip's size, the window's size, and the target
		// offset property to determine the appropriate vertical and horizontal offsets of the popup that the tip is contained in.
		var popup = m_popup;
		if (popup != null)
		{
			switch (GetFlowDirectionAdjustedPlacement(PreferredPlacement))
			{
				case TeachingTipPlacementMode.Auto:
				case TeachingTipPlacementMode.Bottom:
					popup.VerticalOffset = UntargetedTipFarPlacementOffset((float)windowBoundsInCoreWindowSpace.Height, finalTipHeight, offset.Bottom);
					popup.HorizontalOffset = UntargetedTipCenterPlacementOffset((float)windowBoundsInCoreWindowSpace.X, (float)windowBoundsInCoreWindowSpace.Width, finalTipWidth, offset.Left, offset.Right);
					break;

				case TeachingTipPlacementMode.Top:
					popup.VerticalOffset = UntargetedTipNearPlacementOffset((float)windowBoundsInCoreWindowSpace.Y, offset.Top);
					popup.HorizontalOffset = UntargetedTipCenterPlacementOffset((float)windowBoundsInCoreWindowSpace.X, (float)windowBoundsInCoreWindowSpace.Width, finalTipWidth, offset.Left, offset.Right);
					break;

				case TeachingTipPlacementMode.Left:
					popup.VerticalOffset = UntargetedTipCenterPlacementOffset((float)windowBoundsInCoreWindowSpace.Y, (float)windowBoundsInCoreWindowSpace.Height, finalTipHeight, offset.Top, offset.Bottom);
					popup.HorizontalOffset = UntargetedTipNearPlacementOffset((float)windowBoundsInCoreWindowSpace.X, offset.Left);
					break;

				case TeachingTipPlacementMode.Right:
					popup.VerticalOffset = UntargetedTipCenterPlacementOffset((float)windowBoundsInCoreWindowSpace.Y, (float)windowBoundsInCoreWindowSpace.Height, finalTipHeight, offset.Top, offset.Bottom);
					popup.HorizontalOffset = UntargetedTipFarPlacementOffset((float)windowBoundsInCoreWindowSpace.Width, finalTipWidth, offset.Right);
					break;

				case TeachingTipPlacementMode.TopRight:
					popup.VerticalOffset = UntargetedTipNearPlacementOffset((float)windowBoundsInCoreWindowSpace.Y, offset.Top);
					popup.HorizontalOffset = UntargetedTipFarPlacementOffset((float)windowBoundsInCoreWindowSpace.Width, finalTipWidth, offset.Right);
					break;

				case TeachingTipPlacementMode.TopLeft:
					popup.VerticalOffset = UntargetedTipNearPlacementOffset((float)windowBoundsInCoreWindowSpace.Y, offset.Top);
					popup.HorizontalOffset = UntargetedTipNearPlacementOffset((float)windowBoundsInCoreWindowSpace.X, offset.Left);
					break;

				case TeachingTipPlacementMode.BottomRight:
					popup.VerticalOffset = UntargetedTipFarPlacementOffset((float)windowBoundsInCoreWindowSpace.Height, finalTipHeight, offset.Bottom);
					popup.HorizontalOffset = UntargetedTipFarPlacementOffset((float)windowBoundsInCoreWindowSpace.Width, finalTipWidth, offset.Right);
					break;

				case TeachingTipPlacementMode.BottomLeft:
					popup.VerticalOffset = UntargetedTipFarPlacementOffset((float)windowBoundsInCoreWindowSpace.Height, finalTipHeight, offset.Bottom);
					popup.HorizontalOffset = UntargetedTipNearPlacementOffset((float)windowBoundsInCoreWindowSpace.X, offset.Left);
					break;

				case TeachingTipPlacementMode.LeftTop:
					popup.VerticalOffset = UntargetedTipNearPlacementOffset((float)windowBoundsInCoreWindowSpace.Y, offset.Top);
					popup.HorizontalOffset = UntargetedTipNearPlacementOffset((float)windowBoundsInCoreWindowSpace.X, offset.Left);
					break;

				case TeachingTipPlacementMode.LeftBottom:
					popup.VerticalOffset = UntargetedTipFarPlacementOffset((float)windowBoundsInCoreWindowSpace.Height, finalTipHeight, offset.Bottom);
					popup.HorizontalOffset = UntargetedTipNearPlacementOffset((float)windowBoundsInCoreWindowSpace.X, offset.Left);
					break;

				case TeachingTipPlacementMode.RightTop:
					popup.VerticalOffset = UntargetedTipNearPlacementOffset((float)windowBoundsInCoreWindowSpace.Y, offset.Top);
					popup.HorizontalOffset = UntargetedTipFarPlacementOffset((float)windowBoundsInCoreWindowSpace.Width, finalTipWidth, offset.Right);
					break;

				case TeachingTipPlacementMode.RightBottom:
					popup.VerticalOffset = UntargetedTipFarPlacementOffset((float)windowBoundsInCoreWindowSpace.Height, finalTipHeight, offset.Bottom);
					popup.HorizontalOffset = UntargetedTipFarPlacementOffset((float)windowBoundsInCoreWindowSpace.Width, finalTipWidth, offset.Right);
					break;

				case TeachingTipPlacementMode.Center:
					popup.VerticalOffset = UntargetedTipCenterPlacementOffset((float)windowBoundsInCoreWindowSpace.Y, (float)windowBoundsInCoreWindowSpace.Height, finalTipHeight, offset.Top, offset.Bottom);
					popup.HorizontalOffset = UntargetedTipCenterPlacementOffset((float)windowBoundsInCoreWindowSpace.X, (float)windowBoundsInCoreWindowSpace.Width, finalTipWidth, offset.Left, offset.Right);
					break;

				default:
					throw new InvalidOperationException("Provided placement is not supported");
			}
		}

		return tipDoesNotFit;
	}

	private void UpdateSizeBasedTemplateSettings()
	{
		var templateSettings = TemplateSettings;
		(double width, double height) GetActualSize()
		{
			var contentRootGrid = m_contentRootGrid;
			if (contentRootGrid != null)
			{
				var width = contentRootGrid.ActualWidth;
				var height = contentRootGrid.ActualHeight;
				return (width, height);
			}
			return (0.0, 0.0);
		}
		var (width, height) = GetActualSize();

		switch (m_currentEffectiveTailPlacementMode)
		{
			case TeachingTipPlacementMode.Top:
				templateSettings.TopRightHighlightMargin = OtherPlacementTopRightHighlightMargin(width, height);
				templateSettings.TopLeftHighlightMargin = TopEdgePlacementTopLeftHighlightMargin(width, height);
				break;
			case TeachingTipPlacementMode.Bottom:
				templateSettings.TopRightHighlightMargin = BottomPlacementTopRightHighlightMargin(width, height);
				templateSettings.TopLeftHighlightMargin = BottomPlacementTopLeftHighlightMargin(width, height);
				break;
			case TeachingTipPlacementMode.Left:
				templateSettings.TopRightHighlightMargin = OtherPlacementTopRightHighlightMargin(width, height);
				templateSettings.TopLeftHighlightMargin = LeftEdgePlacementTopLeftHighlightMargin(width, height);
				break;
			case TeachingTipPlacementMode.Right:
				templateSettings.TopRightHighlightMargin = OtherPlacementTopRightHighlightMargin(width, height);
				templateSettings.TopLeftHighlightMargin = RightEdgePlacementTopLeftHighlightMargin(width, height);
				break;
			case TeachingTipPlacementMode.TopLeft:
				templateSettings.TopRightHighlightMargin = OtherPlacementTopRightHighlightMargin(width, height);
				templateSettings.TopLeftHighlightMargin = TopEdgePlacementTopLeftHighlightMargin(width, height);
				break;
			case TeachingTipPlacementMode.TopRight:
				templateSettings.TopRightHighlightMargin = OtherPlacementTopRightHighlightMargin(width, height);
				templateSettings.TopLeftHighlightMargin = TopEdgePlacementTopLeftHighlightMargin(width, height);
				break;
			case TeachingTipPlacementMode.BottomLeft:
				templateSettings.TopRightHighlightMargin = BottomLeftPlacementTopRightHighlightMargin(width, height);
				templateSettings.TopLeftHighlightMargin = BottomLeftPlacementTopLeftHighlightMargin(width, height);
				break;
			case TeachingTipPlacementMode.BottomRight:
				templateSettings.TopRightHighlightMargin = BottomRightPlacementTopRightHighlightMargin(width, height);
				templateSettings.TopLeftHighlightMargin = BottomRightPlacementTopLeftHighlightMargin(width, height);
				break;
			case TeachingTipPlacementMode.LeftTop:
				templateSettings.TopRightHighlightMargin = OtherPlacementTopRightHighlightMargin(width, height);
				templateSettings.TopLeftHighlightMargin = LeftEdgePlacementTopLeftHighlightMargin(width, height);
				break;
			case TeachingTipPlacementMode.LeftBottom:
				templateSettings.TopRightHighlightMargin = OtherPlacementTopRightHighlightMargin(width, height);
				templateSettings.TopLeftHighlightMargin = LeftEdgePlacementTopLeftHighlightMargin(width, height);
				break;
			case TeachingTipPlacementMode.RightTop:
				templateSettings.TopRightHighlightMargin = OtherPlacementTopRightHighlightMargin(width, height);
				templateSettings.TopLeftHighlightMargin = RightEdgePlacementTopLeftHighlightMargin(width, height);
				break;
			case TeachingTipPlacementMode.RightBottom:
				templateSettings.TopRightHighlightMargin = OtherPlacementTopRightHighlightMargin(width, height);
				templateSettings.TopLeftHighlightMargin = RightEdgePlacementTopLeftHighlightMargin(width, height);
				break;
			case TeachingTipPlacementMode.Auto:
				templateSettings.TopRightHighlightMargin = OtherPlacementTopRightHighlightMargin(width, height);
				templateSettings.TopLeftHighlightMargin = TopEdgePlacementTopLeftHighlightMargin(width, height);
				break;
			case TeachingTipPlacementMode.Center:
				templateSettings.TopRightHighlightMargin = OtherPlacementTopRightHighlightMargin(width, height);
				templateSettings.TopLeftHighlightMargin = TopEdgePlacementTopLeftHighlightMargin(width, height);
				break;
		}
	}

	private void UpdateButtonsState()
	{
		var actionContent = ActionButtonContent;
		var closeContent = CloseButtonContent;
		bool isLightDismiss = IsLightDismissEnabled;
		if (actionContent != null && closeContent != null)
		{
			VisualStateManager.GoToState(this, "BothButtonsVisible", false);
			VisualStateManager.GoToState(this, "FooterCloseButton", false);
		}
		else if (actionContent != null && isLightDismiss)
		{
			VisualStateManager.GoToState(this, "ActionButtonVisible", false);
			VisualStateManager.GoToState(this, "FooterCloseButton", false);
		}
		else if (actionContent != null)
		{
			VisualStateManager.GoToState(this, "ActionButtonVisible", false);
			VisualStateManager.GoToState(this, "HeaderCloseButton", false);
		}
		else if (closeContent != null)
		{
			VisualStateManager.GoToState(this, "CloseButtonVisible", false);
			VisualStateManager.GoToState(this, "FooterCloseButton", false);
		}
		else if (isLightDismiss)
		{
			VisualStateManager.GoToState(this, "NoButtonsVisible", false);
			VisualStateManager.GoToState(this, "FooterCloseButton", false);
		}
		else
		{
			VisualStateManager.GoToState(this, "NoButtonsVisible", false);
			VisualStateManager.GoToState(this, "HeaderCloseButton", false);
		}
	}

	private void UpdateDynamicHeroContentPlacementToTop()
	{
		if (HeroContentPlacement == TeachingTipHeroContentPlacementMode.Auto)
		{
			UpdateDynamicHeroContentPlacementToTopImpl();
		}
	}

	private void UpdateDynamicHeroContentPlacementToTopImpl()
	{
		VisualStateManager.GoToState(this, "HeroContentTop", false);
		if (m_currentHeroContentEffectivePlacementMode != TeachingTipHeroContentPlacementMode.Top)
		{
			m_currentHeroContentEffectivePlacementMode = TeachingTipHeroContentPlacementMode.Top;
			TeachingTipTestHooks.NotifyEffectiveHeroContentPlacementChanged(this);
		}
	}

	private void UpdateDynamicHeroContentPlacementToBottom()
	{
		if (HeroContentPlacement == TeachingTipHeroContentPlacementMode.Auto)
		{
			UpdateDynamicHeroContentPlacementToBottomImpl();
		}
	}

	private void UpdateDynamicHeroContentPlacementToBottomImpl()
	{
		VisualStateManager.GoToState(this, "HeroContentBottom", false);
		if (m_currentHeroContentEffectivePlacementMode != TeachingTipHeroContentPlacementMode.Bottom)
		{
			m_currentHeroContentEffectivePlacementMode = TeachingTipHeroContentPlacementMode.Bottom;
			TeachingTipTestHooks.NotifyEffectiveHeroContentPlacementChanged(this);
		}
	}

	private void OnIsOpenChanged()
	{
		if (m_ignoreNextIsOpenChanged)
		{
			m_ignoreNextIsOpenChanged = false;
		}
		else
		{
			SharedHelpers.QueueCallbackForCompositionRendering(() =>
			{
				if (m_isIdle)
				{
					if (IsOpen)
					{
						IsOpenChangedToOpen();
					}
					else
					{
						IsOpenChangedToClose();
					}
					TeachingTipTestHooks.NotifyOpenedStatusChanged(this);
				}
				else
				{
					m_ignoreNextIsOpenChanged = true;
					IsOpen = !IsOpen;
				}
			});
		}
	}

	private void IsOpenChangedToOpen()
	{
		//Reset the close reason to the default value of programmatic.
		m_lastCloseReason = TeachingTipCloseReason.Programmatic;

		m_currentBoundsInCoreWindowSpace = this.TransformToVisual(null).TransformBounds(new Rect(
			0.0,
			0.0,
			(float)(this.ActualWidth),
			(float)(this.ActualHeight)));

		Rect GetCurrentTargetBoundsInCoreWindowSpace()
		{
			var target = m_target;
			if (target != null)
			{
				SetViewportChangedEvent(target);
				return target.TransformToVisual(null).TransformBounds(new Rect(
					0.0,
					0.0,
					(float)(target.ActualWidth),
					(float)(target.ActualHeight)));
			}
			return Rect.Empty;
		}

		m_currentTargetBoundsInCoreWindowSpace = GetCurrentTargetBoundsInCoreWindowSpace();

		if (m_lightDismissIndicatorPopup == null)
		{
			CreateLightDismissIndicatorPopup();
		}
		OnIsLightDismissEnabledChanged();

		if (m_contractAnimation == null)
		{
			CreateContractAnimation();
		}
		if (m_expandAnimation == null)
		{
			CreateExpandAnimation();
		}

		// We are about to begin the process of trying to open the teaching tip, so notify that we are no longer idle.
		SetIsIdle(false);

		//If the developer defines their TeachingTip in a resource dictionary it is possible that it's template will have never been applied
		if (!m_isTemplateApplied)
		{
			this.ApplyTemplate();
		}

		if (m_popup == null || m_createNewPopupOnOpen)
		{
			CreateNewPopup();
		}

		// If the tip is not going to open because it does not fit we need to make sure that
		// the open, closing, closed life cycle still fires so that we don't cause apps to leak
		// that depend on this sequence.
		var (ignored, tipDoesNotFit) = DetermineEffectivePlacement();
		if (tipDoesNotFit)
		{
			//__RP_Marker_ClassMemberById(RuntimeProfiler.ProfId_TeachingTip, RuntimeProfiler.ProfMemberId_TeachingTip_TipDidNotOpenDueToSize);
			RaiseClosingEvent(false);
			var closedArgs = new TeachingTipClosedEventArgs(m_lastCloseReason);
			Closed?.Invoke(this, closedArgs);
			IsOpen = false;
		}
		else
		{
			var popup = m_popup;
			if (popup != null)
			{
				if (!popup.IsOpen)
				{
					// We are about to begin the process of trying to open the teaching tip, so notify that we are no longer idle.
					SetIsIdle(false);
					UpdatePopupRequestedTheme();
					popup.Child = m_rootElement;
					var lightDismissIndicatorPopup = m_lightDismissIndicatorPopup;
					if (lightDismissIndicatorPopup != null)
					{
						lightDismissIndicatorPopup.IsOpen = true;
					}
					popup.IsOpen = true;
					if (SharedHelpers.IsAnimationsEnabled()
#if HAS_UNO // Uno specific: Make sure we skip animation when the required API is not supported
						&& ApiInformation.IsTypePresent("Microsoft.UI.Composition.CompositionScopedBatch")
#endif
						)
					{
						StartExpandToOpen();
					}
					else
					{
						// We won't be playing an animation so we're immediately idle.
						SetIsIdle(true);
					}
				}
				else
				{
					// We have become Open but our popup was already open. This can happen when a close is canceled by the closing event, so make sure the idle status is correct.
					if (!m_isExpandAnimationPlaying && !m_isContractAnimationPlaying)
					{
						SetIsIdle(true);
					}
				}
			}
		}

		void HookF6Handling()
		{
			if (XamlRoot is { } xamlRoot && xamlRoot.Content is { } content)
			{
				content.PreviewKeyDown += OnF6PreviewKeyDownClicked;
				m_previewKeyDownForF6Revoker.Disposable = Disposable.Create(() => content.PreviewKeyDown -= OnF6PreviewKeyDownClicked);
				return;
			}

			// TODO: We do not have direct analogue for AcceleratorKeyActivated with DispatcherQueue in Islands/ win32. Please refer Task# 30013704 for  more details.
			if (CoreWindow.GetForCurrentThread() is { } coreWindow)
			{
				Dispatcher.AcceleratorKeyActivated += OnF6AcceleratorKeyClicked;
				m_acceleratorKeyActivatedRevoker.Disposable = Disposable.Create(() => Dispatcher.AcceleratorKeyActivated -= OnF6AcceleratorKeyClicked);
			}
		}

		HookF6Handling();

		// Make sure we are in the correct VSM state after ApplyTemplate and moving the template content from the Control to the Popup:
		OnIsLightDismissEnabledChanged();
	}

	private void IsOpenChangedToClose()
	{
		var popup = m_popup;
		if (popup != null)
		{
			if (popup.IsOpen)
			{
				// We are about to begin the process of trying to close the teaching tip, so notify that we are no longer idle.
				SetIsIdle(false);
				RaiseClosingEvent(true);
			}
			else
			{
				// We have become not Open but our popup was already not open. Lets make sure the idle status is correct.
				if (!m_isExpandAnimationPlaying && !m_isContractAnimationPlaying)
				{
					SetIsIdle(true);
				}
			}
		}

		m_acceleratorKeyActivatedRevoker.Disposable = null;
		m_previewKeyDownForF6Revoker.Disposable = null;
		m_currentEffectiveTipPlacementMode = TeachingTipPlacementMode.Auto;
		TeachingTipTestHooks.NotifyEffectivePlacementChanged(this);
	}

	private void CreateNewPopup()
	{
		m_popupOpenedRevoker.Disposable = null;
		m_popupClosedRevoker.Disposable = null;

		var popup = new Popup();
		// Set XamlRoot on the popup to handle XamlIsland/AppWindow scenarios.
		popup.XamlRoot = XamlRoot;

		popup.Opened += OnPopupOpened;
		m_popupOpenedRevoker.Disposable = Disposable.Create(() => popup.Opened -= OnPopupOpened);
		popup.Closed += OnPopupClosed;
		m_popupClosedRevoker.Disposable = Disposable.Create(() => popup.Closed -= OnPopupClosed);

		popup.ShouldConstrainToRootBounds = ShouldConstrainToRootBounds;

		m_popup = popup;
		SetPopupAutomationProperties();
		m_createNewPopupOnOpen = false;
	}

	private void OnTailVisibilityChanged()
	{
		UpdateTail();
	}

	private void OnIconSourceChanged()
	{
		var templateSettings = TemplateSettings;
		var source = IconSource;
		if (source != null)
		{
			templateSettings.IconElement = SharedHelpers.MakeIconElementFrom(source);
			VisualStateManager.GoToState(this, "Icon", false);
		}
		else
		{
			templateSettings.IconElement = null;
			VisualStateManager.GoToState(this, "NoIcon", false);
		}
	}

	private void OnPlacementMarginChanged()
	{
		if (IsOpen)
		{
			PositionPopup();
		}
	}

	private void OnIsLightDismissEnabledChanged()
	{
		if (IsLightDismissEnabled)
		{
			VisualStateManager.GoToState(this, "LightDismiss", false);
			var lightDismissIndicatorPopup = m_lightDismissIndicatorPopup;
			if (lightDismissIndicatorPopup != null)
			{
				lightDismissIndicatorPopup.IsLightDismissEnabled = true;
				lightDismissIndicatorPopup.Closed += OnLightDismissIndicatorPopupClosed;
				m_lightDismissIndicatorPopupClosedRevoker.Disposable = Disposable.Create(() => lightDismissIndicatorPopup.Closed -= OnLightDismissIndicatorPopupClosed);
				lightDismissIndicatorPopup.PreviewKeyDown += OnF6PopupPreviewKeyDownClicked;
				m_lightDismissIndicatorPopupPreviewKeyDownForF6Revoker.Disposable = Disposable.Create(() => lightDismissIndicatorPopup.PreviewKeyDown -= OnF6PopupPreviewKeyDownClicked);
			}
		}
		else
		{
			VisualStateManager.GoToState(this, "NormalDismiss", false);
			if (m_lightDismissIndicatorPopup is { } lightDismissIndicatorPopup)
			{
				lightDismissIndicatorPopup.IsLightDismissEnabled = false;
			}
			m_lightDismissIndicatorPopupClosedRevoker.Disposable = null;
			m_lightDismissIndicatorPopupPreviewKeyDownForF6Revoker.Disposable = null;
		}
		UpdateButtonsState();
	}

	private void OnShouldConstrainToRootBoundsChanged()
	{
		// ShouldConstrainToRootBounds is a property that can only be set on a popup before it is opened.
		// If we have opened the tip's popup and then this property changes we will need to discard the old popup
		// and replace it with a new popup.  This variable indicates this state.

		if (m_popup is not null)
		{
			m_createNewPopupOnOpen = true;
		}
	}

	private void OnHeroContentPlacementChanged()
	{
		switch (HeroContentPlacement)
		{
			case TeachingTipHeroContentPlacementMode.Auto:
				break;
			case TeachingTipHeroContentPlacementMode.Top:
				UpdateDynamicHeroContentPlacementToTopImpl();
				break;
			case TeachingTipHeroContentPlacementMode.Bottom:
				UpdateDynamicHeroContentPlacementToBottomImpl();
				break;
		}

		// Setting m_currentEffectiveTipPlacementMode to var ensures that the next time position popup is called we'll rerun the DetermineEffectivePlacement
		// algorithm. If we did not do this and the popup was opened the algorithm would maintain the current effective placement mode, which we don't want
		// since the hero content placement contributes to the choice of tip placement mode.
		m_currentEffectiveTipPlacementMode = TeachingTipPlacementMode.Auto;
		TeachingTipTestHooks.NotifyEffectivePlacementChanged(this);
		if (IsOpen)
		{
			PositionPopup();
		}
	}

	private void OnContentSizeChanged(object sender, SizeChangedEventArgs args)
	{
		UpdateSizeBasedTemplateSettings();
		// Reset the currentEffectivePlacementMode so that the tail will be updated for the new size as well.
		m_currentEffectiveTipPlacementMode = TeachingTipPlacementMode.Auto;
		TeachingTipTestHooks.NotifyEffectivePlacementChanged(this);
		if (IsOpen)
		{
			PositionPopup();
		}
		var expandAnimation = m_expandAnimation;
		if (expandAnimation != null)
		{
			expandAnimation.SetScalarParameter("Width", (float)args.NewSize.Width);
			expandAnimation.SetScalarParameter("Height", (float)args.NewSize.Height);
		}
		var contractAnimation = m_contractAnimation;
		if (contractAnimation != null)
		{
			contractAnimation.SetScalarParameter("Width", (float)args.NewSize.Width);
			contractAnimation.SetScalarParameter("Height", (float)args.NewSize.Height);
		}
	}

	private void OnF6AcceleratorKeyClicked(CoreDispatcher coreDispatcher, AcceleratorKeyEventArgs args)
	{
		if (!args.Handled &&
			IsOpen &&
			args.VirtualKey == VirtualKey.F6 &&
			args.EventType == CoreAcceleratorKeyEventType.KeyDown)
		{
			args.Handled = HandleF6Clicked();
		}
	}

	private void OnF6PreviewKeyDownClicked(object sender, KeyRoutedEventArgs args)
	{
		if (!args.Handled && IsOpen && args.Key == VirtualKey.F6)
		{
			args.Handled = HandleF6Clicked();
		}
	}

	private void OnF6PopupPreviewKeyDownClicked(object sender, KeyRoutedEventArgs args)
	{
		if (!args.Handled &&
			IsOpen &&
			args.Key == VirtualKey.F6)
		{
			args.Handled = HandleF6Clicked(/*fromPopup*/true);
		}
	}

	private bool HandleF6Clicked(bool fromPopup = false)
	{
		// Logging usage telemetry
		if (m_hasF6BeenInvoked)
		{
			//__RP_Marker_ClassMemberById(RuntimeProfiler::ProfId_TeachingTip, RuntimeProfiler::ProfMemberId_TeachingTip_F6AccessKey_SubsequentInvocation);
		}
		else
		{
			//__RP_Marker_ClassMemberById(RuntimeProfiler::ProfId_TeachingTip, RuntimeProfiler::ProfMemberId_TeachingTip_F6AccessKey_FirstInvocation);
			m_hasF6BeenInvoked = true;
		}

		bool GetHasFocusInSubtree()
		{
			if (m_rootElement is { } rootElement)
			{
				DependencyObject GetCurrent(UIElement rootElement) =>
					FocusManager.GetFocusedElement(rootElement.XamlRoot) as DependencyObject;

				var current = GetCurrent(rootElement);

				while (current is not null)
				{
					if ((current as UIElement) == rootElement)
					{
						return true;
					}
					current = VisualTreeHelper.GetParent(current);
				}
			}
			return false;
		}

		var hasFocusInSubtree = GetHasFocusInSubtree();

		if (hasFocusInSubtree && fromPopup)
		{
			bool setFocus = false;
			if (m_previouslyFocusedElement is not null &&
				!m_previouslyFocusedElement.IsDisposed &&
				m_previouslyFocusedElement.Target is DependencyObject strongPreviouslyFocused)
			{
				setFocus = SetFocus(strongPreviouslyFocused, FocusState.Programmatic);
			}
			WeakReferencePool.ReturnWeakReference(this, m_previouslyFocusedElement);
			m_previouslyFocusedElement = null;
			return setFocus;
		}
		else if (!hasFocusInSubtree && !fromPopup)
		{
			if (IsLightDismissEnabled)
			{
				var focusable = FocusManager.FindFirstFocusableElement(m_rootElement);
				if (focusable is not null)
				{
					SetFocus(focusable, FocusState.Keyboard);
					return true;
				}
			}

			Button GetF6Button()
			{
				var firstButton = m_closeButton;
				var secondButton = m_alternateCloseButton;
				//Prefer the close button to the alternate, except when there is no content.
				if (CloseButtonContent is null)
				{
					(secondButton, firstButton) = (firstButton, secondButton);
				}
				if (firstButton is not null && firstButton.Visibility == Visibility.Visible)
				{
					return firstButton;
				}
				else if (secondButton is not null && secondButton.Visibility == Visibility.Visible)
				{
					return secondButton;
				}
				return null;
			}

			Button f6Button = GetF6Button();

			if (f6Button is not null)
			{
				void ScopedHandler(object sender, GettingFocusEventArgs args)
				{
					m_previouslyFocusedElement = WeakReferencePool.RentWeakReference(this, args.OldFocusedElement);
				}

				f6Button.GettingFocus += ScopedHandler;
				using var _ = Disposable.Create(() => f6Button.GettingFocus -= ScopedHandler);
				bool setFocus = f6Button.Focus(FocusState.Keyboard);
				return setFocus;
			}
		}
		return false;
	}

	private void OnAutomationNameChanged(object sender, object args) =>
		SetPopupAutomationProperties();

	private void OnAutomationIdChanged(object sender, object args) =>
		SetPopupAutomationProperties();

	private void OnCloseButtonClicked(object sender, RoutedEventArgs args)
	{
		CloseButtonClick?.Invoke(this, null);
		m_lastCloseReason = TeachingTipCloseReason.CloseButton;
		IsOpen = false;
	}

	private void OnActionButtonClicked(object sender, RoutedEventArgs args) =>
		ActionButtonClick?.Invoke(this, null);

	private void OnPopupOpened(object sender, object args)
	{
		if (XamlRoot is { } xamlRoot)
		{
			m_currentXamlRootSize = xamlRoot.Size;
			xamlRoot.Changed += XamlRootChanged;
			m_xamlRootChangedRevoker.Disposable = Disposable.Create(() => xamlRoot.Changed -= XamlRootChanged);
			if (m_popup is { } popup)
			{
				if (popup.Child is { } popupContent)
				{
					// This handler is not required for Winui3 because the framework bug this works around has been fixed.
					popupContent.PreviewKeyDown += OnF6PopupPreviewKeyDownClicked;
					m_popupPreviewKeyDownForF6Revoker.Disposable = Disposable.Create(() => popupContent.PreviewKeyDown -= OnF6PopupPreviewKeyDownClicked);
				}
			}
		}

		var teachingTipPeer = FrameworkElementAutomationPeer.FromElement(this) as TeachingTipAutomationPeer;
		if (teachingTipPeer != null)
		{
			string GetNotificationString()
			{
				string GetAppName()
				{
					try
					{
						var package = Package.Current;
						if (package != null)
						{
							return package.DisplayName;
						}
					}
					catch { }

					return string.Empty;
				}
				var appName = GetAppName();

				if (!string.IsNullOrEmpty(appName))
				{
					return StringUtil.FormatString(
						ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_TeachingTipNotification),
						appName,
						AutomationProperties.GetName(m_popup));
				}
				else
				{
					return StringUtil.FormatString(
						ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_TeachingTipNotificationWithoutAppName),
						AutomationProperties.GetName(m_popup));
				}
			}

			var notificationString = GetNotificationString();
			teachingTipPeer.RaiseWindowOpenedEvent(notificationString);
		}

		if (IsLightDismissEnabled)
		{
			var focusable = FocusManager.FindFirstFocusableElement(m_rootElement);
			if (focusable is not null)
			{
				SetFocus(focusable, FocusState.Programmatic);
			}
		}
	}

	private void OnPopupClosed(object sender, object args)
	{
		m_xamlRootChangedRevoker.Disposable = null;

		var lightDismissIndicatorPopup = m_lightDismissIndicatorPopup;
		if (lightDismissIndicatorPopup != null)
		{
			lightDismissIndicatorPopup.IsOpen = false;
		}

		var popup = m_popup;
		if (popup != null)
		{
			popup.Child = null;
		}

		var myArgs = new TeachingTipClosedEventArgs(m_lastCloseReason);

		Closed?.Invoke(this, myArgs);

		//If we were closed by the close button and we have tracked a previously focused element because F6 was used
		//To give the tip focus, then we return focus when the popup closes.
		if (m_lastCloseReason == TeachingTipCloseReason.CloseButton)
		{
			if (m_previouslyFocusedElement is not null &&
				!m_previouslyFocusedElement.IsDisposed &&
				m_previouslyFocusedElement.Target is DependencyObject previouslyFocusedElement)
			{
				SetFocus(previouslyFocusedElement, FocusState.Programmatic);
			}
		}
		WeakReferencePool.ReturnWeakReference(this, m_previouslyFocusedElement);
		m_previouslyFocusedElement = null;

		var teachingTipPeer = FrameworkElementAutomationPeer.FromElement(this) as TeachingTipAutomationPeer;
		if (teachingTipPeer != null)
		{
			teachingTipPeer.RaiseWindowClosedEvent();
		}
	}

	private void ClosePopupOnUnloadEvent(object sender, RoutedEventArgs args)
	{
		IsOpen = false;
		ClosePopup();
	}

	private void OnLightDismissIndicatorPopupClosed(object sender, object args)
	{
		if (IsOpen)
		{
			m_lastCloseReason = TeachingTipCloseReason.LightDismiss;
		}
		IsOpen = false;
	}

	private void RaiseClosingEvent(bool attachDeferralCompletedHandler)
	{
		var args = new TeachingTipClosingEventArgs(m_lastCloseReason);

		if (attachDeferralCompletedHandler)
		{
			Deferral instance = new Deferral(() =>
			{
				if (!args.Cancel)
				{
					ClosePopupWithAnimationIfAvailable();
				}
				else
				{
					// The developer has changed the Cancel property to true, indicating that they wish to Cancel the
					// closing of this tip, so we need to revert the IsOpen property to true.
					IsOpen = true;
				}
			});

			args.Deferral = instance;

			args.IncrementDeferralCount();
			Closing?.Invoke(this, args);
			args.DecrementDeferralCount();
		}
		else
		{
			Closing?.Invoke(this, args);
		}
	}

	private void ClosePopupWithAnimationIfAvailable()
	{
		if (m_popup != null && m_popup.IsOpen)
		{
			// Contract animation
			if (SharedHelpers.IsAnimationsEnabled()
#if HAS_UNO // Uno specific: Make sure we skip animation when the required API is not supported
				&& ApiInformation.IsTypePresent("Microsoft.UI.Composition.CompositionScopedBatch")
#endif
				)
			{
				StartContractToClose();
			}
			else
			{
				ClosePopup();
			}

			// Under normal circumstances we would have launched an animation just now, if we did not then we should make sure
			// that the idle state is correct.
			if (!m_isContractAnimationPlaying && !m_isExpandAnimationPlaying)
			{
				SetIsIdle(true);
			}
		}
	}

	private void ClosePopup()
	{
		var popup = m_popup;
		if (popup != null)
		{
			popup.IsOpen = false;
		}

		var lightDismissIndicatorPopup = m_lightDismissIndicatorPopup;
		if (lightDismissIndicatorPopup != null)
		{
			lightDismissIndicatorPopup.IsOpen = false;
		}

		if (m_tailOcclusionGrid is { } tailOcclusionGrid)
		{
			// A previous close animation may have left the rootGrid's scale at a very small value and if this teaching tip
			// is shown again then its text would be rasterized at this small scale and blown up ~20x. To fix this we have to
			// reset the scale after the popup has closed so that if the teaching tip is re-shown the render pass does not use the
			// small scale.
			tailOcclusionGrid.Scale = new Vector3(1.0f, 1.0f, 1.0f);
		}
	}

	private TeachingTipPlacementMode GetFlowDirectionAdjustedPlacement(TeachingTipPlacementMode placementMode)
	{
		if (FlowDirection == FlowDirection.LeftToRight)
		{
			return placementMode;
		}
		else
		{
			switch (placementMode)
			{
				case TeachingTipPlacementMode.Auto:
					return TeachingTipPlacementMode.Auto;
				case TeachingTipPlacementMode.Left:
					return TeachingTipPlacementMode.Right;
				case TeachingTipPlacementMode.Right:
					return TeachingTipPlacementMode.Left;
				case TeachingTipPlacementMode.Top:
					return TeachingTipPlacementMode.Top;
				case TeachingTipPlacementMode.Bottom:
					return TeachingTipPlacementMode.Bottom;
				case TeachingTipPlacementMode.LeftBottom:
					return TeachingTipPlacementMode.RightBottom;
				case TeachingTipPlacementMode.LeftTop:
					return TeachingTipPlacementMode.RightTop;
				case TeachingTipPlacementMode.TopLeft:
					return TeachingTipPlacementMode.TopRight;
				case TeachingTipPlacementMode.TopRight:
					return TeachingTipPlacementMode.TopLeft;
				case TeachingTipPlacementMode.RightTop:
					return TeachingTipPlacementMode.LeftTop;
				case TeachingTipPlacementMode.RightBottom:
					return TeachingTipPlacementMode.LeftBottom;
				case TeachingTipPlacementMode.BottomRight:
					return TeachingTipPlacementMode.BottomLeft;
				case TeachingTipPlacementMode.BottomLeft:
					return TeachingTipPlacementMode.BottomRight;
				case TeachingTipPlacementMode.Center:
					return TeachingTipPlacementMode.Center;
			}
		}
		return TeachingTipPlacementMode.Auto;
	}

	private void OnTargetChanged()
	{
		m_targetEffectiveViewportChangedRevoker.Disposable = null;
		m_targetLoadedRevoker.Disposable = null;

		var target = Target;
		m_target = target;

		if (target != null)
		{
			target.Loaded += OnTargetLoaded;
		}

		if (IsOpen)
		{
			if (target is not null && target.IsLoaded)
			{
				m_currentTargetBoundsInCoreWindowSpace = target.TransformToVisual(null).TransformBounds(new Rect(
					0.0,
					0.0,
					(float)(target.ActualWidth),
					(float)(target.ActualHeight)));
				SetViewportChangedEvent(target);
			}

			// if we have a target that is not yet loaded, skip positioning the flayout for now, that will happen once the target loads.
			if (target is null || (target is not null && target.IsLoaded))
			{
				PositionPopup();
			}
		}
	}

	private void SetViewportChangedEvent(FrameworkElement target)
	{
		if (m_tipFollowsTarget)
		{
			target.EffectiveViewportChanged += OnTargetLayoutUpdated;
			m_targetEffectiveViewportChangedRevoker.Disposable = Disposable.Create(() => target.EffectiveViewportChanged -= OnTargetLayoutUpdated);
			this.EffectiveViewportChanged += OnTargetLayoutUpdated;
			m_effectiveViewportChangedRevoker.Disposable = Disposable.Create(() => this.EffectiveViewportChanged -= OnTargetLayoutUpdated);
		}
	}

	private void RevokeViewportChangedEvent()
	{
		m_targetEffectiveViewportChangedRevoker.Disposable = null;
		m_effectiveViewportChangedRevoker.Disposable = null;
	}

	private void XamlRootChanged(XamlRoot xamlRoot, XamlRootChangedEventArgs args)
	{
		// Reposition popup when target has finished determining its own position.
		SharedHelpers.QueueCallbackForCompositionRendering(() =>
		{
			if (xamlRoot.Size != m_currentXamlRootSize)
			{
				m_currentXamlRootSize = xamlRoot.Size;
				RepositionPopup();
			}
		});
	}

	void RepositionPopup()
	{
		if (IsOpen)
		{
			Rect GetNewTargetBounds()
			{
				var target = m_target;
				if (target != null)
				{
					return target.TransformToVisual(null).TransformBounds(
						new Rect(
							0.0,
							0.0,
							(float)(target.ActualWidth),
							(float)(target.ActualHeight)));
				}
				return Rect.Empty;
			}

			var newTargetBounds = GetNewTargetBounds();

			var newCurrentBounds = TransformToVisual(null).TransformBounds(
				new Rect(
					0.0,
					0.0,
					(float)(this.ActualWidth),
					(float)(this.ActualHeight)));

			if (newTargetBounds != m_currentTargetBoundsInCoreWindowSpace || newCurrentBounds != m_currentBoundsInCoreWindowSpace)
			{
				m_currentBoundsInCoreWindowSpace = newCurrentBounds;
				m_currentTargetBoundsInCoreWindowSpace = newTargetBounds;
				PositionPopup();
			}
		}
	}

	private void OnTargetLoaded(object sender, object args)
	{
		RepositionPopup();
	}

	private void OnTargetLayoutUpdated(object sender, object args)
	{
		RepositionPopup();
	}

	private void CreateExpandAnimation()
	{
		// TODO: Uno specific - CompositionEasingFunction and related types not supported yet.
		if (!ApiInformation.IsMethodPresent("Microsoft.UI.Composition.Compositor", "CreateCubicBezierEasingFunction"))
		{
			return;
		}

		var compositor = CompositionTarget.GetCompositorForCurrentThread();

		CompositionEasingFunction GetExpandEasingFunction(Microsoft.UI.Composition.Compositor compositor)
		{
			if (m_expandEasingFunction == null)
			{
				var easingFunction = compositor.CreateCubicBezierEasingFunction(s_expandAnimationEasingCurveControlPoint1, s_expandAnimationEasingCurveControlPoint2);
				m_expandEasingFunction = easingFunction;
				return (CompositionEasingFunction)(easingFunction);
			}
			return m_expandEasingFunction;
		}

		var expandEasingFunction = GetExpandEasingFunction(compositor);

		KeyFrameAnimation GetExpandAnimation(Compositor compositor, CompositionEasingFunction expandEasingFunction)
		{
			var expandAnimation = compositor.CreateVector3KeyFrameAnimation();

			var widthToUse = s_defaultTipHeightAndWidth;
			var heightToUse = s_defaultTipHeightAndWidth;

			if (m_tailOcclusionGrid is { } tailOcclusionGrid)
			{
				if ((tailOcclusionGrid.ActualWidth != 0) && (tailOcclusionGrid.ActualHeight != 0))
				{
					widthToUse = (float)tailOcclusionGrid.ActualWidth;
					heightToUse = (float)tailOcclusionGrid.ActualHeight;
				}
			}
			expandAnimation.SetScalarParameter("Width", widthToUse);
			expandAnimation.SetScalarParameter("Height", heightToUse);

			expandAnimation.InsertExpressionKeyFrame(0.0f, "Vector3(Min(0.01, 20.0 / Width), Min(0.01, 20.0 / Height), 1.0)");
			expandAnimation.InsertKeyFrame(1.0f, new Vector3(1.0f, 1.0f, 1.0f), expandEasingFunction);
			expandAnimation.Duration = m_expandAnimationDuration;
			expandAnimation.Target = s_scaleTargetName;
			return expandAnimation;
		}

		m_expandAnimation = GetExpandAnimation(compositor, expandEasingFunction);

		KeyFrameAnimation GetExpandElevationAnimation(Compositor compositor, CompositionEasingFunction expandEasingFunction)
		{
			var expandElevationAnimation = compositor.CreateVector3KeyFrameAnimation();
			expandElevationAnimation.InsertExpressionKeyFrame(1.0f, "Vector3(this.Target.Translation.X, this.Target.Translation.Y, contentElevation)", expandEasingFunction);
			expandElevationAnimation.SetScalarParameter("contentElevation", m_contentElevation);
			expandElevationAnimation.Duration = m_expandAnimationDuration;
			expandElevationAnimation.Target = s_translationTargetName;
			return expandElevationAnimation;
		}

		m_expandElevationAnimation = GetExpandElevationAnimation(compositor, expandEasingFunction);
	}

	private void CreateContractAnimation()
	{
		// TODO: Uno specific - CompositionEasingFunction and related types not supported yet.
		if (!ApiInformation.IsMethodPresent("Microsoft.UI.Composition.Compositor", "CreateCubicBezierEasingFunction"))
		{
			return;
		}

		var compositor = CompositionTarget.GetCompositorForCurrentThread();

		CompositionEasingFunction GetContractEasingFunction(Compositor compositor)
		{
			if (m_contractEasingFunction == null)
			{
				var easingFunction = compositor.CreateCubicBezierEasingFunction(s_contractAnimationEasingCurveControlPoint1, s_contractAnimationEasingCurveControlPoint2);
				m_contractEasingFunction = easingFunction;
				return easingFunction;
			}
			return m_contractEasingFunction;
		}
		var contractEasingFunction = GetContractEasingFunction(compositor);

		KeyFrameAnimation GetContractAnimation(Compositor compositor, CompositionEasingFunction contractEasingFunction)
		{
			var contractAnimation = compositor.CreateVector3KeyFrameAnimation();
			var tailOcclusionGrid = m_tailOcclusionGrid;
			if (tailOcclusionGrid != null)
			{
				contractAnimation.SetScalarParameter("Width", (float)(tailOcclusionGrid.ActualWidth));
				contractAnimation.SetScalarParameter("Height", (float)(tailOcclusionGrid.ActualHeight));
			}

			else
			{
				contractAnimation.SetScalarParameter("Width", s_defaultTipHeightAndWidth);
				contractAnimation.SetScalarParameter("Height", s_defaultTipHeightAndWidth);
			}

			contractAnimation.InsertKeyFrame(0.0f, new Vector3(1.0f, 1.0f, 1.0f));
			contractAnimation.InsertExpressionKeyFrame(1.0f, "Vector3(20.0 / Width, 20.0 / Height, 1.0)", contractEasingFunction);
			contractAnimation.Duration = m_contractAnimationDuration;
			contractAnimation.Target = s_scaleTargetName;
			return contractAnimation;
		}

		m_contractAnimation = GetContractAnimation(compositor, contractEasingFunction);

		KeyFrameAnimation GetContractElevationAnimation(Compositor compositor, CompositionEasingFunction contractEasingFunction)
		{
			var contractElevationAnimation = compositor.CreateVector3KeyFrameAnimation();
			// animating to 0.01f instead of 0.0f as work around to internal issue 26001712 which was causing text clipping.
			contractElevationAnimation.InsertExpressionKeyFrame(1.0f, "Vector3(this.Target.Translation.X, this.Target.Translation.Y, 0.01f)", contractEasingFunction);
			contractElevationAnimation.Duration = m_contractAnimationDuration;
			contractElevationAnimation.Target = s_translationTargetName;
			return contractElevationAnimation;
		}

		m_contractElevationAnimation = GetContractElevationAnimation(compositor, contractEasingFunction);
	}

	private void StartExpandToOpen()
	{
		if (m_expandAnimation == null)
		{
			CreateExpandAnimation();
		}

		CompositionScopedBatch CreateScopedBatch()
		{
			var scopedBatch = CompositionTarget.GetCompositorForCurrentThread().CreateScopedBatch(CompositionBatchTypes.Animation); ;

			var expandAnimation = m_expandAnimation;
			if (m_expandAnimation != null)
			{
				var tailOcclusionGrid = m_tailOcclusionGrid;
				if (tailOcclusionGrid != null)
				{
					tailOcclusionGrid.StartAnimation(expandAnimation);
					m_isExpandAnimationPlaying = true;
				}
				var tailEdgeBorder = m_tailEdgeBorder;
				if (tailEdgeBorder != null)
				{
					tailEdgeBorder.StartAnimation(expandAnimation);
					m_isExpandAnimationPlaying = true;
				}
			}
			var expandElevationAnimation = m_expandElevationAnimation;
			if (expandElevationAnimation != null)
			{
				var contentRootGrid = m_contentRootGrid;
				if (contentRootGrid != null)
				{
					contentRootGrid.StartAnimation(expandElevationAnimation);
					m_isExpandAnimationPlaying = true;
				}
			}
			return scopedBatch;
		}

		var scopedBatch = CreateScopedBatch();

		scopedBatch.End();

		scopedBatch.Completed += (sender, args) =>
		{
			m_isExpandAnimationPlaying = false;
			if (!m_isContractAnimationPlaying)
			{
				SetIsIdle(true);
			}
		};

		// Under normal circumstances we would have launched an animation just now, if we did not then we should make sure that the idle state is correct
		if (!m_isExpandAnimationPlaying && !m_isContractAnimationPlaying)
		{
			SetIsIdle(true);
		}
	}

	private void StartContractToClose()
	{
		if (m_contractAnimation == null)
		{
			CreateContractAnimation();
		}

		CompositionScopedBatch CreateScopedBatch()
		{
			var scopedBatch = CompositionTarget.GetCompositorForCurrentThread().CreateScopedBatch(CompositionBatchTypes.Animation); ;
			var contractAnimation = m_contractAnimation;
			if (contractAnimation != null)
			{
				var tailOcclusionGrid = m_tailOcclusionGrid;
				if (tailOcclusionGrid != null)
				{
					tailOcclusionGrid.StartAnimation(contractAnimation);
					m_isContractAnimationPlaying = true;
				}
				var tailEdgeBorder = m_tailEdgeBorder;
				if (tailEdgeBorder != null)
				{
					tailEdgeBorder.StartAnimation(contractAnimation);
					m_isContractAnimationPlaying = true;
				}
			}
			var contractElevationAnimation = m_contractElevationAnimation;
			if (contractElevationAnimation != null)
			{
				var contentRootGrid = m_contentRootGrid;
				if (contentRootGrid != null)
				{
					contentRootGrid.StartAnimation(contractElevationAnimation);
					m_isContractAnimationPlaying = true;
				}
			}
			return scopedBatch;
		}

		var scopedBatch = CreateScopedBatch();

		scopedBatch.End();

		scopedBatch.Completed += (sender, args) =>
		{
			m_isContractAnimationPlaying = false;
			ClosePopup();
			if (!m_isExpandAnimationPlaying)
			{
				SetIsIdle(true);
			}
		};
	}

	private Tuple<TeachingTipPlacementMode, bool> DetermineEffectivePlacement()
	{
		// Because we do not have access to APIs to give us details about multi monitor scenarios we do not have the ability to correctly
		// Place the tip in scenarios where we have an out of root bounds tip. Since this is the case we have decided to do no special
		// calculations and return the provided value or top if var was set. This behavior can be removed via the
		// SetReturnTopForOutOfWindowBounds test hook.
		if (!ShouldConstrainToRootBounds && m_returnTopForOutOfWindowPlacement)
		{
			var placement = GetFlowDirectionAdjustedPlacement(PreferredPlacement);
			if (placement == TeachingTipPlacementMode.Auto)
			{
				return Tuple.Create(TeachingTipPlacementMode.Top, false);
			}
			return Tuple.Create(placement, false);
		}

		if (IsOpen && m_currentEffectiveTipPlacementMode != TeachingTipPlacementMode.Auto)
		{
			return Tuple.Create(m_currentEffectiveTipPlacementMode, false);
		}

		(double contentHeight, double contentWidth) GetContentSize()
		{
			var tailOcclusionGrid = m_tailOcclusionGrid;
			if (tailOcclusionGrid != null)
			{
				double contentHeight = tailOcclusionGrid.ActualHeight;
				double contentWidth = tailOcclusionGrid.ActualWidth;
				return (contentHeight, contentWidth);
			}
			return (0.0, 0.0);
		}

		var (contentHeight, contentWidth) = GetContentSize();

		if (m_target != null)
		{
			return DetermineEffectivePlacementTargeted(contentHeight, contentWidth);
		}
		else
		{
			return DetermineEffectivePlacementUntargeted(contentHeight, contentWidth);
		}
	}

	private Tuple<TeachingTipPlacementMode, bool> DetermineEffectivePlacementTargeted(double contentHeight, double contentWidth)
	{
		// These variables will track which positions the tip will fit in. They all start true and are
		// flipped to false when we find a display condition that is not met.
		var availability = new Dictionary<TeachingTipPlacementMode, bool>();
		availability[TeachingTipPlacementMode.Auto] = false;
		availability[TeachingTipPlacementMode.Top] = true;
		availability[TeachingTipPlacementMode.Bottom] = true;
		availability[TeachingTipPlacementMode.Right] = true;
		availability[TeachingTipPlacementMode.Left] = true;
		availability[TeachingTipPlacementMode.TopLeft] = true;
		availability[TeachingTipPlacementMode.TopRight] = true;
		availability[TeachingTipPlacementMode.BottomLeft] = true;
		availability[TeachingTipPlacementMode.BottomRight] = true;
		availability[TeachingTipPlacementMode.LeftTop] = true;
		availability[TeachingTipPlacementMode.LeftBottom] = true;
		availability[TeachingTipPlacementMode.RightTop] = true;
		availability[TeachingTipPlacementMode.RightBottom] = true;
		availability[TeachingTipPlacementMode.Center] = true;

		double tipHeight = contentHeight + TailShortSideLength();
		double tipWidth = contentWidth + TailShortSideLength();

		// We try to avoid having the tail touch the HeroContent so rule out positions where this would be required
		if (HeroContent != null)
		{
			var heroContentBorder = m_heroContentBorder;
			if (heroContentBorder != null)
			{
				var nonHeroContentRootGrid = m_nonHeroContentRootGrid;
				if (nonHeroContentRootGrid != null)
				{
					if (heroContentBorder.ActualHeight > nonHeroContentRootGrid.ActualHeight - TailLongSideActualLength())
					{
						availability[TeachingTipPlacementMode.Left] = false;
						availability[TeachingTipPlacementMode.Right] = false;
					}
				}
			}

			switch (HeroContentPlacement)
			{
				case TeachingTipHeroContentPlacementMode.Bottom:
					availability[TeachingTipPlacementMode.Top] = false;
					availability[TeachingTipPlacementMode.TopRight] = false;
					availability[TeachingTipPlacementMode.TopLeft] = false;
					availability[TeachingTipPlacementMode.RightTop] = false;
					availability[TeachingTipPlacementMode.LeftTop] = false;
					availability[TeachingTipPlacementMode.Center] = false;
					break;
				case TeachingTipHeroContentPlacementMode.Top:
					availability[TeachingTipPlacementMode.Bottom] = false;
					availability[TeachingTipPlacementMode.BottomLeft] = false;
					availability[TeachingTipPlacementMode.BottomRight] = false;
					availability[TeachingTipPlacementMode.RightBottom] = false;
					availability[TeachingTipPlacementMode.LeftBottom] = false;
					break;
			}
		}

		// When ShouldConstrainToRootBounds is true clippedTargetBounds == availableBoundsAroundTarget
		// We have to separate them because there are checks which care about both.
		var (clippedTargetBounds, availableBoundsAroundTarget) = DetermineSpaceAroundTarget();

		// If the edge of the target isn't in the window.
		if (clippedTargetBounds.Left < 0)
		{
			availability[TeachingTipPlacementMode.LeftBottom] = false;
			availability[TeachingTipPlacementMode.Left] = false;
			availability[TeachingTipPlacementMode.LeftTop] = false;
		}
		// If the right edge of the target isn't in the window.
		if (clippedTargetBounds.Right < 0)
		{
			availability[TeachingTipPlacementMode.RightBottom] = false;
			availability[TeachingTipPlacementMode.Right] = false;
			availability[TeachingTipPlacementMode.RightTop] = false;
		}
		// If the top edge of the target isn't in the window.
		if (clippedTargetBounds.Top < 0)
		{
			availability[TeachingTipPlacementMode.TopLeft] = false;
			availability[TeachingTipPlacementMode.Top] = false;
			availability[TeachingTipPlacementMode.TopRight] = false;
		}
		// If the bottom edge of the target isn't in the window
		if (clippedTargetBounds.Bottom < 0)
		{
			availability[TeachingTipPlacementMode.BottomLeft] = false;
			availability[TeachingTipPlacementMode.Bottom] = false;
			availability[TeachingTipPlacementMode.BottomRight] = false;
		}

		// If the horizontal midpoint is out of the window.
		if (clippedTargetBounds.Left < -m_currentTargetBoundsInCoreWindowSpace.Width / 2 ||
			clippedTargetBounds.Right < -m_currentTargetBoundsInCoreWindowSpace.Width / 2)
		{
			availability[TeachingTipPlacementMode.TopLeft] = false;
			availability[TeachingTipPlacementMode.Top] = false;
			availability[TeachingTipPlacementMode.TopRight] = false;
			availability[TeachingTipPlacementMode.BottomLeft] = false;
			availability[TeachingTipPlacementMode.Bottom] = false;
			availability[TeachingTipPlacementMode.BottomRight] = false;
			availability[TeachingTipPlacementMode.Center] = false;
		}

		// If the vertical midpoint is out of the window.
		if (clippedTargetBounds.Top < -m_currentTargetBoundsInCoreWindowSpace.Height / 2 ||
			clippedTargetBounds.Bottom < -m_currentTargetBoundsInCoreWindowSpace.Height / 2)
		{
			availability[TeachingTipPlacementMode.LeftBottom] = false;
			availability[TeachingTipPlacementMode.Left] = false;
			availability[TeachingTipPlacementMode.LeftTop] = false;
			availability[TeachingTipPlacementMode.RightBottom] = false;
			availability[TeachingTipPlacementMode.Right] = false;
			availability[TeachingTipPlacementMode.RightTop] = false;
			availability[TeachingTipPlacementMode.Center] = false;
		}

		// If the tip is too tall to fit between the top of the target and the top edge of the window or screen.
		if (tipHeight > availableBoundsAroundTarget.Top)
		{
			availability[TeachingTipPlacementMode.Top] = false;
			availability[TeachingTipPlacementMode.TopRight] = false;
			availability[TeachingTipPlacementMode.TopLeft] = false;
		}
		// If the total tip is too tall to fit between the center of the target and the top of the window.
		if (tipHeight > availableBoundsAroundTarget.Top + (m_currentTargetBoundsInCoreWindowSpace.Height / 2.0f))
		{
			availability[TeachingTipPlacementMode.Center] = false;
		}
		// If the tip is too tall to fit between the center of the target and the top edge of the window.
		if (contentHeight - MinimumTipEdgeToTailCenter() > availableBoundsAroundTarget.Top + (m_currentTargetBoundsInCoreWindowSpace.Height / 2.0f))
		{
			availability[TeachingTipPlacementMode.RightTop] = false;
			availability[TeachingTipPlacementMode.LeftTop] = false;
		}
		// If the tip is too tall to fit in the window when the tail is centered vertically on the target and the tip.
		if (contentHeight / 2.0f > availableBoundsAroundTarget.Top + (m_currentTargetBoundsInCoreWindowSpace.Height / 2.0f) ||
			contentHeight / 2.0f > availableBoundsAroundTarget.Bottom + (m_currentTargetBoundsInCoreWindowSpace.Height / 2.0f))
		{
			availability[TeachingTipPlacementMode.Right] = false;
			availability[TeachingTipPlacementMode.Left] = false;
		}
		// If the tip is too tall to fit between the center of the target and the bottom edge of the window.
		if (contentHeight - MinimumTipEdgeToTailCenter() > availableBoundsAroundTarget.Bottom + (m_currentTargetBoundsInCoreWindowSpace.Height / 2.0f))
		{
			availability[TeachingTipPlacementMode.RightBottom] = false;
			availability[TeachingTipPlacementMode.LeftBottom] = false;
		}
		// If the tip is too tall to fit between the bottom of the target and the bottom edge of the window.
		if (tipHeight > availableBoundsAroundTarget.Bottom)
		{
			availability[TeachingTipPlacementMode.Bottom] = false;
			availability[TeachingTipPlacementMode.BottomLeft] = false;
			availability[TeachingTipPlacementMode.BottomRight] = false;
		}

		// If the tip is too wide to fit between the left edge of the target and the left edge of the window.
		if (tipWidth > availableBoundsAroundTarget.Left)
		{
			availability[TeachingTipPlacementMode.Left] = false;
			availability[TeachingTipPlacementMode.LeftTop] = false;
			availability[TeachingTipPlacementMode.LeftBottom] = false;
		}
		// If the tip is too wide to fit between the center of the target and the left edge of the window.
		if (contentWidth - MinimumTipEdgeToTailCenter() > availableBoundsAroundTarget.Left + (m_currentTargetBoundsInCoreWindowSpace.Width / 2.0f))
		{
			availability[TeachingTipPlacementMode.TopLeft] = false;
			availability[TeachingTipPlacementMode.BottomLeft] = false;
		}
		// If the tip is too wide to fit in the window when the tail is centered horizontally on the target and the tip.
		if (contentWidth / 2.0f > availableBoundsAroundTarget.Left + (m_currentTargetBoundsInCoreWindowSpace.Width / 2.0f) ||
			contentWidth / 2.0f > availableBoundsAroundTarget.Right + (m_currentTargetBoundsInCoreWindowSpace.Width / 2.0f))
		{
			availability[TeachingTipPlacementMode.Top] = false;
			availability[TeachingTipPlacementMode.Bottom] = false;
			availability[TeachingTipPlacementMode.Center] = false;
		}
		// If the tip is too wide to fit between the center of the target and the right edge of the window.
		if (contentWidth - MinimumTipEdgeToTailCenter() > availableBoundsAroundTarget.Right + (m_currentTargetBoundsInCoreWindowSpace.Width / 2.0f))
		{
			availability[TeachingTipPlacementMode.TopRight] = false;
			availability[TeachingTipPlacementMode.BottomRight] = false;
		}
		// If the tip is too wide to fit between the right edge of the target and the right edge of the window.
		if (tipWidth > availableBoundsAroundTarget.Right)
		{
			availability[TeachingTipPlacementMode.Right] = false;
			availability[TeachingTipPlacementMode.RightTop] = false;
			availability[TeachingTipPlacementMode.RightBottom] = false;
		}

		var wantedDirection = GetFlowDirectionAdjustedPlacement(PreferredPlacement);
		var priorities = GetPlacementFallbackOrder(wantedDirection);

		foreach (var mode in priorities)
		{
			if (availability[mode])
			{
				return Tuple.Create(mode, false);
			}
		}
		// The teaching tip wont fit anywhere, set tipDoesNotFit to indicate that we should not open.
		return Tuple.Create(TeachingTipPlacementMode.Top, true);
	}

	Tuple<TeachingTipPlacementMode, bool> DetermineEffectivePlacementUntargeted(double contentHeight, double contentWidth)
	{
		var windowBounds = GetWindowBounds();
		if (!ShouldConstrainToRootBounds)
		{
			var screenBoundsInCoreWindowSpace = GetEffectiveScreenBoundsInCoreWindowSpace(windowBounds);
			if (screenBoundsInCoreWindowSpace.Height > contentHeight && screenBoundsInCoreWindowSpace.Width > contentWidth)
			{
				return Tuple.Create(TeachingTipPlacementMode.Bottom, false);
			}
		}
		else
		{
			var windowBoundsInCoreWindowSpace = GetEffectiveWindowBoundsInCoreWindowSpace(windowBounds);
			if (windowBoundsInCoreWindowSpace.Height > contentHeight && windowBoundsInCoreWindowSpace.Width > contentWidth)
			{
				return Tuple.Create(TeachingTipPlacementMode.Bottom, false);
			}
		}

		// The teaching tip doesn't fit in the window/screen set tipDoesNotFit to indicate that we should not open.
		return Tuple.Create(TeachingTipPlacementMode.Top, true);
	}

	private Tuple<Thickness, Thickness> DetermineSpaceAroundTarget()
	{
		var shouldConstrainToRootBounds = ShouldConstrainToRootBounds;

		(Rect windowBoundsInCoreWindowSpace, Rect screenBoundsInCoreWindowSpace) GetBoundsInCoreWindowSpace()
		{
			var windowBounds = GetWindowBounds();
			return
				(GetEffectiveWindowBoundsInCoreWindowSpace(windowBounds),
				GetEffectiveScreenBoundsInCoreWindowSpace(windowBounds));
		}

		var (windowBoundsInCoreWindowSpace, screenBoundsInCoreWindowSpace) = GetBoundsInCoreWindowSpace();


		Thickness windowSpaceAroundTarget = new Thickness(
			// Target.Left - Window.Left
			m_currentTargetBoundsInCoreWindowSpace.X - /* 0 except with test window bounds */ windowBoundsInCoreWindowSpace.X,
			// Target.Top - Window.Top
			m_currentTargetBoundsInCoreWindowSpace.Y - /* 0 except with test window bounds */ windowBoundsInCoreWindowSpace.Y,
			// Window.Right - Target.Right
			(windowBoundsInCoreWindowSpace.X + windowBoundsInCoreWindowSpace.Width) - (m_currentTargetBoundsInCoreWindowSpace.X + m_currentTargetBoundsInCoreWindowSpace.Width),
			// Screen.Right - Target.Right
			(windowBoundsInCoreWindowSpace.Y + windowBoundsInCoreWindowSpace.Height) - (m_currentTargetBoundsInCoreWindowSpace.Y + m_currentTargetBoundsInCoreWindowSpace.Height));

		Thickness GetScreenSpaceAroundTarget(Rect screenBoundsInCoreWindowSpace, Thickness windowSpaceAroundTarget)
		{
			if (!ShouldConstrainToRootBounds)
			{
				return new Thickness(
					// Target.Left - Screen.Left
					m_currentTargetBoundsInCoreWindowSpace.X - screenBoundsInCoreWindowSpace.X,
					// Target.Top - Screen.Top
					m_currentTargetBoundsInCoreWindowSpace.Y - screenBoundsInCoreWindowSpace.Y,
					// Screen.Right - Target.Right
					(screenBoundsInCoreWindowSpace.X + screenBoundsInCoreWindowSpace.Width) - (m_currentTargetBoundsInCoreWindowSpace.X + m_currentTargetBoundsInCoreWindowSpace.Width),
					// Screen.Bottom - Target.Bottom
					(screenBoundsInCoreWindowSpace.Y + screenBoundsInCoreWindowSpace.Height) - (m_currentTargetBoundsInCoreWindowSpace.Y + m_currentTargetBoundsInCoreWindowSpace.Height));
			}
			return windowSpaceAroundTarget;
		}

		Thickness screenSpaceAroundTarget = GetScreenSpaceAroundTarget(screenBoundsInCoreWindowSpace, windowSpaceAroundTarget);

		return Tuple.Create(windowSpaceAroundTarget, screenSpaceAroundTarget);
	}

	private Rect GetEffectiveWindowBoundsInCoreWindowSpace(Rect windowBounds)
	{
		if (m_useTestWindowBounds)
		{
			return m_testWindowBoundsInCoreWindowSpace;
		}
		else
		{
			return new Rect(0, 0, windowBounds.Width, windowBounds.Height);
		}

	}

	private Rect GetEffectiveScreenBoundsInCoreWindowSpace(Rect windowBounds)
	{
		if (!m_useTestScreenBounds && !ShouldConstrainToRootBounds)
		{
			if (m_returnTopForOutOfWindowPlacement)
			{
				throw new InvalidOperationException("When returnTopForOutOfWindowPlacement is true we will never need to get the screen bounds");
			}

			var displayInfo = DisplayInformation.GetForCurrentView();
			var scaleFactor = displayInfo.RawPixelsPerViewPixel;
			return new Rect(
				-windowBounds.X,
				-windowBounds.Y,
				displayInfo.ScreenHeightInRawPixels / (float)(scaleFactor),
				displayInfo.ScreenWidthInRawPixels / (float)(scaleFactor));
		}
		return m_testScreenBoundsInCoreWindowSpace;
	}

	private Rect GetWindowBounds()
	{
		if (XamlRoot is { } xamlRoot)
		{
			return new(0, 0, xamlRoot.Size.Width, xamlRoot.Size.Height);
		}
		return default;
	}

	private TeachingTipPlacementMode[] GetPlacementFallbackOrder(TeachingTipPlacementMode preferredPlacement)
	{
		var priorityList = new TeachingTipPlacementMode[13];
		priorityList[0] = TeachingTipPlacementMode.Top;
		priorityList[1] = TeachingTipPlacementMode.Bottom;
		priorityList[2] = TeachingTipPlacementMode.Left;
		priorityList[3] = TeachingTipPlacementMode.Right;
		priorityList[4] = TeachingTipPlacementMode.TopLeft;
		priorityList[5] = TeachingTipPlacementMode.TopRight;
		priorityList[6] = TeachingTipPlacementMode.BottomLeft;
		priorityList[7] = TeachingTipPlacementMode.BottomRight;
		priorityList[8] = TeachingTipPlacementMode.LeftTop;
		priorityList[9] = TeachingTipPlacementMode.LeftBottom;
		priorityList[10] = TeachingTipPlacementMode.RightTop;
		priorityList[11] = TeachingTipPlacementMode.RightBottom;
		priorityList[12] = TeachingTipPlacementMode.Center;


		if (IsPlacementBottom(preferredPlacement))
		{
			// Swap to bottom > top
			(priorityList[1], priorityList[0]) = (priorityList[0], priorityList[1]);
			(priorityList[6], priorityList[4]) = (priorityList[4], priorityList[6]);
			(priorityList[7], priorityList[5]) = (priorityList[5], priorityList[7]);
		}
		else if (IsPlacementLeft(preferredPlacement))
		{
			// swap to lateral > vertical
			(priorityList[2], priorityList[0]) = (priorityList[0], priorityList[2]);
			(priorityList[3], priorityList[1]) = (priorityList[1], priorityList[3]);
			(priorityList[8], priorityList[4]) = (priorityList[4], priorityList[8]);
			(priorityList[9], priorityList[5]) = (priorityList[5], priorityList[9]);
			(priorityList[10], priorityList[6]) = (priorityList[6], priorityList[10]);
			(priorityList[11], priorityList[7]) = (priorityList[7], priorityList[11]);
		}
		else if (IsPlacementRight(preferredPlacement))
		{
			// swap to lateral > vertical
			(priorityList[2], priorityList[0]) = (priorityList[0], priorityList[2]);
			(priorityList[3], priorityList[1]) = (priorityList[1], priorityList[3]);
			(priorityList[8], priorityList[4]) = (priorityList[4], priorityList[8]);
			(priorityList[9], priorityList[5]) = (priorityList[5], priorityList[9]);
			(priorityList[10], priorityList[6]) = (priorityList[6], priorityList[10]);
			(priorityList[11], priorityList[7]) = (priorityList[7], priorityList[11]);

			// swap to right > left
			(priorityList[1], priorityList[0]) = (priorityList[0], priorityList[1]);
			(priorityList[6], priorityList[4]) = (priorityList[4], priorityList[6]);
			(priorityList[7], priorityList[5]) = (priorityList[5], priorityList[7]);
		}

		//Switch the preferred placement to first.
		var pivotPosition = priorityList.IndexOf(preferredPlacement);

		if (pivotPosition > 0)
		{
			//TODO:MZ: Verify this logic
			priorityList = priorityList
				.Skip(pivotPosition)
				.Concat(priorityList.Take(pivotPosition))
				.ToArray();
		}

		return priorityList;
	}


	private void EstablishShadows()
	{
#if TAIL_SHADOW
#if DBG
		if (m_tipShadow)
		{
			auto && tailPolygon = m_tailPolygon.get()
	
		if (tailPolygon && !tailPolygon.Shadow())
			{
				// This facilitates an experiment around faking a proper tail shadow, shadows are expensive though so we don't want it present for release builds.
				auto const tailShadow = winrt::Microsoft::UI::Xaml::Media::ThemeShadow{ };
				tailShadow.Receivers().Append(m_target.get());
				tailPolygon.Shadow(tailShadow);
				auto const tailPolygonTranslation = tailPolygon.Translation()

			tailPolygon.Translation({ tailPolygonTranslation.x, tailPolygonTranslation.y, m_tailElevation });
			}
		}
		else
		{
			m_tailPolygon.get().Shadow(nullptr);
		}
#endif
#endif
		if (m_tipShouldHaveShadow)
		{
			if (m_contentRootGrid.Shadow is null)
			{
				m_contentRootGrid.Shadow = new ThemeShadow();
				if (m_contentRootGrid is { } contentRootGrid)
				{
					var contentRootGridTranslation = contentRootGrid.Translation;
					contentRootGrid.Translation = new(contentRootGridTranslation.X, contentRootGridTranslation.Y, m_contentElevation);
				}
			}
		}
		else
		{
			m_contentRootGrid.Shadow = null;
		}
	}

	private void TrySetCenterPoint(UIElement element, Vector3 centerPoint)
	{
		if (element != null)
		{
			element.CenterPoint = centerPoint;
		}
	}

	private float TailLongSideActualLength()
	{
		var tailPolygon = m_tailPolygon;
		if (tailPolygon != null)
		{
			return (float)(Math.Max(tailPolygon.ActualHeight, tailPolygon.ActualWidth));
		}
		return 0;
	}

	private float TailLongSideLength()
	{
		return (float)(TailLongSideActualLength() - (2 * s_tailOcclusionAmount));
	}

	private float TailShortSideLength()
	{
		var tailPolygon = m_tailPolygon;
		if (tailPolygon != null)
		{
			return (float)(Math.Min(tailPolygon.ActualHeight, tailPolygon.ActualWidth) - s_tailOcclusionAmount);
		}
		return 0;
	}

	private float MinimumTipEdgeToTailEdgeMargin()
	{
		var tailOcclusionGrid = m_tailOcclusionGrid;
		if (tailOcclusionGrid != null)
		{
			return tailOcclusionGrid.ColumnDefinitions.Count > 1 ?
				(float)(tailOcclusionGrid.ColumnDefinitions[1].ActualWidth + s_tailOcclusionAmount)
				: 0.0f;
		}
		return 0;
	}

	private float MinimumTipEdgeToTailCenter()
	{
		var tailOcclusionGrid = m_tailOcclusionGrid;
		if (tailOcclusionGrid != null)
		{
			var tailPolygon = m_tailPolygon;
			if (tailPolygon != null)
			{
				return tailOcclusionGrid.ColumnDefinitions.Count > 1 ?
					(float)(tailOcclusionGrid.ColumnDefinitions[0].ActualWidth +
						tailOcclusionGrid.ColumnDefinitions[1].ActualWidth +
						(Math.Max(tailPolygon.ActualHeight, tailPolygon.ActualWidth) / 2))
					: 0.0f;
			}
		}
		return 0;
	}

	private CornerRadius GetTeachingTipCornerRadius() => CornerRadius;

	////////////////
	// Test Hooks //
	////////////////
	internal void SetExpandEasingFunction(CompositionEasingFunction easingFunction)
	{
		m_expandEasingFunction = easingFunction;
		CreateExpandAnimation();
	}

	internal void SetContractEasingFunction(CompositionEasingFunction easingFunction)
	{
		m_contractEasingFunction = easingFunction;
		CreateContractAnimation();
	}

	internal void SetTipShouldHaveShadow(bool tipShouldHaveShadow)
	{
		if (m_tipShouldHaveShadow != tipShouldHaveShadow)
		{
			m_tipShouldHaveShadow = tipShouldHaveShadow;
			EstablishShadows();
		}
	}

	internal void SetContentElevation(float elevation)
	{
		m_contentElevation = elevation;

		if (m_contentRootGrid is { } contentRootGrid)
		{
			var contentRootGridTranslation = contentRootGrid.Translation;
			m_contentRootGrid.Translation = new(contentRootGridTranslation.X, contentRootGridTranslation.Y, m_contentElevation);
		}
		if (m_expandElevationAnimation is not null)
		{
			m_expandElevationAnimation.SetScalarParameter("contentElevation", m_contentElevation);
		}
	}

	internal void SetTailElevation(float elevation)
	{
		m_tailElevation = elevation;
		if (m_tailPolygon is not null)
		{
			var tailPolygon = m_tailPolygon;
			if (tailPolygon != null)
			{
				var tailPolygonTranslation = tailPolygon.Translation;
				tailPolygon.Translation = new Vector3(tailPolygonTranslation.X, tailPolygonTranslation.Y, m_tailElevation);
			}
		}
	}

	internal void SetUseTestWindowBounds(bool useTestWindowBounds)
	{
		m_useTestWindowBounds = useTestWindowBounds;
	}

	internal void SetTestWindowBounds(Rect testWindowBounds)
	{
		m_testWindowBoundsInCoreWindowSpace = testWindowBounds;
	}

	internal void SetUseTestScreenBounds(bool useTestScreenBounds)
	{
		m_useTestScreenBounds = useTestScreenBounds;
	}

	internal void SetTestScreenBounds(Rect testScreenBounds)
	{
		m_testScreenBoundsInCoreWindowSpace = testScreenBounds;
	}

	internal void SetTipFollowsTarget(bool tipFollowsTarget)
	{
		if (m_tipFollowsTarget != tipFollowsTarget)
		{
			m_tipFollowsTarget = tipFollowsTarget;
			if (tipFollowsTarget)
			{
				var target = m_target;
				if (target != null)
				{
					SetViewportChangedEvent(target);
				}
			}
			else
			{
				RevokeViewportChangedEvent();
			}
		}
	}

	internal void SetReturnTopForOutOfWindowPlacement(bool returnTopForOutOfWindowPlacement)
	{
		m_returnTopForOutOfWindowPlacement = returnTopForOutOfWindowPlacement;
	}

	internal void SetExpandAnimationDuration(TimeSpan expandAnimationDuration)
	{
		m_expandAnimationDuration = expandAnimationDuration;
		var expandAnimation = m_expandAnimation;
		if (expandAnimation != null)
		{
			expandAnimation.Duration = m_expandAnimationDuration;
		}
		var expandElevationAnimation = m_expandElevationAnimation;
		if (expandElevationAnimation != null)
		{
			expandElevationAnimation.Duration = m_expandAnimationDuration;
		}
	}

	internal void SetContractAnimationDuration(TimeSpan contractAnimationDuration)
	{
		m_contractAnimationDuration = contractAnimationDuration;
		var contractAnimation = m_contractAnimation;
		if (contractAnimation != null)
		{
			contractAnimation.Duration = m_contractAnimationDuration;
		}
		var contractElevationAnimation = m_contractElevationAnimation;
		if (contractElevationAnimation != null)
		{
			contractElevationAnimation.Duration = m_contractAnimationDuration;
		}
	}

	internal bool GetIsIdle()
	{
		return m_isIdle;
	}

	private void SetIsIdle(bool isIdle)
	{
		if (m_isIdle != isIdle)
		{
			m_isIdle = isIdle;
			TeachingTipTestHooks.NotifyIdleStatusChanged(this);
		}
	}

	internal TeachingTipPlacementMode GetEffectivePlacement()
	{
		return m_currentEffectiveTipPlacementMode;
	}

	internal TeachingTipHeroContentPlacementMode GetEffectiveHeroContentPlacement()
	{
		return m_currentHeroContentEffectivePlacementMode;
	}

	internal double GetHorizontalOffset()
	{
		var popup = m_popup;
		if (popup != null)
		{
			return popup.HorizontalOffset;
		}
		return 0.0;
	}

	internal double GetVerticalOffset()
	{
		var popup = m_popup;
		if (popup != null)
		{
			return popup.VerticalOffset;
		}
		return 0.0;
	}

	internal Visibility GetTitleVisibility()
	{
		if (GetTemplateChild<UIElement>(s_titleTextBoxName) is { } titleTextBox)
		{
			return titleTextBox.Visibility;
		}
		return Visibility.Collapsed;
	}

	internal Visibility GetSubtitleVisibility()
	{
		if (GetTemplateChild<UIElement>(s_subtitleTextBoxName) is { } subtitleTextBox)
		{
			return subtitleTextBox.Visibility;
		}
		return Visibility.Collapsed;
	}

	private void UpdatePopupRequestedTheme()
	{
		// The way that TeachingTip reparents its content tree breaks ElementTheme calculations. Hook up a listener to
		// ActualTheme on the TeachingTip and then set the Popup's RequestedTheme to match when it changes.

		void OnUpdated(object sender, object args)
		{
			UpdatePopupRequestedTheme();
		}

		if (m_actualThemeChangedRevoker.Disposable is null)
		{
			ActualThemeChanged += OnUpdated;
			m_actualThemeChangedRevoker.Disposable = Disposable.Create(() => ActualThemeChanged -= OnUpdated);
		}

		if (m_popup is { } popup)
		{
			popup.RequestedTheme = ActualTheme;
		}
	}
}
