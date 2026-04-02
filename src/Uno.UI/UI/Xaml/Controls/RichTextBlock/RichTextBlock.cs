#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Markup;

namespace Microsoft.UI.Xaml.Controls
{
	[ContentProperty(Name = nameof(Blocks))]
	public partial class RichTextBlock : global::Microsoft.UI.Xaml.FrameworkElement
	{
		public RichTextBlock() : base()
		{
			Blocks = new Documents.BlockCollection();
			Blocks.SetParent(this);
			Blocks.VectorChanged += (_, _) => InvalidateForContentChange();
		}

		public BlockCollection Blocks { get; }

		internal override bool CanHaveChildren() => true;

		public new bool Focus(FocusState value) => base.Focus(value);

		internal void InvalidateForContentChange()
		{
			InvalidateMeasure();
		}
	}
}
