#nullable enable

#pragma warning disable CS8305 // The Tabular types are [Experimental], as in WinUI

using System;
using System.Collections.Generic;
using System.Globalization;
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
	public void When_Reference_Keys_Then_Fallback_Keys_Compared_Ordinally()
	{
		// The fallback keys go through hstring operator<, not CompareStringEx: 'C' (U+0043) < 'b' (U+0062).
		Assert.IsTrue(ShapingHelpers.ValueComparer.Compare(new Person("C"), new Person("b")) < 0);
		Assert.IsTrue(ShapingHelpers.ValueComparer.Compare(new Person("b"), new Person("C")) > 0);
	}

	[TestMethod]
	public void When_Null_Keys_Then_Sorted_First()
	{
		Assert.AreEqual(0, ShapingHelpers.ValueComparer.Compare(null, null));
		Assert.IsTrue(ShapingHelpers.ValueComparer.Compare(null, 1) < 0);
		Assert.IsTrue(ShapingHelpers.ValueComparer.Compare(1, null) > 0);
		Assert.IsTrue(ShapingHelpers.ValueComparer.Compare(null, new Person("A")) < 0);
	}

	[TestMethod]
	public void When_Mixed_Numeric_Keys_Then_Compared_By_Value()
	{
		Assert.IsTrue(ShapingHelpers.ValueComparer.Compare(2, 2.5) < 0);
		Assert.IsTrue(ShapingHelpers.ValueComparer.Compare(2.5, 2) > 0);
		Assert.IsTrue(ShapingHelpers.ValueComparer.Compare(3L, 2.5) > 0);
		Assert.AreEqual(0, ShapingHelpers.ValueComparer.Compare(2, 2.0));
		Assert.IsTrue(ShapingHelpers.ValueComparer.Compare(-1, 1u) < 0);
		Assert.IsTrue(ShapingHelpers.ValueComparer.Compare(ulong.MaxValue, long.MaxValue) > 0);
	}

	[TestMethod]
	public void When_NaN_Keys_Then_Sorted_After_Numbers()
	{
		Assert.IsTrue(ShapingHelpers.ValueComparer.Compare(double.NaN, 1.0) > 0);
		Assert.IsTrue(ShapingHelpers.ValueComparer.Compare(1.0, double.NaN) < 0);
		Assert.AreEqual(0, ShapingHelpers.ValueComparer.Compare(double.NaN, double.NaN));
		Assert.IsTrue(ShapingHelpers.ValueComparer.Compare(double.NaN, int.MaxValue) > 0);
		Assert.IsTrue(ShapingHelpers.ValueComparer.Compare(float.NaN, 1.0f) > 0);
	}

	[TestMethod]
	public void When_Keys_Of_Different_Classes_Then_Ordered_By_Class_Rank()
	{
		// ValueClassRank: Numeric < String < Guid < Boolean < ... < Stringable.
		Assert.IsTrue(ShapingHelpers.ValueComparer.Compare(1, "a") < 0);
		Assert.IsTrue(ShapingHelpers.ValueComparer.Compare("a", 1) > 0);
		Assert.IsTrue(ShapingHelpers.ValueComparer.Compare("a", Guid.Empty) < 0);
		Assert.IsTrue(ShapingHelpers.ValueComparer.Compare(Guid.Empty, true) < 0);
		Assert.IsTrue(ShapingHelpers.ValueComparer.Compare("a", new Person("a")) < 0, "a property value ranks before a Stringable");
		Assert.IsTrue(ShapingHelpers.ValueComparer.Compare(new Person("a"), "a") > 0);
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

	private enum AppKind
	{
		First,
		Second,
	}

	[TestMethod]
	public void When_Non_WinRT_Value_Keys_Then_Fallback()
	{
		// The CsWinRT CCW of these values offers no IPropertyValue, so WinUI orders them by IStringable.
		Assert.IsTrue(ShapingHelpers.ValueComparer.UsesFallbackKey(1.5m));
		Assert.IsTrue(ShapingHelpers.ValueComparer.UsesFallbackKey(new DateTime(2026, 1, 1)));
		Assert.IsTrue(ShapingHelpers.ValueComparer.UsesFallbackKey((1, 2)));
		Assert.IsTrue(ShapingHelpers.ValueComparer.UsesFallbackKey(AppKind.Second));
	}

	[TestMethod]
	public void When_StableSortByKeys_On_Decimal_Keys_Then_Ordered_By_ToString()
	{
		var originalCulture = CultureInfo.CurrentCulture;
		try
		{
			CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
			List<object?> items = new() { 9.99m, 12.5m, 1m };

			ShapingHelpers.StableSortByKeys(items, 1, (item, _) => item, _ => SortDirection.Ascending);

			// "x:1" < "x:12.5" < "x:9.99", ordinal, not source order.
			CollectionAssert.AreEqual(new object[] { 1m, 12.5m, 9.99m }, items.ToArray());
		}
		finally
		{
			CultureInfo.CurrentCulture = originalCulture;
		}
	}
}
