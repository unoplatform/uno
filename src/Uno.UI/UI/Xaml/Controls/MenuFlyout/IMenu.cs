namespace Windows.UI.Xaml.Controls
{
	internal interface IMenu
	{
		void Close();

		IMenu ParentMenu
		{
			get;
			set;
		}
	}
}


