#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Tests.DependencyPropertyTests;

[TestClass]
public class Given_DependencyPropertyDetailsCollection
{
	private static readonly FieldInfo _storeProperties = typeof(DependencyObjectStore).GetField("_properties", BindingFlags.Instance | BindingFlags.NonPublic)!;
	private static readonly FieldInfo _entryOffsets = typeof(DependencyPropertyDetailsCollection).GetField("_entryOffsets", BindingFlags.Instance | BindingFlags.NonPublic)!;

	[TestMethod]
	public void When_DependencyObjects_Collected_Then_Storage_Arrays_Collected()
	{
		var (owners, arrays) = CreateAndDropColumnDefinitions(100);

		CollectAll();

		Assert.AreEqual(0, owners.Count(o => o.IsAlive), "The DependencyObjects should have been collected");
		// An array that outlives its owner is held by a static pool.
		Assert.AreEqual(0, arrays.Count(a => a.IsAlive), $"Storage arrays of collected DependencyObjects still alive (out of {arrays.Count})");
	}

	[TestMethod]
	public void When_Store_Disposed_Then_Storage_Arrays_Not_Returned_To_Pools()
	{
		var sut = new ColumnDefinition { MinWidth = 10, MaxWidth = 100 };

		var arrays = DisposeStore(sut);

		CollectAll();

		Assert.AreEqual(0, arrays.Count(a => a.IsAlive), "Storage arrays of a disposed store still alive");
		GC.KeepAlive(sut);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static (List<WeakReference> Owners, List<WeakReference> Arrays) CreateAndDropColumnDefinitions(int count)
	{
		var owners = new List<WeakReference>();
		var arrays = new List<WeakReference>();

		for (var i = 0; i < count; i++)
		{
			var owner = new ColumnDefinition { MinWidth = 10, MaxWidth = 100 };

			owners.Add(new WeakReference(owner));
			arrays.AddRange(GetStorageArrays(owner));
		}

		return (owners, arrays);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static List<WeakReference> DisposeStore(DependencyObject owner)
	{
		var arrays = GetStorageArrays(owner);

		((IDependencyObjectStoreProvider)owner).Store.Dispose();

		return arrays;
	}

	private static List<WeakReference> GetStorageArrays(DependencyObject owner)
	{
		var properties = GetProperties(owner);
		var entries = properties.GetAllDetails();
		var offsets = (short[]?)_entryOffsets.GetValue(properties);

		Assert.AreNotEqual(0, entries.Length, "The test object should have rented an entries array");
		Assert.IsNotNull(offsets, "The test object should have rented an offsets array");

		// Tracking resurrection, as the store's finalizer is what used to hand the arrays to the pools.
		return [new WeakReference(entries, trackResurrection: true), new WeakReference(offsets, trackResurrection: true)];
	}

	private static DependencyPropertyDetailsCollection GetProperties(DependencyObject owner)
		=> (DependencyPropertyDetailsCollection)_storeProperties.GetValue(((IDependencyObjectStoreProvider)owner).Store)!;

	private static void CollectAll()
	{
		for (var i = 0; i < 3; i++)
		{
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}

		GC.Collect();
	}
}
