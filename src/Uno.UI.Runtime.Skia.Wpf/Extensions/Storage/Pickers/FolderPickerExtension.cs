#nullable enable

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Uno.UI.Runtime.Skia;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace Uno.Extensions.Storage.Pickers
{
	internal class FolderPickerExtension : IFolderPickerExtension
	{
		private readonly FolderPicker _picker;

		public FolderPickerExtension(FolderPicker owner)
		{
			_picker = owner ?? throw new ArgumentNullException(nameof(owner));
		}

		public async Task<StorageFolder?> PickSingleFolderAsync(CancellationToken token)
		{
			NativeMethods.BROWSEINFO pbi = new NativeMethods.BROWSEINFO();
			pbi.ulFlags = 0x41; //BIF_RETURNONLYFSDIRS | BIF_NEWDIALOGSTYLE
			pbi.pszDisplayName = new string('\0', NativeMethods.MAX_PATH);

			IntPtr pidl = NativeMethods.SHBrowseForFolder(ref pbi);
			if (pidl != IntPtr.Zero)
			{
                try
                {
					string path = NativeMethods.SHGetPathFromIDListLong(pidl);
					if (!string.IsNullOrEmpty(path))
						return new StorageFolder(path);
                }
				finally
                {
					Marshal.FreeCoTaskMem(pidl);
				}
			}

			return null;
		}

		private static class NativeMethods
		{
			internal const int MAX_PATH = 260;
			private const int MAX_LONG_PATH = 32767;

			[DllImport("shell32", EntryPoint ="SHBrowseForFolderW", CharSet=CharSet.Unicode)]
			internal static extern IntPtr SHBrowseForFolder(ref BROWSEINFO lpbi);

			internal static string? SHGetPathFromIDListLong(IntPtr pidl)
            {
				if (pidl == IntPtr.Zero)
					return null;

				int chars = MAX_PATH;

				IntPtr buffer = Marshal.AllocHGlobal(chars * 2);

				try
				{
					while (!SHGetPathFromIDListEx(pidl, buffer, chars * 2, 0))
					{
						chars *= 2;

						if (chars > MAX_LONG_PATH)
							return null;

						buffer = Marshal.ReAllocHGlobal(buffer, (IntPtr)(chars * 2));
					}

					return Marshal.PtrToStringUni(buffer);
				}
				finally
                {
					Marshal.FreeHGlobal(buffer);
                }
			}

			[DllImport("shell32")]
			private static extern bool SHGetPathFromIDListEx(IntPtr pidl, IntPtr pszPath, int len, int flags);

			internal struct BROWSEINFO
			{
				internal IntPtr hwndOwner;
				internal IntPtr pidlRoot;
				internal string pszDisplayName;
				internal string lpszTitle;
				internal uint ulFlags;
				internal IntPtr lpfn;
				internal IntPtr lParam;
				internal int iImage;
			}
		}
	}
}
