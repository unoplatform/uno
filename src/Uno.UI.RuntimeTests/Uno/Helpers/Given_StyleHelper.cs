using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Uno_Helpers;

[TestClass]
[RunsOnUIThread]
public class Given_StyleHelper
{
	[TestMethod]
#if WINAPPSDK
	[Ignore("UseUwpStyles is a no-op on WinUI")]
#endif
	public void When_UseUwpStyles_Leaked_Then_Restore_Brings_Back_Fluent()
	{
		var initialCount = XamlControlsResourcesCount();
		Assert.IsGreaterThan(0, initialCount, "The test app is expected to use Fluent styles");

		// Mirrors a test that throws before disposing its UWP styles override.
		using (StyleHelper.UseUwpStyles())
		{
			Assert.AreEqual(initialCount - 1, XamlControlsResourcesCount());

			Assert.IsTrue(StyleHelper.RestoreFluentStyles());

			Assert.AreEqual(initialCount, XamlControlsResourcesCount());
			Assert.IsTrue(Application.Current.Resources.ContainsKey("TextFillColorPrimaryBrush"));
		}

		// The late dispose of the leaked override must not insert the dictionary a second time.
		Assert.AreEqual(initialCount, XamlControlsResourcesCount());
	}

	[TestMethod]
	public void When_UseUwpStyles_Disposed_Then_Nothing_To_Restore()
	{
		var initialCount = XamlControlsResourcesCount();

		using (StyleHelper.UseUwpStyles())
		{
		}

		Assert.IsFalse(StyleHelper.RestoreFluentStyles());
		Assert.AreEqual(initialCount, XamlControlsResourcesCount());
	}

	private static int XamlControlsResourcesCount()
		=> Application.Current.Resources.MergedDictionaries.OfType<XamlControlsResources>().Count();
}
