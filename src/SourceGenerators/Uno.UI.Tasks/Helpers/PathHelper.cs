#nullable disable

using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Uno.UI.Tasks.Helpers
{
	internal class PathHelper
	{
		public static string GetRelativePath(string fromPath, string toPath)
		{
			if (!Path.IsPathRooted(toPath))
			{
				toPath = Path.GetFullPath(toPath);
			}

			int fromAttr = GetPathAttribute(fromPath);
			int toAttr = GetPathAttribute(toPath);

			StringBuilder path = new StringBuilder(260); // MAX_PATH
			if (PathRelativePathTo(
				path,
				fromPath,
				fromAttr,
				toPath,
				toAttr) == 0)
			{
				return toPath; // Return final path 
			}
			return path.ToString();
		}


		private static int GetPathAttribute(string path)
		{
			DirectoryInfo di = new DirectoryInfo(path);
			if (di.Exists)
			{
				return FILE_ATTRIBUTE_DIRECTORY;
			}

			FileInfo fi = new FileInfo(path);
			if (fi.Exists)
			{
				return FILE_ATTRIBUTE_NORMAL;
			}

			throw new FileNotFoundException();
		}

		private const int FILE_ATTRIBUTE_DIRECTORY = 0x10;
		private const int FILE_ATTRIBUTE_NORMAL = 0x80;

		[DllImport("shlwapi.dll", SetLastError = true)]
		private static extern int PathRelativePathTo(
			StringBuilder pszPath, string pszFrom, int dwAttrFrom, string pszTo, int dwAttrTo);
	}
}
