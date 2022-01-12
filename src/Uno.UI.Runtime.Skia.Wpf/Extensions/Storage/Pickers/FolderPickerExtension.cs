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
			pbi.ulFlags = 0x41;
			pbi.pszDisplayName = new string('\0', 256);

			IntPtr pidl = NativeMethods.SHBrowseForFolder(ref pbi);
			if (pidl != IntPtr.Zero)
			{
				IntPtr pathp = Marshal.AllocHGlobal(256);
				bool success = NativeMethods.SHGetPathFromIDList(pidl, pathp);
				Marshal.FreeCoTaskMem(pidl);

				if (success)
				{
					string path = Marshal.PtrToStringAnsi(pathp);
					Marshal.FreeHGlobal(pathp);
					return new StorageFolder(path);
				}
			}

			return null;
		}

		private static class NativeMethods
		{
			[DllImport("shell32")]
			internal static extern IntPtr SHBrowseForFolder(ref BROWSEINFO lpbi);

			[DllImport("shell32")]
			internal static extern bool SHGetPathFromIDList(IntPtr pidl, IntPtr pszPath);

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
