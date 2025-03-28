#nullable enable

using System.Collections.Generic;
using Windows.Foundation;
using Uno;

namespace Windows.UI.Xaml.Controls
{
	internal partial class TextBlockMeasureCache
	{
		/// <summary>
		/// Result of a single measure
		/// </summary>
		class MeasureSizeEntry
		{
			public MeasureSizeEntry(Size measuredSize, global::System.Collections.Generic.LinkedListNode<Uno.CachedTuple<double, double>> node)
			{
				MeasuredSize = measuredSize;
				ListNode = node;
			}

			/// <summary>
			/// Computed Size
			/// </summary>
			public Size MeasuredSize { get; }

			public LinkedListNode<CachedTuple<double, double>> ListNode { get; }
		}
	}
}
