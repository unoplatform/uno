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

		[TestMethod]
		[RunsOnUIThread]
		public void When_Insert_Overwrites_Existing()
		{
			const string key = "TestKey";
			const string originalValue = "original";
			const string newValue = "newValue";

			var resourceDictionary = new ResourceDictionary();
			resourceDictionary[key] = originalValue;
			var wasOverwrite = resourceDictionary.Insert(key, newValue);

			Assert.AreEqual(newValue, resourceDictionary[key]);
			Assert.IsTrue(wasOverwrite);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_Insert_New_Key()
		{
			const string key = "TestKey";			
			const string newValue = "newValue";

			var resourceDictionary = new ResourceDictionary();
			var wasOverwrite = resourceDictionary.Insert(key, newValue);

			Assert.AreEqual(newValue, resourceDictionary[key]);
			Assert.IsFalse(wasOverwrite);
		}
	}
}
