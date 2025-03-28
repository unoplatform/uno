namespace Windows.UI.Xaml.Controls
{
	public partial interface IMenu
	{
		void Close();

		IMenu ParentMenu
		{
			get;
			set;
		}
	}
}


