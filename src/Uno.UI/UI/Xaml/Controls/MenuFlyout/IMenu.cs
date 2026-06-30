namespace Microsoft.UI.Xaml.Controls;

internal partial interface IMenu
{
	void Close();

	IMenu ParentMenu
	{
		get;
		set;
	}
}


