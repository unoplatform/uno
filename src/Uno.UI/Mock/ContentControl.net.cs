#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	public  partial class ContentControl
	{
		private bool HasParent() => true;

		partial void RegisterContentTemplateRoot()
		{
			AddChild((FrameworkElement)ContentTemplateRoot);
		}
	}
}
