using Uno.UI.DevServer.Cli.Helpers;

namespace Uno.UI.DevServer.Cli.Mcp;

/// <summary>
/// Maps structural fields from <see cref="DiscoveryInfo"/> to <see cref="ValidationIssue"/> instances
/// for inclusion in health reports. Uses nullability of discovery fields (not string matching on
/// warning/error text) to determine which issues to report. Also explains why an explicitly selected solution
/// is not served, which needs a look at the file system.
/// </summary>
/// <seealso href="../health-diagnostics.md"/>
internal static class DiscoveryIssueMapper
{
	/// <summary>
	/// Why an explicitly selected solution does not resolve to an Uno workspace: it is missing, its nearest global.json
	/// (where the resolver stops, as the .NET SDK does) has no usable Uno SDK, or it has none. A kept selection
	/// (<paramref name="selectionIsKept"/>) starts by itself once fixed; a rejected one must be selected again.
	/// </summary>
	public static ValidationIssue MapUnresolvedSelection(string solutionPath, bool selectionIsKept)
	{
		var then = selectionIsKept
			? "the selection then starts by itself; or select another solution with uno_app_select_solution."
			: "then call uno_app_select_solution again, or select another solution.";

		if (!File.Exists(solutionPath))
		{
			return Issue($"The selected solution {solutionPath} does not exist.", $"Restore it ({then})");
		}

		if (GlobalJsonLocator.FindGlobalJson(Path.GetDirectoryName(solutionPath) ?? solutionPath) is { } globalJsonPath)
		{
			return Issue(
				$"The nearest global.json above {solutionPath} ({globalJsonPath}) does not declare a usable Uno.Sdk or Uno.Sdk.Private version in msbuild-sdks.",
				$"Declare the Uno SDK in that global.json's msbuild-sdks ({then})");
		}

		// "Next to the solution": a kept selection without a global.json is watched from its own folder
		// (ProxyLifecycleManager.GetSelectedSolutionObservationRoot), so one added higher up is not seen.
		return Issue(
			$"No global.json was found above {solutionPath}.",
			$"Add a global.json declaring Uno.Sdk in msbuild-sdks next to the solution ({then})");

		static ValidationIssue Issue(string message, string remediation)
			=> new()
			{
				Code = IssueCode.WorkspaceNotResolved,
				Severity = ValidationSeverity.Fatal,
				Message = message,
				Remediation = remediation,
			};
	}

	/// <summary>
	/// The uno_health issue for an explicit selection the DevServer does not serve: it does not resolve, or the monitor
	/// found no solution to launch because its file went away before the workspace watcher noticed. The monitor's flag
	/// counts only while the file is really missing, since it can lag behind a restored file.
	/// </summary>
	public static ValidationIssue? MapSelectionIssue(WorkspaceSelectionSnapshot? selection, bool monitorFoundNoSolution)
		=> selection is { IsExplicitSolutionSelection: true, SelectedSolutionPath: { Length: > 0 } selectedSolution }
			&& (selection.EffectiveWorkspaceDirectory is null || (monitorFoundNoSolution && !File.Exists(selectedSolution)))
			? MapUnresolvedSelection(selectedSolution, selectionIsKept: true)
			: null;

	public static List<ValidationIssue> MapDiscoveryIssues(DiscoveryInfo? discovery)
	{
		var issues = new List<ValidationIssue>();

		if (discovery is null)
		{
			return issues;
		}

		if (discovery.ResolutionKind == WorkspaceResolutionKind.Ambiguous)
		{
			issues.Add(new ValidationIssue
			{
				Code = IssueCode.WorkspaceAmbiguous,
				Severity = ValidationSeverity.Warning,
				Message = "Multiple Uno solutions matched the current directory. The DevServer host was not started automatically.",
				Remediation = "Call uno_app_select_solution with the absolute path of the intended .sln or .slnx (candidateSolutions lists the ones found), or start the MCP bridge from a more specific directory or with --solution-dir.",
			});
			return issues;
		}

		if (discovery.ResolutionKind == WorkspaceResolutionKind.NoValidWorkspace)
		{
			issues.Add(new ValidationIssue
			{
				Code = IssueCode.WorkspaceNotResolved,
				Severity = ValidationSeverity.Fatal,
				Message = "Solution files were found, but none resolved to a valid Uno workspace with a global.json declaring Uno.Sdk.",
				Remediation = "Ensure the intended workspace contains a global.json with Uno.Sdk in msbuild-sdks. If the repo changed after startup, restart the MCP bridge after fixing the workspace.",
			});
			return issues;
		}

		if (discovery.ResolutionKind == WorkspaceResolutionKind.NoCandidates)
		{
			issues.Add(new ValidationIssue
			{
				Code = IssueCode.NoSolutionFound,
				Severity = ValidationSeverity.Warning,
				Message = "No .sln or .slnx file found in the working directory or its subdirectories.",
				Remediation = "Create an Uno Platform project first (e.g. 'dotnet new unoapp'), then tools will become available automatically.",
			});
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
				Remediation = "Run 'dotnet restore' to download the Uno SDK package, then call 'uno_app_select_solution' again with the intended solution path to retry startup.",
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
				Remediation = "Run 'dotnet restore' to download the DevServer package, then call 'uno_app_select_solution' again with the intended solution path to retry startup.",
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

		// Add-in specific issues
		if (discovery.SettingsPackageVersion is not null && discovery.SettingsPackagePath is null)
		{
			issues.Add(new ValidationIssue
			{
				Code = IssueCode.AddInPackageNotCached,
				Severity = ValidationSeverity.Warning,
				Message = $"Add-in package uno.settings.devserver {discovery.SettingsPackageVersion} not found in NuGet cache.",
				Remediation = "Run 'dotnet restore' to download the package, then call 'uno_app_select_solution' again with the intended solution path to retry startup.",
			});
		}

		if (discovery.AddInDiscoveryFailed)
		{
			issues.Add(new ValidationIssue
			{
				Code = IssueCode.AddInDiscoveryFallback,
				Severity = ValidationSeverity.Warning,
				Message = "Convention-based add-in discovery failed. The server will use MSBuild fallback.",
				Remediation = "Check that NuGet packages are properly restored.",
			});
		}

		return issues;
	}
}
