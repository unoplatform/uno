using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Xaml;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml
{
	[TestClass]
	public class Given_ResourceDictionary
    {
		[TestMethod]
		[RunsOnUIThread]
		public void When_Key_Overwritten()
		{
			const string key = "TestKey";
			const string originalValue = "original";
			const string newValue = "newValue";

			var resourceDictionary = new ResourceDictionary();
			resourceDictionary[key] = originalValue;
			resourceDictionary[key] = newValue;

			Assert.AreEqual(newValue, resourceDictionary[key]);
		}
	}
}
