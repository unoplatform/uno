#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	public  partial class TextBlock : FrameworkElement
	{
		public TextBlock()
		{
			Inlines = new Documents.InlineCollection(this);
		}

		public global::Windows.UI.Xaml.Documents.InlineCollection Inlines { get; }

	}
}
