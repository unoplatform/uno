#if !NET8_0_OR_GREATER
#nullable enable

using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Threading;
using System;
using Windows.Storage;

namespace Uno.Extensions.Storage.Pickers;

partial class FolderPickerExtension
{
	private const int MAX_LONG_PATH = 32767;

	public Task<StorageFolder?> PickSingleFolderAsync(CancellationToken token)
	{
		NativeMethods.BROWSEINFO pbi = new NativeMethods.BROWSEINFO();
		pbi.ulFlags = 0x41; //BIF_RETURNONLYFSDIRS | BIF_NEWDIALOGSTYLE
		pbi.pszDisplayName = new string('\0', NativeMethods.MAX_PATH);
		var callback = new WndProc(FolderBrowserDialog_BrowseCallbackProc);
		var cp = Marshal.GetFunctionPointerForDelegate(callback);
		GC.KeepAlive(callback);
		pbi.lpfn = cp;
		IntPtr pidl = NativeMethods.SHBrowseForFolder(ref pbi);
		if (pidl != IntPtr.Zero)
		{
			try
			{
				var path = NativeMethods.SHGetPathFromIDListLong(pidl);
				if (!string.IsNullOrEmpty(path))
					return Task.FromResult<StorageFolder?>(new StorageFolder(path!));
			}
			finally
			{
				Marshal.FreeCoTaskMem(pidl);
			}
		}

		return Task.FromResult<StorageFolder?>(null);
	}

	private uint FolderBrowserDialog_BrowseCallbackProc(IntPtr hwnd, uint msg, IntPtr lParam, IntPtr lpData)
	{
		switch (msg)
		{
			case NativeMethods.BFFM_INITIALIZED:
				var selectedPath = GetPickerIdLocationPath() ?? string.Empty;
				var buttonText = _picker.CommitButtonText;
				if (!string.IsNullOrWhiteSpace(selectedPath))
				{
					NativeMethods.SendMessage(new HandleRef(null, hwnd)
						, NativeMethods.BFFM_SETSELECTION
						, 1
						, selectedPath);
				}
				if (!string.IsNullOrWhiteSpace(buttonText))
				{
					NativeMethods.SendMessage(new HandleRef(null, hwnd)
						, NativeMethods.BFFM_SETOKTEXT
						, 1
						, buttonText);
				}
				break;
			case NativeMethods.BFFM_SELCHANGED:
				if (lParam != IntPtr.Zero)
				{
					var path = NativeMethods.SHGetPathFromIDListLong(lParam);
					NativeMethods.SendMessage(new HandleRef(null, hwnd)
						, NativeMethods.BFFM_ENABLEOK
						, 0
						, !string.IsNullOrWhiteSpace(path) ? 1 : 0);
				}
				break;
		}

		return 0;
	}

	static partial class NativeMethods
	{
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

		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		internal static extern IntPtr SendMessage(HandleRef hWnd, uint msg, int wParam, string? lParam);

		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		internal static extern IntPtr SendMessage(HandleRef hWnd, uint msg, int wParam, int lParam);

		[DllImport("shell32", EntryPoint = "SHBrowseForFolderW", CharSet = CharSet.Unicode)]
		internal static extern IntPtr SHBrowseForFolder(ref BROWSEINFO lpbi);

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
#endif
