#nullable enable

using System;
using System.IO;
using System.Threading.Tasks;

namespace Windows.Storage;

partial class ApplicationData
{
	internal Task EnablePersistenceAsync() => Task.CompletedTask;

	private static string GetLocalCacheFolder() => "";

	private static string GetTemporaryFolder() => "";

	private static string GetLocalFolder() => "";

	private static string GetRoamingFolder() => "";

	private static string GetSharedLocalFolder() => "";
}
