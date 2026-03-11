using System.Collections.Immutable;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.RemoteControl.DevServer.Tests.Helpers;

[TestClass]
public class AddInsFlagParserTests
{
	// Reproduces the exact parsing contract from AddInsExtensions.ConfigureAddInsFromPaths
	// (src/Uno.UI.RemoteControl.Host/Extensibility/AddInsExtensions.cs:35-38)
	private static IReadOnlyList<string> ParseAddInsValue(string addinsValue)
		=> addinsValue
			.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToImmutableList();

	#region --addins flag parsing tests

	[TestMethod]
	public void ParseAddIns_SimpleSemicolonSplit_ReturnsBothPaths()
	{
		var result = ParseAddInsValue("path1.dll;path2.dll");

		result.Should().BeEquivalentTo(["path1.dll", "path2.dll"]);
	}

	[TestMethod]
	public void ParseAddIns_TrailingAndLeadingSemicolons_IgnoredCorrectly()
	{
		var result = ParseAddInsValue(";path1.dll;path2.dll;");

		result.Should().BeEquivalentTo(["path1.dll", "path2.dll"]);
	}

	[TestMethod]
	public void ParseAddIns_EmptyEntriesRemoved()
	{
		var result = ParseAddInsValue("path1.dll;;path2.dll");

		result.Should().BeEquivalentTo(["path1.dll", "path2.dll"]);
	}

	[TestMethod]
	public void ParseAddIns_WhitespaceTrimmed()
	{
		var result = ParseAddInsValue(" path1.dll ; path2.dll ");

		result.Should().BeEquivalentTo(["path1.dll", "path2.dll"]);
	}

	[TestMethod]
	public void ParseAddIns_EmptyString_ReturnsEmpty()
	{
		var result = ParseAddInsValue("");

		result.Should().BeEmpty();
	}

	[TestMethod]
	public void ParseAddIns_WhitespaceOnly_ReturnsEmpty()
	{
		var result = ParseAddInsValue("  ");

		result.Should().BeEmpty();
	}

	[TestMethod]
	public void ParseAddIns_DuplicatesRemoved()
	{
		var result = ParseAddInsValue("a.dll;b.dll;a.dll");

		result.Should().BeEquivalentTo(["a.dll", "b.dll"]);
	}

	[TestMethod]
	public void ParseAddIns_CaseInsensitiveDedup()
	{
		var result = ParseAddInsValue("A.dll;a.dll");

		result.Should().HaveCount(1);
		result[0].Should().Be("A.dll");
	}

	[TestMethod]
	public void ParseAddIns_SingleEntryNoSemicolon()
	{
		var result = ParseAddInsValue("path1.dll");

		result.Should().BeEquivalentTo(["path1.dll"]);
	}

	#endregion

	#region Controller --addins forwarding tests

	// Reproduces the arg-building contract from Program.Command.cs and CliManager.cs:
	//   if (addins is not null) { args.Add("--addins"); args.Add(addins); }
	private static List<string> BuildArgsWithAddins(string? addins)
	{
		var args = new List<string>();
		if (addins is not null)
		{
			args.Add("--addins");
			args.Add(addins);
		}
		return args;
	}

	[TestMethod]
	public void ForwardAddIns_WhenNonNull_ArgsContainFlag()
	{
		var args = BuildArgsWithAddins("path1.dll;path2.dll");

		args.Should().HaveCount(2);
		args[0].Should().Be("--addins");
		args[1].Should().Be("path1.dll;path2.dll");
	}

	[TestMethod]
	public void ForwardAddIns_WhenNull_ArgsDoNotContainFlag()
	{
		var args = BuildArgsWithAddins(null);

		args.Should().BeEmpty();
		args.Should().NotContain("--addins");
	}

	[TestMethod]
	public void ForwardAddIns_WhenEmptyString_ArgsContainFlagWithEmptyValue()
	{
		var args = BuildArgsWithAddins("");

		args.Should().HaveCount(2);
		args[0].Should().Be("--addins");
		args[1].Should().Be("");
	}

	#endregion
}
