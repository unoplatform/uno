#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
using Windows.Foundation;

namespace Windows.UI.Xaml.Controls
{
	public partial class ContentControl
	{
		private void SetUpdateControlTemplate() { }
		private bool HasParent() => true;

		partial void RegisterContentTemplateRoot()
		{
		}

		protected override Size MeasureOverride(Size availableSize) => base.MeasureOverride(availableSize);
	}
}
