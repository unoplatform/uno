#nullable enable

#if HAS_UNO
using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Controls
{
	partial class LinedFlowLayout
	{
		// Uno maps std::vector to List<T> and the C++ ItemsLayout value type to a class.
		// Keep these managed adaptations out of the source-ordered MUX files.
		private static void ClearAndFill<T>(List<T> list, int count, T value)
		{
			list.Clear();

			for (int i = 0; i < count; i++)
			{
				list.Add(value);
			}
		}

		private static ItemsLayout CloneItemsLayout(ItemsLayout source) =>
			new()
			{
				m_lineItemCounts = new List<int>(source.m_lineItemCounts),
				m_lineItemWidths = new List<double>(source.m_lineItemWidths),
				m_availableLineItemsWidth = source.m_availableLineItemsWidth,
				m_drawback = source.m_drawback,
				m_smallestHeadItemWidth = source.m_smallestHeadItemWidth,
				m_smallestTailItemWidth = source.m_smallestTailItemWidth,
				m_bestEqualizingHeadItemDrawbackImprovement = source.m_bestEqualizingHeadItemDrawbackImprovement,
				m_bestEqualizingTailItemDrawbackImprovement = source.m_bestEqualizingTailItemDrawbackImprovement,
				m_smallestHeadItemIndex = source.m_smallestHeadItemIndex,
				m_smallestTailItemIndex = source.m_smallestTailItemIndex,
				m_smallestHeadLineIndex = source.m_smallestHeadLineIndex,
				m_smallestTailLineIndex = source.m_smallestTailLineIndex,
				m_bestEqualizingHeadItemIndex = source.m_bestEqualizingHeadItemIndex,
				m_bestEqualizingTailItemIndex = source.m_bestEqualizingTailItemIndex,
				m_bestEqualizingHeadLineIndex = source.m_bestEqualizingHeadLineIndex,
				m_bestEqualizingTailLineIndex = source.m_bestEqualizingTailLineIndex,
			};

		private static void ResizeList<T>(List<T> list, int count, T value)
		{
			if (count < list.Count)
			{
				list.RemoveRange(count, list.Count - count);
			}
			else
			{
				while (list.Count < count)
				{
					list.Add(value);
				}
			}
		}
	}
}
#endif
