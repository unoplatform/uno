using System;
using System.Collections.Generic;
using System.Text;
using Uno.UI;
using System.Linq;
using Windows.UI.Xaml.Controls.Primitives;
using System.Diagnostics;
using Uno.Extensions;

namespace Windows.UI.Xaml.Controls
{
	public partial class VirtualizingPanelLayout
	{
		/// <summary>
		/// A row or column.
		/// </summary>
		[DebuggerDisplay("{DebugDisplay,nq}")]
		private protected class Line
		{
			public int NumberOfViews { get; set; }
			/// <summary>
			/// Dimension of this line in the direction of scroll, in physical units
			/// </summary>
			public int Extent { get; set; }
			/// <summary>
			/// Dimension of this line orthogonal to the direction of scroll, in physical units
			/// </summary>
			public int Breadth { get; set; }

			public Uno.UI.IndexPath FirstItem { get; set; }

			public Uno.UI.IndexPath LastItem { get; set; }

			public bool Contains(Uno.UI.IndexPath index) => index >= FirstItem && index <= LastItem;

			public IEnumerable<Uno.UI.IndexPath> Indices
			{
				get
				{
					if (FirstItem.Section != LastItem.Section)
					{
						throw new InvalidOperationException("All items in a line must be in the same section.");
					}
					var current = FirstItem;
					while (current <= LastItem)
					{
						yield return current;
						current = Uno.UI.IndexPath.FromRowSection(current.Row + 1, current.Section);
					}
				}
			}

			public override string ToString()
			{
				if (FirstItem == LastItem)
				{
					return $"Line:{FirstItem}";
				}
				else
				{
					return $"Line:{FirstItem}-{LastItem}";
				}
			}

			private string DebugDisplay => ToString();
		}

		/// <summary>
		/// A materialized group.
		/// </summary>
		private class Group
		{
			private readonly Deque<Line> _lines = new Deque<Line>();

			public Group(int groupIndex)
			{
				GroupIndex = groupIndex;
			}

			public IReadOnlyCollection<Line> Lines => _lines;
			/// <summary>
			/// The absolute index of this group within the source collection.
			/// </summary>
			public int GroupIndex { get; }

			public int Extent => RelativeHeaderPlacement == RelativeHeaderPlacement.Inline ?
				ItemsExtentOffset + ItemsExtent :
				Math.Max(ItemsExtent, HeaderExtent);

			public int Breadth => ItemsBreadthOffset + ItemsBreadth;
			/// <summary>
			/// The extent of all materialized lines.
			/// </summary>
			public int ItemsExtent => _lines.Sum(l => l.Extent);
			/// <summary>
			/// The breadth of the broadest materialized line.
			/// </summary>
			public int ItemsBreadth => _lines.Count > 0 ? _lines.Max(l => l.Breadth) : 0;

			/// <summary>
			/// The offset of the group relative to the top/left of panel (equivalent to GetChildStart()).
			/// </summary>
			public int Start
			{
				get;
				set;
			}

			public int End => Start + Extent;

			public RelativeHeaderPlacement RelativeHeaderPlacement { get; set; }

			/// <summary>
			/// The extent of the header, in physical units.
			/// </summary>
			public int HeaderExtent { get; set; }

			/// <summary>
			/// The breadth of the header, in physical units.
			/// </summary>
			public int HeaderBreadth { get; set; }

			public int ItemsExtentOffset => RelativeHeaderPlacement == RelativeHeaderPlacement.Inline ? HeaderExtent : 0;

			public int ItemsBreadthOffset => RelativeHeaderPlacement == RelativeHeaderPlacement.Adjacent ? HeaderBreadth : 0;

			public Line GetTrailingLine(GeneratorDirection fillDirection)
			{
				return fillDirection == GeneratorDirection.Forward ?
					GetFirstLine() :
					GetLastLine();
			}

			public Line GetLeadingLine(GeneratorDirection fillDirection)
			{
				return fillDirection == GeneratorDirection.Forward ?
					GetLastLine() :
					GetFirstLine();
			}

			public Line GetLeadingLine(GeneratorDirection fillDirection, Func<Uno.UI.IndexPath, bool> condition)
			{
				return fillDirection == GeneratorDirection.Forward ?
					GetLastLine(condition) :
					GetFirstLine(condition);
			}

			public void AddLine(Line newLine, GeneratorDirection fillDirection)
			{
				Debug.Assert(_lines.None(l => l.FirstItem == newLine.FirstItem), "Duplicate line detected");
				if (fillDirection == GeneratorDirection.Forward)
				{
					_lines.AddToBack(newLine);
				}
				else
				{
					_lines.AddToFront(newLine);
					Start -= newLine.Extent;
				}
			}

			public void RemoveTrailingLine(GeneratorDirection fillDirection)
			{
				if (fillDirection == GeneratorDirection.Forward)
				{
					var removed = _lines.RemoveFromFront();
					//Move Start forward because we are removing a line from the start
					Start += removed.Extent;
				}
				else
				{
					_lines.RemoveFromBack();
				}
			}

			public int GetLeadingEdge(GeneratorDirection fillDirection)
			{
				return fillDirection == GeneratorDirection.Forward ?
					End :
					Start;
			}

			public Uno.UI.IndexPath GetLeadingMaterializedItem(GeneratorDirection fillDirection)
			{
				return fillDirection == GeneratorDirection.Forward ?
					GetLastLine().LastItem :
					GetFirstLine().FirstItem;
			}

			public Uno.UI.IndexPath GetLeadingMaterializedItem(GeneratorDirection fillDirection, Func<Uno.UI.IndexPath, bool> condition)
			{
				return fillDirection == GeneratorDirection.Forward ?
					GetLastLine(condition).LastItem :
					GetFirstLine(condition).FirstItem;
			}

			public Uno.UI.IndexPath GetTrailingMaterializedItem(GeneratorDirection fillDirection)
			{
				return fillDirection == GeneratorDirection.Forward ?
					GetFirstLine().FirstItem :
					GetLastLine().LastItem;
			}

			private Line GetFirstLine()
			{
				if (_lines.Count == 0)
				{
					return null;
				}
				return _lines[0];
			}

			private Line GetLastLine()
			{
				if (_lines.Count == 0)
				{
					return null;
				}
				return _lines[_lines.Count - 1];
			}

			private Line GetFirstLine(Func<Uno.UI.IndexPath, bool> condition)
			{
				for (int i = 0; i < _lines.Count; i++)
				{
					var line = _lines[i];
					if (condition(line.FirstItem))
					{
						return line;
					}
				}

				return null;
			}

			private Line GetLastLine(Func<Uno.UI.IndexPath, bool> condition)
			{
				for (int i = _lines.Count - 1; i >= 0; i--)
				{
					var line = _lines[i];
					if (condition(line.FirstItem))
					{
						return line;
					}
				}

				return null;
			}
		}
	}
}
