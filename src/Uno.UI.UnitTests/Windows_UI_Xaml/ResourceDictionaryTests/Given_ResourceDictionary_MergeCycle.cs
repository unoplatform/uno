using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Tests.ResourceDictionaryTests
{
	[UITestClass]
	public class Given_ResourceDictionary_MergeCycle
	{
		private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26428")]
		public void When_Dictionary_Merged_Into_Itself_Then_Throws()
		{
			var dictionary = new ResourceDictionary();

			AssertThrowsWithoutHanging(() => dictionary.MergedDictionaries.Add(dictionary));

			Assert.AreEqual(0, dictionary.MergedDictionaries.Count);
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26428")]
		public void When_Dictionaries_Merged_Into_Each_Other_Then_Throws()
		{
			var a = new ResourceDictionary();
			var b = new ResourceDictionary();
			a.MergedDictionaries.Add(b);

			AssertThrowsWithoutHanging(() => b.MergedDictionaries.Add(a));

			Assert.AreEqual(0, b.MergedDictionaries.Count);
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26428")]
		public void When_Ancestor_Merged_Into_Descendant_Then_Throws()
		{
			var a = new ResourceDictionary();
			var b = new ResourceDictionary();
			var c = new ResourceDictionary();
			a.MergedDictionaries.Add(b);
			b.MergedDictionaries.Add(c);

			AssertThrowsWithoutHanging(() => c.MergedDictionaries.Add(a));

			Assert.AreEqual(0, c.MergedDictionaries.Count);
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26428")]
		public void When_Dictionary_Replaced_By_Itself_Then_Throws()
		{
			var a = new ResourceDictionary();
			var b = new ResourceDictionary();
			a.MergedDictionaries.Add(b);

			AssertThrowsWithoutHanging(() => a.MergedDictionaries[0] = a);

			Assert.AreSame(b, a.MergedDictionaries[0]);
		}

		[TestMethod]
		public void When_Same_Dictionary_Merged_Into_Two_Parents_Then_Allowed()
		{
			// Uno itself shares merged dictionaries between parents (ResourceDictionary.Source copies them).
			var shared = new ResourceDictionary();
			var first = new ResourceDictionary();
			var second = new ResourceDictionary();

			first.MergedDictionaries.Add(shared);
			second.MergedDictionaries.Add(shared);

			Assert.AreSame(shared, second.MergedDictionaries[0]);
		}

		// Without the check the add never returns, so run it on another thread with a timeout.
		private static void AssertThrowsWithoutHanging(Action action)
		{
			var task = Task.Run(action);
			Assert.IsTrue(Task.WhenAny(task, Task.Delay(Timeout)).Result == task, "The operation did not complete.");
			Assert.IsInstanceOfType<InvalidOperationException>(task.Exception?.InnerException);
		}
	}
}
