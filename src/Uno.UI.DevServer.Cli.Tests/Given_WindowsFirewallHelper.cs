using AwesomeAssertions;
using Uno.UI.DevServer.Cli.Helpers;

namespace Uno.UI.DevServer.Cli.Tests;

[TestClass]
public class Given_WindowsFirewallHelper
{
	private const string DotnetPath = @"C:\Program Files\dotnet\dotnet.exe";

	// -------------------------------------------------------------------------
	// ParseHasRequiredRule — profile coverage
	// -------------------------------------------------------------------------

	[TestMethod]
	[Description("A rule that covers only Public must not satisfy the check — Public is already covered by the SDK installer rule.")]
	public void ParseHasRequiredRule_PublicOnly_ReturnsFalse()
	{
		var output = MakeBlock(DotnetPath, profiles: "Public", action: "Allow", enabled: "Yes");

		WindowsFirewallHelper.ParseHasRequiredRule(output, DotnetPath).Should().BeFalse();
	}

	[TestMethod]
	[Description("A rule that covers Private must satisfy the check.")]
	public void ParseHasRequiredRule_Private_ReturnsTrue()
	{
		var output = MakeBlock(DotnetPath, profiles: "Private", action: "Allow", enabled: "Yes");

		WindowsFirewallHelper.ParseHasRequiredRule(output, DotnetPath).Should().BeTrue();
	}

	[TestMethod]
	[Description("A rule that covers Domain must satisfy the check — corporate AD networks use Domain profile.")]
	public void ParseHasRequiredRule_Domain_ReturnsTrue()
	{
		var output = MakeBlock(DotnetPath, profiles: "Domain", action: "Allow", enabled: "Yes");

		WindowsFirewallHelper.ParseHasRequiredRule(output, DotnetPath).Should().BeTrue();
	}

	[TestMethod]
	[Description("A rule that covers Private,Domain (as added by the CLI) must satisfy the check.")]
	public void ParseHasRequiredRule_PrivateDomain_ReturnsTrue()
	{
		var output = MakeBlock(DotnetPath, profiles: "Private,Domain", action: "Allow", enabled: "Yes");

		WindowsFirewallHelper.ParseHasRequiredRule(output, DotnetPath).Should().BeTrue();
	}

	[TestMethod]
	[Description("A rule with Profiles=Any covers all three profiles and must satisfy the check.")]
	public void ParseHasRequiredRule_Any_ReturnsTrue()
	{
		var output = MakeBlock(DotnetPath, profiles: "Any", action: "Allow", enabled: "Yes");

		WindowsFirewallHelper.ParseHasRequiredRule(output, DotnetPath).Should().BeTrue();
	}

	// -------------------------------------------------------------------------
	// ParseHasRequiredRule — false-positive guards
	// -------------------------------------------------------------------------

	[TestMethod]
	[Description("'LocalIP: Any' and 'RemoteIP: Any' must not be mistaken for Profiles=Any.")]
	public void ParseHasRequiredRule_AnyInOtherFields_DoesNotFalsePositive()
	{
		// Public-only rule with LocalIP=Any / RemoteIP=Any — the word "Any" appears
		// in fields other than Profiles.  The check must parse the Profiles field
		// specifically and return false.
		var output = MakeBlock(DotnetPath, profiles: "Public", action: "Allow", enabled: "Yes",
			extraFields: "LocalIP:                              Any\r\nRemoteIP:                             Any");

		WindowsFirewallHelper.ParseHasRequiredRule(output, DotnetPath).Should().BeFalse();
	}

	[TestMethod]
	[Description("A rule for a different program must not match.")]
	public void ParseHasRequiredRule_DifferentProgram_ReturnsFalse()
	{
		var output = MakeBlock(@"C:\Program Files\other\other.exe",
			profiles: "Private,Domain", action: "Allow", enabled: "Yes");

		WindowsFirewallHelper.ParseHasRequiredRule(output, DotnetPath).Should().BeFalse();
	}

	[TestMethod]
	[Description("A disabled rule must not satisfy the check even if profile and action match.")]
	public void ParseHasRequiredRule_Disabled_ReturnsFalse()
	{
		var output = MakeBlock(DotnetPath, profiles: "Private,Domain", action: "Allow", enabled: "No");

		WindowsFirewallHelper.ParseHasRequiredRule(output, DotnetPath).Should().BeFalse();
	}

	[TestMethod]
	[Description("A Block action rule must not satisfy the check.")]
	public void ParseHasRequiredRule_BlockAction_ReturnsFalse()
	{
		var output = MakeBlock(DotnetPath, profiles: "Private,Domain", action: "Block", enabled: "Yes");

		WindowsFirewallHelper.ParseHasRequiredRule(output, DotnetPath).Should().BeFalse();
	}

	[TestMethod]
	[Description("Empty netsh output (no rules) must return false.")]
	public void ParseHasRequiredRule_EmptyOutput_ReturnsFalse()
	{
		WindowsFirewallHelper.ParseHasRequiredRule(string.Empty, DotnetPath).Should().BeFalse();
	}

	[TestMethod]
	[Description("Multiple rules in the output: only the matching one satisfies the check.")]
	public void ParseHasRequiredRule_MultipleRules_MatchesCorrectOne()
	{
		var publicRule = MakeBlock(DotnetPath, profiles: "Public", action: "Allow", enabled: "Yes");
		var privateRule = MakeBlock(DotnetPath, profiles: "Private,Domain", action: "Allow", enabled: "Yes");
		var output = publicRule + "\r\n\r\n" + privateRule;

		WindowsFirewallHelper.ParseHasRequiredRule(output, DotnetPath).Should().BeTrue();
	}

	[TestMethod]
	[Description("Path comparison must be case-insensitive on Windows.")]
	public void ParseHasRequiredRule_PathCaseInsensitive_ReturnsTrue()
	{
		var output = MakeBlock(@"C:\PROGRAM FILES\DOTNET\DOTNET.EXE",
			profiles: "Private", action: "Allow", enabled: "Yes");

		WindowsFirewallHelper.ParseHasRequiredRule(output, DotnetPath).Should().BeTrue();
	}

	// -------------------------------------------------------------------------
	// Helpers
	// -------------------------------------------------------------------------

	/// <summary>
	/// Builds a minimal netsh rule block that mirrors the verbose output format.
	/// </summary>
	private static string MakeBlock(
		string program,
		string profiles,
		string action,
		string enabled,
		string? extraFields = null)
	{
		var lines = new List<string>
		{
			$"Rule Name:                            Test Rule",
			$"Direction:                            In",
			$"Profiles:                             {profiles}",
			$"Action:                               {action}",
			$"Enabled:                              {enabled}",
			$"Program:                              {program}",
		};

		if (extraFields is not null)
		{
			lines.Add(extraFields);
		}

		return string.Join("\r\n", lines);
	}
}
