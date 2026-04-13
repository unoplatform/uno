using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Windows.Foundation;
using Uno.UI.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	public partial class Given_ListViewBase
	{
		// Reproduction for https://github.com/unoplatform/uno/issues/12081
		// Reported: on Android native, toggling a CheckBox inside an expandable
		// ListView item causes the ListView to be re-measured many times (up to 23)
		// and items to be detached/re-attached. Runtime tests run on Skia, so this
		// test acts as regression coverage on the Skia path for the same scenario.
		private partial class MeasureCountingListView : ListView
		{
			public int MeasureCount;
			public int ArrangeCount;

			protected override Size MeasureOverride(Size availableSize)
			{
				MeasureCount++;
				return base.MeasureOverride(availableSize);
			}

			protected override Size ArrangeOverride(Size finalSize)
			{
				ArrangeCount++;
				return base.ArrangeOverride(finalSize);
			}
		}

		[TestMethod]
		[RunsOnUIThread]
#if __APPLE_UIKIT__ || __ANDROID__
		[Ignore("https://github.com/unoplatform/uno/issues/12081 - native ListView re-measure path.")]
#endif
		public async Task When_Expandable_Item_CheckBox_Toggled_12081()
		{
			var itemTemplate = (DataTemplate)XamlReader.Load(
				"""
				<DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
							  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
					<StackPanel>
						<CheckBox x:Name="CheckBox" Content="IsExpanded" />
						<Border Width="100" Height="100" Background="Red"
								Visibility="{Binding IsChecked, ElementName=CheckBox, FallbackValue=Collapsed}" />
					</StackPanel>
				</DataTemplate>
				""");

			var SUT = new MeasureCountingListView
			{
				Height = 400,
				Width = 300,
				ItemsSource = Enumerable.Range(0, 5).Select(i => $"Item {i}").ToList(),
				ItemTemplate = itemTemplate,
			};

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.MeasureCount = 0;
			SUT.ArrangeCount = 0;

			var firstContainer = SUT.ContainerFromIndex(0) as ListViewItem;
			Assert.IsNotNull(firstContainer, "First container should exist");

			var firstCheckBox = firstContainer.FindFirstDescendant<CheckBox>();
			Assert.IsNotNull(firstCheckBox, "CheckBox should exist in first item");

			firstCheckBox.IsChecked = true;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(
				SUT.MeasureCount <= 5,
				$"Expected ListView to be re-measured a small number of times after toggling a CheckBox inside an item, " +
				$"but MeasureCount={SUT.MeasureCount} (ArrangeCount={SUT.ArrangeCount}). " +
				$"See https://github.com/unoplatform/uno/issues/12081");
		}
	}
}
