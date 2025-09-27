﻿using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

public partial interface ISubMenuOwner
{
	void PrepareSubMenu();

	void OpenSubMenu(Point position);

	void PositionSubMenu(Point position);

	void ClosePeerSubMenus();

	void CloseSubMenu();

	void CloseSubMenuTree();

	void DelayCloseSubMenu();

	void CancelCloseSubMenu();

	void RaiseAutomationPeerExpandCollapse(bool isOpen);

	void SetSubMenuDirection(bool isSubMenuDirectionUp);

	bool IsSubMenuOpen { get; }

	bool IsSubMenuPositionedAbsolutely { get; }

	ISubMenuOwner ParentOwner { get; set; }
}


