#nullable enable

using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation.Collections;

namespace Uno.UI.Tests.Windows_Foundation_Collections;

[TestClass]
public class Given_ObservableVector_SetAt
{
	[TestMethod]
	public void When_Same_Item_Set_Then_ItemChanged_Raised()
	{
		var item = new object();
		ObservableVector<object> vector = new() { new object(), item };
		List<(CollectionChange Change, uint Index)> typed = new();
		List<(CollectionChange Change, uint Index)> untyped = new();
		vector.VectorChanged += (s, e) => typed.Add((e.CollectionChange, e.Index));
		((IObservableVector)vector).UntypedVectorChanged += (s, e) => untyped.Add((e.CollectionChange, e.Index));

		vector[1] = item;

		CollectionAssert.AreEqual(new[] { (CollectionChange.ItemChanged, 1u) }, typed);
		CollectionAssert.AreEqual(new[] { (CollectionChange.ItemChanged, 1u) }, untyped);
		Assert.AreSame(item, vector[1]);
		Assert.AreEqual(2, vector.Count);
	}

	[TestMethod]
	public void When_Different_Item_Set_Then_ItemChanged_Raised()
	{
		ObservableVector<string> vector = new() { "one", "two" };
		List<(CollectionChange Change, uint Index)> changes = new();
		vector.VectorChanged += (s, e) => changes.Add((e.CollectionChange, e.Index));

		vector[0] = "uno";

		CollectionAssert.AreEqual(new[] { (CollectionChange.ItemChanged, 0u) }, changes);
		Assert.AreEqual("uno", vector[0]);
	}

	[TestMethod]
	public void When_Same_Item_Set_Repeatedly_Then_Raised_Each_Time()
	{
		ObservableVector<string> vector = new() { "one" };
		var count = 0;
		vector.VectorChanged += (s, e) => count++;

		vector[0] = vector[0];
		vector[0] = vector[0];

		Assert.AreEqual(2, count);
	}
}
