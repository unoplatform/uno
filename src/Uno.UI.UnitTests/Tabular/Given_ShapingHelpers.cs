#nullable enable

#pragma warning disable CS8305 // The Tabular types are [Experimental], as in WinUI

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml.Controls.Tabular;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Tests.Tabular;

[TestClass]
public class Given_ShapingHelpers
{
	private sealed class Person(string name)
	{
		public string Name { get; } = name;

		public override string ToString() => Name;
	}

	[TestMethod]
	public void When_Reference_Keys_Then_Ordered_By_ToString()
	{
		var b = new Person("B");
		var a = new Person("A");

		Assert.IsTrue(ShapingHelpers.ValueComparer.UsesFallbackKey(a));
		Assert.IsTrue(ShapingHelpers.ValueComparer.UsesFallbackKey(b));
		Assert.IsTrue(ShapingHelpers.ValueComparer.Compare(a, b) < 0);
		Assert.IsTrue(ShapingHelpers.ValueComparer.Compare(b, a) > 0);
	}

	[TestMethod]
	public void When_StableSortByKeys_On_Reference_Keys_Then_Ordered_By_ToString()
	{
		List<object?> items = new() { new Person("B"), new Person("A") };

		ShapingHelpers.StableSortByKeys(items, 1, (item, _) => item, _ => SortDirection.Ascending);

		CollectionAssert.AreEqual(new[] { "A", "B" }, items.Select(i => ((Person)i!).Name).ToArray());
	}

	[TestMethod]
	public void When_Value_Keys_Then_Not_Fallback()
	{
		Assert.IsFalse(ShapingHelpers.ValueComparer.UsesFallbackKey(42));
		Assert.IsFalse(ShapingHelpers.ValueComparer.UsesFallbackKey("text"));
		Assert.IsFalse(ShapingHelpers.ValueComparer.UsesFallbackKey(SortDirection.Ascending));
	}

	[TestMethod]
	public void When_Guid_Keys_Then_Data1_Compared_Unsigned()
	{
		var low = Guid.Parse("7fffffff-0000-0000-0000-000000000000");
		var high = Guid.Parse("80000000-0000-0000-0000-000000000000");

		Assert.IsTrue(ShapingHelpers.ValueComparer.Compare(low, high) < 0);
		Assert.IsTrue(ShapingHelpers.ValueComparer.Compare(high, low) > 0);
		Assert.AreEqual(0, ShapingHelpers.ValueComparer.Compare(high, Guid.Parse("80000000-0000-0000-0000-000000000000")));
	}

	[TestMethod]
	public void When_Guid_Key_Formatted_Then_Uppercase_D_Layout()
	{
		var guid = Guid.Parse("0a1b2c3d-4e5f-6a7b-8c9d-aebfc0d1e2f3");

		Assert.IsTrue(ShapingHelpers.ValueKey.TryFormatPropertyValue(guid, out var key, false));
		Assert.AreEqual("g:0A1B2C3D-4E5F-6A7B-8C9D-AEBFC0D1E2F3", key);
	}
}
