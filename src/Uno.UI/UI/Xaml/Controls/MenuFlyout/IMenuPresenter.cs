namespace Windows.UI.Xaml.Controls
{
	internal interface IMenuPresenter
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


