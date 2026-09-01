using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Tests.Windows_UI_Xaml.Controls;

namespace Uno.UI.Tests.Windows_UI_Xaml
{
	/// <summary>
	/// x:Name on a resource entry produces a backing field, as it does in WinUI.
	/// https://github.com/unoplatform/uno/issues/24293
	/// </summary>
	[TestClass]
	public class Given_NamedResource
	{
		[TestInitialize]
		public void Init() => UnitTestsApp.App.EnsureApplication();

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24293")]
		public void When_Resource_Is_Named_It_Has_A_Backing_Field()
		{
			var SUT = new When_Named_Resource();

			Assert.IsNotNull(SUT.NamedFlyout, "the named MenuFlyout resource has no backing field value");
			Assert.AreEqual(SUT.Resources["NamedFlyout"], SUT.NamedFlyout);
			Assert.IsNotNull(SUT.NamedTemplate);
		}
	}
}
