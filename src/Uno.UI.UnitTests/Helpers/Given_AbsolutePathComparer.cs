#nullable enable
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Helpers;

#pragma warning disable IDE0055 // Fix formatting
namespace Uno.UI.Tests.Helpers;

[TestClass]
public class Given_AbsolutePathComparer
{
	[TestMethod]
	// (path1, path2, expected equality)
	[DataRow(@"C:\folder", @"C:\folder", true, DisplayName = "Same path")]
	[DataRow(@"C:\FOLDER", @"c:\folder", true, DisplayName = "Same path, different case")]
	[DataRow(@"C:\FOLDER\sub-folder\file.ABC", @"c:/folder/sub-folder/file.abc", true, DisplayName = "Same path, different separator + case")]
	[DataRow(@"C:\folder1", @"C:\folder2", false, DisplayName = "Different path")]
	[DataRow(@"C:\folder", @"D:\folder", false, DisplayName = "Different drive")]
	[DataRow(@"C:\folder", @"C:\folder\subfolder", false, DisplayName = "Different path length")]
	[DataRow(@"C:folder", @"C:folder", false, DisplayName = "Invalid path (no slash)")]
	[DataRow(@".\something", @"something", false, DisplayName = "Relative path (invalid)")]
	[DataRow(@"\\server\share", @"\\server\share", true, DisplayName = "UNC path")]
	[DataRow(@"\\server\share", @"/server/share", false, DisplayName = "UNC vs absolute path")]
	[DataRow(@"\\SERVER\share", @"\\server\Share\sub1\sub2\..\..", true, DisplayName = "UNC path, different case, with 2x'..'")]
	[DataRow(@"\\server\share\folder", @"\\server\share\Folder", true, DisplayName = "UNC path, folder case difference")]
	[DataRow(@"\\server\share\folder\..\sub", @"\\server\share\sub", true, DisplayName = "UNC path with '..' (parent folder)")]
	[DataRow(@"/usr/local/bin", @"/usr/local/bin", true, DisplayName = "Unix path")]
	[DataRow(@"/usr/local/bin", @"/usr/LOCAL/bin", true, DisplayName = "Unix path, different case")]
	[DataRow(@"/usr/local/bin", @"/usr/local/lib", false, DisplayName = "Unix path, different")]
	public void GivenCaseInsensitive_WhenEqualsIsCalled_ThenItReturnsExpectedResult(string? path1, string? path2, bool expected)
	{
		// Arrange
		var comparer = AbsolutePathComparer.ComparerIgnoreCase; // or new AbsolutePathComparer(true);

		// Act
		var result = comparer.Equals(path1, path2);

		// Assert
		Assert.AreEqual(expected, result);
	}

