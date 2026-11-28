#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Storage.Pickers.Internal;

namespace Uno.UI.Tests.Windows_Storage;

/// <summary>
/// Tests for <see cref="PhotoPickerRepresentation"/>, the pure part of copying what is picked with the iOS photo picker
/// (the item providers it returns need a photo library and a user).
/// </summary>
[UITestClass]
public class Given_PhotoPickerRepresentation
{
	private const string Jpeg = "public.jpeg";
	private const string Heic = "public.heic";
	private const string Png = "public.png";
	private const string Gif = "com.compuserve.gif";
	private const string QuickTimeMovie = "com.apple.quicktime-movie";
	private const string Mpeg4Movie = "public.mpeg-4";
	private const string LivePhotoBundle = "com.apple.live-photo-bundle";
	private const string MailMovie = "com.apple.private.photos.mail-movie-export";
	private const string Thumbnail = "com.apple.private.photos.thumbnail.standard";

	// The types as iOS 26 and 27 declare them, and the item providers of their simulators register them.
	private static readonly Dictionary<string, PhotoPickerType> _types = new()
	{
		[Jpeg] = new(["jpeg", "jpg", "jpe"], ConformsToData: true),
		[Heic] = new(["heic"], ConformsToData: true),
		[Png] = new(["png"], ConformsToData: true),
		[Gif] = new(["gif"], ConformsToData: true),
		[QuickTimeMovie] = new(["mov", "qt"], ConformsToData: true),
		[Mpeg4Movie] = new(["mp4", "mpg4"], ConformsToData: true),
		[LivePhotoBundle] = new(["pvt"], ConformsToData: false),
		[MailMovie] = new(["mov"], ConformsToData: true),
		[Thumbnail] = new(["thumb.standard"], ConformsToData: false),
	};

