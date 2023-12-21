namespace Microsoft.UI.Xaml.Controls
{
	public partial interface ISubMenuOwner
	{
		void PrepareSubMenu();

		void OpenSubMenu(Windows.Foundation.Point position);

		void PositionSubMenu(Windows.Foundation.Point position);

		void ClosePeerSubMenus();

		void CloseSubMenu();

		void CloseSubMenuTree();

		void DelayCloseSubMenu();

		void CancelCloseSubMenu();

		void RaiseAutomationPeerExpandCollapse(bool isOpen);

		bool IsSubMenuOpen
		{
			get;
		}

		bool IsSubMenuPositionedAbsolutely
		{
			get;
		}

		ISubMenuOwner ParentOwner
		{
			get;
			set;
		}
	}
}


