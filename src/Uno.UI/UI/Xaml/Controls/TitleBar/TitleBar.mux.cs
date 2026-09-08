// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TitleBar.cpp, commit fc2f82117

using System.Collections.Generic;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Media;
using Uno.Disposables;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;
using Windows.Graphics;

namespace Microsoft.UI.Xaml.Controls;

partial class TitleBar
{
	public TitleBar()
	{
		//TITLEBAR_TRACE_INFO(nullptr, TRACE_MSG_METH, METH_NAME, this);

		//__RP_Marker_ClassById(RuntimeProfiler::ProfId_TitleBar);

		SetValue(TemplateSettingsProperty, new TitleBarTemplateSettings());

		this.SetDefaultStyleKey();

		SizeChanged += OnSizeChanged;
		m_sizeChangedRevoker.Disposable = Disposable.Create(() => SizeChanged -= OnSizeChanged);

		var flowDirectionChangedToken = RegisterPropertyChangedCallback(
			FrameworkElement.FlowDirectionProperty,
			OnFlowDirectionChanged);
		m_flowDirectionChangedRevoker.Disposable = Disposable.Create(() =>
			UnregisterPropertyChangedCallback(FrameworkElement.FlowDirectionProperty, flowDirectionChangedToken));

		// Uno specific: scope the InputActivationListener subscription to the live tree
		// (the Uno equivalent of the C++ ~TitleBar cleanup below).
		Loaded += OnLoaded;
		Unloaded += OnUnloaded;
	}

