// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\dxaml\lib\AccessKeyManager_partial.cpp, tag winui3/release/1.4.3, commit 685d2bf
// MUX Reference dxaml\xcp\dxaml\lib\AccessKeyEvents.h, tag winui3/release/1.4.3, commit 685d2bf

#nullable enable
#pragma warning disable CS0067 // Event is never used
#pragma warning disable CS0649 // Field is never assigned to

using System;
using Uno.UI.Xaml.Core;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Input;

public partial class AccessKeyManager
{
	private static bool _areKeyTipsEnabled = true;
	private static bool _isDisplayModeEnabledForCurrentThread;

	/// <summary>
	/// Gets or sets a value indicating whether KeyTips are enabled.
	/// </summary>
	public static bool AreKeyTipsEnabled
	{
		get => _areKeyTipsEnabled;
		set => _areKeyTipsEnabled = value;
	}

	/// <summary>
	/// Gets a value indicating whether access key display mode is active for any content root on the thread.
	/// </summary>
	public static bool IsDisplayModeEnabled => _isDisplayModeEnabledForCurrentThread;

	/// <summary>
	/// Event raised when IsDisplayModeEnabled changes.
	/// </summary>
	public static event TypedEventHandler<object, object>? IsDisplayModeEnabledChanged;

	/// <summary>
	/// Enter access key display mode for the given XamlRoot.
	/// </summary>
	public static void EnterDisplayMode(XamlRoot xamlRoot)
	{
		if (xamlRoot is null)
		{
			throw new ArgumentNullException(nameof(xamlRoot));
		}

#if __SKIA__
		var visualTree = xamlRoot.VisualTree;
		var contentRoot = visualTree.ContentRoot;
		contentRoot.AccessKeyExport.EnterAccessKeyMode();
#endif
	}

	/// <summary>
	/// Exit access key display mode for all content roots on the thread.
	/// </summary>
	public static void ExitDisplayMode()
	{
#if __SKIA__
		var coreServices = CoreServices.Instance;
		var coordinator = coreServices.ContentRootCoordinator;

		foreach (var contentRoot in coordinator.ContentRoots)
		{
			contentRoot.AccessKeyExport.ExitAccessKeyMode();
		}
#endif
	}

#if __SKIA__
	/// <summary>
	/// Callback from AKModeContainer when IsActive changes on any ContentRoot.
	/// </summary>
	internal static void OnIsActiveChanged(ContentRoot contentRoot)
	{
		bool isDisplayModeEnabled = false;

		// Check if *any* ContentRoot has active AK mode
		var coreServices = CoreServices.Instance;
		var coordinator = coreServices.ContentRootCoordinator;
		foreach (var cr in coordinator.ContentRoots)
		{
			if (cr.AccessKeyExport.IsActive)
			{
				isDisplayModeEnabled = true;
				break;
			}
		}

		bool previousIsDisplayModeEnabled = _isDisplayModeEnabledForCurrentThread;
		bool didChange = previousIsDisplayModeEnabled != isDisplayModeEnabled;

		if (didChange)
		{
			_isDisplayModeEnabledForCurrentThread = isDisplayModeEnabled;
		}

		// Tell the FocusManager about the mode change (for caret visibility etc.)
		contentRoot.FocusManager.OnAccessKeyDisplayModeChanged();

		if (didChange)
		{
			// Fire the static event
			IsDisplayModeEnabledChanged?.Invoke(null!, null!);
		}
	}
#endif
}
