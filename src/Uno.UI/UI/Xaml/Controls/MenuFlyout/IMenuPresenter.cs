namespace Windows.UI.Xaml.Controls
{
	public partial interface IMenuPresenter
	{
		void CloseSubMenu();

		IMenu OwningMenu
		{
			get;
			set;
		}

		ISubMenuOwner Owner
		{
			get;
			set;
		}

		IMenuPresenter SubPresenter
		{
			get;
			set;
		}
	}
}


