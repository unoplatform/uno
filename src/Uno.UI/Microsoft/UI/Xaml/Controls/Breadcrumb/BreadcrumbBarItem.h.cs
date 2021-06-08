// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable

using Uno.Disposables;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class BreadcrumbBarItem : ContentControl
	{
		// Common item fields

		// Contains the 1-indexed assigned to the element
		private uint m_index;
		private bool m_isEllipsisDropDownItem;

		// Inline item fields

		private bool m_isEllipsisItem;
		private bool m_isLastItem;

		// BreadcrumbBarItem visual representation
		private Button? m_button = null;
		// Parent BreadcrumbBarItem to ask for hidden elements
		private BreadcrumbBar? m_parentBreadcrumb = null;

		// Flyout content for ellipsis item
		private Flyout? m_ellipsisFlyout = null;
		private ItemsRepeater? m_ellipsisItemsRepeater = null;
		private DataTemplate? m_ellipsisDropDownItemDataTemplate = null;
		private BreadcrumbElementFactory? m_ellipsisElementFactory = null;

		// Ellipsis dropdown item fields

		// BreadcrumbBarItem that owns the flyout
		private BreadcrumbBarItem m_ellipsisItem = null;

		// Visual State tracking
		private uint m_trackedPointerId = 0;
		private bool m_isPressed = false;
		private bool m_isPointerOver = false;

		// Common item token & revoker

		event_token m_childPreviewKeyDownToken
		{ ;
			RoutedEventHandler_revoker m_keyDownRevoker
			{
				;

				// Inline item token & revokers
				event_token m_flowDirectionChangedToken {
					;
					Button.Loaded_revoker m_buttonLoadedRevoker {
						;
						Button.Click_revoker m_buttonClickRevoker {
							;

							ItemsRepeater.ElementPrepared_revoker m_ellipsisRepeaterElementPreparedRevoker {
								;
								ItemsRepeater.ElementIndexChanged_revoker m_ellipsisRepeaterElementIndexChangedRevoker {
									;

									PropertyChanged_revoker m_isPressedButtonRevoker {
										;
										PropertyChanged_revoker m_isPointerOverButtonRevoker {
											;
											PropertyChanged_revoker m_isEnabledButtonRevoker {
												;

												// Ellipsis dropdown item revoker

												IsEnabledChanged_revoker m_isEnabledChangedRevoker {
													;

		// Common Visual States
		private const string s_normalStateName = "Normal";
		private const string s_currentStateName = "Current";
		private const string s_pointerOverStateName = "PointerOver";
		private const string s_pressedStateName = "Pressed";
		private const string s_disabledStateName = "Disabled";

		// Inline Item Type Visual States
		private const string s_ellipsisStateName = "Ellipsis";
		private const string s_ellipsisRTLStateName = "EllipsisRTL";
		private const string s_lastItemStateName = "LastItem";
		private const string s_defaultStateName = "Default";
		private const string s_defaultRTLStateName = "DefaultRTL";

		// Item Type Visual States
		private const string s_inlineStateName = "Inline";
		private const string s_ellipsisDropDownStateName = "EllipsisDropDown";

		// Template Parts
		private const string s_ellipsisItemsRepeaterPartName = "PART_EllipsisItemsRepeater";
		private const string s_itemButtonPartName = "PART_ItemButton";
		private const string s_itemEllipsisFlyoutPartName = "PART_EllipsisFlyout";

		// Automation Names
		private const string s_ellipsisFlyoutAutomationName = "EllipsisFlyout";
		private const string s_ellipsisItemsRepeaterAutomationName = "EllipsisItemsRepeater";
	}
}
