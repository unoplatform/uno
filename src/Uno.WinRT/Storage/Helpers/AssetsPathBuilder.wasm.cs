#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
		[return: NotNullIfNotNull("contentRelativePath")]
		public static string? BuildAssetUri(string? contentRelativePath)
			=> !string.IsNullOrEmpty(UNO_BOOTSTRAP_APP_BASE)
				// Concatenates the app's base path (used to support deep-linking), with the generated app based content folder name.
				// See https://github.com/unoplatform/Uno.Wasm.Bootstrap#configuration-environment-variables for more details.
				? $"{UNO_BOOTSTRAP_WEBAPP_BASE_PATH}{UNO_BOOTSTRAP_APP_BASE.TrimEnd('/')}/{contentRelativePath?.TrimStart('/')}"
				: contentRelativePath;
	}
}
