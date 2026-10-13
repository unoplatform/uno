#nullable enable

using System.Collections.Concurrent;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.ApplicationModel.DataTransfer;
using Uno.UI.Runtime.Wayland.Protocols;

namespace Uno.UI.Runtime.Wayland.Tests;

[TestClass]
public sealed class ClipboardTests
{
	[TestMethod]
	public async Task NoInputSerialKeepsLocalCopyAfterPublicationIsCancelled()
	{
		var device = new ClipboardDevice();
		var clipboard = new WaylandClipboardExtension(device, action => action());
		var package = new DataPackage();
		package.SetText("unowned copy");
		clipboard.SetContent(package);
		Assert.AreEqual("unowned copy", await clipboard.GetContent().GetTextAsync());
		device.CancelPendingSelection();
		device.NotifySelection(null);
		Assert.AreEqual("unowned copy", await clipboard.GetContent().GetTextAsync());
		Assert.AreEqual(0, device.Published);
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public async Task RefocusOfferBeforeKeyboardEnterPreservesDeferredCopyOrClear(bool clear)
	{
		new DisplayTests().RequireLibwayland();
		using var peer = new DisplayTests.ProtocolPeer(dataDeviceManager: true);
		using var display = peer.Connect();
		using var manager = (WlDataDeviceManager)display.Bind(WlDataDeviceManager.ProtocolName, 3);
		var device = new ClipboardDevice(manager);
		var clipboard = new WaylandClipboardExtension(device, action => action());
		device.NotifySelection("existing foreign selection");
		var package = new DataPackage();
		package.SetText("new local copy");
		if (clear)
		{
			clipboard.Clear();
		}
		else
		{
			clipboard.SetContent(package);
		}
		// wl_data_device.selection is delivered before wl_keyboard.enter on refocus.
		device.NotifySelection("existing foreign selection");
		Assert.AreEqual(0, device.Published);
		if (clear)
		{
			Assert.AreEqual(0, clipboard.GetContent().AvailableFormats.Count);
		}
		else
		{
			Assert.AreEqual("new local copy", await clipboard.GetContent().GetTextAsync());
		}
		device.Focus();
		Assert.AreEqual(1, device.Published);
		Assert.AreEqual(clear, device.LastSource is null);
		if (!clear)
		{
			device.NotifySelection("own echoed offer");
			Assert.AreEqual("new local copy", await clipboard.GetContent().GetTextAsync());
		}
	}

	[TestMethod]
	public void FocusedForeignSelectionReplacesPublishedClearIncludingEmptySelection()
	{
		var device = new ClipboardDevice();
		var clipboard = new WaylandClipboardExtension(device, action => action());
		device.Focus();
		clipboard.Clear();
		device.NotifySelection("foreign");
		Assert.IsTrue(clipboard.GetContent().Contains(StandardDataFormats.Text));
		device.NotifySelection(null);
		Assert.AreEqual(0, clipboard.GetContent().AvailableFormats.Count);
	}

	[TestMethod]
	[DataRow(false, false)]
	[DataRow(true, false)]
	[DataRow(true, true)]
	public async Task OwnershipLossWhileUnfocusedInvalidatesOnlyOwnedGenerationAndDispatchesNotification(bool newerCopy, bool clear)
	{
		new DisplayTests().RequireLibwayland();
		using var peer = new DisplayTests.ProtocolPeer(dataDeviceManager: true);
		using var display = peer.Connect();
		using var manager = (WlDataDeviceManager)display.Bind(WlDataDeviceManager.ProtocolName, 3);
		var device = new ClipboardDevice(manager);
		var notifications = new ConcurrentQueue<Action>();
		var clipboard = new WaylandClipboardExtension(device, action => notifications.Enqueue(action));
		var observed = 0;
		clipboard.ContentChanged += (_, _) => observed++;
		clipboard.StartContentChanged();
		device.Focus();
		var package = new DataPackage();
		package.SetText("owned copy");
		clipboard.SetContent(package);
		device.Leave();
		if (newerCopy)
		{
			var replacement = new DataPackage();
			replacement.SetText("deferred copy");
			if (clear)
			{
				clipboard.Clear();
			}
			else
			{
				clipboard.SetContent(replacement);
			}
		}
		while (notifications.TryDequeue(out var notification))
		{
			notification();
		}
		var before = observed;
		display.StartDispatch();
		peer.CancelSource();
		Assert.IsTrue(SpinWait.SpinUntil(() => !notifications.IsEmpty, TimeSpan.FromSeconds(5)), "Ownership loss did not enqueue ContentChanged.");
		display.StopDispatch();
		Assert.IsNull(display.DispatchError);
		Assert.AreEqual(before, observed, "Protocol callbacks must not notify on the dispatch thread.");
		while (notifications.TryDequeue(out var notification))
		{
			notification();
		}
		Assert.AreEqual(before + 1, observed);
		if (newerCopy)
		{
			device.NotifySelection(null);
			if (clear)
			{
				Assert.AreEqual(0, clipboard.GetContent().AvailableFormats.Count);
			}
			else
			{
				Assert.AreEqual("deferred copy", await clipboard.GetContent().GetTextAsync());
			}
			display.StartDispatch();
			var published = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			display.Post(() =>
			{
				device.Focus();
				published.SetResult();
			});
			await published.Task.WaitAsync(TimeSpan.FromSeconds(5));
			display.StopDispatch();
			Assert.IsNull(display.DispatchError);
			Assert.AreEqual(2, device.Published);
			Assert.AreEqual(clear, device.LastSource is null);
		}
		else
		{
			Assert.AreEqual(0, clipboard.GetContent().AvailableFormats.Count, "No foreign offer is delivered while unfocused.");
			device.NotifySelection(null);
			Assert.AreEqual(0, clipboard.GetContent().AvailableFormats.Count);
		}
	}

	[TestMethod]
	public void ClearHidesAnOlderForeignOfferBeforeOwnershipIsGranted()
	{
		var device = new ClipboardDevice();
		var clipboard = new WaylandClipboardExtension(device, action => action());
		device.NotifySelection("foreign");
		Assert.IsTrue(clipboard.GetContent().Contains(StandardDataFormats.Text));
		clipboard.Clear();
		Assert.AreEqual(0, clipboard.GetContent().AvailableFormats.Count);
	}

	[TestMethod]
	public async Task NoSeatKeepsLocalTextAndRichFormats()
	{
		var clipboard = new WaylandClipboardExtension(null, action => action());
		Assert.IsNotNull(clipboard.GetContent());
		Assert.AreEqual(0, clipboard.GetContent().AvailableFormats.Count);
		var package = new DataPackage();
		package.SetText("local copy");
		package.SetRtf("{\\rtf1 local copy}");
		clipboard.SetContent(package);
		clipboard.Flush();
		Assert.AreEqual("local copy", await clipboard.GetContent().GetTextAsync());
		Assert.AreEqual("{\\rtf1 local copy}", await clipboard.GetContent().GetRtfAsync());
		clipboard.Clear();
		Assert.IsNotNull(clipboard.GetContent());
		Assert.AreEqual(0, clipboard.GetContent().AvailableFormats.Count);
	}

	[TestMethod]
	public async Task NoSeatKeepsDeferredProviderAndLatestCopy()
	{
		var clipboard = new WaylandClipboardExtension(null, action => action());
		var calls = 0;
		var package = new DataPackage();
		package.SetDataProvider(StandardDataFormats.Text, request =>
		{
			calls++;
			request.SetData("deferred copy");
		});
		clipboard.SetContent(package);
		Assert.AreEqual(0, calls);
		Assert.AreEqual("deferred copy", await clipboard.GetContent().GetTextAsync());
		var replacement = new DataPackage();
		replacement.SetText("latest copy");
		clipboard.SetContent(replacement);
		Assert.AreEqual("latest copy", await clipboard.GetContent().GetTextAsync());
	}

	[TestMethod]
	public async Task LocalChangesNotifyWithoutCompositorEvents()
	{
		var clipboard = new WaylandClipboardExtension(null, action => action());
		var observed = new List<string>();
		clipboard.ContentChanged += (_, _) => observed.Add(clipboard.GetContent().Contains(StandardDataFormats.Text) ? "text" : "empty");
		clipboard.StartContentChanged();
		var package = new DataPackage();
		package.SetText("copy");
		clipboard.SetContent(package);
		Assert.AreEqual("copy", await clipboard.GetContent().GetTextAsync());
		clipboard.Clear();
		clipboard.StopContentChanged();
		clipboard.SetContent(package);
		CollectionAssert.AreEqual(new[] { "text", "empty" }, observed);
	}

	private sealed class ClipboardDevice(WlDataDeviceManager? manager = null) : IWaylandClipboardDevice
	{
		private readonly WaylandSelectionSerial _serial = new();
		private string? _foreignText;
		public event EventHandler? SelectionChanged;
		public bool HasSelection => _foreignText is not null;
		internal int Published { get; private set; }
		internal WlDataSource? LastSource { get; private set; }
		public void Post(Action action) => action();
		public void RequestSelection(Action publish, Action? cancel = null) => _serial.Request(publish, cancel);
		public void CancelPendingSelection() => _serial.CancelPendingSelection();
		public WlDataSource CreateSource() => manager?.CreateDataSource() ?? throw new InvalidOperationException("Selection must not be published without input focus.");
		public void SetSelection(WlDataSource? source)
		{
			LastSource = source;
			Published++;
		}
		public void FillContent(DataPackage package)
		{
			if (_foreignText is { } text)
			{
				package.SetText(text);
			}
		}
		internal void Focus() => _serial.Enter(1);
		internal void Leave() => _serial.Leave();
		internal void NotifySelection(string? text)
		{
			_foreignText = text;
			SelectionChanged?.Invoke(this, EventArgs.Empty);
		}
	}
}
