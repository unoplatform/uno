#nullable enable

using System;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Graphics;
using Windows.Graphics.Imaging;

namespace Uno.UI.Tests.Foundation;

/// <summary>
/// WinRT structs get memberwise equality from the CsWinRT projection (==, !=, Equals, GetHashCode, IEquatable&lt;T&gt;);
/// the WinAppSDK sync generator emits the same members for Uno.
/// </summary>
[TestClass]
public class Given_WinRTStructEquality
{
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24776")]
	public void When_SizeInt32()
	{
		SizeInt32 value = new(10, 20);

		Assert.IsTrue(value == new SizeInt32(10, 20));
		Assert.IsFalse(value != new SizeInt32(10, 20));
		Assert.IsTrue(value != new SizeInt32(11, 20));
		Assert.IsTrue(value != new SizeInt32(10, 21));
		AssertMemberwiseEquality(value, new SizeInt32(10, 20), new SizeInt32(11, 20), new SizeInt32(10, 21));
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24776")]
	public void When_PointInt32()
	{
		PointInt32 value = new(10, 20);

		Assert.IsTrue(value == new PointInt32(10, 20));
		Assert.IsFalse(value != new PointInt32(10, 20));
		Assert.IsTrue(value != new PointInt32(11, 20));
		Assert.IsTrue(value != new PointInt32(10, 21));
		AssertMemberwiseEquality(value, new PointInt32(10, 20), new PointInt32(11, 20), new PointInt32(10, 21));
	}

	[TestMethod]
	public void When_RectInt32()
	{
		RectInt32 value = new(1, 2, 3, 4);

		Assert.IsTrue(value == new RectInt32(1, 2, 3, 4));
		Assert.IsTrue(value != new RectInt32(1, 2, 3, 5));
		AssertMemberwiseEquality(value, new RectInt32(1, 2, 3, 4), new RectInt32(0, 2, 3, 4), new RectInt32(1, 0, 3, 4), new RectInt32(1, 2, 0, 4), new RectInt32(1, 2, 3, 0));
	}

	[TestMethod]
	public void When_DisplayAdapterId()
	{
		DisplayAdapterId value = new() { LowPart = 1, HighPart = 2 };

		Assert.IsTrue(value == new DisplayAdapterId { LowPart = 1, HighPart = 2 });
		Assert.IsTrue(value != new DisplayAdapterId { LowPart = 1, HighPart = 3 });
		AssertMemberwiseEquality(value, new DisplayAdapterId { LowPart = 1, HighPart = 2 }, new DisplayAdapterId { LowPart = 0, HighPart = 2 }, new DisplayAdapterId { LowPart = 1, HighPart = 0 });
	}

	[TestMethod]
	public void When_RawElementProviderRuntimeId()
	{
		RawElementProviderRuntimeId value = new() { Part1 = 1, Part2 = 2 };

		Assert.IsTrue(value == new RawElementProviderRuntimeId { Part1 = 1, Part2 = 2 });
		Assert.IsTrue(value != new RawElementProviderRuntimeId { Part1 = 1, Part2 = 3 });
		AssertMemberwiseEquality(value, new RawElementProviderRuntimeId { Part1 = 1, Part2 = 2 }, new RawElementProviderRuntimeId { Part1 = 0, Part2 = 2 }, new RawElementProviderRuntimeId { Part1 = 1, Part2 = 0 });
	}

	[TestMethod]
	public void When_LoadMoreItemsResult()
	{
		LoadMoreItemsResult value = new() { Count = 5 };

		Assert.IsTrue(value == new LoadMoreItemsResult { Count = 5 });
		Assert.IsTrue(value != new LoadMoreItemsResult { Count = 6 });
		AssertMemberwiseEquality(value, new LoadMoreItemsResult { Count = 5 }, new LoadMoreItemsResult { Count = 6 });
	}

	[TestMethod]
	public void When_TextRange()
	{
		TextRange value = new() { StartIndex = 1, Length = 2 };

		Assert.IsTrue(value == new TextRange { StartIndex = 1, Length = 2 });
		Assert.IsTrue(value != new TextRange { StartIndex = 1, Length = 3 });
		AssertMemberwiseEquality(value, new TextRange { StartIndex = 1, Length = 2 }, new TextRange { StartIndex = 0, Length = 2 }, new TextRange { StartIndex = 1, Length = 0 });
	}

#pragma warning disable UNO0001 // BitmapSize is a NotImplemented stub; its generated equality still works.
	[TestMethod]
	public void When_NotImplemented_Struct()
	{
		BitmapSize value = new() { Width = 1, Height = 2 };

		Assert.IsTrue(value == new BitmapSize { Width = 1, Height = 2 });
		Assert.IsTrue(value != new BitmapSize { Width = 1, Height = 3 });
		AssertMemberwiseEquality(value, new BitmapSize { Width = 1, Height = 2 }, new BitmapSize { Width = 0, Height = 2 }, new BitmapSize { Width = 1, Height = 0 });
	}
#pragma warning restore UNO0001

	private static void AssertMemberwiseEquality<T>(T value, T same, params T[] different)
		where T : struct, IEquatable<T>
	{
		Assert.IsTrue(value.Equals(same));
		Assert.IsTrue(value.Equals((object)same));
		Assert.AreEqual(value.GetHashCode(), same.GetHashCode());
		Assert.IsFalse(value.Equals(null));
		Assert.IsFalse(value.Equals(new object()));

		foreach (var other in different)
		{
			Assert.IsFalse(value.Equals(other), $"{typeof(T).Name} should differ from {other}");
			Assert.IsFalse(value.Equals((object)other));
		}
	}
}
