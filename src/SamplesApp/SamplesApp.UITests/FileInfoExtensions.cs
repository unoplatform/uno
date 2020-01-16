using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace SamplesApp.UITests
{
	public static class FileInfoExtensions
	{
		private static bool PlatformRequiresLongPathNormalization { get; } = CheckIfNeedToNormalizeLongPaths();

		// https://docs.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-getfullpathnamea
		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		private static extern uint GetFullPathName(string lpFileName, uint nBufferLength,  StringBuilder lpBuffer, IntPtr lpFilePart);


		private static bool CheckIfNeedToNormalizeLongPaths()
		{
			if (Type.GetType("Mono.Runtime") != null)
			{
				return false;
			}

			if (RuntimeInformation.FrameworkDescription.StartsWith(".NET Core", StringComparison.OrdinalIgnoreCase))
			{
				return false; // no need to normalize long paths on .net core
			}

			switch (Environment.OSVersion.Platform)
			{
				case PlatformID.Win32NT:
				case PlatformID.Win32S:
				case PlatformID.Win32Windows:
					return true;
			}

			return false;
		}


		public static string GetNormalizedLongPath(this string fileName)
		{
			if (PlatformRequiresLongPathNormalization)
			{
				if (fileName.StartsWith(@"\\?"))
				{
					return fileName; // already normalized
				}

				var sb = new StringBuilder(fileName.Length + 1);
				var length = GetFullPathName(fileName, (uint)sb.Capacity, sb, IntPtr.Zero);

				if (length > sb.Capacity)
				{
					// https://docs.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-getfullpathnamea#return-value
					sb.Capacity = (int)length; // increase capacity
					length = GetFullPathName(fileName, (uint)sb.Capacity, sb, IntPtr.Zero);
				}

				if (length < 260)
				{
					return sb.ToString();
				}

				return @"\\?\" + sb;
			}

			return fileName;
		}
	}
}
