#nullable enable

using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation.Collections;

namespace Uno.UI.Tests.Windows_Foundation_Collections;

[TestClass]
public class Given_ObservableVector_ReplaceAll
{
	[TestMethod]
	public void When_ReplaceAll_Then_Single_Reset()
	{
		ObservableVector<string> vector = new() { "one", "two", "three" };
		List<(CollectionChange Change, uint Index)> typed = new();
		List<(CollectionChange Change, uint Index)> untyped = new();
		vector.VectorChanged += (s, e) => typed.Add((e.CollectionChange, e.Index));
		((IObservableVector)vector).UntypedVectorChanged += (s, e) => untyped.Add((e.CollectionChange, e.Index));

		vector.ReplaceAll(new[] { "a", "b" });

		CollectionAssert.AreEqual(new[] { "a", "b" }, vector.ToArray());
		CollectionAssert.AreEqual(new[] { (CollectionChange.Reset, 0u) }, typed);
		CollectionAssert.AreEqual(new[] { (CollectionChange.Reset, 0u) }, untyped);
	}

	[TestMethod]
	public void When_ReplaceAll_With_Empty_Then_Single_Reset()
	{
		ObservableVector<int> vector = new() { 1, 2, 3 };
		var resetCount = 0;
		var otherCount = 0;
		vector.VectorChanged += (s, e) =>
		{
			if (e.CollectionChange == CollectionChange.Reset)
			{
				resetCount++;
			}
			else
			{
				otherCount++;
			}
		};

		vector.ReplaceAll(Enumerable.Empty<int>());

		Assert.AreEqual(0, vector.Count);
		Assert.AreEqual(1, resetCount);
		Assert.AreEqual(0, otherCount);
	}

	[TestMethod]
	public void When_ReplaceAll_With_Own_Contents_Then_Contents_Kept()
	{
		ObservableVector<int> vector = new() { 1, 2, 3 };

		vector.ReplaceAll(vector.Where(i => i != 2));

		CollectionAssert.AreEqual(new[] { 1, 3 }, vector.ToArray());
	}

	[TestMethod]
	public void When_Contents_Read_In_Handler_Then_New_Contents_Visible()
	{
		ObservableVector<string> vector = new() { "old" };
		string[]? seen = null;
		vector.VectorChanged += (s, e) => seen = vector.ToArray();

		vector.ReplaceAll(new[] { "x", "y", "z" });

		CollectionAssert.AreEqual(new[] { "x", "y", "z" }, seen);
	}
}
