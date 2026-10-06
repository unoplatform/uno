#if HAS_UNO
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_ListViewBaseItemChrome_RoundedFlag
{
	private const string Key = "ListViewBaseItemRoundedChromeEnabled";

	[TestInitialize]
	public void Init() => ListViewBaseItemPresenter.ClearIsRoundedListViewBaseItemChromeEnabledCache();

	[TestCleanup]
	public void Cleanup() => ListViewBaseItemPresenter.ClearIsRoundedListViewBaseItemChromeEnabledCache();

	[TestMethod]
	public void When_Key_Absent()
	{
		Assert.IsFalse(DependencyProperty.GetBooleanThemeResourceValue("NoSuchBooleanThemeResourceKey"));
	}

	[TestMethod]
	public void When_Fluent_Resource_True()
	{
		using (ListViewChromeHelper.UseRoundedChromeResource(true))
		{
			Assert.IsTrue(ListViewBaseItemPresenter.IsRoundedListViewBaseItemChromeEnabledStatic());
		}
	}

	[TestMethod]
	public void When_UwpStyles_Fluent_Resource_Still_Wins()
	{
		// The Fluent True is a system-level resource, so it outranks Generic.xaml's False.
		using (StyleHelper.UseUwpStyles())
		{
			ListViewBaseItemPresenter.ClearIsRoundedListViewBaseItemChromeEnabledCache();
			Assert.IsTrue(ListViewBaseItemPresenter.IsRoundedListViewBaseItemChromeEnabledStatic());
		}
	}

	[TestMethod]
	public void When_ListView_Resources_Override_Ignored()
	{
		using (ListViewChromeHelper.UseNonRoundedChrome())
		{
			var listView = new ListView();
			listView.Resources[Key] = true;

			Assert.IsFalse(ListViewBaseItemPresenter.IsRoundedListViewBaseItemChromeEnabledStatic());
		}
	}

	[TestMethod]
	public void When_Result_Is_Cached_Until_Cleared()
	{
		var chrome = new ListViewItemPresenter();

		using (ListViewChromeHelper.UseRoundedChromeResource(false))
		{
			Assert.IsFalse(chrome.IsRoundedListViewBaseItemChromeEnabled());

			Application.Current.Resources[Key] = true;
			Assert.IsFalse(chrome.IsRoundedListViewBaseItemChromeEnabled(), "cached");

			ListViewBaseItemPresenter.ClearIsRoundedListViewBaseItemChromeEnabledCache();
			Assert.IsTrue(chrome.IsRoundedListViewBaseItemChromeEnabled());
		}
	}

	[TestMethod]
	public void When_Forced()
	{
		using (ListViewChromeHelper.UseNonRoundedChrome())
		using (ListViewBaseItemChromeRuntimeFeatures.Override(forceRounded: true))
		{
			Assert.IsTrue(ListViewBaseItemPresenter.IsRoundedListViewBaseItemChromeForced());
			Assert.IsTrue(ListViewBaseItemPresenter.IsRoundedListViewBaseItemChromeEnabledStatic());
		}

		Assert.IsFalse(ListViewBaseItemPresenter.IsRoundedListViewBaseItemChromeForced());
	}

	[TestMethod]
	public void When_Deny_Cancels_Force_Only()
	{
		using (ListViewChromeHelper.UseRoundedChromeResource(true))
		using (ListViewBaseItemChromeRuntimeFeatures.Override(forceRounded: true, denyRounded: true))
		{
			Assert.IsFalse(ListViewBaseItemPresenter.IsRoundedListViewBaseItemChromeForced());
			// Deny does not touch the resource path.
			Assert.IsTrue(ListViewBaseItemPresenter.IsRoundedListViewBaseItemChromeEnabledStatic());
		}
	}

	[TestMethod]
	public void When_ListView_Created_After_UseUwpStyles()
	{
		using (StyleHelper.UseUwpStyles())
		{
			Assert.IsTrue(new ListViewItemPresenter().IsRoundedListViewBaseItemChromeEnabled());
		}

		Assert.IsTrue(new ListViewItemPresenter().IsRoundedListViewBaseItemChromeEnabled());
	}
}
#endif
