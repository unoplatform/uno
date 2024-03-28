using System.Collections.Generic;

namespace Windows.UI.Xaml.Documents
{
	public partial class BlockCollection : DependencyObjectCollection<Block>, IList<Block>, IEnumerable<Block>
	{
		/// <remarks>For backward compatibility</remarks>
		public new void Add(Block block)
		{
			base.Add(block);
		}
	}
}
