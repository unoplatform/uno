#nullable enable

using System;
using Uno.Foundation.Logging;

namespace UIKit;

internal static class UISceneSessionExtensions
{
	/// <summary>
	/// Gets whether the session belongs to an application scene, rather than to an external display or another role.
	/// </summary>
	internal static bool IsApplicationRole(this UISceneSession session)
		=> session.HasRole(UIWindowSceneSessionRole.Application);

	/// <summary>
	/// Gets whether the session has <paramref name="role"/>, which is false for a role the bindings don't know.
	/// </summary>
	internal static bool HasRole(this UISceneSession session, UIWindowSceneSessionRole role)
		=> session.TryGetRole(out var sessionRole) && sessionRole == role;

	/// <summary>
	/// Gets the session's role, unless it is one the bindings don't know, whose reading throws.
	/// </summary>
	internal static bool TryGetRole(this UISceneSession session, out UIWindowSceneSessionRole role)
	{
		try
		{
			role = session.Role;
			return true;
		}
		catch (NotSupportedException e)
		{
			// A role introduced by a newer OS.
			var log = typeof(UISceneSessionExtensions).Log();

			if (log.IsEnabled(LogLevel.Debug))
			{
				log.Debug($"Ignoring a scene with an unknown role: {e}");
			}

			role = default;
			return false;
		}
	}

	/// <summary>
	/// Describes the session's role for logs, as reading a role the bindings don't know throws.
	/// </summary>
	internal static string DescribeRole(this UISceneSession session)
		=> session.TryGetRole(out var role) ? role.ToString() : "unknown";
}
