#nullable enable

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace Uno.Extensions.Storage.Pickers
{
	internal class FolderPickerExtension : IFolderPickerExtension
	{
		private readonly FolderPicker _picker;

		public Guid? SuggestedStartLocation { get; private set; }

		public FolderPickerExtension(FolderPicker owner)
		{
			_picker = owner ?? throw new ArgumentNullException(nameof(owner));
		}

		public async Task<StorageFolder?> PickSingleFolderAsync(CancellationToken token)
		{
			SuggestedStartLocation = _picker.SuggestedStartLocation switch
			{
				PickerLocationId.Desktop => NativeMethods.KnownFolder.Desktop,
				PickerLocationId.HomeGroup => NativeMethods.KnownFolder.Profile,
				PickerLocationId.Downloads => NativeMethods.KnownFolder.Downloads,
				PickerLocationId.ComputerFolder => NativeMethods.KnownFolder.ComputerFolder,
				PickerLocationId.DocumentsLibrary => NativeMethods.KnownFolder.DocumentsLibrary,
				PickerLocationId.MusicLibrary => NativeMethods.KnownFolder.Music,
				PickerLocationId.Objects3D => NativeMethods.KnownFolder.Objects3D,
				PickerLocationId.PicturesLibrary => NativeMethods.KnownFolder.Pictures,
				PickerLocationId.VideosLibrary => NativeMethods.KnownFolder.Videos,
				PickerLocationId.Unspecified => default,
				_ => throw new NotImplementedException(),
			};
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
						return new StorageFolder(path!);
                }
				finally
                {
					Marshal.FreeCoTaskMem(pidl);
				}
			}

			return null;
		}

		private uint FolderBrowserDialog_BrowseCallbackProc(IntPtr hwnd, uint msg, IntPtr lParam, IntPtr lpData)
		{
			
			switch (msg)
			{
				case NativeMethods.BFFM_INITIALIZED:
					var selectedPath = string.Empty;
					var buttonText = _picker.CommitButtonText;
					if (SuggestedStartLocation is Guid rfid)
					{
						selectedPath = NativeMethods.SHGetKnownFolderPath(rfid);
					}
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

		[UnmanagedFunctionPointer(CallingConvention.Winapi)]
		private delegate uint WndProc(IntPtr hwnd, uint msg, IntPtr lParam, IntPtr lpData);

		private static class NativeMethods
		{
			const uint WM_USER = 0x0400;
			internal const uint BFFM_INITIALIZED = 1;
			internal const uint BFFM_SELCHANGED = 2;
			private const uint BFFM_SETSELECTIONA = (WM_USER + 102);
			private const uint BFFM_SETSELECTIONW = (WM_USER + 103);
			internal const uint BFFM_ENABLEOK = (WM_USER + 101);
			internal const uint BFFM_SETOKTEXT = (WM_USER + 105);
			internal readonly static uint BFFM_SETSELECTION = Marshal.SystemDefaultCharSize == 1
				? BFFM_SETSELECTIONA
				: BFFM_SETSELECTIONW;
			internal const int MAX_PATH = 260;
			private const int MAX_LONG_PATH = 32767;

			internal static class KnownFolder
			{
				public static readonly Guid Desktop = new Guid("B4BFCC3A-DB2C-424C-B029-7FE99A87C641");
				public static readonly Guid Profile = new Guid("5E6C858F-0E22-4760-9AFE-EA3317B67173");
				public static readonly Guid Downloads = new Guid("374DE290-123F-4565-9164-39C4925E467B");
				public static readonly Guid ComputerFolder = new Guid("0AC0837C-BBF8-452A-850D-79D08E667CA7");
				public static readonly Guid DocumentsLibrary = new Guid("7B0DB17D-9CD2-4A93-9733-46CC89022E7C");
				public static readonly Guid Music = new Guid("4BD8D571-6D19-48D3-BE97-422220080E43");
				public static readonly Guid Objects3D = new Guid("31C0DD25-9439-4F12-BF41-7FF4EDA38722");
				public static readonly Guid Pictures = new Guid("33E28130-4E1E-4676-835A-98395C3BC3BB");
				public static readonly Guid Videos = new Guid("18989B1D-99B5-455B-841C-AB7C74E4DDFC");
			}


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

			[DllImport("user32.dll", CharSet = CharSet.Auto)]
			internal static extern IntPtr SendMessage(HandleRef hWnd, uint msg, int wParam, string? lParam);

			[DllImport("user32.dll", CharSet = CharSet.Auto)]
			internal static extern IntPtr SendMessage(HandleRef hWnd, uint msg, int wParam, int lParam);


			internal static string? SHGetKnownFolderPath(Guid rfid, uint dwFlags = 0, IntPtr hToken = default(IntPtr))
			{
				IntPtr pszPath;
				SHGetKnownFolderPath(rfid, dwFlags, hToken, out pszPath);

				try
				{
					return Marshal.PtrToStringUni(pszPath);
				}
				finally
				{
					Marshal.FreeCoTaskMem(pszPath);
				}
			}

			[DllImport("shell32.dll")]
			private static extern int SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid rfid,
				uint dwFlags
				, IntPtr hToken
				, out IntPtr pszPath);

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
