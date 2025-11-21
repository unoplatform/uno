using System.Collections.Generic;
using Microsoft.UI.Input;
using Uno.Disposables;

namespace Microsoft.UI.Xaml.Controls;

partial class TitleBar
{
	private readonly SerialDisposable m_inputActivationChangedToken = new();
	private readonly SerialDisposable m_windowRectChangedToken = new();
	private readonly SerialDisposable m_backButtonClickRevoker = new();
	private readonly SerialDisposable m_paneToggleButtonClickRevoker = new();
	private readonly SerialDisposable m_sizeChangedRevoker = new();
	private readonly SerialDisposable m_iconLayoutUpdatedRevoker = new();
	private readonly SerialDisposable m_flowDirectionChangedRevoker = new();

	private readonly List<FrameworkElement> m_interactableElementsList = new();
	private InputActivationListener m_inputActivationListener;
	private InputNonClientPointerSource m_inputNonClientPointerSource;
	private WindowId m_lastAppWindowId;

	private ColumnDefinition m_leftPaddingColumn;
	private ColumnDefinition m_rightPaddingColumn;
	private Button m_backButton;
	private Button m_paneToggleButton;
	private FrameworkElement m_iconViewbox;
	private Grid m_contentAreaGrid;
	private FrameworkElement m_leftHeaderArea;
	private FrameworkElement m_contentArea;
	private FrameworkElement m_rightHeaderArea;

	private double m_compactModeThresholdWidth;
	private bool m_isCompact;

	private const string s_leftPaddingColumnName = "LeftPaddingColumn";
	private const string s_rightPaddingColumnName = "RightPaddingColumn";
	//private const string s_layoutRootPartName = "PART_LayoutRoot";
	private const string s_backButtonPartName = "PART_BackButton";
	private const string s_paneToggleButtonPartName = "PART_PaneToggleButton";
	private const string s_iconViewboxPartName = "PART_Icon";
	private const string s_leftHeaderPresenterPartName = "PART_LeftHeaderPresenter";
	private const string s_contentPresenterGridPartName = "PART_ContentPresenterGrid";
	private const string s_contentPresenterPartName = "PART_ContentPresenter";
	private const string s_rightHeaderPresenterPartName = "PART_RightHeaderPresenter";

	private const string s_compactVisualStateName = "Compact";
	private const string s_expandedVisualStateName = "Expanded";
	private const string s_compactHeightVisualStateName = "CompactHeight";
	private const string s_expandedHeightVisualStateName = "ExpandedHeight";
	private const string s_defaultSpacingVisualStateName = "DefaultSpacing";
	private const string s_negativeInsetVisualStateName = "NegativeInsetSpacing";
	private const string s_iconVisibleVisualStateName = "IconVisible";
	private const string s_iconCollapsedVisualStateName = "IconCollapsed";
	private const string s_iconDeactivatedVisualStateName = "IconDeactivated";
	private const string s_backButtonVisibleVisualStateName = "BackButtonVisible";
	private const string s_backButtonCollapsedVisualStateName = "BackButtonCollapsed";
	private const string s_backButtonDeactivatedVisualStateName = "BackButtonDeactivated";
	private const string s_paneToggleButtonVisibleVisualStateName = "PaneToggleButtonVisible";
	private const string s_paneToggleButtonCollapsedVisualStateName = "PaneToggleButtonCollapsed";
	private const string s_paneToggleButtonDeactivatedVisualStateName = "PaneToggleButtonDeactivated";
	private const string s_titleTextVisibleVisualStateName = "TitleTextVisible";
	private const string s_titleTextCollapsedVisualStateName = "TitleTextCollapsed";
	private const string s_titleTextDeactivatedVisualStateName = "TitleTextDeactivated";
	private const string s_subtitleTextVisibleVisualStateName = "SubtitleTextVisible";
	private const string s_subtitleTextCollapsedVisualStateName = "SubtitleTextCollapsed";
	private const string s_subtitleTextDeactivatedVisualStateName = "SubtitleTextDeactivated";
	private const string s_leftHeaderVisibleVisualStateName = "LeftHeaderVisible";
	private const string s_leftHeaderCollapsedVisualStateName = "LeftHeaderCollapsed";
	private const string s_leftHeaderDeactivatedVisualStateName = "LeftHeaderDeactivated";
	private const string s_contentVisibleVisualStateName = "ContentVisible";
	private const string s_contentCollapsedVisualStateName = "ContentCollapsed";
	private const string s_contentDeactivatedVisualStateName = "ContentDeactivated";
	private const string s_rightHeaderVisibleVisualStateName = "RightHeaderVisible";
	private const string s_rightHeaderCollapsedVisualStateName = "RightHeaderCollapsed";
	private const string s_rightHeaderDeactivatedVisualStateName = "RightHeaderDeactivated";
}
