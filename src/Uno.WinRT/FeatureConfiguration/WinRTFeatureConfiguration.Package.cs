using System.Collections.Generic;

namespace Uno;

partial class WinRTFeatureConfiguration
{
	public static class Package
	{
		/// <summary>
		/// Folders searched, in order, for a file of an <c>ms-appx:///</c> URI that the app's install folder does not
		/// have. Only on Skia targets that read the app's content from the file system.
		/// </summary>
		/// <remarks>
		/// For a host whose install folder is read-only, such as an MSIX package, and that shows content it only
		/// has at run time: it keeps that content in a folder it can write to and names the folder here. Set it
		/// before that content is first requested: a path resolved while no folder was named is kept for the life of
		/// the process. The folders can gain files later: an image that is not found is looked for again.
		/// </remarks>
		public static IReadOnlyList<string> AdditionalContentRoots { get; set; } = [];
	}
}
