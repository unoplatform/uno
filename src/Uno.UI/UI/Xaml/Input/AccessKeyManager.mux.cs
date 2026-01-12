// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\dxaml\lib\AccessKeyManager_partial.cpp, tag winui3/release/1.5.3

#nullable enable

#if __SKIA__
using System;
using Windows.Foundation;
using Uno.UI.Xaml.Core;

namespace Microsoft.UI.Xaml.Input;

public partial class AccessKeyManager
{
	private static bool _areKeyTipsEnabled = true;
	private static bool _isDisplayModeEnabledForCurrentThread;
	private static TypedEventHandler<object, object>? _isDisplayModeEnabledChanged;

	/// <summary>
	/// Gets or sets whether key tips are enabled.
	/// </summary>
	public static bool AreKeyTipsEnabled
	{
		get => _areKeyTipsEnabled;
		set => _areKeyTipsEnabled = value;
	}

	/// <summary>
	/// Gets whether any Xaml content on the thread is in access key display mode.
	/// </summary>
	public static bool IsDisplayModeEnabled => _isDisplayModeEnabledForCurrentThread;

	/// <summary>
	/// Enters access key display mode for the specified XamlRoot.
	/// </summary>
	/// <param name="xamlRoot">The XamlRoot to enter display mode for.</param>
	public static void EnterDisplayMode(XamlRoot xamlRoot)
	{
		if (xamlRoot is null)
		{
			throw new ArgumentNullException(nameof(xamlRoot));
		}

		var contentRoot = xamlRoot.VisualTree?.ContentRoot;
		contentRoot?.AccessKeyExport.EnterAccessKeyMode();
	}

	/// <summary>
	/// Exits access key display mode for all Xaml content on the thread.
	/// </summary>
	public static void ExitDisplayMode()
	{
		// In WinUI, this iterates all content roots and exits AK mode for each.
		var contentRootCoordinator = CoreServices.Instance.ContentRootCoordinator;
		foreach (var contentRoot in contentRootCoordinator.ContentRoots)
		{
			contentRoot?.AccessKeyExport.ExitAccessKeyMode();
		}
	}

	/// <summary>
	/// Occurs when IsDisplayModeEnabled changes.
	/// </summary>
	public static event TypedEventHandler<object, object> IsDisplayModeEnabledChanged
	{
		add => _isDisplayModeEnabledChanged += value;
		remove => _isDisplayModeEnabledChanged -= value;
	}

	/// <summary>
	/// Called when access key mode state changes on any content root.
	/// Updates IsDisplayModeEnabled and raises the event if needed.
	/// </summary>
	internal static void OnAccessKeyModeChanged(FocusManager? focusManager)
	{
		var previousIsDisplayModeEnabled = _isDisplayModeEnabledForCurrentThread;
		var newIsDisplayModeEnabled = false;

		// Check if any content root is in AK mode
		var contentRootCoordinator = CoreServices.Instance.ContentRootCoordinator;
		foreach (var contentRoot in contentRootCoordinator.ContentRoots)
		{
			if (contentRoot?.AccessKeyExport.IsActive == true)
			{
				newIsDisplayModeEnabled = true;
				break;
			}
		}

		_isDisplayModeEnabledForCurrentThread = newIsDisplayModeEnabled;

		// Notify focus manager of mode change
		focusManager?.OnAccessKeyDisplayModeChanged();

		// Fire event if display mode changed
		if (previousIsDisplayModeEnabled != newIsDisplayModeEnabled)
		{
			_isDisplayModeEnabledChanged?.Invoke(null!, null!);
		}
	}
}
#endif
