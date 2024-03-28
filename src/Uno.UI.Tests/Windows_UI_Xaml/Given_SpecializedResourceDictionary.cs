using Microsoft.VisualStudio.TestTools.UnitTesting;

using ResourceKey = Windows.UI.Xaml.SpecializedResourceDictionary.ResourceKey;

namespace Uno.UI.Tests.Windows_UI_Xaml
{
	[TestClass]
	public class Given_SpecializedResourceDictionary
	{
		[TestInitialize]
		public void Init()
		{
			UnitTestsApp.App.EnsureApplication();
		}

		[TestMethod]
		public void When_ResourceKey_HashCode_Equal()
		{
			var s1 = "StringKey";
			var s2 = "StringKey";

			var t1 = typeof(string);
			var t2 = typeof(string);

			var k1 = new ResourceKey(s1);
			var k2 = new ResourceKey(s2);
			var k3 = new ResourceKey(t1);
			var k4 = new ResourceKey(t2);

			Assert.AreEqual(k1.HashCode, k2.HashCode);
			Assert.AreEqual(k3.HashCode, k4.HashCode);
		}

		[TestMethod]
		public void When_ResourceKey_HashCode_NotEqual()
		{
			var s1 = "StringKey";
			var s2 = "ByteKey";

			var t1 = typeof(string);
			var t2 = typeof(byte);

			var k1 = new ResourceKey(s1);
			var k2 = new ResourceKey(s2);
			var k3 = new ResourceKey(t1);
			var k4 = new ResourceKey(t2);

			Assert.AreNotEqual(k1.HashCode, k2.HashCode);
			Assert.AreNotEqual(k3.HashCode, k4.HashCode);
		}
	}
}
