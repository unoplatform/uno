#if !__WASM__
using System.Collections.Generic;
using Windows.UI.Xaml.Documents.TextFormatting;

namespace Windows.UI.Xaml.Documents
{
	/// <summary>
	/// Represents a collection of Block elements.
	/// </summary>
	public partial class BlockCollection : SegmentsCollection<Block>, IList<Block>, IEnumerable<Block>
	{
		internal BlockCollection(DependencyObject parent)
			: base(parent)
		{

		}

		private protected override void OnCollectionChanged()
		{
#if !IS_UNIT_TESTS
			base.OnCollectionChanged();
			switch (this.GetParent())
			{
				case ISegmentsElement textVisualElement:
					textVisualElement.InvalidateSegments();
					break;
				case Block inline:
					inline.InvalidateSegments();
					break;
				default:
					break;
			}
#endif
		}
	}
}
#endif
