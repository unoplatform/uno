using System;
using Uno.UI.RemoteControl.Host.Helpers;

namespace Uno.UI.RemoteControl.DevServer.Tests;

/// <summary>
/// Unit tests for the <c>UNO_DEVSERVER_DISABLE_ADDIN_ALC</c> kill switch wired through
/// <see cref="HostAssemblyResolution"/>. Behavioural coverage of the legacy
/// <c>Assembly.LoadFrom</c> fallback path in <c>AssemblyHelper.Load</c> is provided
/// by the end-to-end DevServer integration tests.
/// </summary>
[TestClass]
public class Given_AssemblyHelper
{
	// ------------------------------------------------------------------ kill switch

	[TestMethod]
	[Description("IsKillSwitchActive must mirror UNO_DEVSERVER_DISABLE_ADDIN_ALC without caching so that changes take effect on the next call.")]
	public void IsKillSwitchActive_ReflectsEnvironmentVariable()
	{
		var previous = Environment.GetEnvironmentVariable("UNO_DEVSERVER_DISABLE_ADDIN_ALC");
		try
		{
			Environment.SetEnvironmentVariable("UNO_DEVSERVER_DISABLE_ADDIN_ALC", null);
			HostAssemblyResolution.IsKillSwitchActive.Should().BeFalse(
				"env var is absent");

			Environment.SetEnvironmentVariable("UNO_DEVSERVER_DISABLE_ADDIN_ALC", "0");
			HostAssemblyResolution.IsKillSwitchActive.Should().BeFalse(
				"env var is '0', not '1'");

			Environment.SetEnvironmentVariable("UNO_DEVSERVER_DISABLE_ADDIN_ALC", "1");
			HostAssemblyResolution.IsKillSwitchActive.Should().BeTrue(
				"env var is '1'");
		}
		finally
		{
			Environment.SetEnvironmentVariable("UNO_DEVSERVER_DISABLE_ADDIN_ALC", previous);
		}
	}
}
