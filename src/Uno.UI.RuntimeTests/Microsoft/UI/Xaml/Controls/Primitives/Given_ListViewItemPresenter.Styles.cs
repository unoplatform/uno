#if HAS_UNO
#nullable enable

using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

public partial class Given_ListViewItemPresenter
{
	[TestMethod]
	public async Task When_DefaultListViewItemStyle_Renders_Rounded_Presenter()
	{
		ListViewBaseItemPresenter.ClearIsRoundedListViewBaseItemChromeEnabledCache();
		try
		{
			var style = (Style)Application.Current.Resources["DefaultListViewItemStyle"];
			var item = new ListViewItem { Content = "Item", Style = style };

			WindowHelper.WindowContent = item;
			await WindowHelper.WaitForLoaded(item);
			await WindowHelper.WaitForIdle();

			var presenter = (ListViewItemPresenter)VisualTreeHelper.GetChild(item, 0);
			Assert.AreEqual(new CornerRadius(4), presenter.CornerRadius);
			Assert.IsNotNull(ChromeTestHelper.GetField<Border>(presenter, "m_backplateRectangle"), "Rounded chrome creates the backplate");
		}
		finally
		{
			ListViewBaseItemPresenter.ClearIsRoundedListViewBaseItemChromeEnabledCache();
		}
	}

	[TestMethod]
	public async Task When_DefaultGridViewItemStyle_Roots_On_ListViewItemPresenter()
	{
		var style = (Style)Application.Current.Resources["DefaultGridViewItemStyle"];
		var item = new GridViewItem { Content = "Item", Style = style };

		WindowHelper.WindowContent = item;
		await WindowHelper.WaitForLoaded(item);
		await WindowHelper.WaitForIdle();

		Assert.IsInstanceOfType(VisualTreeHelper.GetChild(item, 0), typeof(ListViewItemPresenter));
	}

	[TestMethod]
	public void When_Rounded_Chrome_Resource_And_New_Keys_Present()
	{
		var resources = Application.Current.Resources;

		Assert.AreEqual(true, resources["ListViewBaseItemRoundedChromeEnabled"]);
		Assert.AreEqual(new CornerRadius(4), resources["ListViewItemCornerRadius"]);
		Assert.AreEqual(new CornerRadius(1.5), resources["ListViewItemSelectionIndicatorCornerRadius"]);
		Assert.IsNotNull(resources["ListViewItemSelectionIndicatorBrush"]);
		Assert.IsNotNull(resources["ListViewItemCheckBoxSelectedBrush"]);
		Assert.IsNotNull(resources["ListViewItemBackgroundSelectedDisabled"]);
		Assert.IsInstanceOfType(resources["ListViewItemExpanded"], typeof(Style));
		Assert.IsInstanceOfType(resources["ListPickerFlyoutPresenterItemStyle"], typeof(Style));
	}
}
#endif
