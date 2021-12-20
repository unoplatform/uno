#nullable enable

using System;

namespace Windows.System.Profile.Internal;

internal static class VersionHelpers
{
	internal static long ToLong(Version version)
	{
		var major = (ushort)version.Major;
		var minor = (ushort)version.Minor;
		var build = (ushort)version.Build;
		var revision = (ushort)version.Revision;

		var versionNumber = major << 48 | minor << 32 | build << 16 | revision;
		return versionNumber;
	}
}
