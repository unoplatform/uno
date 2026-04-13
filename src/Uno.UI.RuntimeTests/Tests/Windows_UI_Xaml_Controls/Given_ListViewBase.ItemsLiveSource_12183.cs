using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	public partial class Given_ListViewBase
	{
		// Reproduction for https://github.com/unoplatform/uno/issues/12183
		// When a non-observable collection (List<T>) is assigned to ItemsSource,
		// ItemsControl.Items is expected to stay live-linked to the source on UWP
		// (Items[i] == ItemsSource[i], counts match). Uno regressed after #12158
		// to snapshot the source, so mutations to the source no longer show up
		// through Items even before/without a re-render.
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Non_Observable_ItemsSource_Items_Reflect_Source_Mutations_12183()
		{
			var source = new List<string> { "a", "b", "c" };

			var SUT = new ListView
			{
				ItemsSource = source,
			};

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(3, SUT.Items.Count, "Initial Items.Count should match source.");
			Assert.AreEqual("a", SUT.Items[0]);
			Assert.AreEqual("b", SUT.Items[1]);
			Assert.AreEqual("c", SUT.Items[2]);

			// Mutate the underlying non-observable source.
			source[1] = "B!";
			source.Add("d");

			// Per UWP behavior, Items should reflect the live source state
			// (even though the rendered UI won't update without INotifyCollectionChanged).
			Assert.AreEqual(4, SUT.Items.Count, "Items.Count should track live source after mutation.");
			Assert.AreEqual("a", SUT.Items[0]);
			Assert.AreEqual("B!", SUT.Items[1]);
			Assert.AreEqual("c", SUT.Items[2]);
			Assert.AreEqual("d", SUT.Items[3]);
		}
	}
}
