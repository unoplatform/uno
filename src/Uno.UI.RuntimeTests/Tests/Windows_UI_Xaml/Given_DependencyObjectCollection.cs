using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ObjectiveC;
using Microsoft.UI.Xaml;
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
}
