using Uno.UI.DevServer.Cli.Helpers;

namespace Uno.UI.DevServer.Cli.Mcp;

/// <summary>
/// Maps structural fields from <see cref="DiscoveryInfo"/> to <see cref="ValidationIssue"/> instances
/// for inclusion in health reports. Uses nullability of discovery fields (not string matching on
/// warning/error text) to determine which issues to report.
/// </summary>
internal static class DiscoveryIssueMapper
{
	public static List<ValidationIssue> MapDiscoveryIssues(DiscoveryInfo? discovery)
	{
		var issues = new List<ValidationIssue>();

		if (discovery is null)
		{
			return issues;
		}

		if (discovery.GlobalJsonPath is null)
		{
			issues.Add(new ValidationIssue
			{
				Code = IssueCode.GlobalJsonNotFound,
				Severity = ValidationSeverity.Fatal,
				Message = "No global.json found in working directory or parent directories.",
				Remediation = "Create a global.json with the Uno.Sdk in msbuild-sdks.",
			});
			return issues; // No point checking further if global.json is missing
		}

		if (discovery.UnoSdkPackage is null || discovery.UnoSdkVersion is null)
		{
			issues.Add(new ValidationIssue
			{
				Code = IssueCode.UnoSdkNotInGlobalJson,
				Severity = ValidationSeverity.Fatal,
				Message = "global.json does not define Uno.Sdk or Uno.Sdk.Private in msbuild-sdks.",
				Remediation = "Add Uno.Sdk to the msbuild-sdks section of global.json.",
			});
			return issues;
		}

		if (discovery.UnoSdkPath is null)
		{
			issues.Add(new ValidationIssue
			{
				Code = IssueCode.SdkNotInCache,
				Severity = ValidationSeverity.Fatal,
				Message = $"Uno SDK package {discovery.UnoSdkPackage} {discovery.UnoSdkVersion} not found in NuGet cache.",
				Remediation = "Run 'dotnet restore' to download the Uno SDK package.",
			});
			return issues;
		}

		if (discovery.PackagesJsonPath is null)
		{
			issues.Add(new ValidationIssue
			{
				Code = IssueCode.PackagesJsonNotFound,
				Severity = ValidationSeverity.Warning,
				Message = "packages.json not found in the Uno SDK package.",
				Remediation = "This may indicate a corrupted SDK package. Try clearing the NuGet cache and restoring.",
			});
		}

		if (discovery.DotNetVersion is null)
		{
			issues.Add(new ValidationIssue
			{
				Code = IssueCode.DotNetNotFound,
				Severity = ValidationSeverity.Fatal,
				Message = "Unable to determine the installed .NET version.",
				Remediation = "Ensure 'dotnet' is available on the PATH.",
			});
		}

		if (discovery.DevServerPackageVersion is not null && discovery.DevServerPackagePath is null)
		{
			issues.Add(new ValidationIssue
			{
				Code = IssueCode.DevServerPackageNotCached,
				Severity = ValidationSeverity.Fatal,
				Message = $"Uno.WinUI.DevServer {discovery.DevServerPackageVersion} not found in NuGet cache.",
				Remediation = "Run 'dotnet restore' to download the DevServer package.",
			});
		}

		if (discovery.DevServerPackagePath is not null && discovery.DotNetTfm is not null && discovery.HostPath is null)
		{
			issues.Add(new ValidationIssue
			{
				Code = IssueCode.HostBinaryNotFound,
				Severity = ValidationSeverity.Fatal,
				Message = "DevServer host binary not found in the expected package location.",
				Remediation = "The DevServer package may be corrupted. Try clearing the NuGet cache and restoring.",
			});
		}

		return issues;
	}
}
