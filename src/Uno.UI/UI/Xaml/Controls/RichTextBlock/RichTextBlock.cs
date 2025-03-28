#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Markup;

namespace Windows.UI.Xaml.Controls
{
	[ContentProperty(Name = nameof(Blocks))]
	[global::Uno.NotImplemented]
	public partial class RichTextBlock : global::Windows.UI.Xaml.FrameworkElement
	{
		[global::Uno.NotImplemented]
		public RichTextBlock() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.RichTextBlock", "RichTextBlock.RichTextBlock()");

			Blocks = new Documents.BlockCollection();
		}

		public BlockCollection Blocks { get; }

		internal override bool CanHaveChildren() => true;

		public new bool Focus(FocusState value) => base.Focus(value);
	}
}
