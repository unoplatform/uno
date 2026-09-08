#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Uno.UI.Xaml.Controls;

namespace Uno.UI.Runtime.Skia.AppleUIKit;

/// <summary>
/// Correlates windows waiting for a scene with the scene UIKit eventually connects.
/// </summary>
/// <remarks>
/// A scene activation request carries a token on its user activity, which lets the connecting scene
/// find the exact window that asked for it. UIKit does not guarantee the activity survives, so a
/// scene arriving without a usable token falls back to the oldest pending window.
/// </remarks>
internal static class SceneWindowRegistry
{
	// Reverse-DNS per Apple's convention for activity types. The activity never leaves the process
	// (it is read back from UISceneConnectionOptions), so it needs no NSUserActivityTypes entry.
	internal const string ActivityType = "uno.platform.window";
	internal const string TokenKey = "uno-window-token";

	private static readonly object _gate = new();
	private static readonly List<(string Token, NativeWindowWrapper Wrapper)> _pending = new();

	internal static string Register(NativeWindowWrapper wrapper)
	{
		var token = Guid.NewGuid().ToString("N");

		lock (_gate)
		{
			_pending.Add((token, wrapper));
		}

		return token;
	}

	internal static bool TryTake(string? token, [NotNullWhen(true)] out NativeWindowWrapper? wrapper)
	{
		lock (_gate)
		{
			var index = token is null ? -1 : _pending.FindIndex(entry => entry.Token == token);

			if (index < 0)
			{
				index = _pending.Count > 0 ? 0 : -1;
			}

			if (index < 0)
			{
				wrapper = null;
				return false;
			}

			wrapper = _pending[index].Wrapper;
			_pending.RemoveAt(index);
			return true;
		}
	}

	internal static void Remove(string token)
	{
		lock (_gate)
		{
			_pending.RemoveAll(entry => entry.Token == token);
		}
	}

	internal static void Remove(NativeWindowWrapper wrapper)
	{
		lock (_gate)
		{
			_pending.RemoveAll(entry => entry.Wrapper == wrapper);
		}
	}
}
