using System.Collections.Generic;

namespace Uno;

partial class WinRTFeatureConfiguration
{
	public static class Package
	{
		/// <summary>
		/// Folders searched, in order, for a file of an <c>ms-appx:///</c> URI that the app's install folder does not
		/// have. Skia desktop targets only.
		/// </summary>
		/// <remarks>
		/// For a host whose install folder is read-only, such as an MSIX package, and that shows content it only
		/// has at run time: it keeps that content in a folder it can write to and names the folder here. Set it at
		/// startup. The folders can gain files later: an image that is not found is looked for again the next time.
		/// </remarks>
		public static IReadOnlyList<string> AdditionalContentRoots { get; set; } = [];
	}
}
