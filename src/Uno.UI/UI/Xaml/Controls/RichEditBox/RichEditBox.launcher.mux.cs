// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml/xcp/dxaml/lib/Launcher.cpp, commit 3c9c168844f06c6ac000a97977f0bb3f4c90fd75
#nullable enable

using System;
using System.Threading.Tasks;

namespace Microsoft.UI.Xaml.Controls;

partial class RichEditBox
{
	// Is scheme found in list of schemes?
	private static bool FindScheme(string scheme, ReadOnlySpan<string> schemes)
	{
		foreach (var candidate in schemes)
		{
			if (string.Equals(scheme, candidate, StringComparison.Ordinal))
			{
				return true;
			}
		}

		return false;
	}

	// Is scheme allowed to be launched in another app?
	private static bool IsUriSchemeAllowedToLaunch(string scheme)
	{
		// Schemes which cannot be opened in new window
		ReadOnlySpan<string> unallowedSchemes = ["file", "res"];
		return !FindScheme(scheme, unallowedSchemes);
	}

	// Is scheme trusted to be launched in another app without a warning message?
	private static bool IsUriSchemeTrustedForLaunch(string scheme)
	{
		ReadOnlySpan<string> safeSchemes = ["http", "https"];
		return FindScheme(scheme, safeSchemes);
	}

	// Invoke default protocol handler app using Windows.System.Launcher
	private async Task InvokeLauncher(Uri uri, bool trusted)
	{
		if (!trusted)
		{
#if HAS_UNO
			// Uno's launcher has no TreatAsUntrusted implementation; confirm in the owning XamlRoot.
			if (!await ConfirmLinkLaunchAsync(uri))
			{
				return;
			}
#else
			// TODO Uno: Native Windows supplies the confirmation through LauncherOptions.
			// IFC(spLauncherOptions->put_TreatAsUntrusted(true));
			// IFC(spLauncher->LaunchUriWithOptionsAsync(pUri, spLauncherOptions.Get(), &spLaunchOperation));
#endif
		}

#if HAS_UNO
		await LaunchLinkPlatformAsync(uri);
#else
		// IFC(spLauncher->LaunchUriAsync(pUri, &spLaunchOperation));
		// IFC(spLaunchOperation->put_Completed(spCallback.Get()));
#endif
	}

	// Check scheme and invoke default protocol handler app if check passes.
	private async Task TryInvokeLauncher(Uri uri)
	{
		var scheme = uri.Scheme;

		// Can scheme be opened in external app?
		if (!IsUriSchemeAllowedToLaunch(scheme))
		{
			return;
		}

		// Does scheme need a user warning before opening external app?
		var trusted = IsUriSchemeTrustedForLaunch(scheme);

		// Launch URI in external app
		await InvokeLauncher(uri, trusted);
	}
}
