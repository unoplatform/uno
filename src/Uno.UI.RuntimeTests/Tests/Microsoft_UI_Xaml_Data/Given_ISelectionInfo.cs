using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Data
{
	[TestClass]
	public class Given_ISelectionInfo
	{
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_SelectOne()
		{
			var source = SelectionSource.Create(10, isPreselected: x => false);
			var lv = new ListView { ItemsSource = source };

			WindowHelper.WindowContent = lv;
			await WindowHelper.WaitForLoaded(lv);

			var selectionRanges = new List<ItemIndexRange>();
			source.SelectRangeOverride = range =>
			{
				selectionRanges.Add(range);
				source.SelectRangeImpl(range);
			};

			lv.SelectedIndex = 3;

			Assert.IsTrue(source.IsSelected(3), "source.IsSelected(3) == true");
			Assert.AreEqual(1, selectionRanges.Count, "selectionRanges.Count == 1");
			var expectedRange = new ItemIndexRange(3, 1);
			Assert.AreEqual(expectedRange.FirstIndex, selectionRanges[0].FirstIndex, "selectionRanges[0] == { 3..3, count = 1 }");
			Assert.AreEqual(expectedRange.LastIndex, selectionRanges[0].LastIndex, "selectionRanges[0] == { 3..3, count = 1 }");
			Assert.AreEqual(expectedRange.Length, selectionRanges[0].Length, "selectionRanges[0] == { 3..3, count = 1 }");
		}

#if HAS_UNO // On Windows this test fails, because the SelectedIndex remains -1, even though visually the item is selected
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_PreselectOne()
		{
			var source = SelectionSource.Create(10, isPreselected: x => x == 3);
			var lv = new ListView { ItemsSource = source };

			WindowHelper.WindowContent = lv;
			await WindowHelper.WaitForLoaded(lv);

			Assert.AreEqual(3, lv.SelectedIndex, "SelectedIndex should be 3");
		}
#endif

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_SelectOne_Denied()
		{
			var source = SelectionSource.Create(10, isPreselected: x => false);
			var lv = new ListView { ItemsSource = source };

			WindowHelper.WindowContent = lv;
			await WindowHelper.WaitForLoaded(lv);

			source.SelectRangeOverride = range =>
			{
				// denied
			};

			lv.SelectedIndex = 3;

			Assert.IsFalse(source.IsSelected(3), "source.IsSelected(3) == false");
			Assert.AreEqual(-1, lv.SelectedIndex, "lv.SelectedIndex == -1");
			Assert.IsNull(lv.SelectedItem, "lv.SelectedItem == null");
		}

		private class SelectionData
		{
			public int Value { get; set; }
			public bool Selected { get; set; }

			public override string ToString() => Value.ToString();
		}
		private class SelectionSource : List<SelectionData>, ISelectionInfo
		{
			public Action<ItemIndexRange> SelectRangeOverride { get; set; }
			public Action<ItemIndexRange> DeselectRangeOverride { get; set; }
			public Func<int, bool> IsSelectedOverride { get; set; }
			public Func<IReadOnlyList<ItemIndexRange>> GetSelectedRangesOverride { get; set; }

			private SelectionSource(IEnumerable<SelectionData> source) : base(source)
			{
				this.SelectRangeOverride = SelectRangeImpl;
				this.DeselectRangeOverride = DeselectRangeImpl;
				this.IsSelectedOverride = IsSelectedImpl;
				this.GetSelectedRangesOverride = GetSelectedRangesImpl;
			}
			public static SelectionSource Create(int count, Func<int, bool> isPreselected) => Create(Enumerable.Range(0, count), isPreselected);
			public static SelectionSource Create(IEnumerable<int> source, Func<int, bool> isPreselected)
			{
				return new(source.Select(x => new SelectionData
				{
					Value = x,
					Selected = isPreselected(x),
				}));
			}

			// ISelectionInfo
			public void SelectRange(ItemIndexRange itemIndexRange) => SelectRangeOverride(itemIndexRange);
			public void DeselectRange(ItemIndexRange itemIndexRange) => DeselectRangeOverride(itemIndexRange);
			public bool IsSelected(int index) => IsSelectedOverride(index);
			public IReadOnlyList<ItemIndexRange> GetSelectedRanges() => GetSelectedRangesOverride();

			// ISelectionInfo impl
			internal void SelectRangeImpl(ItemIndexRange itemIndexRange)
			{
				foreach (var index in ExpandRange(itemIndexRange))
				{
					this[index].Selected = true;
				}
			}
			internal void DeselectRangeImpl(ItemIndexRange itemIndexRange)
			{
				foreach (var index in ExpandRange(itemIndexRange))
				{
					this[index].Selected = false;
				}
			}
			internal bool IsSelectedImpl(int index) => this[index].Selected;
			internal IReadOnlyList<ItemIndexRange> GetSelectedRangesImpl()
			{
				return ReduceToRange(this
					.Select((x, i) => (Index: i, x.Selected))
					.Where(x => x.Selected)
					.Select(x => x.Index)
				).ToArray();
			}

			// helper methods
			internal static IEnumerable<ItemIndexRange> ReduceToRange(IEnumerable<int> indexes)
			{
				int first = int.MinValue;
				uint n = 0;
				foreach (var i in indexes.OrderBy(x => x))
				{
					if (first + n == i)
					{
						n++;
					}
					else
					{
						if (n > 0) yield return new(first, n);

						first = i;
						n = 1;
					}
				}

				if (n > 0)
				{
					yield return new(first, n);
				}
			}
			internal static IEnumerable<int> ExpandRange(ItemIndexRange range) => Enumerable.Range(range.FirstIndex, (int)range.Length);
		}
	}
}
