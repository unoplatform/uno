using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Uno.Disposables;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;
using Windows.Graphics;

namespace Microsoft.UI.Xaml.Controls;

partial class TitleBar
{
	public TitleBar()
	{
		SetValue(TemplateSettingsProperty, new TitleBarTemplateSettings());

		this.SetDefaultStyleKey();

		SizeChanged += OnSizeChanged;
		m_sizeChangedRevoker.Disposable = Disposable.Create(() => SizeChanged -= OnSizeChanged);

		m_flowDirectionChangedRevoker.Disposable = this.RegisterDisposablePropertyChangedCallback(
			FrameworkElement.FlowDirectionProperty,
			OnFlowDirectionChanged);
	}

	//TitleBar::~TitleBar()
	//{
	//	TITLEBAR_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);

	//	m_sizeChangedRevoker.revoke();
	//	m_flowDirectionChangedRevoker.revoke();

	//	if (m_inputActivationChangedToken.value)
	//	{
	//		m_inputActivationListener.InputActivationChanged(m_inputActivationChangedToken);
	//		m_inputActivationChangedToken.value = 0;
	//	}
	//}

	protected override AutomationPeer OnCreateAutomationPeer() =>
		new TitleBarAutomationPeer(this);


	protected override void OnApplyTemplate()
	{
		base.OnApplyTemplate();

		m_leftPaddingColumn = GetTemplateChild<ColumnDefinition>(s_leftPaddingColumnName);
		m_rightPaddingColumn = GetTemplateChild<ColumnDefinition>(s_rightPaddingColumnName);

		var appWindowId = GetAppWindowId();

		if (appWindowId.Value != 0)
		{
			m_inputActivationListener = InputActivationListener.GetForWindowId(appWindowId);
			m_inputActivationListener.InputActivationChanged += OnInputActivationChanged;
			m_inputActivationChangedToken.Disposable = Disposable.Create(() => m_inputActivationListener.InputActivationChanged -= OnInputActivationChanged);
		}

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

	private void OnPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		DependencyProperty property = args.Property;

		if (property == IsBackButtonVisibleProperty)
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
		if (property == IconSourceProperty)
		{
			UpdateIcon();
		}
		else if (property == TitleProperty)
		{
			UpdateTitle();
		}
		else if (property == SubtitleProperty)
		{
			UpdateSubtitle();
		}
		if (property == LeftHeaderProperty)
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

	private void GoToState(string stateName, bool useTransitions) =>
		VisualStateManager.GoToState(this, stateName, useTransitions);

	private void OnSizeChanged(object sender, SizeChangedEventArgs args)
	{
		if (Content != null)
		{
			var contentArea = m_contentArea;
			var contentAreaGrid = m_contentAreaGrid;

			if (contentArea is not null && contentAreaGrid is not null)
			{
				if (m_compactModeThresholdWidth != 0 && contentArea.DesiredSize.Width >= contentAreaGrid.ActualWidth)
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

	private void OnFlowDirectionChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args) =>
		UpdatePadding();

	private void OnInputActivationChanged(InputActivationListener sender, InputActivationListenerActivationChangedEventArgs args)
	{
		bool isDeactivated = sender.State == InputActivationState.Deactivated;

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

	private void OnWindowRectChanged(InputNonClientPointerSource sender, WindowRectChangedEventArgs args) =>
		UpdateIconRegion();

	private void OnBackButtonClick(object sender, RoutedEventArgs args) =>
		BackRequested?.Invoke(this, null);

	private void OnPaneToggleButtonClick(object sender, RoutedEventArgs args) =>
		PaneToggleRequested?.Invoke(this, null);

	private void UpdateIcon()
	{
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

	private void OnIconLayoutUpdated(object sender, object args) =>
		UpdateIconRegion();

	private void UpdateBackButton()
	{
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
		GoToState((Content == null && LeftHeader == null && RightHeader == null) ?
			s_compactHeightVisualStateName : s_expandedHeightVisualStateName,
			false);
	}

	private void UpdatePadding()
	{
		var appWindowId = GetAppWindowId();

		if (appWindowId.Value != 0)
		{
			AppWindow appWindow = AppWindow.GetFromWindowId(appWindowId);

			// TODO 50724421: Bind to appTitleBar Left and Right inset changed event.
			if (appWindow.TitleBar is { } appTitleBar)

			{
				if (m_leftPaddingColumn is { } leftColumn)

				{
					var leftColumnInset =
						FlowDirection == FlowDirection.LeftToRight ?
						appTitleBar.LeftInset :
						appTitleBar.RightInset;

					leftColumn.Width = GridLengthHelper.FromPixels(leftColumnInset);
				}

				if (m_rightPaddingColumn is { } rightColumn)

				{
					var rightColumnInset =
						FlowDirection == FlowDirection.LeftToRight ?
						appTitleBar.RightInset :
						appTitleBar.LeftInset;

					rightColumn.Width = GridLengthHelper.FromPixels(rightColumnInset);
				}
			}
		}
	}

	private void UpdateTitle()
	{
		var titleText = Title;

		if (string.IsNullOrEmpty(titleText))
		{
			GoToState(s_titleTextCollapsedVisualStateName, false);
		}
		else
		{
			GoToState(s_titleTextVisibleVisualStateName, false);
		}
	}

	private void UpdateSubtitle()
	{
		var subTitleText = Subtitle;

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

			GoToState(s_contentVisibleVisualStateName, false);
		}

		UpdateHeight();
		UpdateInteractableElementsList();
	}

	private void UpdateRightHeader()
	{
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
			width,
			height));

		var scale = XamlRoot.RasterizationScale;
		var returnRect = new RectInt32(
			(int)(bounds.X * scale),
			(int)(bounds.Y * scale),
			(int)(bounds.Width * scale),
			(int)(bounds.Height * scale));

		return returnRect;
	}

	// Once TitleBar control is set as the Window titlebar in developer codebehind, the entire region's input is marked as non-client
	// and becomes non interactable. We need to punch out a hole for each interactable region in TitleBar. 
	private void UpdateDragRegion()
	{
		if (GetInputNonClientPointerSource() is { } nonClientPointerSource)
		{
			if (m_interactableElementsList.Count > 0)
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

				// Set list of rects as passthrough regions for the non-client area.
				nonClientPointerSource.SetRegionRects(NonClientRegionKind.Passthrough, passthroughRects.ToArray());
			}
			else
			{
				// There is no interactable areas. Clear previous passthrough rects.
				nonClientPointerSource.ClearRegionRects(NonClientRegionKind.Passthrough);
			}
		}
	}

	private void UpdateIconRegion()
	{
		if (GetInputNonClientPointerSource() is { } nonClientPointerSource)
		{
			if (IconSource is not null)
			{
				if (m_iconViewbox is { } iconViewbox)

				{
					List<RectInt32> iconRects = new();

					var iconRect = GetBounds(iconViewbox);

					if (iconRect.X >= 0 || iconRect.Y >= 0)
					{
						iconRects.Add(iconRect);
					}

					nonClientPointerSource.SetRegionRects(NonClientRegionKind.Icon, iconRects.ToArray());
				}
			}
			else
			{
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
				m_interactableElementsList.Add(backButton);
			}
		}

		if (IsPaneToggleButtonVisible)
		{
			if (m_paneToggleButton is { } paneToggleButton)
			{
				m_interactableElementsList.Add(paneToggleButton);
			}
		}

		if (LeftHeader != null)
		{
			if (m_leftHeaderArea is { } leftHeaderArea)
			{
				m_interactableElementsList.Add(leftHeaderArea);
			}
		}

		if (Content != null)
		{
			if (m_contentArea is { } contentArea)
			{
				m_interactableElementsList.Add(contentArea);
			}
		}

		if (RightHeader != null)
		{
			if (m_rightHeaderArea is { } rightHeaderArea)
			{
				m_interactableElementsList.Add(rightHeaderArea);
			}
		}
	}

	private void UpdateLeftHeaderSpacing()
	{
		GoToState(
			IsBackButtonVisible == IsPaneToggleButtonVisible ?
			s_defaultSpacingVisualStateName : s_negativeInsetVisualStateName,
			false);
	}

	private void LoadBackButton()
	{
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
}
