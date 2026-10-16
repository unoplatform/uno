#nullable enable

using System;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Tests;

[UITestClass]
public partial class Given_DependencyPropertyDetailsCollection
{
	private const int PaddedPropertyCount = 256;

	// DependencyProperty.UniqueId comes from a process-wide counter, so pad well past it to get a property
	// whose id is large regardless of what the rest of the run registered first.
	private static readonly DependencyProperty _highIdProperty = RegisterPaddedProperties();

	private static readonly DependencyProperty[] _attachedProperties = Enumerable.Range(0, 64)
		.Select(i => DependencyProperty.RegisterAttached($"Attached{i}", typeof(int), typeof(PaddedObject), new PropertyMetadata(0)))
		.ToArray();

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
		// How many properties a bare Border sets in its ctor varies, so set one explicitly.
		Border SUT = new() { Name = "b" };
		var entries = GetEntries(SUT);
		var count = entries.Count(e => e is not null);
		var maxSlots = Math.Max(4, (int)BitOperations.RoundUpToPowerOf2((uint)(count * 2)));

		Assert.IsTrue(count >= 1, "Name should be stored.");
		Assert.IsTrue(entries.Length <= maxSlots, $"{entries.Length} slots allocated to hold {count} properties.");
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

	[TestMethod]
	public void When_Property_Is_Registered_Far_From_The_Others_Then_Storage_Costs_The_Same()
	{
		// Each object sets First plus one other property, registered just before it, just after it, or far away.
		// One of the two neighbors always shares an id-aligned group with First, whatever ids the run assigned.
		var candidates = new[] { OrderObject.PreviousProperty, OrderObject.NextProperty, OrderObject.FarProperty };

		// Warm every case up first, so one-time costs (JIT, static and callback caches) stay out of the measurement.
		foreach (var candidate in candidates)
		{
			CreateOrderObject(candidate);
		}

		var previous = MeasureStorage(OrderObject.PreviousProperty);
		var next = MeasureStorage(OrderObject.NextProperty);
		var far = MeasureStorage(OrderObject.FarProperty);

		Assert.AreEqual(previous, far, "Storage depends on registration order.");
		Assert.AreEqual(next, far, "Storage depends on registration order.");
	}

	[TestMethod]
	public void When_Cloned_For_Hot_Reload_Then_Every_Local_Value_Is_Carried_Over()
	{
		PaddedObject source = new();
		PaddedObject target = new();

		for (var i = 0; i < PaddedPropertyCount; i += 7)
		{
			source.SetValue(GetPaddedProperty(i), i + 1);
		}

		source.ClonePropertiesToAnotherStoreForHotReload(target);

		for (var i = 0; i < PaddedPropertyCount; i++)
		{
			Assert.AreEqual(i % 7 == 0 ? i + 1 : 0, target.GetValue(GetPaddedProperty(i)));
		}
	}

	[TestMethod]
	public void When_Values_Are_Cleared_Then_The_Others_Stay_Addressable()
	{
		const int Count = 64;
		PaddedObject SUT = new();

		for (var i = 0; i < Count; i++)
		{
			SUT.SetValue(GetPaddedProperty(i), i + 1);
		}

		for (var i = 0; i < Count; i += 2)
		{
			SUT.ClearValue(GetPaddedProperty(i));
		}

		for (var i = 0; i < Count; i++)
		{
			Assert.AreEqual(i % 2 == 0 ? 0 : i + 1, SUT.GetValue(GetPaddedProperty(i)));
		}

		for (var i = 0; i < Count; i += 2)
		{
			SUT.SetValue(GetPaddedProperty(i), -i);
		}

		for (var i = 0; i < Count; i++)
		{
			Assert.AreEqual(i % 2 == 0 ? -i : i + 1, SUT.GetValue(GetPaddedProperty(i)));
		}

		// Clearing keeps the details (and their callbacks); it must not leave duplicates behind on re-set.
		Assert.AreEqual(Count, GetEntries(SUT).Count(e => e is not null));
	}

	[TestMethod]
	public void When_DataContext_Shares_Storage_With_Many_Properties_Then_It_Stays_Cached()
	{
		Border SUT = new();

		foreach (var attached in _attachedProperties)
		{
			SUT.SetValue(attached, 1);
		}

		var dataContext = new object();
		SUT.DataContext = dataContext;

		var collection = GetCollection(SUT);

		Assert.AreSame(dataContext, SUT.DataContext);
		Assert.AreSame(collection.FindPropertyDetails(FrameworkElement.DataContextProperty), collection.DataContextPropertyDetails);
	}

	[TestMethod]
	public void When_Properties_Are_Set_Then_GetAllDetails_Lists_Each_Once()
	{
		PaddedObject SUT = new();
		var expected = Enumerable.Range(0, PaddedPropertyCount / 5).Select(i => GetPaddedProperty(i * 5)).ToList();

		foreach (var property in expected)
		{
			SUT.SetValue(property, 1);
		}

		var listed = GetCollection(SUT).GetAllDetails().Where(d => d is not null).Select(d => d!.Property).ToList();

		CollectionAssert.AllItemsAreUnique(listed);
		CollectionAssert.AreEquivalent(expected, listed);
	}

	private static (int Slots, long Bytes) MeasureStorage(DependencyProperty other)
	{
		var bytes = long.MaxValue;
		OrderObject o = null!;

		for (var i = 0; i < 3; i++)
		{
			var before = GC.GetAllocatedBytesForCurrentThread();
			o = CreateOrderObject(other);
			bytes = Math.Min(bytes, GC.GetAllocatedBytesForCurrentThread() - before);
		}

		return (GetEntries(o).Length, bytes);
	}

	private static OrderObject CreateOrderObject(DependencyProperty other)
	{
		OrderObject o = new();
		o.SetValue(OrderObject.FirstProperty, "a");
		o.SetValue(other, "b");
		return o;
	}

	private static DependencyPropertyDetailsCollection GetCollection(DependencyObject o)
		=> (DependencyPropertyDetailsCollection)typeof(DependencyObject)
			.GetField("_properties", BindingFlags.Instance | BindingFlags.NonPublic)!
			.GetValue(o)!;

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

public partial class OrderObject : DependencyObject
{
	public static DependencyProperty PreviousProperty { get; } = Register("Prev");
	public static DependencyProperty FirstProperty { get; } = Register("First");
	public static DependencyProperty NextProperty { get; } = Register("Next");

	// Registered after a run of unrelated properties, so its UniqueId is far from the others. Names are
	// of equal length, since the SetValue path also allocates by property name length in this test host.
	public static DependencyProperty FarProperty { get; } = RegisterAfterPadding("Away");

	private static DependencyProperty Register(string name)
		=> DependencyProperty.Register(name, typeof(string), typeof(OrderObject), new PropertyMetadata(null));

	private static DependencyProperty RegisterAfterPadding(string name)
	{
		for (var i = 0; i < 64; i++)
		{
			Register($"OrderPad{i}");
		}

		return Register(name);
	}
}
