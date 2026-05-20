using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Uno.UI.DevServer.Cli.Helpers;

namespace Uno.UI.DevServer.Cli.Tests;

[TestClass]
[DoNotParallelize] // Tests mutate the process-wide environment; must not run concurrently.
public class Given_WindowsFirewallHelper
{
	// -------------------------------------------------------------------------
	// Opt-out via environment variable
	// -------------------------------------------------------------------------

	[TestMethod]
	[Description("Setting UNO_DEVSERVER_SKIP_FIREWALL_CHECK=1 must be detected as opted-out.")]
	public void IsOptedOut_WhenSetToOne_ReturnsTrue()
	{
		WithEnv("UNO_DEVSERVER_SKIP_FIREWALL_CHECK", "1", () =>
			WindowsFirewallHelper.IsOptedOut.Should().BeTrue());
	}

	[TestMethod]
	[Description("Setting UNO_DEVSERVER_SKIP_FIREWALL_CHECK=true (case-insensitive) must be detected as opted-out.")]
	public void IsOptedOut_WhenSetToTrueCaseInsensitive_ReturnsTrue()
	{
		WithEnv("UNO_DEVSERVER_SKIP_FIREWALL_CHECK", "TRUE", () =>
			WindowsFirewallHelper.IsOptedOut.Should().BeTrue());
	}

	[TestMethod]
	[Description("UNO_DEVSERVER_SKIP_FIREWALL_CHECK=0 must not be treated as opted-out.")]
	public void IsOptedOut_WhenSetToZero_ReturnsFalse()
	{
		WithEnv("UNO_DEVSERVER_SKIP_FIREWALL_CHECK", "0", () =>
			WindowsFirewallHelper.IsOptedOut.Should().BeFalse());
	}

	[TestMethod]
	[Description("Absent UNO_DEVSERVER_SKIP_FIREWALL_CHECK must not be treated as opted-out.")]
	public void IsOptedOut_WhenAbsent_ReturnsFalse()
	{
		WithEnv("UNO_DEVSERVER_SKIP_FIREWALL_CHECK", null, () =>
			WindowsFirewallHelper.IsOptedOut.Should().BeFalse());
	}

	[DataTestMethod]
	[DataRow("false")]
	[DataRow("")]
	[DataRow("yes")]
	[Description("Values other than '1' or 'true' (case-insensitive) must not opt out; covers edge cases a refactor could accidentally break.")]
	public void IsOptedOut_WhenSetToNonOptOutValue_ReturnsFalse(string value)
	{
		WithEnv("UNO_DEVSERVER_SKIP_FIREWALL_CHECK", value, () =>
			WindowsFirewallHelper.IsOptedOut.Should().BeFalse());
	}

	// -------------------------------------------------------------------------
	// hostExePath validation
	// -------------------------------------------------------------------------

	[TestMethod]
	[Description("EnsureFirewallRuleAsync must return without error when hostExePath is not fully qualified.")]
	public async Task EnsureFirewallRuleAsync_WhenPathNotFullyQualified_ReturnsWithoutError()
	{
		var act = async () => await WindowsFirewallHelper.EnsureFirewallRuleAsync(
			@"relative\Uno.UI.RemoteControl.Host.exe", NullLogger.Instance, CancellationToken.None);

		await act.Should().NotThrowAsync();
	}

	[TestMethod]
	[Description("EnsureFirewallRuleAsync must return without error when hostExePath contains a quote character (injection guard).")]
	public async Task EnsureFirewallRuleAsync_WhenPathContainsQuote_ReturnsWithoutError()
	{
		var act = async () => await WindowsFirewallHelper.EnsureFirewallRuleAsync(
			@"C:\legit\" + "\"" + @"\Uno.UI.RemoteControl.Host.exe", NullLogger.Instance, CancellationToken.None);

		await act.Should().NotThrowAsync();
	}

	[TestMethod]
	[Description("EnsureFirewallRuleAsync must return without error when hostExePath does not exist on disk.")]
	public async Task EnsureFirewallRuleAsync_WhenPathDoesNotExist_ReturnsWithoutError()
	{
		var act = async () => await WindowsFirewallHelper.EnsureFirewallRuleAsync(
			@"C:\does\not\exist\Uno.UI.RemoteControl.Host.exe", NullLogger.Instance, CancellationToken.None);

		await act.Should().NotThrowAsync();
	}

	[TestMethod]
	[Description("EnsureFirewallRuleAsync must return immediately without error when opted-out, regardless of OS state.")]
	public async Task EnsureFirewallRuleAsync_WhenOptedOut_ReturnsWithoutError()
	{
		await WithEnvAsync("UNO_DEVSERVER_SKIP_FIREWALL_CHECK", "1", async () =>
		{
			// NullLogger means no side effects to assert on; the test verifies it doesn't throw.
			var act = async () => await WindowsFirewallHelper.EnsureFirewallRuleAsync(
				@"C:\fake\Uno.UI.RemoteControl.Host.exe", NullLogger.Instance, CancellationToken.None);

			await act.Should().NotThrowAsync();
		});
	}

	// -------------------------------------------------------------------------
	// Helpers
	// -------------------------------------------------------------------------

	private static void WithEnv(string name, string? value, Action action)
	{
		var previous = Environment.GetEnvironmentVariable(name);
		try
		{
			Environment.SetEnvironmentVariable(name, value);
			action();
		}
		finally
		{
			Environment.SetEnvironmentVariable(name, previous);
		}
	}

	private static async Task WithEnvAsync(string name, string? value, Func<Task> action)
	{
		var previous = Environment.GetEnvironmentVariable(name);
		try
		{
			Environment.SetEnvironmentVariable(name, value);
			await action();
		}
		finally
		{
			Environment.SetEnvironmentVariable(name, previous);
		}
	}
}
