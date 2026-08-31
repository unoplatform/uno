#nullable enable

using System;
using System.Numerics;
using System.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Tests;

[TestClass]
public partial class Given_DependencyPropertyDetailsCollection
{
	private const int PaddedPropertyCount = 256;

	// DependencyProperty.UniqueId comes from a process-wide counter, so the bucket index of any given
	// property depends on what the rest of the run registered first. Pad it well past the array pool's
	// 16-element minimum bucket, otherwise the sizing assertion below holds no matter what is rented.
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
	public void When_Property_Has_High_UniqueId_Then_Offsets_Are_Not_Over_Rented()
	{
		PaddedObject SUT = new();
		SUT.SetValue(_highIdProperty, 42);

		var offsets = GetEntryOffsets(SUT);

		// The offsets array is indexed by the bucket index alone, so it only ever needs bucketIndex + 1 slots.
		var bucketIndex = _highIdProperty.UniqueId >> 4;
		var needed = bucketIndex + 1;

		// ArrayPool rounds a request up to a power-of-two bucket (16 minimum) and may satisfy it from one
		// bucket above that, hence the doubling.
		var tolerated = Math.Max(16, (int)BitOperations.RoundUpToPowerOf2((uint)needed)) * 2;

		Assert.IsTrue(
			offsets.Length <= tolerated,
			$"Offsets array is over-rented: {offsets.Length} slots for bucket index {bucketIndex} "
			+ $"(needs {needed}, tolerated {tolerated}) = {offsets.Length * sizeof(short)} bytes per DependencyObject.");
	}

	[TestMethod]
	public void When_Offsets_Grow_Then_Every_Covered_Bucket_Stays_Addressable()
	{
		PaddedObject SUT = new();

		// Grow from the highest bucket downwards, so the array is sized once from the top and then written
		// at every lower index it claims to cover.
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
	public void When_Offsets_Grow_Upwards_Then_Earlier_Buckets_Survive()
	{
		PaddedObject SUT = new();

		// The opposite order: each step resizes and copies the previous offsets forward.
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

	private static short[] GetEntryOffsets(DependencyObject o)
	{
		var properties = typeof(DependencyObject)
			.GetField("_properties", BindingFlags.Instance | BindingFlags.NonPublic)!
			.GetValue(o)!;

		return (short[])properties.GetType()
			.GetField("_entryOffsets", BindingFlags.Instance | BindingFlags.NonPublic)!
			.GetValue(properties)!;
	}
}

public partial class PaddedObject : DependencyObject
{
}
