#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Uno.UI.NativeElementHosting;
using Uno.UI.RuntimeTests.Helpers;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.DragDrop.Core;
using Windows.Foundation;
using Windows.Storage;

namespace Uno.UI.RuntimeTests.Tests;

// Drives the window's OLE drop target in-process, the way DoDragDrop calls it, with the formats Explorer and Outlook offer.
[TestClass]
[RunsOnUIThread]
[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32)]
public class Given_Win32ExternalFileDrop
{
	private const string Content = "Dropped file content";
	private const ushort CF_HDROP = 15;

	private static readonly string _copiesRoot = Path.Combine(Path.GetTempPath(), "unoplatform-dragdrop");

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26482")]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_Real_Files_Dragged_Then_Original_Paths_And_No_Copies(bool useShellDataObject)
	{
		// Explorer offers real files as CF_HDROP, and as FileGroupDescriptorW and FileContents too.
		// The shell's own data object for the file is what Explorer starts a drag with.
		var sourceDirectory = Directory.CreateTempSubdirectory("uno-dragdrop-test-").FullName;
		try
		{
			var sourcePath = Path.Combine(sourceDirectory, "real.txt");
			File.WriteAllText(sourcePath, Content);
			Func<nint> createDataObject = useShellDataObject
				? () => CreateShellDataObject(sourcePath)
				: () => CreateDataObject(sourcePath, "real.txt", useStream: false);
			var copiesBefore = GetCopies();

			var targets = await TestTargets.Load();
			using (var drag = ExternalDrag.Enter(targets.NotAccepting, createDataObject()))
			{
				drag.Over();
				drag.Leave();
			}
			CollectionAssert.AreEquivalent(copiesBefore, GetCopies(), "Dragging over an element that doesn't accept the drop copied the files.");

			using (var drag = ExternalDrag.Enter(targets.Accepting, createDataObject()))
			{
				drag.Over();
				drag.Drop();
			}
			var items = await targets.WaitForDroppedItems();
			Assert.AreEqual(1, items.Count);
			Assert.AreEqual(sourcePath, items[0].Path, ignoreCase: true);
			CollectionAssert.AreEquivalent(copiesBefore, GetCopies(), "Dropping real files copied them.");
		}
		finally
		{
			Directory.Delete(sourceDirectory, recursive: true);
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26482")]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_Virtual_Files_Dragged_Then_Copied_Only_On_Accepted_Drop(bool useStream)
	{
		// Outlook attachments and items in a zip folder only come as FileGroupDescriptorW and FileContents, usually as a stream.
		var copiesBefore = GetCopies();
		var targets = await TestTargets.Load();

		using (var drag = ExternalDrag.Enter(targets.NotAccepting, CreateDataObject(hdropPath: null, "virtual.txt", useStream)))
		{
			drag.Over();
			drag.Leave();
		}
		CollectionAssert.AreEquivalent(copiesBefore, GetCopies(), "Dragging over an element that doesn't accept the drop copied the files.");

		using (var drag = ExternalDrag.Enter(targets.NotAccepting, CreateDataObject(hdropPath: null, "virtual.txt", useStream)))
		{
			drag.Over();
			drag.Drop();
		}
		CollectionAssert.AreEquivalent(copiesBefore, GetCopies(), "A drop that wasn't accepted copied the files.");

		using (var drag = ExternalDrag.Enter(targets.Accepting, CreateDataObject(hdropPath: null, "virtual.txt", useStream)))
		{
			drag.Over();
			drag.Drop();
		}

		// The app's Drop handler asks for the items after the source's DoDragDrop has returned.
		var items = await targets.WaitForDroppedItems();
		var copies = GetCopies().Except(copiesBefore).ToArray();
		try
		{
			Assert.AreEqual(1, items.Count);
			var file = (StorageFile)items[0];
			Assert.AreEqual("virtual.txt", file.Name);
			Assert.AreEqual(Content, await FileIO.ReadTextAsync(file));
			Assert.AreEqual(1, copies.Length);
			StringAssert.StartsWith(file.Path, copies[0] + Path.DirectorySeparatorChar);
		}
		finally
		{
			foreach (var copy in copies)
			{
				Directory.Delete(copy, recursive: true);
			}
		}
	}

	private static string[] GetCopies() => Directory.Exists(_copiesRoot) ? Directory.GetDirectories(_copiesRoot) : [];

	// The file as CF_HDROP when hdropPath is set, and always as FileGroupDescriptorW and FileContents.
	private static unsafe nint CreateDataObject(string? hdropPath, string virtualName, bool useStream)
	{
		var iid = new Guid("0000010E-0000-0000-C000-000000000046"); // IID_IDataObject
		Assert.AreEqual(0, SHCreateDataObject(0, 0, 0, 0, iid, out var dataObject));
		try
		{
			if (hdropPath is not null)
			{
				// DROPFILES: pFiles, pt, fNC, fWide, then a double-null-terminated list of paths.
				var header = new byte[20];
				BitConverter.GetBytes(header.Length).CopyTo(header, 0);
				BitConverter.GetBytes(1).CopyTo(header, 16);
				SetData(dataObject, CF_HDROP, -1, [.. header, .. Encoding.Unicode.GetBytes(hdropPath + "\0\0")]);
			}

			// FILEGROUPDESCRIPTORW with one FILEDESCRIPTORW, whose name is at offset 72 and size at offsets 64 and 68.
			var content = Encoding.UTF8.GetBytes(Content);
			var descriptor = new byte[4 + 592];
			BitConverter.GetBytes(1).CopyTo(descriptor, 0);
			BitConverter.GetBytes(0x40 /* FD_FILESIZE */).CopyTo(descriptor, 4);
			BitConverter.GetBytes(content.Length).CopyTo(descriptor, 4 + 68);
			Encoding.Unicode.GetBytes(virtualName).CopyTo(descriptor, 4 + 72);
			SetData(dataObject, (ushort)RegisterClipboardFormat("FileGroupDescriptorW"), -1, descriptor);
			SetData(dataObject, (ushort)RegisterClipboardFormat("FileContents"), 0, content, useStream);
			return dataObject;
		}
		catch
		{
			ExternalDrag.CallRelease(dataObject);
			throw;
		}
	}

	private static unsafe void SetData(nint dataObject, ushort format, int index, byte[] data, bool useStream = false)
	{
		if (useStream)
		{
			var stream = SHCreateMemStream(data, (uint)data.Length);
			Assert.AreNotEqual(0, stream);
			var streamFormat = new FORMATETC { cfFormat = format, dwAspect = 1 /* DVASPECT_CONTENT */, lindex = index, tymed = 4 /* TYMED_ISTREAM */ };
			var streamMedium = new STGMEDIUM { tymed = 4, hGlobal = stream };
			var streamResult = ExternalDrag.CallSetData(dataObject, &streamFormat, &streamMedium, release: 1);
			if (streamResult != 0)
			{
				ExternalDrag.CallRelease(stream);
				Assert.Fail($"IDataObject::SetData failed for a stream: 0x{streamResult:X8}");
			}
			return;
		}

		var handle = GlobalAlloc(0x0002 /* GMEM_MOVEABLE */, (nuint)data.Length);
		Assert.AreNotEqual(0, handle);
		Marshal.Copy(data, 0, GlobalLock(handle), data.Length);
		GlobalUnlock(handle);

		var formatEtc = new FORMATETC { cfFormat = format, dwAspect = 1 /* DVASPECT_CONTENT */, lindex = index, tymed = 1 /* TYMED_HGLOBAL */ };
		var medium = new STGMEDIUM { tymed = 1, hGlobal = handle };
		var hResult = ExternalDrag.CallSetData(dataObject, &formatEtc, &medium, release: 1); // The data object owns the handle on success.
		if (hResult != 0)
		{
			GlobalFree(handle);
			Assert.Fail($"IDataObject::SetData failed: 0x{hResult:X8}");
		}
	}

	private static unsafe nint CreateShellDataObject(string path)
	{
		var shellItemIid = new Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE"); // IID_IShellItem
		Assert.AreEqual(0, SHCreateItemFromParsingName(path, 0, shellItemIid, out var shellItem));
		try
		{
			var handler = new Guid("B8C0BD9F-ED24-455C-83E6-D5390C4FE8C4"); // BHID_DataObject
			var dataObjectIid = new Guid("0000010E-0000-0000-C000-000000000046"); // IID_IDataObject
			nint dataObject;
			var hResult = ((delegate* unmanaged[Stdcall]<nint, nint, Guid*, Guid*, nint*, int>)ExternalDrag.Slot(shellItem, 3))(shellItem, 0, &handler, &dataObjectIid, &dataObject);
			Assert.AreEqual(0, hResult, "IShellItem::BindToHandler(BHID_DataObject) failed.");
			return dataObject;
		}
		finally
		{
			ExternalDrag.CallRelease(shellItem);
		}
	}

	private sealed class TestTargets
	{
		private Task<IReadOnlyList<IStorageItem>>? _droppedItems;

		private TestTargets(Border accepting, Border notAccepting)
		{
			Accepting = accepting;
			NotAccepting = notAccepting;
		}

		public Border Accepting { get; }

		public Border NotAccepting { get; }

		// Read in the app's Drop handler, as apps do.
		public async Task<IReadOnlyList<IStorageItem>> WaitForDroppedItems()
		{
			await TestServices.WindowHelper.WaitFor(() => _droppedItems is not null, message: "The drop target didn't get the drop.");
			return await _droppedItems!;
		}

		public static async Task<TestTargets> Load()
		{
			var accepting = new Border { Width = 100, Height = 100, AllowDrop = true, Background = new SolidColorBrush(Colors.DeepSkyBlue) };
			var notAccepting = new Border { Width = 100, Height = 100, Background = new SolidColorBrush(Colors.Gray) };
			var targets = new TestTargets(accepting, notAccepting);
			accepting.DragOver += (_, e) => e.AcceptedOperation = DataPackageOperation.Copy;
			accepting.Drop += (_, e) => targets._droppedItems = e.DataView.GetStorageItemsAsync().AsTask();

			await UITestHelper.Load(new StackPanel { Children = { accepting, notAccepting } });
			return targets;
		}
	}

	private sealed class ExternalDrag : IDisposable
	{
		private const uint MK_LBUTTON = 0x0001;
		private const uint DROPEFFECT_COPY_MOVE_LINK = 0x7;
		private const BindingFlags NonPublicInstance = BindingFlags.Instance | BindingFlags.NonPublic;

		private readonly nint _dropTarget;
		private readonly nint _dataObject;
		private readonly POINTL _point;
		private bool _isOver;

		private ExternalDrag(nint dropTarget, nint dataObject, POINTL point)
		{
			_dropTarget = dropTarget;
			_dataObject = dataObject;
			_point = point;
		}

		// Takes over the reference on the data object.
		public static unsafe ExternalDrag Enter(FrameworkElement target, nint dataObject)
		{
			var window = TestServices.WindowHelper.CurrentTestWindow;
			var hwnd = ((Win32NativeWindow)window.NativeWindow!).Hwnd;

			var coreManager = CoreDragDropManager.GetOrCreateForWindowId(window.AppWindow.Id);
			var extension = GetField(GetField(coreManager, "_manager"), "_hostExtension");
			var dropTarget = (nint)GetField(GetField(extension, "_dropTarget"), "_value"); // ComScope<IDropTarget>
			if (dropTarget == 0)
			{
				CallRelease(dataObject);
				Assert.Fail("The window has no drop target.");
			}

			// DoDragDrop passes screen coordinates in physical pixels.
			var center = target.TransformToVisual(null).TransformPoint(new Point(target.ActualWidth / 2, target.ActualHeight / 2));
			var scale = target.XamlRoot!.RasterizationScale;
			var point = new POINTL { X = (int)(center.X * scale), Y = (int)(center.Y * scale) };
			Assert.IsTrue(ClientToScreen(hwnd, ref point));

			var drag = new ExternalDrag(dropTarget, dataObject, point);
			var effect = DROPEFFECT_COPY_MOVE_LINK;
			var hResult = CallDragEnter(drag._dropTarget, drag._dataObject, MK_LBUTTON, drag._point, &effect);
			drag._isOver = true;
			if (hResult != 0)
			{
				drag.Dispose();
				Assert.Fail($"IDropTarget::DragEnter failed: 0x{hResult:X8}");
			}
			return drag;
		}

		public unsafe void Over()
		{
			var effect = DROPEFFECT_COPY_MOVE_LINK;
			Assert.AreEqual(0, CallDragOver(_dropTarget, MK_LBUTTON, _point, &effect));
		}

		public void Leave()
		{
			if (_isOver)
			{
				_isOver = false;
				Assert.AreEqual(0, CallDragLeave(_dropTarget));
			}
		}

		public unsafe void Drop()
		{
			_isOver = false;
			var effect = DROPEFFECT_COPY_MOVE_LINK;
			Assert.AreEqual(0, CallDrop(_dropTarget, _dataObject, MK_LBUTTON, _point, &effect));
		}

		public void Dispose()
		{
			try
			{
				Leave();
			}
			finally
			{
				CallRelease(_dataObject);
			}
		}

		private static object GetField(object owner, string name)
			=> (owner.GetType().GetField(name, NonPublicInstance) ?? throw new InvalidOperationException($"{owner.GetType().Name} has no field {name}")).GetValue(owner)!;

		public static unsafe void* Slot(nint comObject, int index) => (*(void***)comObject)[index];

		public static unsafe uint CallRelease(nint unknown)
			=> ((delegate* unmanaged[Stdcall]<nint, uint>)Slot(unknown, 2))(unknown);

		public static unsafe int CallSetData(nint dataObject, FORMATETC* format, STGMEDIUM* medium, int release)
			=> ((delegate* unmanaged[Stdcall]<nint, FORMATETC*, STGMEDIUM*, int, int>)Slot(dataObject, 7))(dataObject, format, medium, release);

		private static unsafe int CallDragEnter(nint dropTarget, nint dataObject, uint keys, POINTL point, uint* effect)
			=> ((delegate* unmanaged[Stdcall]<nint, nint, uint, POINTL, uint*, int>)Slot(dropTarget, 3))(dropTarget, dataObject, keys, point, effect);

		private static unsafe int CallDragOver(nint dropTarget, uint keys, POINTL point, uint* effect)
			=> ((delegate* unmanaged[Stdcall]<nint, uint, POINTL, uint*, int>)Slot(dropTarget, 4))(dropTarget, keys, point, effect);

		private static unsafe int CallDragLeave(nint dropTarget)
			=> ((delegate* unmanaged[Stdcall]<nint, int>)Slot(dropTarget, 5))(dropTarget);

		private static unsafe int CallDrop(nint dropTarget, nint dataObject, uint keys, POINTL point, uint* effect)
			=> ((delegate* unmanaged[Stdcall]<nint, nint, uint, POINTL, uint*, int>)Slot(dropTarget, 6))(dropTarget, dataObject, keys, point, effect);
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct POINTL
	{
		public int X;
		public int Y;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct FORMATETC
	{
		public ushort cfFormat;
		public nint ptd;
		public uint dwAspect;
		public int lindex;
		public uint tymed;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct STGMEDIUM
	{
		public uint tymed;
		public nint hGlobal;
		public nint pUnkForRelease;
	}

	[DllImport("shell32.dll")]
	private static extern int SHCreateDataObject(nint pidlFolder, uint cidl, nint apidl, nint pdtInner, in Guid riid, out nint ppv);

	[DllImport("shell32.dll", CharSet = CharSet.Unicode)]
	private static extern int SHCreateItemFromParsingName(string path, nint bindContext, in Guid riid, out nint ppv);

	[DllImport("shlwapi.dll")]
	private static extern nint SHCreateMemStream(byte[] init, uint size);

	[DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern uint RegisterClipboardFormat(string format);

	[DllImport("user32.dll")]
	private static extern bool ClientToScreen(nint hWnd, ref POINTL lpPoint);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern nint GlobalAlloc(uint flags, nuint bytes);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern nint GlobalLock(nint handle);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool GlobalUnlock(nint handle);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern nint GlobalFree(nint handle);
}
