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

		var versionNumber = (long)major << 48 | (long)minor << 32 | (long)build << 16 | (long)revision;
		return versionNumber;
	}
}
