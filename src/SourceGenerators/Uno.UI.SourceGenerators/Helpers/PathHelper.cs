using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Uno.MsBuildTasks.Utils
{
	internal class PathHelper
	{
		/// <summary>
		/// Gets the relative path of a base path from another path.
		/// </summary>
		/// <param name="folder">A folder for which to get a relative path from <paramref name="filespec"/>.</param>
		/// <param name="filespec">A relative or absolute path for which to determine the relative path from <paramref name="folder"/>.</param>
		/// <returns>A relative path.</returns>
		public static string GetRelativePath(string folder, string filespec)
		{
			var pathUri = new Uri(Path.GetFullPath(filespec));

			// Folders must end in a slash
			if (!folder.EndsWith(Path.DirectorySeparatorChar.ToString()))
			{
				folder += Path.DirectorySeparatorChar;
			}

			var folderUri = new Uri(folder);

			return Uri.UnescapeDataString(
				folderUri
					.MakeRelativeUri(pathUri)
					.OriginalString
					.Replace('/', Path.DirectorySeparatorChar)
				);
		}
	}
}
