#nullable enable

using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Helpers;

namespace Uno.UI.Tests.Helpers;

[TestClass]
public class Given_MarkupHelper
{
	[TestMethod]
	public void When_SetParent_Then_DataContextIsInherited()
	{
		var parent = new Border { DataContext = 42 };
		var child = new Border();

		MarkupHelper.SetParent(child, parent);

		Assert.AreEqual(42, child.DataContext);
	}

	[TestMethod]
	public void When_SetParent_Called_Again_Then_DataContextFollowsNewParent()
	{
		var parent1 = new Border { DataContext = 10 };
		var parent2 = new Border { DataContext = 42 };
		var child = new Border();

		MarkupHelper.SetParent(child, parent1);
		Assert.AreEqual(10, child.DataContext);

		MarkupHelper.SetParent(child, parent2);
		Assert.AreEqual(42, child.DataContext);
	}

	[TestMethod]
	public void When_SetParent_Null_Then_DataContextIsCleared()
	{
		var parent = new Border { DataContext = 42 };
		var child = new Border();

		MarkupHelper.SetParent(child, parent);
		Assert.AreEqual(42, child.DataContext);

		MarkupHelper.SetParent(child, null);
		Assert.IsNull(child.DataContext);
	}
}
