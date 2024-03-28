namespace Windows.UI.Xaml
{

	/// <summary>
	/// Internal interface for x:Uid enabled elements. Used through <see cref="Uno.UI.Helpers.MarkupHelper.SetXUid(object, string)"/>
	/// </summary>
	internal interface IXUidProvider
	{
		string Uid { get; set; }
	}
}
