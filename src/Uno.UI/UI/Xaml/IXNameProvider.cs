namespace Microsoft.UI.Xaml;

/// <summary>
/// Internal interface for elements whose Name is get-only but can be set by x:Name.
/// The XAML generator applies x:Name on implementers through <see cref="Uno.UI.Helpers.MarkupHelper.SetXName(object, string)"/>.
/// </summary>
internal interface IXNameProvider
{
	void SetXName(string name);
}
