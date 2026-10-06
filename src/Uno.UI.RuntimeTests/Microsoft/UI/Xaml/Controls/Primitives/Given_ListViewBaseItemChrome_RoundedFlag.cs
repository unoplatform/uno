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
	public void Init() => ListViewBaseItemChrome.ClearIsRoundedListViewBaseItemChromeEnabledCache();

	[TestCleanup]
	public void Cleanup() => ListViewBaseItemChrome.ClearIsRoundedListViewBaseItemChromeEnabledCache();

	[TestMethod]
	public void When_Key_Absent()
	{
		Assert.IsFalse(DependencyProperty.GetBooleanThemeResourceValue("NoSuchBooleanThemeResourceKey"));
	}

	[TestMethod]
	public void When_Fluent_Resource_True()
	{
		// TODO: the Fluent theme only ships the True value from the style-tables chunk; set it explicitly until then.
		using (ListViewChromeHelper.UseRoundedChromeResource(true))
		{
			Assert.IsTrue(ListViewBaseItemChrome.IsRoundedListViewBaseItemChromeEnabledStatic());
		}
	}

	[TestMethod]
	public void When_Generic_Resource_False()
	{
		using (StyleHelper.UseUwpStyles())
		{
			ListViewBaseItemChrome.ClearIsRoundedListViewBaseItemChromeEnabledCache();
			Assert.IsFalse(ListViewBaseItemChrome.IsRoundedListViewBaseItemChromeEnabledStatic());
		}
	}

	[TestMethod]
	public void When_ListView_Resources_Override_Ignored()
	{
		using (ListViewChromeHelper.UseNonRoundedChrome())
		{
			var listView = new ListView();
			listView.Resources[Key] = true;

			Assert.IsFalse(ListViewBaseItemChrome.IsRoundedListViewBaseItemChromeEnabledStatic());
		}
	}

	[TestMethod]
	public void When_Result_Is_Cached_Until_Cleared()
	{
		var chrome = new ListViewBaseItemChrome();

		using (ListViewChromeHelper.UseRoundedChromeResource(false))
		{
			Assert.IsFalse(chrome.IsRoundedListViewBaseItemChromeEnabled());

			Application.Current.Resources[Key] = true;
			Assert.IsFalse(chrome.IsRoundedListViewBaseItemChromeEnabled(), "cached");

			ListViewBaseItemChrome.ClearIsRoundedListViewBaseItemChromeEnabledCache();
			Assert.IsTrue(chrome.IsRoundedListViewBaseItemChromeEnabled());
		}
	}

	[TestMethod]
	public void When_Forced()
	{
		using (ListViewChromeHelper.UseNonRoundedChrome())
		using (ListViewBaseItemChromeRuntimeFeatures.Override(forceRounded: true))
		{
			Assert.IsTrue(ListViewBaseItemChrome.IsRoundedListViewBaseItemChromeForced());
			Assert.IsTrue(ListViewBaseItemChrome.IsRoundedListViewBaseItemChromeEnabledStatic());
		}

		Assert.IsFalse(ListViewBaseItemChrome.IsRoundedListViewBaseItemChromeEnabledStatic());
	}

	[TestMethod]
	public void When_Deny_Cancels_Force_Only()
	{
		using (ListViewChromeHelper.UseRoundedChromeResource(true))
		using (ListViewBaseItemChromeRuntimeFeatures.Override(forceRounded: true, denyRounded: true))
		{
			Assert.IsFalse(ListViewBaseItemChrome.IsRoundedListViewBaseItemChromeForced());
			// Deny does not touch the resource path.
			Assert.IsTrue(ListViewBaseItemChrome.IsRoundedListViewBaseItemChromeEnabledStatic());
		}
	}

	[TestMethod]
	public void When_ListView_Created_After_UseUwpStyles()
	{
		using (ListViewChromeHelper.UseRoundedChromeResource(true))
		{
			using (StyleHelper.UseUwpStyles())
			{
			}

			var chrome = new ListViewBaseItemChrome();
			Assert.IsTrue(chrome.IsRoundedListViewBaseItemChromeEnabled());
		}
	}
}
#endif
