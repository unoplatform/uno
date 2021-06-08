#nullable enable

using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.Storage.Helpers
{
	/// <summary>
	/// WebAssembly assets path builder
	/// </summary>
	internal static class AssetsPathBuilder
	{
		public static readonly string UNO_BOOTSTRAP_APP_BASE = Environment.GetEnvironmentVariable(nameof(UNO_BOOTSTRAP_APP_BASE)) ?? "";
		private static readonly string UNO_BOOTSTRAP_WEBAPP_BASE_PATH = Environment.GetEnvironmentVariable(nameof(UNO_BOOTSTRAP_WEBAPP_BASE_PATH)) ?? "";

		/// <summary>
		/// Builds an actual asset path
		/// </summary>
		public static string? BuildAssetUri(string? contentRelativePath)
			=> !string.IsNullOrEmpty(UNO_BOOTSTRAP_APP_BASE)
				? $"{UNO_BOOTSTRAP_WEBAPP_BASE_PATH}{UNO_BOOTSTRAP_APP_BASE}/{contentRelativePath}"
				: contentRelativePath;
	}
}
