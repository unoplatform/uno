#nullable enable

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace Uno.Extensions.Storage.Pickers;

internal partial class FolderPickerExtension : IFolderPickerExtension
{
	private readonly FolderPicker _picker;

	public Guid? SuggestedStartLocation { get; private set; }

	public FolderPickerExtension(FolderPicker owner)
	{
		_picker = owner ?? throw new ArgumentNullException(nameof(owner));
	}

	public Task<StorageFolder?> PickSingleFolderAsync(CancellationToken token)
	{
		var openFolderDialog = new OpenFolderDialog();

		var initialDirectory = GetPickerIdLocationPath() ??
			PickerHelpers.GetInitialDirectory(_picker.SuggestedStartLocation);

		if (initialDirectory is not null)
		{
			openFolderDialog.InitialDirectory = initialDirectory;
		}

		if (openFolderDialog.ShowDialog() == true &&
			!string.IsNullOrEmpty(openFolderDialog.FolderName))
		{
			return Task.FromResult<StorageFolder?>(new StorageFolder(openFolderDialog.FolderName));
		}

		return Task.FromResult<StorageFolder?>(null);
	}

	private string? GetPickerIdLocationPath() =>
		GetPickerLocationIdGuid() is { } guid ?
			NativeMethods.SHGetKnownFolderPath(guid) : default;

	private Guid? GetPickerLocationIdGuid() =>
		_picker.SuggestedStartLocation switch
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

	[UnmanagedFunctionPointer(CallingConvention.Winapi)]
	private delegate uint WndProc(IntPtr hwnd, uint msg, IntPtr lParam, IntPtr lpData);

	private static partial class NativeMethods
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

		[DllImport("shell32")]
		private static extern bool SHGetPathFromIDListEx(IntPtr pidl, IntPtr pszPath, int len, int flags);

		internal static string? SHGetKnownFolderPath(Guid rfid, uint dwFlags = 0, IntPtr hToken = default(IntPtr))
		{
			IntPtr pszPath;
#pragma warning disable CA1806 // Do not ignore method results
			SHGetKnownFolderPath(rfid, dwFlags, hToken, out pszPath);
#pragma warning restore CA1806 // Do not ignore method results

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
	}
}