	private static readonly string[] _imageFilter = [".jpg", ".jpeg", ".png", ".bmp"];

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25889")]
	public void When_Heic_Not_Allowed_Then_Jpeg()
	{
		AssertChosen([Heic, Jpeg, Thumbnail], _imageFilter, Jpeg, "jpeg");
		AssertChosen([Jpeg, Heic, Thumbnail], _imageFilter, Jpeg, "jpeg");
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25889")]
	public void When_Heic_Allowed_Then_Heic()
	{
		AssertChosen([Jpeg, Heic, Thumbnail], [".heic"], Heic, "heic");
		AssertChosen([Heic, Jpeg, Thumbnail], [".heic"], Heic, "heic");
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25889")]
	public void When_Filter_Has_Other_Extension_Of_Type_Then_That_Extension()
	{
		AssertChosen([Heic, Jpeg, Thumbnail], [".jpg"], Jpeg, "jpg");
		AssertChosen([Heic, Jpeg, Thumbnail], [".JPG"], Jpeg, "jpg");
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25889")]
	public void When_Filter_Allows_Several_Then_First_Registered()
	{
		AssertChosen([Heic, Jpeg, Thumbnail], [".jpg", ".heic"], Heic, "heic");
		AssertChosen([Jpeg, Heic, Thumbnail], [".jpg", ".heic"], Jpeg, "jpg");
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25889")]
	public void When_Type_Not_Allowed_And_No_Allowed_Version_Then_None()
	{
		AssertNone([Gif, Thumbnail], _imageFilter);
		AssertNone([Png, Thumbnail], [".jpg"]);
		AssertNone([Heic, Jpeg, Thumbnail], [".mov"]);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25889")]
	public void When_Video_Container_Not_Allowed_Then_Not_Mail_Copy()
	{
		AssertNone([Mpeg4Movie, MailMovie, Thumbnail], [".mov"]);
		AssertNone([QuickTimeMovie, MailMovie, Thumbnail], [".mp4"]);
		AssertChosen([Mpeg4Movie, MailMovie, Thumbnail], [".mov", ".mp4"], Mpeg4Movie, "mp4");
		AssertChosen([QuickTimeMovie, MailMovie, Thumbnail], [".mov"], QuickTimeMovie, "mov");
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25889")]
	public void When_Live_Photo_Then_Jpeg_Still_If_Allowed()
	{
		AssertChosen([LivePhotoBundle, Heic, Jpeg, Thumbnail], ["*"], Jpeg, "jpeg");
		AssertChosen([LivePhotoBundle, Heic, Jpeg, Thumbnail], [".heic", ".jpg"], Jpeg, "jpg");
		AssertChosen([LivePhotoBundle, Heic, Jpeg, Thumbnail], [".heic"], Heic, "heic");
		AssertChosen([LivePhotoBundle, Heic, Thumbnail], ["*"], Heic, "heic");
		AssertNone([LivePhotoBundle, Jpeg, Heic, Thumbnail], [".pvt"]);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25889")]
	public void When_All_Allowed_Then_Same_Type_As_Without_Filtering()
	{
		AssertChosen([Heic, Jpeg, Thumbnail], ["*"], Heic, "heic");
		AssertChosen([Mpeg4Movie, QuickTimeMovie, Thumbnail], ["*"], QuickTimeMovie, "mov");
		AssertChosen([Mpeg4Movie, QuickTimeMovie, Thumbnail], [".mp4"], Mpeg4Movie, "mp4");
		AssertChosen([Gif, Thumbnail], [".png", "*"], Gif, "gif");
		AssertNone([LivePhotoBundle, Thumbnail], ["*"]);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25889")]
	public void When_Type_Unknown_Then_Skipped()
		=> AssertChosen(["com.example.unknown", Jpeg], ["*"], Jpeg, "jpeg");

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25889")]
	public void When_Type_Has_No_Extension_And_All_Allowed_Then_No_Extension()
	{
		var chosen = PhotoPickerRepresentation.Choose(["com.example.data"], _ => new([], ConformsToData: true), ["*"]);

		Assert.AreEqual(new PhotoPickerRepresentation("com.example.data", null), chosen);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25889")]
	public void When_Private_Types_Then_Not_Looked_Up()
	{
		var lookedUp = new List<string>();
		PhotoPickerType? LookUp(string identifier)
		{
			lookedUp.Add(identifier);
			return _types[identifier];
		}

		PhotoPickerRepresentation.Choose([Gif, MailMovie, Thumbnail], LookUp, _imageFilter);

		CollectionAssert.AreEqual(new[] { Gif }, lookedUp);
	}

	[TestMethod]
	[DataRow("IMG_0001", "jpeg", "IMG_0001.jpeg")]
	[DataRow("IMG_0001", "jpg", "IMG_0001.jpg")]
	[DataRow("IMG_0001.JPG", "jpg", "IMG_0001.jpg")]
	[DataRow("IMG_0001.heic", "jpeg", "IMG_0001.heic.jpeg")]
	[DataRow("Trip 2025.Day 1", "mov", "Trip 2025.Day 1.mov")]
	[DataRow("Photo", null, "Photo")]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25889")]
	public void When_Suggested_Name_Then_Name_With_Extension(string suggestedName, string? extension, string expected)
		=> Assert.AreEqual(expected, new PhotoPickerRepresentation("public.data", extension).GetFileName(suggestedName, "Fallback"));

	[TestMethod]
	[DataRow((string?)null)]
	[DataRow("")]
	[DataRow("  ")]
	[DataRow("..")]
	[DataRow(". .")]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25889")]
	public void When_No_Suggested_Name_Then_Fallback(string? suggestedName)
		=> Assert.AreEqual("Fallback.png", new PhotoPickerRepresentation(Png, "png").GetFileName(suggestedName, "Fallback"));

	[TestMethod]
	[DataRow("Front/Back", "Front_Back.jpeg")]
	[DataRow("10:30", "10_30.jpeg")]
	[DataRow("Line\nBreak", "Line_Break.jpeg")]
	[DataRow(".hidden", "hidden.jpeg")]
	[DataRow(". .hidden", "hidden.jpeg")]
	[DataRow(" .\u3000.x", "x.jpeg")]
	[DataRow(" Padded ", "Padded.jpeg")]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25889")]
	public void When_Suggested_Name_Not_Usable_As_Is_Then_Cleaned(string suggestedName, string expected)
		=> Assert.AreEqual(expected, new PhotoPickerRepresentation(Jpeg, "jpeg").GetFileName(suggestedName, "Fallback"));

	// Attribute arguments can't hold lone surrogates, so these are in code.
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25889")]
	public void When_Suggested_Name_Has_Lone_Surrogate_Then_Replaced()
	{
		var representation = new PhotoPickerRepresentation(Jpeg, "jpeg");

		Assert.AreEqual("_abc.jpeg", representation.GetFileName("\uD800abc", "Fallback"));
		Assert.AreEqual("abc_.jpeg", representation.GetFileName("abc\uDC00", "Fallback"));
		Assert.AreEqual("a_b.jpeg", representation.GetFileName("a\uDC00b", "Fallback"));
	}

	// The file system limits the decomposed form (NFD) of a name to 255 bytes: an accented letter decomposes to 3 bytes and a Hangul
	// syllable to 6 or 9. Names can also come decomposed already.
	[TestMethod]
	[DataRow("a", 300, 250)]
	[DataRow("\u00E9", 200, null)]
	[DataRow("\uD55C", 30, 27)]
	[DataRow("\u1112\u1161\u11AB", 30, 27)]
	[DataRow("\u4E2D", 100, 83)]
	[DataRow("\U0001F600", 100, null)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25889")]
	public void When_Suggested_Name_Too_Long_Then_Within_255_Bytes_Decomposed(string unit, int count, int? expectedUnits)
	{
		var name = new PhotoPickerRepresentation(Jpeg, "jpeg").GetFileName(string.Concat(Enumerable.Repeat(unit, count)), "Fallback");

		var decomposedBytes = Encoding.UTF8.GetByteCount(name.Normalize(NormalizationForm.FormD));
		Assert.IsTrue(decomposedBytes <= 255, $"{decomposedBytes} bytes decomposed");
		StringAssert.EndsWith(name, unit + ".jpeg", StringComparison.Ordinal);

		if (expectedUnits is { } units)
		{
			Assert.AreEqual(string.Concat(Enumerable.Repeat(unit, units)) + ".jpeg", name);
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25889")]
	public void When_Truncated_Then_Text_Elements_Kept_Whole()
	{
		var accented = "e\u0301";
		var family = "\U0001F468\u200D\U0001F469\u200D\U0001F467\u200D\U0001F466";

		foreach (var (input, element) in new[]
		{
			(string.Concat(Enumerable.Repeat(accented, 150)), accented),
			("abc" + string.Concat(Enumerable.Repeat(family, 20)), family),
		})
		{
			var name = new PhotoPickerRepresentation(Jpeg, "jpeg").GetFileName(input, "Fallback");
			var stem = name[..^".jpeg".Length];

			Assert.AreNotEqual(input, stem, "The name wasn't truncated.");
			Assert.AreEqual(element, stem[StringInfo.ParseCombiningCharacters(stem).Last()..], "The last text element was split.");
		}
	}

	private static void AssertChosen(string[] registered, string[] filter, string expectedIdentifier, string? expectedExtension)
		=> Assert.AreEqual(
			new PhotoPickerRepresentation(expectedIdentifier, expectedExtension),
			PhotoPickerRepresentation.Choose(registered, Describe, filter));

	private static void AssertNone(string[] registered, string[] filter)
		=> Assert.IsNull(PhotoPickerRepresentation.Choose(registered, Describe, filter), "A type the filter doesn't allow was chosen.");

	private static PhotoPickerType? Describe(string identifier) => _types.GetValueOrDefault(identifier);
}
