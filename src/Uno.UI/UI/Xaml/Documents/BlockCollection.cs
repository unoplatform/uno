using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Documents
{
	public partial class BlockCollection : DependencyObjectCollection<Block>, IList<Block>, IEnumerable<Block>
	{
		/// <remarks>For backward compatibility</remarks>
		public new void Add(Block block)
		{
			base.Add(block);
		}

		// CBlockCollection::MarkDirty — drops the cached block lengths. The owner is a RichTextBlock
		// rather than a text element, so the invalidation chain ends here.
		internal void MarkDirty() => MarkDirtyPartial();

		partial void MarkDirtyPartial();
	}
}
