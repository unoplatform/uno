using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ObjectiveC;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Documents;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

[TestClass]
public class Given_DependencyObjectCollection
{
	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void When_Add_Multiple_And_Invoke()
	{
		DependencyObjectCollection c = new();

		List<string> list = [];

		void One(object sender, object args) => list.Add("One");
		void Two(object sender, object args) => list.Add("Two");

		c.VectorChanged += One;
		c.VectorChanged += Two;
		c.VectorChanged += One;
		c.VectorChanged -= One;

		c.Add(c);

		Assert.IsTrue(list.SequenceEqual(["One", "Two"]));
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void When_Indexer_Get_IndexOutOfRange()
	{
		Assert.IsNull(new DependencyObjectCollection()[int.MaxValue]);
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void When_Indexer_Get_NegativeIndex()
	{
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = new DependencyObjectCollection()[-1]);
	}

	// Reproduces https://github.com/unoplatform/uno/issues/22932
	// UWPSyncGenerator emits a [NotImplemented] stub for BlockCollection.this[int] that
	// shadows the working DependencyObjectCollection<Block> base class implementation,
	// causing NotSupportedException when accessing elements by index.

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/22932")]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void When_BlockCollection_Indexer_Get_Returns_Added_Item()
	{
		var collection = new BlockCollection();
		var paragraph = new Paragraph();
		collection.Add(paragraph);

		// This should return the paragraph but currently throws NotSupportedException
		// because the generated stub in BlockCollection.cs shadows DependencyObjectCollection<Block>.this[int]
		var retrieved = collection[0];

		Assert.AreSame(paragraph, retrieved);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/22932")]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void When_BlockCollection_Indexer_Set_Replaces_Item()
	{
		var collection = new BlockCollection();
		var original = new Paragraph();
		var replacement = new Paragraph();
		collection.Add(original);

		// This should replace the item but currently throws NotSupportedException
		// because the generated stub in BlockCollection.cs shadows DependencyObjectCollection<Block>.this[int]
		collection[0] = replacement;

		Assert.AreSame(replacement, collection[0]);
	}
}
