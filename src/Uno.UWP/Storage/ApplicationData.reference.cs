#nullable enable

using System;
using System.IO;

namespace Windows.Storage;

partial class ApplicationData
{
	private static string GetLocalCacheFolder() => "";

	private static string GetTemporaryFolder() => "";

	private static string GetLocalFolder() => "";

	private static string GetRoamingFolder() => "";

	private static string GetSharedLocalFolder() => "";
}