	[TestMethod]
	[DataRow(null, null, false, DisplayName = "null vs null => false")]
	[DataRow(null, @"C:\folder", false, DisplayName = "null vs path => false")]
	[DataRow(@"C:\folder", null, false, DisplayName = "path vs null => false")]
	[DataRow("", "", false, DisplayName = "empty => not fully qualified => false")]
	[DataRow("/", "/", false, DisplayName = "Unix root => 0 segment => false")]
	[DataRow("/../../..", "/../../..", false, DisplayName = "Invalid backtracking => false")]
	[DataRow(@"C:\folder\", @"C:\folder", true, DisplayName = "ending slash => true")]
	[DataRow(@"C:\folder\\sub", @"C:\folder\sub", true, DisplayName = "duplicate slashes => true")]
	[DataRow(@"C:\folder\.", @"C:\folder", true, DisplayName = "'.' ignored => true")]
	[DataRow(@"C:\.\.\.\./././folder\.", @"C:\folder/././", true, DisplayName = "multiple '.' ignored => true")]
	[DataRow(@"C:\folder\sub\..", @"C:\folder", true, DisplayName = "'..' => true")]
	[DataRow(@"C:\folder\sub\..\..", @"C:\", false, DisplayName = "2x '..' => 0 segments => false")]
	[DataRow(@"C:\folder", @"c:\folder", true, DisplayName = "Different case => true")]
	public void GivenCaseInsensitive_WhenEqualsIsCalled_WithEdgeCases_ThenItReturnsExpectedResult(string? path1, string? path2, bool expected)
	{
		// Arrange
		var comparer = AbsolutePathComparer.ComparerIgnoreCase;

		// Act
		var result = comparer.Equals(path1, path2);

		// Assert
		Assert.AreEqual(expected, result, $"Expected {expected} for '{path1}' and '{path2}', but got {result}.");
	}

	[TestMethod]
	// (path1, path2, expected equality)
	[DataRow(@"C:\folder", @"C:\folder", true, DisplayName = "Same path (case-sensitive)")]
	[DataRow(@"C:\folder", @"c:\folder", true, DisplayName = "Same path, different case for drive letter => true")]
	[DataRow(@"C:\folder", @"C:\FOLDER", false, DisplayName = "Case mismatch => not equal")]
	[DataRow(@"/usr/local/bin", @"/usr/local/bin", true, DisplayName = "Same Unix path (case-sensitive)")]
	[DataRow(@"/usr/local/bin", @"/usr/LOCAL/bin", false, DisplayName = "Case mismatch (Unix)")]
	[DataRow(@"C:folder", @"C:folder", false, DisplayName = "Invalid path, no slash")]
	public void GivenCaseSensitive_WhenEqualsIsCalled_ThenItReturnsExpectedResult(string? path1, string? path2, bool expected)
	{
		// Arrange
		var comparer = AbsolutePathComparer.Comparer; // or new AbsolutePathComparer(false);

		// Act
		var result = comparer.Equals(path1, path2);

		// Assert
		Assert.AreEqual(expected, result);
	}

	[TestMethod]
	[DataRow(@"C:\folder", @"C:\folder", true, DisplayName = "Same path (case-sensitive)")]
	[DataRow(@"C:\folder", @"c:\folder", true, DisplayName = "Same path, different case => true")]
	[DataRow(@"C:\folder", @"c:\FOLDER", true, DisplayName = "Same path, different case => true")]
	[DataRow(@"C:\folder1", @"C:\folder2", false, DisplayName = "Different path")]
	[DataRow(null, @"C:\folder2", false, DisplayName = "null vs path => false")]
	[DataRow(@"C:\folder1", null, false, DisplayName = "path vs null => false")]
	public void GivenAPathCaseInsensitive_WhenGetHashCodeIsCalled_ThenItIsConsistentWithEquals(string? path1, string? path2, bool expectSameHash)
	{
		// Arrange
		var comparer = AbsolutePathComparer.ComparerIgnoreCase; // or new AbsolutePathComparer(true);

		// Act
		var hash1 = comparer.GetHashCode(path1);
		var hash2 = comparer.GetHashCode(path2);
		var sameHash = (hash1 == hash2);

		// Assert
		if (expectSameHash)
		{
			Assert.IsTrue(sameHash, $"Expected same hash code for '{path1}' and '{path2}', but got {hash1} and {hash2}.");
		}
		else
		{
			Assert.IsFalse(sameHash, $"Expected different hash code for '{path1}' and '{path2}', but both got {hash1}.");
		}
	}

	[TestMethod]
	[DataRow(@"C:\folder", @"C:\folder", true, DisplayName = "Same path (case-sensitive)")]
	[DataRow(@"C:\folder", @"C:\FOLDER", false, DisplayName = "Case mismatch => not equal")]
	[DataRow(@"/usr/local/bin", @"/usr/local/bin", true, DisplayName = "Same Unix path (case-sensitive)")]
	[DataRow(@"/usr/local/bin", @"/usr/LOCAL/bin", false, DisplayName = "Case mismatch (Unix)")]
	public void GivenAPathCaseSensitive_WhenGetHashCodeIsCalled_ThenItIsConsistentWithEquals(string? path1, string? path2, bool expectSameHash)
	{
		// Arrange
		var comparer = AbsolutePathComparer.Comparer; // or new AbsolutePathComparer(false);

		// Act
		var hash1 = comparer.GetHashCode(path1);
		var hash2 = comparer.GetHashCode(path2);
		var sameHash = (hash1 == hash2);

		// Assert
		if (expectSameHash)
		{
			Assert.IsTrue(sameHash,	$"Expected same hash code for '{path1}' and '{path2}', but got {hash1} and {hash2}.");
		}
		else
		{
			Assert.IsFalse(sameHash, $"Expected different hash code for '{path1}' and '{path2}', but both got {hash1}.");
		}
	}

	[TestMethod]
	// (path1, path2, expectedEquals)
	// Both are UNC
	[DataRow(@"\\server\share",    @"\\server\share",   true,  DisplayName = "UNC vs UNC (identical) => true")]
	[DataRow(@"\\Server\Share",    @"\\server\share",   true,  DisplayName = "UNC vs UNC (case diff) => true (ignoreCase)")]
	[DataRow(@"\\server\Share\..", @"\\server\",        true,  DisplayName = "UNC path with '..' => same final segments => true")]
	[DataRow(@"\\server\share",    @"/server/share",    false, DisplayName = "UNC vs Unix => false")]

	// Both are Windows
	[DataRow(@"C:\folder",         @"C:\folder",        true,  DisplayName = "Windows vs Windows => true")]
	[DataRow(@"C:\FOLDER",         @"c:\folder",        true,  DisplayName = "Windows drive letter / segment case => true (ignoreCase)")]
	[DataRow(@"C:\folder\..",      @"C:\",              false, DisplayName = "Windows path with '..' => 0 segments => false (code requires >= 1)")]

	// Both are Unix
	[DataRow(@"/usr/bin",          @"/usr/bin",         true,  DisplayName = "Unix vs Unix => true")]
	[DataRow(@"/usr/BIN",          @"/usr/bin",         true,  DisplayName = "Unix vs Unix (case diff) => true (ignoreCase)")]
	[DataRow(@"/usr/bin/..",       @"/usr",             true, DisplayName = "Unix path with '..' => true")]

	// Relative checks
	[DataRow(@"folder\sub",       @"folder\sub",       false, DisplayName = "Relative vs Relative => false (no segments)")]
	[DataRow(@"C:folder",         @"C:\folder",        false, DisplayName = "Relative (C:folder) vs Windows (C:\\folder) => false")]
	[DataRow(@".\folder",         @"..\folder",        false, DisplayName = "Both relative => still false (code doesn't handle them as fully qualified)")]

	public void WhenComparingPaths_CaseInsensitive_ThenResultIsAsExpected(string? path1, string? path2, bool expectedEquals)
	{
		// Arrange
		var comparer = AbsolutePathComparer.ComparerIgnoreCase;

		// Act
		var result = comparer.Equals(path1, path2);

		// Assert
		Assert.AreEqual(expectedEquals, result,"Case-insensitive: '{path1}' vs '{path2}' should be {expectedEquals}.");
	}

	[TestMethod]
	// (path1, path2, expectedEquals)
	// UNC
	[DataRow(@"\\server\share",  @"\\server\share",   true,  DisplayName = "UNC vs UNC => true")]
	[DataRow(@"\\Server\Share",  @"\\server\share",   false, DisplayName = "UNC vs UNC => false if case is different in segments")]
	[DataRow(@"\\server\share",  @"/server/share",    false, DisplayName = "UNC vs Unix => false")]

	// Windows
	[DataRow(@"C:\folder",       @"C:\folder",        true,  DisplayName = "Windows vs Windows (exact case) => true")]
	[DataRow(@"C:\folder",       @"C:\FOLDER",        false, DisplayName = "Windows vs Windows (case diff) => false")]
	[DataRow(@"C:\folder\..",    @"C:\",              false, DisplayName = "Windows '..' => drive root => false")]

	// Unix
	[DataRow(@"/usr/bin",        @"/usr/bin",         true,  DisplayName = "Unix vs Unix => true")]
	[DataRow(@"/usr/BIN",        @"/usr/bin",         false, DisplayName = "Unix vs Unix (case diff) => false")]
	[DataRow(@"/usr/bin/..",     @"/usr",             true,  DisplayName = "Unix '..' => 1 segment => true")]
	[DataRow(@"/usr/..",         @"/",                false, DisplayName = "Unix '..' => 0 segments => false")]

	// Relative
	[DataRow(@"folder\sub",      @"folder\sub",       false, DisplayName = "Relative vs Relative => false (no segments)")]
	[DataRow(@"C:folder",        @"C:\folder",        false, DisplayName = "Relative vs Windows => false")]

	public void WhenComparingPaths_CaseSensitive_ThenResultIsAsExpected(string? path1, string? path2, bool expectedEquals)
	{
		// Arrange
		var comparer = AbsolutePathComparer.Comparer;

		// Act
		var result = comparer.Equals(path1, path2);

		// Assert
		Assert.AreEqual(expectedEquals, result,"Case-sensitive: '{path1}' vs '{path2}' should be {expectedEquals}.");
	}

	[TestMethod]
	// (path1, path2, expectSameHash)
	[DataRow(@"\\server\share",  @"\\server\share",   true,  DisplayName = "UNC vs UNC => same hash")]
	[DataRow(@"\\server\share",  @"/server/share",    false, DisplayName = "UNC vs Unix => different hash (types differ)")]
	[DataRow(@"C:\folder",       @"C:\folder",        true,  DisplayName = "Windows vs Windows => same hash")]
	[DataRow(@"C:\folder",       @"C:\FOLDER",        true,  DisplayName = "Windows vs Windows (ignoreCase) => same hash if ignoring case")]
	[DataRow(@"/usr/bin",        @"/usr/bin",         true,  DisplayName = "Unix vs Unix => same hash")]
	[DataRow(@"folder\sub",      @"folder\sub",       true,  DisplayName = "Relative vs Relative => both parse to no segments => same hash = 0? Actually let's see.")]
	public void WhenGetHashCode_CaseInsensitive_ThenItMatchesEquality(string? path1, string? path2, bool expectSameHash)
	{
		// Arrange
		var comparer = AbsolutePathComparer.ComparerIgnoreCase;

		// Act
		var equalsResult = comparer.Equals(path1, path2);
		var hash1 = comparer.GetHashCode(path1);
		var hash2 = comparer.GetHashCode(path2);
		var sameHash = (hash1 == hash2);

		// Assert
		if (equalsResult)
		{
			Assert.IsTrue(sameHash, $"Paths '{path1}' and '{path2}' are equal, so they should have the same hash code. Got: {hash1}, {hash2}.");
		}
		else if (expectSameHash)
		{
			Assert.IsTrue(sameHash, $"Expected same hash code for '{path1}' and '{path2}', but got {hash1} vs {hash2}.");
		}
		else
		{
			// Usually we expect different hash, but collisions are still possible in theory.
			Assert.IsFalse(sameHash, $"Expected different hash codes for '{path1}' and '{path2}', but both are {hash1}.");
		}
	}

	[TestMethod]
	[DataRow(@"\\server\share",  @"\\server\share",  true,  DisplayName = "UNC vs UNC => same hash (exact)")]
	[DataRow(@"\\Server\share",  @"\\server\share",  false, DisplayName = "UNC vs UNC (case diff in first segment) => different hash")]
	[DataRow(@"C:\folder",       @"C:\folder",       true,  DisplayName = "Windows vs Windows => same hash")]
	[DataRow(@"C:\folder",       @"C:\FOLDER",       false, DisplayName = "Windows vs Windows (case diff) => different hash")]
	[DataRow(@"/usr/bin",        @"/usr/bin",        true,  DisplayName = "Unix vs Unix => same hash")]
	[DataRow(@"/usr/bin",        @"/usr/BIN",        false, DisplayName = "Unix vs Unix (case diff) => different hash")]
	[DataRow(@"folder\sub",      @"folder\sub",      true,  DisplayName = "Relative => parse as no segments => same hash = 0?")]
	public void WhenGetHashCode_CaseSensitive_ThenItMatchesEquality(string? path1, string? path2, bool expectSameHash)
	{
		// Arrange
		var comparer = AbsolutePathComparer.Comparer;

		// Act
		var equalsResult = comparer.Equals(path1, path2);
		var hash1 = comparer.GetHashCode(path1);
		var hash2 = comparer.GetHashCode(path2);
		var sameHash = (hash1 == hash2);

		// Assert
		if (equalsResult)
		{
			Assert.IsTrue(sameHash, $"Paths '{path1}' and '{path2}' are equal, so they should have the same hash code. Got: {hash1}, {hash2}.");
		}
		else if (expectSameHash)
		{
			Assert.IsTrue(sameHash, $"Expected same hash code for '{path1}' and '{path2}', but got {hash1} vs {hash2}.");
		}
		else
		{
			// Collisions can happen, but typically we expect different values.
			Assert.IsFalse(sameHash, $"Expected different hash codes for '{path1}' and '{path2}', but both are {hash1}.");
		}
	}
	
	[TestMethod]
	[DataRow(@"C:\folder\🦄", @"C:\folder\🦄", true, DisplayName = "Windows path with emoji")]
	[DataRow("/usr/local/🦄", "/usr/local/🦄", true, DisplayName = "Unix path with emoji")]
	[DataRow(@"C:\folder\مرحبا", @"C:\folder\مرحبا", true, DisplayName = "Windows path with Arabic")]
	[DataRow("/usr/local/你好", "/usr/local/你好", true, DisplayName = "Unix path with Chinese")]
	public void GivenPaths_WhenComparingWithUnicodeCharacters_ThenItReturnsExpectedResult(string? path1, string? path2, bool expectedEquals)
	{
		// Arrange
		var comparer = AbsolutePathComparer.ComparerIgnoreCase;

		// Act
		var result = comparer.Equals(path1, path2);

		// Assert
		Assert.AreEqual(expectedEquals, result, $"Paths '{path1}' and '{path2}' should be {expectedEquals}.");
	}
}