	// Uno specific: Uno has no deterministic finalizer for the original C++ destructor (~TitleBar,
	// below). The only leak-prone subscription — the external InputActivationListener — is instead
	// scoped to the live tree via OnLoaded/OnUnloaded. The SizeChanged/FlowDirection revokers observe
	// only this control's own events (self-references, no leak), and the destructor's ResetTitle-on-GC
	// cannot be reproduced without a finalizer (title reset is still handled by HandleTitleChange).
	//
	// TitleBar::~TitleBar()
	// {
	//     TITLEBAR_TRACE_INFO(nullptr, TRACE_MSG_METH, METH_NAME, this);
	//
	//     m_sizeChangedRevoker.revoke();
	//     m_flowDirectionChangedRevoker.revoke();
	//
	//     if (m_inputActivationChangedToken.value)
	//     {
	//         m_inputActivationListener.InputActivationChanged(m_inputActivationChangedToken);
	//         m_inputActivationChangedToken.value = 0;
	//     }
	//
	//     const auto appWindow = TryGetAppWindow();
	//     if (appWindow)
	//     {
	//         // Safely restore default if the window title still matches what we last applied
	//         ResetTitle(Title());
	//     }
	// }

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		AttachInputActivationListener();
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
		// Uno specific: detach the InputActivationListener so it does not root this TitleBar
		// while it is out of the visual tree (the C++ destructor's revoke equivalent).
		m_inputActivationChangedToken.Disposable = null;
		m_inputActivationListener = null;
	}

	// Uno specific: extracted from OnApplyTemplate so the subscription can be (re)attached on Loaded.
	private void AttachInputActivationListener()
	{
		var appWindowId = GetAppWindowId();

		if (appWindowId.Value != 0)
		{
			var inputActivationListener = InputActivationListener.GetForWindowId(appWindowId);
			m_inputActivationListener = inputActivationListener;
			inputActivationListener.InputActivationChanged += OnInputActivationChanged;
			m_inputActivationChangedToken.Disposable = Disposable.Create(() => inputActivationListener.InputActivationChanged -= OnInputActivationChanged);
		}
	}

	protected override AutomationPeer OnCreateAutomationPeer() =>
		new TitleBarAutomationPeer(this);

	protected override void OnApplyTemplate()
	{
		//TITLEBAR_TRACE_INFO(*this, TRACE_MSG_METH, METH_NAME, this);

		base.OnApplyTemplate();

		m_leftPaddingColumn = GetTemplateChild<ColumnDefinition>(s_leftPaddingColumnName);
		m_rightPaddingColumn = GetTemplateChild<ColumnDefinition>(s_rightPaddingColumnName);

		// Uno specific: the InputActivationListener subscription that WinUI performs here is
		// instead attached on Loaded (and detached on Unloaded) — see AttachInputActivationListener.

		UpdateHeight();
		UpdatePadding();
		UpdateIcon();
		UpdateBackButton();
		UpdatePaneToggleButton();
		UpdateTitle();
		UpdateSubtitle();
		UpdateLeftHeader();
		UpdateContent();
		UpdateRightHeader();
		UpdateInteractableElementsList();
		UpdateDragRegion();
		UpdateIconRegion();
	}

	private void HandleTitleChange(string oldTitle, string newTitle)
	{
		// If transitioning from non-empty to empty, prefer ResetTitle to avoid overwriting external titles.
		if (!string.IsNullOrEmpty(oldTitle) && string.IsNullOrEmpty(newTitle))
		{
			ResetTitle(oldTitle);
			GoToState(s_titleTextCollapsedVisualStateName, false);
		}
		else
		{
			UpdateTitle();
		}
	}

	private void OnPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		DependencyProperty property = args.Property;

		if (property == AutoRefreshDragRegionsProperty)
		{
			UpdateAutoRefreshDragRegions();
		}
		else if (property == IsBackButtonVisibleProperty)
		{
			UpdateBackButton();
		}
		else if (property == IsBackButtonEnabledProperty)
		{
			UpdateInteractableElementsList();
		}
		else if (property == IsPaneToggleButtonVisibleProperty)
		{
			UpdatePaneToggleButton();
		}
		else if (property == IconSourceProperty)
		{
			UpdateIcon();
		}
		else if (property == TitleProperty)
		{
			var oldTitle = args.OldValue as string ?? "";
			var newTitle = Title;

			HandleTitleChange(oldTitle, newTitle);
		}
		else if (property == SubtitleProperty)
		{
			UpdateSubtitle();
		}
		else if (property == LeftHeaderProperty)
		{
			UpdateLeftHeader();
		}
		else if (property == ContentProperty)
		{
			UpdateContent();
		}
		else if (property == RightHeaderProperty)
		{
			UpdateRightHeader();
		}

		UpdateDragRegion();
		UpdateIconRegion();
	}

	private void GoToState(string stateName, bool useTransitions)
	{
		//TITLEBAR_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR_INT, METH_NAME, this, stateName.data(), useTransitions);

		VisualStateManager.GoToState(this, stateName, useTransitions);
	}

	private void OnSizeChanged(object sender, SizeChangedEventArgs args)
	{
		//TITLEBAR_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

		if (Content != null)
		{
			var contentArea = m_contentArea;
			var contentAreaGrid = m_contentAreaGrid;

			if (contentArea is not null && contentAreaGrid is not null)
			{
				if (m_compactModeThresholdWidth == 0.0 && contentArea.DesiredSize.Width >= contentAreaGrid.ActualWidth)
				{
					m_compactModeThresholdWidth = args.NewSize.Width;
					m_isCompact = true;
					GoToState(s_compactVisualStateName, false);
				}
				else if (m_isCompact && args.NewSize.Width >= m_compactModeThresholdWidth)
				{
					m_compactModeThresholdWidth = 0.0;
					m_isCompact = false;
					GoToState(s_expandedVisualStateName, false);
					UpdateTitle();
					UpdateSubtitle();
				}
			}
		}

		UpdateDragRegion();
		UpdateIconRegion();
	}

	private void OnFlowDirectionChanged(DependencyObject sender, DependencyProperty args)
	{
		//TITLEBAR_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

		UpdatePadding();
	}

	private void OnInputActivationChanged(InputActivationListener sender, InputActivationListenerActivationChangedEventArgs args)
	{
		bool isDeactivated = sender.State == InputActivationState.Deactivated;

		//TITLEBAR_TRACE_VERBOSE_DBG(*this, TRACE_MSG_METH_STR_INT, METH_NAME, this, L"isDeactivated:", isDeactivated);

		if (IsBackButtonVisible && IsBackButtonEnabled)
		{
			GoToState(isDeactivated ? s_backButtonDeactivatedVisualStateName : s_backButtonVisibleVisualStateName, false);
		}

		if (IsPaneToggleButtonVisible)
		{
			GoToState(isDeactivated ? s_paneToggleButtonDeactivatedVisualStateName : s_paneToggleButtonVisibleVisualStateName, false);
		}

		if (IconSource != null)
		{
			GoToState(isDeactivated ? s_iconDeactivatedVisualStateName : s_iconVisibleVisualStateName, false);
		}

		if (!string.IsNullOrEmpty(Title))
		{
			if (!m_isCompact)
			{
				GoToState(isDeactivated ? s_titleTextDeactivatedVisualStateName : s_titleTextVisibleVisualStateName, false);
			}
		}

		if (!string.IsNullOrEmpty(Subtitle))
		{
			if (!m_isCompact)
			{
				GoToState(isDeactivated ? s_subtitleTextDeactivatedVisualStateName : s_subtitleTextVisibleVisualStateName, false);
			}
		}

		if (LeftHeader != null)
		{
			GoToState(isDeactivated ? s_leftHeaderDeactivatedVisualStateName : s_leftHeaderVisibleVisualStateName, false);
		}

		if (Content != null)
		{
			GoToState(isDeactivated ? s_contentDeactivatedVisualStateName : s_contentVisibleVisualStateName, false);
		}

		if (RightHeader != null)
		{
			GoToState(isDeactivated ? s_rightHeaderDeactivatedVisualStateName : s_rightHeaderVisibleVisualStateName, false);
		}

		UpdateIconRegion();
	}

	private void OnWindowRectChanged(InputNonClientPointerSource sender, WindowRectChangedEventArgs args)
	{
		//TITLEBAR_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

		UpdateIconRegion();
	}

	private void OnBackButtonClick(object sender, RoutedEventArgs args)
	{
		//TITLEBAR_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

		BackRequested?.Invoke(this, null);
	}

	private void OnPaneToggleButtonClick(object sender, RoutedEventArgs args)
	{
		//TITLEBAR_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

		PaneToggleRequested?.Invoke(this, null);
	}

	private void UpdateIcon()
	{
		//TITLEBAR_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

		var templateSettings = TemplateSettings;
		if (IconSource is { } source)
		{
			if (m_iconViewbox is null)
			{
				m_iconViewbox = GetTemplateChild<FrameworkElement>(s_iconViewboxPartName);
			}

			// 55625016
			// AppWindowTitleBar's InputNonClientPointerSource currently resets all non-passthrough region rects on every interaction.
			// As such, we added extra event listeners to manually update the nonClientPointerSource::Icon rect as a workaround.
			if (m_iconViewbox is { } iconViewbox)
			{
				iconViewbox.LayoutUpdated += OnIconLayoutUpdated;
				m_iconLayoutUpdatedRevoker.Disposable = Disposable.Create(() => iconViewbox.LayoutUpdated -= OnIconLayoutUpdated);
			}
			else
			{
				m_iconLayoutUpdatedRevoker.Disposable = null;
			}

			if (GetInputNonClientPointerSource() is { } nonClientPointerSource)
			{
				nonClientPointerSource.WindowRectChanged += OnWindowRectChanged;
				m_windowRectChangedToken.Disposable = Disposable.Create(() => nonClientPointerSource.WindowRectChanged -= OnWindowRectChanged);
			}
			else if (m_inputNonClientPointerSource is not null)
			{
				m_windowRectChangedToken.Disposable = null;
			}

			templateSettings.IconElement = SharedHelpers.MakeIconElementFrom(source);
			GoToState(s_iconVisibleVisualStateName, false);
		}
		else
		{
			m_iconLayoutUpdatedRevoker.Disposable = null;

			if (m_inputNonClientPointerSource is not null)
			{
				m_windowRectChangedToken.Disposable = null;
			}

			templateSettings.IconElement = null;
			GoToState(s_iconCollapsedVisualStateName, false);
		}

		UpdateDragRegion();
		UpdateIconRegion();
	}

	private void OnIconLayoutUpdated(object sender, object args)
	{
		//TITLEBAR_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

		UpdateIconRegion();
	}

	private void OnContentLayoutUpdated(object sender, object args)
	{
		//TITLEBAR_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

		// Content layout has changed (children added/removed/resized at runtime).
		// Refresh the interactable elements list and drag region.
		UpdateInteractableElementsList();
		UpdateDragRegion();

		// If auto-refresh is disabled, unsubscribe after the first update.
		// This gives us one-shot behavior: the initial layout pass updates
		// drag regions, then we stop listening to avoid repeated tree walks.
		if (!AutoRefreshDragRegions)
		{
			m_contentLayoutUpdatedRevoker.Disposable = null;
		}
	}

	// Static callback for IsDragRegion attached property changes.
	// Walks up the visual tree to find the parent TitleBar and triggers an update of its drag regions.
	// This handles dynamic changes to IsDragRegion at runtime (including hot reload scenarios).
	//
	// Note: If element is not yet in the visual tree (e.g., during initial XAML parsing),
	// GetParent returns null and we exit immediately. The TitleBar will discover the
	// IsDragRegion value later via FindInteractableElements() during layout updates.
	private static void OnIsDragRegionPropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		// Walk up from sender to find the owning TitleBar and refresh its drag regions.
		// Note: If multiple elements have their IsDragRegion changed at the same time,
		// this will result in duplicate work. A future optimization could defer the update
		// using CompositionTarget.Rendered to coalesce multiple changes into a single pass.
		var current = sender;
		while (current != null)
		{
			if (current is TitleBar titleBar)
			{
				titleBar.UpdateInteractableElementsList();
				titleBar.UpdateDragRegion();
				return;
			}
			current = VisualTreeHelper.GetParent(current);
		}
	}

	private void UpdateBackButton()
	{
		//TITLEBAR_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

		if (IsBackButtonVisible)
		{
			if (m_backButton is null)
			{
				LoadBackButton();
			}

			GoToState(s_backButtonVisibleVisualStateName, false);
		}
		else
		{
			GoToState(s_backButtonCollapsedVisualStateName, false);
		}

		UpdateInteractableElementsList();
		UpdateLeftHeaderSpacing();
	}

	private void UpdatePaneToggleButton()
	{
		//TITLEBAR_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

		if (IsPaneToggleButtonVisible)
		{
			if (m_paneToggleButton is null)
			{
				LoadPaneToggleButton();
			}

			GoToState(s_paneToggleButtonVisibleVisualStateName, false);
		}
		else
		{
			GoToState(s_paneToggleButtonCollapsedVisualStateName, false);
		}

		UpdateInteractableElementsList();
		UpdateLeftHeaderSpacing();
	}

	private void UpdateHeight()
	{
		//TITLEBAR_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

		GoToState((Content == null && LeftHeader == null && RightHeader == null) ?
			s_compactHeightVisualStateName : s_expandedHeightVisualStateName,
			false);
	}

	private void UpdatePadding()
	{
		//TITLEBAR_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

		var appWindow = TryGetAppWindow();
		if (appWindow is not null)
		{
			// TODO 50724421: Bind to appTitleBar Left and Right inset changed event.
			if (appWindow.TitleBar is { } appTitleBar)
			{
				if (m_leftPaddingColumn is { } leftColumn)
				{
					var leftColumnInset =
						FlowDirection == FlowDirection.LeftToRight ?
						appTitleBar.LeftInset :
						appTitleBar.RightInset;

					//TITLEBAR_TRACE_VERBOSE_DBG(*this, TRACE_MSG_METH_STR_INT, METH_NAME, this,
					//    L"LeftColumn width:",
					//    leftColumnInset);

					leftColumn.Width = GridLengthHelper.FromPixels(leftColumnInset);
				}

				if (m_rightPaddingColumn is { } rightColumn)
				{
					var rightColumnInset =
						FlowDirection == FlowDirection.LeftToRight ?
						appTitleBar.RightInset :
						appTitleBar.LeftInset;

					//TITLEBAR_TRACE_VERBOSE_DBG(*this, TRACE_MSG_METH_STR_INT, METH_NAME, this,
					//    L"RightColumn width:",
					//    rightColumnInset);

					rightColumn.Width = GridLengthHelper.FromPixels(rightColumnInset);
				}
			}
		}
	}

	private void UpdateTitle()
	{
		var titleText = Title;

		//TITLEBAR_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR, METH_NAME, this, titleText.c_str());

		var appWindow = TryGetAppWindow();

		// Capture default title once.
		if (appWindow is not null && !m_hasDefaultAppWindowTitle)
		{
			m_defaultAppWindowTitle = appWindow.Title;
			m_hasDefaultAppWindowTitle = true;
		}

		if (string.IsNullOrEmpty(titleText))
		{
			// Do not set appWindow.Title here. Reset is handled by ResetTitle via OnPropertyChanged.
			GoToState(s_titleTextCollapsedVisualStateName, false);
			return;
		}

		// Only set the window title if it actually needs to change.
		if (appWindow is not null)
		{
			var currentTitle = appWindow.Title;
			if (currentTitle != titleText)
			{
				appWindow.Title = titleText;
			}
		}

		GoToState(s_titleTextVisibleVisualStateName, false);
	}

	private void ResetTitle(string lastAppliedTitle)
	{
		//TITLEBAR_TRACE_INFO(nullptr, TRACE_MSG_METH, METH_NAME, nullptr);

		if (!m_hasDefaultAppWindowTitle)
		{
			return;
		}

		var appWindow = TryGetAppWindow();
		if (appWindow is null)
		{
			return;
		}

		// Restore only if the current title matches what we previously applied
		var currentTitle = appWindow.Title;
		if (lastAppliedTitle == currentTitle && currentTitle != m_defaultAppWindowTitle)
		{
			appWindow.Title = m_defaultAppWindowTitle;
			m_hasDefaultAppWindowTitle = false;
		}
	}

	private void UpdateSubtitle()
	{
		var subTitleText = Subtitle;

		//TITLEBAR_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR, METH_NAME, this, subTitleText.c_str());

		if (string.IsNullOrEmpty(subTitleText))
		{
			GoToState(s_subtitleTextCollapsedVisualStateName, false);
		}
		else
		{
			GoToState(s_subtitleTextVisibleVisualStateName, false);
		}
	}

	private void UpdateLeftHeader()
	{
		//TITLEBAR_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

		if (LeftHeader == null)
		{
			GoToState(s_leftHeaderCollapsedVisualStateName, false);
		}
		else
		{
			if (m_leftHeaderArea is null)
			{
				m_leftHeaderArea = GetTemplateChild<FrameworkElement>(s_leftHeaderPresenterPartName);
			}
			GoToState(s_leftHeaderVisibleVisualStateName, false);
		}

		UpdateHeight();
		UpdateInteractableElementsList();
	}

	private void UpdateContent()
	{
		//TITLEBAR_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

		// Revoke previous handler
		m_contentLayoutUpdatedRevoker.Disposable = null;

		if (Content == null)
		{
			GoToState(s_contentCollapsedVisualStateName, false);
		}
		else
		{
			if (m_contentArea is null)
			{
				m_contentAreaGrid = GetTemplateChild<Grid>(s_contentPresenterGridPartName);
				m_contentArea = GetTemplateChild<FrameworkElement>(s_contentPresenterPartName);
			}

			// Always subscribe to LayoutUpdated which fires after Loaded + Measure + Arrange,
			// ensuring elements have valid bounds. If AutoRefreshDragRegions is false, the
			// handler will self-unregister after the first update (one-shot behavior).
			if (Content is FrameworkElement content)
			{
				content.LayoutUpdated += OnContentLayoutUpdated;
				m_contentLayoutUpdatedRevoker.Disposable = Disposable.Create(() => content.LayoutUpdated -= OnContentLayoutUpdated);
			}

			GoToState(s_contentVisibleVisualStateName, false);
		}

		UpdateHeight();
		UpdateInteractableElementsList();
	}

	private void UpdateRightHeader()
	{
		//TITLEBAR_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

		if (RightHeader == null)
		{
			GoToState(s_rightHeaderCollapsedVisualStateName, false);
		}
		else
		{
			if (m_rightHeaderArea is null)
			{
				m_rightHeaderArea = GetTemplateChild<FrameworkElement>(s_rightHeaderPresenterPartName);
			}
			GoToState(s_rightHeaderVisibleVisualStateName, false);
		}

		UpdateHeight();
		UpdateInteractableElementsList();
	}

	private RectInt32 GetBounds(FrameworkElement element)
	{
		var transformBounds = element.TransformToVisual(null);
		var width = element.ActualWidth;
		var height = element.ActualHeight;
		var bounds = transformBounds.TransformBounds(new Rect(
			0.0f,
			0.0f,
			(float)width,
			(float)height));

		var scale = XamlRoot.RasterizationScale;
		var returnRect = new RectInt32(
			(int)(bounds.X * scale),
			(int)(bounds.Y * scale),
			(int)(bounds.Width * scale),
			(int)(bounds.Height * scale));

		//TITLEBAR_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR_STR,
		//    METH_NAME, this,
		//    element.Name().c_str(),
		//    TypeLogging::RectInt32ToString(returnRect).c_str());

		return returnRect;
	}

	// Once TitleBar control is set as the Window titlebar in developer codebehind, the entire region's input is marked as non-client
	// and becomes non interactable. We need to punch out a hole for each interactable region in TitleBar.
	private void UpdateDragRegion()
	{
		if (GetInputNonClientPointerSource() is { } nonClientPointerSource)
		{
			if (m_interactableElementsList.Count != 0)
			{
				List<RectInt32> passthroughRects = new();

				// Get rects for each interactable element in TitleBar.
				foreach (var frameworkElement in m_interactableElementsList)
				{
					var transparentRect = GetBounds(frameworkElement);

					if (transparentRect.X >= 0 || transparentRect.Y >= 0)
					{
						passthroughRects.Add(transparentRect);
					}
				}

				// Skip the SetRegionRects call if the rects haven't changed since last update.
				bool rectsUnchanged = passthroughRects.Count == m_previousPassthroughRects.Count;
				for (int i = 0; rectsUnchanged && i < passthroughRects.Count; i++)
				{
					var a = passthroughRects[i];
					var b = m_previousPassthroughRects[i];
					rectsUnchanged = a.X == b.X && a.Y == b.Y && a.Width == b.Width && a.Height == b.Height;
				}

				if (rectsUnchanged)
				{
					//TITLEBAR_TRACE_VERBOSE_DBG(*this, TRACE_MSG_METH_STR, METH_NAME, this, L"Passthrough rects unchanged, skipping SetRegionRects");
					return;
				}

				//TITLEBAR_TRACE_VERBOSE_DBG(*this, L"%s[0x%p](SetRegionRects - Size: %d)\n", METH_NAME, this, passthroughRects.size());

				m_previousPassthroughRects.Clear();
				m_previousPassthroughRects.AddRange(passthroughRects);

				// Set list of rects as passthrough regions for the non-client area.
				nonClientPointerSource.SetRegionRects(NonClientRegionKind.Passthrough, passthroughRects.ToArray());
			}
			else
			{
				// Skip if we already had no rects previously.
				if (m_previousPassthroughRects.Count == 0)
				{
					return;
				}

				//TITLEBAR_TRACE_VERBOSE_DBG(*this, TRACE_MSG_METH_STR, METH_NAME, this, L"Clear Passthrough RegionRects");

				m_previousPassthroughRects.Clear();

				// There is no interactable areas. Clear previous passthrough rects.
				nonClientPointerSource.ClearRegionRects(NonClientRegionKind.Passthrough);
			}
		}
	}

	private void UpdateIconRegion()
	{
		if (GetInputNonClientPointerSource() is { } nonClientPointerSource)
		{
			if (IconSource != null)
			{
				if (m_iconViewbox is { } iconViewbox)
				{
					List<RectInt32> iconRects = new();

					var iconRect = GetBounds(iconViewbox);

					if (iconRect.X >= 0 || iconRect.Y >= 0)
					{
						iconRects.Add(iconRect);
					}

					//TITLEBAR_TRACE_VERBOSE_DBG(*this, L"%s[0x%p](Set Icon RegionRects)\n", METH_NAME, this);

					nonClientPointerSource.SetRegionRects(NonClientRegionKind.Icon, iconRects.ToArray());
				}
			}
			else
			{
				//TITLEBAR_TRACE_VERBOSE_DBG(*this, TRACE_MSG_METH_STR, METH_NAME, this, L"Clear Icon RegionRects");

				nonClientPointerSource.ClearRegionRects(NonClientRegionKind.Icon);
			}
		}
	}

	private void UpdateInteractableElementsList()
	{
		m_interactableElementsList.Clear();

		if (IsBackButtonVisible && IsBackButtonEnabled)
		{
			if (m_backButton is { } backButton)
			{
				//TITLEBAR_TRACE_VERBOSE_DBG(*this, TRACE_MSG_METH_STR, METH_NAME, this, L"Append backButton to m_interactableElementsList");

				m_interactableElementsList.Add(backButton);
			}
		}

		if (IsPaneToggleButtonVisible)
		{
			if (m_paneToggleButton is { } paneToggleButton)
			{
				//TITLEBAR_TRACE_VERBOSE_DBG(*this, TRACE_MSG_METH_STR, METH_NAME, this, L"Append paneToggleButton to m_interactableElementsList");

				m_interactableElementsList.Add(paneToggleButton);
			}
		}

		if (LeftHeader != null)
		{
			if (m_leftHeaderArea is { } leftHeaderArea)
			{
				//TITLEBAR_TRACE_VERBOSE_DBG(*this, TRACE_MSG_METH_STR, METH_NAME, this, L"Append headerArea to m_interactableElementsList");

				m_interactableElementsList.Add(leftHeaderArea);
			}
		}

		if (Content != null)
		{
			if (m_contentArea is { } contentArea)
			{
				//TITLEBAR_TRACE_VERBOSE_DBG(*this, TRACE_MSG_METH_STR, METH_NAME, this, L"Find controls in contentArea");

				// Recursively find interactive controls, respecting IsDragRegion
				FindInteractableElements(contentArea, false);
			}
		}

		if (RightHeader != null)
		{
			if (m_rightHeaderArea is { } rightHeaderArea)
			{
				//TITLEBAR_TRACE_VERBOSE_DBG(*this, TRACE_MSG_METH_STR, METH_NAME, this, L"Append footerArea to m_interactableElementsList");

				m_interactableElementsList.Add(rightHeaderArea);
			}
		}

		//TITLEBAR_TRACE_VERBOSE_DBG(*this, TRACE_MSG_METH_STR_INT, METH_NAME, this,
		//    L"m_interactableElementsList Size:",
		//    m_interactableElementsList.size());
	}

	private void UpdateLeftHeaderSpacing()
	{
		//TITLEBAR_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

		GoToState(
			IsBackButtonVisible == IsPaneToggleButtonVisible ?
			s_defaultSpacingVisualStateName : s_negativeInsetVisualStateName,
			false);
	}

	public void RecomputeDragRegions()
	{
		//TITLEBAR_TRACE_INFO(*this, TRACE_MSG_METH, METH_NAME, this);

		// Force a synchronous layout pass so the visual tree reflects any
		// recently added/removed children and elements have valid bounds.
		UpdateLayout();

		UpdateInteractableElementsList();
		UpdateDragRegion();
	}

	private void UpdateAutoRefreshDragRegions()
	{
		//TITLEBAR_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

		if (AutoRefreshDragRegions)
		{
			// Re-subscribe to LayoutUpdated if not already subscribed
			// (it may have been revoked by the one-shot unregister in OnContentLayoutUpdated).
			if (Content != null)
			{
				if (Content is FrameworkElement content)
				{
					content.LayoutUpdated += OnContentLayoutUpdated;
					m_contentLayoutUpdatedRevoker.Disposable = Disposable.Create(() => content.LayoutUpdated -= OnContentLayoutUpdated);
				}
			}

			// Recompute interactable elements now so drag regions are current immediately.
			// Note: UpdateDragRegion() is not called here because OnPropertyChanged already
			// calls it unconditionally after this method returns.
			UpdateInteractableElementsList();
		}
		else
		{
			// App opted out of automatic refresh — stop listening.
			m_contentLayoutUpdatedRevoker.Disposable = null;
		}
	}

	private void LoadBackButton()
	{
		//TITLEBAR_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

		m_backButton = GetTemplateChild<Button>(s_backButtonPartName);

		if (m_backButton is { } backButton)
		{
			backButton.Click += OnBackButtonClick;
			m_backButtonClickRevoker.Disposable = Disposable.Create(() => backButton.Click -= OnBackButtonClick);

			// Do localization for the back button
			if (string.IsNullOrEmpty(AutomationProperties.GetName(backButton)))
			{
				var backButtonName = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_NavigationBackButtonName);
				AutomationProperties.SetName(backButton, backButtonName);
			}

			// Setup the tooltip for the back button
			var tooltip = new ToolTip();
			var backButtonTooltipText = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_NavigationBackButtonToolTip);
			tooltip.Content = backButtonTooltipText;
			ToolTipService.SetToolTip(backButton, tooltip);
		}
	}

	private void LoadPaneToggleButton()
	{
		//TITLEBAR_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

		m_paneToggleButton = GetTemplateChild<Button>(s_paneToggleButtonPartName);

		if (m_paneToggleButton is { } paneToggleButton)
		{
			paneToggleButton.Click += OnPaneToggleButtonClick;
			m_paneToggleButtonClickRevoker.Disposable = Disposable.Create(() => paneToggleButton.Click -= OnPaneToggleButtonClick);

			// Do localization for paneToggleButton
			if (string.IsNullOrEmpty(AutomationProperties.GetName(paneToggleButton)))
			{
				var paneToggleButtonName = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_NavigationButtonToggleName);
				AutomationProperties.SetName(paneToggleButton, paneToggleButtonName);
			}

			// Setup the tooltip for the paneToggleButton
			var tooltip = new ToolTip();
			tooltip.Content = AutomationProperties.GetName(paneToggleButton);
			ToolTipService.SetToolTip(paneToggleButton, tooltip);
		}
	}

	private WindowId GetAppWindowId()
	{
		WindowId appWindowId = default;

		if (XamlRoot is { } xamlRoot)
		{
			if (xamlRoot.ContentIslandEnvironment is { } contentIslandEnvironment)
			{
				appWindowId = contentIslandEnvironment.AppWindowId;
			}
		}

		if (appWindowId.Value != m_lastAppWindowId.Value)
		{
			m_lastAppWindowId = appWindowId;

			m_inputNonClientPointerSource = null;

			m_appWindow = null;
		}

		return appWindowId;
	}

	private InputNonClientPointerSource GetInputNonClientPointerSource()
	{
		var appWindowId = GetAppWindowId();

		if (m_inputNonClientPointerSource is null && appWindowId.Value != 0)
		{
			m_inputNonClientPointerSource = InputNonClientPointerSource.GetForWindowId(appWindowId);
		}

		return m_inputNonClientPointerSource;
	}

	// Helper to retrieve and cache AppWindow for current WindowId
	private AppWindow TryGetAppWindow()
	{
		var appWindowId = GetAppWindowId();
		if (appWindowId.Value == 0)
		{
			m_appWindow = null;
			return null;
		}

		// Refresh cache if WindowId changed or cache is empty
		if (m_appWindow is null)
		{
			m_appWindow = AppWindow.GetFromWindowId(appWindowId);
		}

		return m_appWindow;
	}

	private void FindInteractableElements(DependencyObject element, bool parentIsDragRegion)
	{
		if (element is null)
		{
			return;
		}

		var uiElement = element as UIElement;

		// All children from VisualTreeHelper should be UIElements; bail out if not.
		if (uiElement is null)
		{
			return;
		}

		// Skip elements that are not visible or not hit-testable.
		// Note: UIElement.IsHitTestVisible() is a separate property and does NOT reflect Visibility.
		// We must check Visibility explicitly to skip Collapsed elements.
		if (uiElement.Visibility != Visibility.Visible || !uiElement.IsHitTestVisible)
		{
			return;
		}

		bool currentIsDragRegion = parentIsDragRegion;

		// Check if IsDragRegion is explicitly set (non-null) on this element.
		// IReference<bool>: null = unset, false = clickable, true = draggable.
		var isDragRegion = GetIsDragRegion(uiElement);
		if (isDragRegion != null)
		{
			if (!isDragRegion.Value)
			{
				// IsDragRegion=false: Add this entire element as interactable and stop recursing
				// into its children (the element's bounds cover all its internal elements).
				if (uiElement is FrameworkElement fe)
				{
					//TITLEBAR_TRACE_VERBOSE_DBG(*this, TRACE_MSG_METH_STR, METH_NAME, this,
					//    fe.Name().empty() ? L"(element with IsDragRegion=false)" : fe.Name().c_str());
					m_interactableElementsList.Add(fe);
				}
				return;
			}

			// IsDragRegion=true: This element is explicitly marked as a drag region.
			// If it's a Control, treat the entire control as draggable — don't recurse
			// into its template children. Only recurse for non-Control containers (e.g. Panel)
			// where children may override with IsDragRegion=false.
			if (uiElement is Control)
			{
				//TITLEBAR_TRACE_VERBOSE_DBG(*this, TRACE_MSG_METH_STR, METH_NAME, this, L"Control with IsDragRegion=true, skipping subtree");
				return;
			}

			//TITLEBAR_TRACE_VERBOSE_DBG(*this, TRACE_MSG_METH_STR, METH_NAME, this, L"Element with IsDragRegion=true, recursing children");
			currentIsDragRegion = true;
		}

		// Check if this element is an interactive control (Button, TextBox, ComboBox, etc.)
		// Only apply auto-detection when not inside a parent with IsDragRegion=true.
		if (!currentIsDragRegion)
		{
			if (uiElement is Control control)
			{
				if (control.IsEnabled)
				{
					// Add enabled control as interactable. Don't recurse into children —
					// the control's bounds already cover all its internal elements.
					//TITLEBAR_TRACE_VERBOSE_DBG(*this, TRACE_MSG_METH_STR, METH_NAME, this,
					//    control.Name().empty() ? L"(unnamed control)" : control.Name().c_str());
					m_interactableElementsList.Add(control);
					return;
				}
				// Disabled controls: fall through to recurse into children,
				// propagating currentIsDragRegion to preserve ancestor intent.
			}
		}

		// Recursively process all children in the visual tree (for Panels, disabled controls, etc.)
		var childCount = VisualTreeHelper.GetChildrenCount(element);
		for (int i = 0; i < childCount; i++)
		{
			FindInteractableElements(VisualTreeHelper.GetChild(element, i), currentIsDragRegion);
		}
	}
}
