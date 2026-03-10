namespace Microsoft.UI.Xaml.Controls
{
	partial class RichTextBlock
	{
		internal override bool IsViewHit() => Blocks.Count > 0 || base.IsViewHit();
	}
}
