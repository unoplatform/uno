using System.Collections.Generic;
using Uno;
using Windows.Foundation;

namespace Windows.UI.Xaml.Controls
{
	public partial class TextBlockMeasureCache
	{
		class MeasureSizeEntry
		{
			public MeasureSizeEntry(Size measuredSize, global::System.Collections.Generic.LinkedListNode<Uno.CachedTuple<double, double>> node)
			{
				MeasuredSize = measuredSize;
				ListNode = node;
			}

			public Size MeasuredSize { get; }
			public LinkedListNode<CachedTuple<double, double>> ListNode { get; }
		}
	}
}
