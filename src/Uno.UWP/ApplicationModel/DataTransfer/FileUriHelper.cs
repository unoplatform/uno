using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Windows.ApplicationModel.DataTransfer
{
	internal static class FileUriHelper
	{
		private static readonly char[] urlAllowedChars = "-._~".ToCharArray();

		/// <summary>
		/// Encodes a file path to a file:// Url.
		/// While the built-in Uri class can handle this, it does not process files
		/// such as '/home/user/%51.txt' correctly.
		/// </summary>
		/// <param name="path">Path to the file</param>
		/// <returns>file:// url to the file</returns>
		public static string UrlEncode(string path)
		{
			var uri = new StringBuilder();
			foreach (var ch in path)
			{
				if (('a' <= ch && ch <= 'z') || ('A' <= ch && ch <= 'Z') || ('0' <= ch && ch <= '9') ||
					urlAllowedChars.Contains(ch))
				{
					uri.Append(ch);
				}
				else if (ch == Path.DirectorySeparatorChar || ch == Path.AltDirectorySeparatorChar)
				{
					uri.Append('/');
				}
				else
				{
					var bytes = Encoding.UTF8.GetBytes(new[] { ch });
					foreach (var b in bytes)
					{
						uri.Append(CultureInfo.InvariantCulture, $"%{b:X2}");
					}
				}
			}
			return "file://" + uri;
		}
	}
}
