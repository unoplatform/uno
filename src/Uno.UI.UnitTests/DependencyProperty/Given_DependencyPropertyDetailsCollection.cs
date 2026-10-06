#nullable enable

using System;
using System.Linq;
using System.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Tests;

[TestClass]
public partial class Given_DependencyPropertyDetailsCollection
{
	private const int PaddedPropertyCount = 256;

	// DependencyProperty.UniqueId comes from a process-wide counter, so pad well past it to get a property
	// whose id is large regardless of what the rest of the run registered first.
	private static readonly DependencyProperty _highIdProperty = RegisterPaddedProperties();

	private static DependencyProperty RegisterPaddedProperties()
	{
		DependencyProperty last = null!;

		for (var i = 0; i < PaddedPropertyCount; i++)
		{
			last = DependencyProperty.Register($"Pad{i}", typeof(int), typeof(PaddedObject), new PropertyMetadata(0));
		}

		return last;
	}

	[TestMethod]
	public void When_Property_Has_High_UniqueId_Then_Storage_Stays_Small()
	{
		PaddedObject SUT = new();
		SUT.SetValue(_highIdProperty, 42);

		// Storage must scale with the number of properties set, not with the property's global id.
		var entries = GetEntries(SUT);

		Assert.IsTrue(entries.Length <= 4, $"{entries.Length} slots allocated to hold a single property.");
	}

	[TestMethod]
	public void When_Border_Is_Created_Then_Storage_Fits_Its_Properties()
	{
		// A fresh Border sets two properties (hit-test visibility and DataContext) in its ctor.
		Border SUT = new();
		var entries = GetEntries(SUT);

		Assert.AreEqual(2, entries.Count(e => e is not null));
		Assert.IsTrue(entries.Length <= 4, $"{entries.Length} slots allocated to hold 2 properties.");
	}

	[TestMethod]
	public void When_Borders_Are_Created_Then_Allocation_Stays_Bounded()
	{
		const int Count = 1000;

		for (var i = 0; i < 100; i++)
		{
			_ = new Border();
		}

		var before = GC.GetAllocatedBytesForCurrentThread();

		for (var i = 0; i < Count; i++)
		{
			_ = new Border();
		}

		var perBorder = (GC.GetAllocatedBytesForCurrentThread() - before) / Count;

		// Generous ceiling, the point is catching property storage that is reallocated oversized on every element.
		Assert.IsTrue(perBorder < 3200, $"new Border() allocates {perBorder} bytes.");
	}

	[TestMethod]
	public void When_Unset_Property_Shares_A_Slot_Then_Default_Is_Returned()
	{
		PaddedObject SUT = new();

		// Every 16th property lands on the same slot of any table of up to 16 slots.
		for (var i = 0; i < PaddedPropertyCount; i += 32)
		{
			SUT.SetValue(GetPaddedProperty(i), i + 1);
		}

		for (var i = 16; i < PaddedPropertyCount; i += 32)
		{
			Assert.AreEqual(0, SUT.GetValue(GetPaddedProperty(i)));
		}

		for (var i = 0; i < PaddedPropertyCount; i += 32)
		{
			Assert.AreEqual(i + 1, SUT.GetValue(GetPaddedProperty(i)));
		}
	}

	[TestMethod]
	public void When_Every_Property_Is_Set_Then_All_Stay_Addressable()
	{
		PaddedObject SUT = new();

		for (var i = 0; i < PaddedPropertyCount; i++)
		{
			SUT.SetValue(GetPaddedProperty(i), i + 1);
		}

		for (var i = 0; i < PaddedPropertyCount; i++)
		{
			Assert.AreEqual(i + 1, SUT.GetValue(GetPaddedProperty(i)));
		}

		Assert.AreEqual(PaddedPropertyCount, GetEntries(SUT).Count(e => e is not null));
	}

	[TestMethod]
	public void When_Storage_Grows_Downwards_Then_Every_Property_Stays_Addressable()
	{
		PaddedObject SUT = new();

		// Ids 16 apart, so they collide in small tables and are rehashed on every growth.
		for (var i = PaddedPropertyCount - 1; i >= 0; i -= 16)
		{
			SUT.SetValue(GetPaddedProperty(i), i);
		}

		for (var i = PaddedPropertyCount - 1; i >= 0; i -= 16)
		{
			Assert.AreEqual(i, SUT.GetValue(GetPaddedProperty(i)));
		}
	}

	[TestMethod]
	public void When_Storage_Grows_Upwards_Then_Earlier_Properties_Survive()
	{
		PaddedObject SUT = new();

		// The opposite order, so earlier properties are carried over by each growth.
		for (var i = 0; i < PaddedPropertyCount; i += 16)
		{
			SUT.SetValue(GetPaddedProperty(i), i);
		}

		for (var i = 0; i < PaddedPropertyCount; i += 16)
		{
			Assert.AreEqual(i, SUT.GetValue(GetPaddedProperty(i)));
		}
	}

	private static DependencyProperty GetPaddedProperty(int index)
	{
		// Static field access, so the padded registrations above are guaranteed to have run.
		_ = _highIdProperty;

		return DependencyProperty.GetProperty(typeof(PaddedObject), $"Pad{index}")!;
	}

	private static object?[] GetEntries(DependencyObject o)
	{
		var properties = typeof(DependencyObject)
			.GetField("_properties", BindingFlags.Instance | BindingFlags.NonPublic)!
			.GetValue(o)!;

		return (object?[])properties.GetType()
			.GetField("_entries", BindingFlags.Instance | BindingFlags.NonPublic)!
			.GetValue(properties)!;
	}
}

public partial class PaddedObject : DependencyObject
{
}
