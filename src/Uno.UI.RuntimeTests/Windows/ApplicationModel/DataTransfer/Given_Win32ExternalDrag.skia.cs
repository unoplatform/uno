#nullable enable

using System;
using System.Buffers.Binary;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Private.Infrastructure;
using Uno.UI.Extensions;
using Uno.UI.NativeElementHosting;
using Uno.UI.RuntimeTests.Helpers;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.DragDrop.Core;
using Windows.Foundation;

namespace Uno.UI.RuntimeTests.Tests;

// Drives the window's OLE drop target in-process, the way DoDragDrop calls it, with a shell data object that has a drag image,
// as a drag source sets it up.
[TestClass]
[RunsOnUIThread]
[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32)]
public class Given_Win32ExternalDrag
{
	private const string Caption = "Drop to test";

	public enum ContentOverride
	{
		None,
		Bitmap,
		Hidden,
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25873")]
	public async Task When_Bitmap_Dragged_Over_Then_No_Preview_Is_Generated()
	{
		// WinUI shows the drag source's own image for an external drag, and never builds one from the dragged data.
		// The issue's minimal repro; the None row of When_Drop_Target_Overrides_DragUI_Then_Drag_Visual_Shows_It checks this and more.
		var target = await TestDropTarget.Load(ContentOverride.None);
		using var drag = ExternalDrag.Enter(target.Element, CreateDib(width: 2, height: 3));

		var view = await drag.WaitForDragView();

		Assert.IsNull(view.Content);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25873")]
	[DataRow(ContentOverride.None)]
	[DataRow(ContentOverride.Bitmap)]
	[DataRow(ContentOverride.Hidden)]
	public async Task When_Drop_Target_Overrides_DragUI_Then_Drag_Visual_Shows_It(ContentOverride contentOverride)
	{
		var target = await TestDropTarget.Load(contentOverride);
		using var drag = ExternalDrag.Enter(target.Element, CreateDib(width: 2, height: 3));

		// The DragOver handler takes no deferral, so it has already run within the window's IDropTarget::DragEnter.
		var view = await drag.WaitForDragView();
		Assert.AreEqual(Caption, view.Caption);
		Assert.AreEqual(Visibility.Visible, view.CaptionVisibility);
		Assert.AreEqual(Visibility.Collapsed, view.GlyphVisibility);
		Assert.AreEqual(contentOverride is ContentOverride.Hidden ? Visibility.Collapsed : Visibility.Visible, view.ContentVisibility);
		if (contentOverride is ContentOverride.Bitmap)
		{
			Assert.AreSame(target.Content, view.Content);
		}
		else
		{
			Assert.IsNull(view.Content);
		}

		// The source's image gives way to the content the drop target sets or hides, as in WinUI:
		// the shell's drag-image manager isn't entered at DragEnter, so the image never shows over the window.
		var isReplacedOrHidden = contentOverride is not ContentOverride.None;
		Assert.AreEqual(isReplacedOrHidden, drag.Manager.IsContentReplacedOrHidden(drag.PointerId));
		Assert.IsTrue(drag.HasDropTargetHelper);
		Assert.AreEqual(!isReplacedOrHidden, drag.IsDropTargetHelperEntered);

		drag.Over(); // As DoDragDrop keeps doing.
		Assert.IsTrue(drag.HasDropTargetHelper);
		Assert.AreEqual(!isReplacedOrHidden, drag.IsDropTargetHelperEntered);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25873")]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_Content_Hidden_Then_Shown_Again_Then_Shell_Drag_Image_Manager_Is_Left_Then_Entered_Again(bool drop)
	{
		var target = await TestDropTarget.Load(ContentOverride.None);
		using var drag = ExternalDrag.Enter(target.Element, CreateDib(width: 2, height: 3));
		var view = await drag.WaitForDragView();
		drag.Over();
		Assert.IsTrue(drag.IsDropTargetHelperEntered);

		// The drop target hides the content: the manager is left, and kept for the rest of the drag.
		// The DragOver handler takes no deferral, so it has run when the window's IDropTarget::DragOver returns.
		target.ContentOverride = ContentOverride.Hidden;
		drag.Over();
		Assert.AreEqual(Visibility.Collapsed, view.ContentVisibility);
		Assert.IsFalse(drag.IsDropTargetHelperEntered);
		drag.Over();
		Assert.IsFalse(drag.IsDropTargetHelperEntered);
		Assert.IsTrue(drag.HasDropTargetHelper);

		// The drop target shows the content again: the manager is entered again.
		target.ContentOverride = ContentOverride.None;
		drag.Over();
		Assert.AreEqual(Visibility.Visible, view.ContentVisibility);
		Assert.IsTrue(drag.IsDropTargetHelperEntered);

		if (drop)
		{
			drag.Drop();
			await TestServices.WindowHelper.WaitFor(() => target.IsDropped, message: "The drop target didn't get the drop.");
		}
		else
		{
			drag.Leave();
		}
		Assert.IsFalse(drag.HasDropTargetHelper);
		Assert.IsFalse(drag.IsDropTargetHelperEntered);
		drag.AssertDataObjectReleased();
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25873")]
	[DataRow(false, true)]
	[DataRow(true, true)]
	[DataRow(false, false)]
	[DataRow(true, false)]
	public async Task When_External_Drag_Ends_Then_Shell_Drag_Image_Manager_And_Data_Object_Are_Released(bool drop, bool hasDragImage)
	{
		// Without a drag image, the manager's DragEnter fails, but the manager still keeps the data object until DragLeave or Drop.
		// So they're forwarded anyway, as Chromium and WinForms do.
		var target = await TestDropTarget.Load(ContentOverride.None);
		using var drag = ExternalDrag.Enter(target.Element, CreateDib(width: 2, height: 3), hasDragImage);
		Assert.IsTrue(drag.HasDropTargetHelper);

		await drag.WaitForDragView(); // The drop target has accepted the drag.
		drag.Over();
		Assert.IsTrue(drag.HasDropTargetHelper);
		Assert.IsTrue(drag.IsDropTargetHelperEntered);

		if (drop)
		{
			drag.Drop();
			Assert.IsFalse(drag.HasDropTargetHelper);
			await TestServices.WindowHelper.WaitFor(() => target.IsDropped, message: "The drop target didn't get the drop.");
		}
		else
		{
			drag.Leave();
			Assert.IsFalse(drag.HasDropTargetHelper);
		}
		drag.AssertDataObjectReleased();
	}

	private static byte[] CreateDib(int width, int height)
	{
		const int HeaderSize = 40; // BITMAPINFOHEADER, 32-bit BI_RGB
		var dib = new byte[HeaderSize + width * height * 4];
		BinaryPrimitives.WriteInt32LittleEndian(dib.AsSpan(0), HeaderSize); // biSize
		BinaryPrimitives.WriteInt32LittleEndian(dib.AsSpan(4), width);
		BinaryPrimitives.WriteInt32LittleEndian(dib.AsSpan(8), height);
		BinaryPrimitives.WriteUInt16LittleEndian(dib.AsSpan(12), 1); // biPlanes
		BinaryPrimitives.WriteUInt16LittleEndian(dib.AsSpan(14), 32); // biBitCount
		BinaryPrimitives.WriteInt32LittleEndian(dib.AsSpan(20), width * height * 4); // biSizeImage
		return dib;
	}

	private sealed class TestDropTarget
	{
		private TestDropTarget(Border element, BitmapImage content)
		{
			Element = element;
			Content = content;
		}

		public Border Element { get; }

		public BitmapImage Content { get; }

		public bool IsDropped { get; private set; }

		public ContentOverride ContentOverride { get; set; }

		public static async Task<TestDropTarget> Load(ContentOverride contentOverride)
		{
			var element = new Border
			{
				Width = 200,
				Height = 200,
				AllowDrop = true,
				Background = new SolidColorBrush(Colors.DeepSkyBlue),
			};
			var target = new TestDropTarget(element, new BitmapImage()) { ContentOverride = contentOverride };
			element.DragOver += (_, e) =>
			{
				e.AcceptedOperation = DataPackageOperation.Copy;
				e.DragUIOverride.Caption = Caption;
				e.DragUIOverride.IsGlyphVisible = false;
				e.DragUIOverride.IsContentVisible = target.ContentOverride is not ContentOverride.Hidden;
				if (target.ContentOverride is ContentOverride.Bitmap)
				{
					e.DragUIOverride.SetContentFromBitmapImage(target.Content);
				}
			};
			element.Drop += (_, _) => target.IsDropped = true;

			await UITestHelper.Load(element);
			return target;
		}
	}

	private sealed class ExternalDrag : IDisposable
	{
		private const uint MK_LBUTTON = 0x0001;
		private const uint DROPEFFECT_COPY_MOVE_LINK = 0x7;
		private const ushort CF_DIB = 8;
		private const BindingFlags NonPublicInstance = BindingFlags.Instance | BindingFlags.NonPublic;

		private readonly object _extension;
		private readonly nint _dropTarget;
		private readonly nint _dataObject;
		private readonly POINTL _point;
		private uint _dataObjectReferenceCountBeforeEnter;
		private bool _isOver;

		private ExternalDrag(DragDropManager manager, object extension, nint dropTarget, nint dataObject, POINTL point)
		{
			Manager = manager;
			_extension = extension;
			_dropTarget = dropTarget;
			_dataObject = dataObject;
			_point = point;
		}

		public DragDropManager Manager { get; }

		public long PointerId => (long)FindField(_extension.GetType(), "_fakePointerId", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null)!;

		public unsafe bool HasDropTargetHelper => System.Reflection.Pointer.Unbox(GetField(_extension, "_dropTargetHelper")) != null;

		public bool IsDropTargetHelperEntered => (bool)GetField(_extension, "_isDropTargetHelperEntered");

		public static unsafe ExternalDrag Enter(FrameworkElement target, byte[] dib, bool hasDragImage = true)
		{
			var window = TestServices.WindowHelper.CurrentTestWindow;
			var hwnd = ((Win32NativeWindow)window.NativeWindow!).Hwnd;

			var coreManager = CoreDragDropManager.GetOrCreateForWindowId(window.AppWindow.Id);
			var manager = (DragDropManager)GetField(coreManager, "_manager");
			var extension = GetField(manager, "_hostExtension");
			var dropTarget = (nint)GetField(GetField(extension, "_dropTarget"), "_value"); // ComScope<IDropTarget>
			Assert.AreNotEqual(0, dropTarget);

			// DoDragDrop passes screen coordinates in physical pixels.
			var center = target.TransformToVisual(null).TransformPoint(new Point(target.ActualWidth / 2, target.ActualHeight / 2));
			var scale = target.XamlRoot!.RasterizationScale;
			var point = new POINTL { X = (int)(center.X * scale), Y = (int)(center.Y * scale) };
			Assert.IsTrue(ClientToScreen(hwnd, ref point));

			var drag = new ExternalDrag(manager, extension, dropTarget, CreateDataObject(dib, hasDragImage), point);
			drag._dataObjectReferenceCountBeforeEnter = GetReferenceCount(drag._dataObject);
			var effect = DROPEFFECT_COPY_MOVE_LINK;
			var hResult = CallDragEnter(drag._dropTarget, drag._dataObject, MK_LBUTTON, drag._point, &effect);
			drag._isOver = true;
			if (hResult != 0)
			{
				drag.Dispose(); // Ends whatever the drop target started, and releases the data object.
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

		public async Task<DragView> WaitForDragView()
		{
			DragView? view = null;
			await TestServices.WindowHelper.WaitFor(
				() => (view = TestServices.WindowHelper.XamlRoot.VisualTree.RootElement.FindFirstDescendant<DragView>()) is { Caption: Caption },
				message: "The drag visual didn't get the drop target's caption.");
			return view!;
		}

		public void AssertDataObjectReleased()
			=> Assert.AreEqual(
				_dataObjectReferenceCountBeforeEnter,
				GetReferenceCount(_dataObject),
				"The drop target kept a reference to the data object after the drag ended.");

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

		private static unsafe nint CreateDataObject(byte[] dib, bool hasDragImage)
		{
			var iid = new Guid("0000010E-0000-0000-C000-000000000046"); // IID_IDataObject
			Assert.AreEqual(0, SHCreateDataObject(0, 0, 0, 0, iid, out var dataObject));

			var handle = GlobalAlloc(0x0002 /* GMEM_MOVEABLE */, (nuint)dib.Length);
			Assert.AreNotEqual(0, handle);
			var memory = GlobalLock(handle);
			Marshal.Copy(dib, 0, memory, dib.Length);
			GlobalUnlock(handle);

			var format = new FORMATETC { cfFormat = CF_DIB, dwAspect = 1 /* DVASPECT_CONTENT */, lindex = -1, tymed = 1 /* TYMED_HGLOBAL */ };
			var medium = new STGMEDIUM { tymed = 1, hGlobal = handle };
			var hResult = CallSetData(dataObject, &format, &medium, release: 1); // The data object owns the handle on success.
			if (hResult != 0)
			{
				GlobalFree(handle);
				CallRelease(dataObject);
				Assert.Fail($"IDataObject::SetData failed: 0x{hResult:X8}");
			}

			hResult = hasDragImage ? SetDragImage(dataObject) : 0;
			if (hResult != 0)
			{
				CallRelease(dataObject);
				Assert.Fail($"Setting the drag image failed: 0x{hResult:X8}");
			}
			return dataObject;
		}

		// As a drag source does through the shell's IDragSourceHelper. Without a drag image, IDropTargetHelper::DragEnter fails.
		private static unsafe int SetDragImage(nint dataObject)
		{
			var clsid = new Guid("4657278A-411B-11D2-839A-00C04FD918D0"); // CLSID_DragDropHelper
			var iid = new Guid("DE5BF786-477A-11D2-839D-00C04FD918D0"); // IID_IDragSourceHelper
			var hResult = CoCreateInstance(clsid, 0, 1 /* CLSCTX_INPROC_SERVER */, iid, out var sourceHelper);
			if (hResult != 0)
			{
				return hResult;
			}

			try
			{
				const int Size = 16;
				var pixels = new uint[Size * Size];
				Array.Fill(pixels, 0xFF1E90FFu); // Opaque
				nint bitmap;
				fixed (uint* bits = pixels)
				{
					bitmap = CreateBitmap(Size, Size, 1, 32, bits);
				}
				if (bitmap == 0)
				{
					return Marshal.GetHRForLastWin32Error();
				}

				var image = new SHDRAGIMAGE { Width = Size, Height = Size, OffsetX = Size / 2, OffsetY = Size / 2, Bitmap = bitmap, ColorKey = 0xFFFFFFFF /* CLR_NONE */ };
				hResult = CallInitializeFromBitmap(sourceHelper, &image, dataObject);
				if (hResult != 0)
				{
					DeleteObject(bitmap); // The shell only owns the bitmap on success.
				}
				return hResult;
			}
			finally
			{
				CallRelease(sourceHelper);
			}
		}

		private static object GetField(object owner, string name) => FindField(owner.GetType(), name, NonPublicInstance).GetValue(owner)!;

		private static FieldInfo FindField(Type type, string name, BindingFlags flags)
			=> type.GetField(name, flags) ?? throw new InvalidOperationException($"{type.Name} has no field {name}");

		// AddRef and Release return the new reference count, a value that COM documents for test purposes only.
		private static uint GetReferenceCount(nint unknown)
		{
			CallAddRef(unknown);
			return CallRelease(unknown);
		}

		private static unsafe void* Slot(nint comObject, int index) => (*(void***)comObject)[index];

		private static unsafe uint CallAddRef(nint unknown)
			=> ((delegate* unmanaged[Stdcall]<nint, uint>)Slot(unknown, 1))(unknown);

		private static unsafe uint CallRelease(nint unknown)
			=> ((delegate* unmanaged[Stdcall]<nint, uint>)Slot(unknown, 2))(unknown);

		private static unsafe int CallSetData(nint dataObject, FORMATETC* format, STGMEDIUM* medium, int release)
			=> ((delegate* unmanaged[Stdcall]<nint, FORMATETC*, STGMEDIUM*, int, int>)Slot(dataObject, 7))(dataObject, format, medium, release);

		private static unsafe int CallInitializeFromBitmap(nint sourceHelper, SHDRAGIMAGE* image, nint dataObject)
			=> ((delegate* unmanaged[Stdcall]<nint, SHDRAGIMAGE*, nint, int>)Slot(sourceHelper, 3))(sourceHelper, image, dataObject);

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

	[StructLayout(LayoutKind.Sequential)]
	private struct SHDRAGIMAGE
	{
		public int Width;
		public int Height;
		public int OffsetX;
		public int OffsetY;
		public nint Bitmap;
		public uint ColorKey;
	}

	[DllImport("shell32.dll")]
	private static extern int SHCreateDataObject(nint pidlFolder, uint cidl, nint apidl, nint pdtInner, in Guid riid, out nint ppv);

	[DllImport("ole32.dll")]
	private static extern int CoCreateInstance(in Guid clsid, nint outer, uint context, in Guid iid, out nint ppv);

	[DllImport("gdi32.dll", SetLastError = true)]
	private static extern unsafe nint CreateBitmap(int width, int height, uint planes, uint bitsPerPixel, void* bits);

	[DllImport("gdi32.dll")]
	private static extern bool DeleteObject(nint handle);

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
