#nullable enable

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.RuntimeTests.Tests.Uno_Core;

[TestClass]
public class Given_CachedTuple
{
	[TestMethod]
	public void When_Only_Later_Item_Differs_Then_Hash_Differs()
	{
		Assert.AreNotEqual(global::Uno.CachedTuple.Create("a", 1).GetHashCode(), global::Uno.CachedTuple.Create("a", 2).GetHashCode());
		Assert.AreNotEqual(global::Uno.CachedTuple.Create("a", 1, 3).GetHashCode(), global::Uno.CachedTuple.Create("a", 1, 4).GetHashCode());
		Assert.AreNotEqual(global::Uno.CachedTuple.Create("a", 1, 3, 5).GetHashCode(), global::Uno.CachedTuple.Create("a", 1, 3, 6).GetHashCode());
		Assert.AreNotEqual(global::Uno.CachedTuple.Create("a", 1f, 3, 5, 7).GetHashCode(), global::Uno.CachedTuple.Create("a", 2f, 3, 5, 7).GetHashCode());
	}

	[TestMethod]
	public void When_Items_Equal_Then_Hash_Equal()
		=> Assert.AreEqual(global::Uno.CachedTuple.Create("a", 1f, 3, 5, 7).GetHashCode(), global::Uno.CachedTuple.Create("a", 1f, 3, 5, 7).GetHashCode());

	[TestMethod]
	public void When_First_Item_Null_Then_Other_Items_Hashed()
		=> Assert.AreNotEqual(global::Uno.CachedTuple.Create((string?)null, 1).GetHashCode(), global::Uno.CachedTuple.Create((string?)null, 2).GetHashCode());
}
