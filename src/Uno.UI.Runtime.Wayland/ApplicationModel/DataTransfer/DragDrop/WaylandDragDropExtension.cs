#nullable enable

using System;
using System.Threading;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Wayland.Protocols;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.DragDrop;
using Windows.ApplicationModel.DataTransfer.DragDrop.Core;
using Windows.Foundation;

namespace Uno.UI.Runtime.Wayland;

// Drops from other clients onto one window. All members run on the UI thread.
internal sealed class WaylandDragDropExtension : IDragDropExtension
{
	private static readonly long _fakePointerId = Pointer.CreateUniqueIdForUnknownPointer();
	private static readonly TimeSpan _acceptancePollInterval = TimeSpan.FromMilliseconds(100);

	private readonly DragDropManager _manager;
	private readonly WaylandDataDevice _device;
	private Session? _session;
	// Wayland sends motion only while the pointer moves, so a DragOver deferral that completes later is polled.
	private Timer? _acceptancePoll;

	internal WaylandDragDropExtension(DragDropManager manager, WaylandDataDevice device)
	{
		_manager = manager;
		_device = device;
		device.AddDropTarget(this);
		XamlRootMap.Unregistered += OnXamlRootUnregistered;
	}

	private XamlRoot XamlRoot => _manager.ContentRoot.GetOrCreateXamlRoot();

	internal WlSurface? Surface => (XamlRootMap.GetHostForRoot(XamlRoot) as WaylandXamlRootHost)?.Surface;

	private CoreDragDropManager CoreDragDropManager => XamlRoot.GetCoreDragDropManager(XamlRoot);

	internal void Enter(WaylandDataOffer offer, uint serial, Point position)
	{
		var package = new DataPackage();
		WaylandDataFormats.Fill(package, offer);
		// Offers before wl_data_offer v3 cannot negotiate actions and always copy.
		var operations = offer.Version >= 3 ? WaylandDataFormats.ToOperations(offer.SourceActions) : DataPackageOperation.Copy;
		var session = new Session(offer, serial);
		_session = session;

		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"Drag entered with operations {operations} and formats {string.Join(", ", offer.MimeTypes)}.");
		}

		var info = new CoreDragInfo(new DragEventSource(position), package.GetView(), operations);
		info.RegisterCompletedCallback(session.Complete);
		CoreDragDropManager.DragStarted(info);
		Motion(position);
		StopAcceptancePoll();
		_acceptancePoll = new Timer(_ => _device.PostToUI(PollAcceptance), null, _acceptancePollInterval, _acceptancePollInterval);
	}

	internal void Motion(Point position)
	{
		if (_session is not { } session)
		{
			return;
		}
		session.Position = position;
		UpdateAcceptance(session, CoreDragDropManager.ProcessMoved(new DragEventSource(position)));
	}

	private void PollAcceptance()
	{
		if (_session is { } session)
		{
			// An already processed frame id only reads the operation accepted by the latest DragOver.
			UpdateAcceptance(session, CoreDragDropManager.ProcessMoved(new DragEventSource(session.Position, frameId: 0)));
		}
	}

	private void UpdateAcceptance(Session session, DataPackageOperation accepted)
	{
		if (accepted != session.Accepted)
		{
			session.Accepted = accepted;
			// A single action is offered, so the action the compositor selects is the one the target accepted.
			var sourceActions = session.Offer.Version >= 3 ? session.Offer.SourceActions : WaylandDataFormats.DndActionCopy;
			var action = WaylandDropNegotiation.ChooseAction(WaylandDataFormats.ToDndActions(accepted), sourceActions);
			session.Offer.Accept(session.Serial, action != 0 && session.Offer.MimeTypes is [var mime, ..] ? mime : null, action, action);
		}
	}

	private void StopAcceptancePoll()
	{
		_acceptancePoll?.Dispose();
		_acceptancePoll = null;
	}

	internal void Leave()
	{
		if (_session is { } session)
		{
			_session = null;
			StopAcceptancePoll();
			session.Leave();
			CoreDragDropManager.ProcessAborted(_fakePointerId);
		}
	}

	internal void Drop()
	{
		if (_session is not { } session)
		{
			return;
		}
		_session = null;
		StopAcceptancePoll();
		session.Dropped = true;
		session.DropAction = session.Offer.MarkDropped();
		if (session.DropAction == 0)
		{
			session.Leave();
			CoreDragDropManager.ProcessAborted(_fakePointerId);
			return;
		}
		var accepted = CoreDragDropManager.ProcessDropped(new DragEventSource(session.Position));

		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"Drop processed, last accepted operation {accepted}.");
		}
	}

	// Drags leaving the window keep the implicit pointer grab on Wayland, so an internal drag simply
	// continues and can come back. Like X11, dragging to other applications is not supported.
	public void StartNativeDrag(CoreDragInfo info, Action<DataPackageOperation> onCompleted) { }

	private void OnXamlRootUnregistered(object? sender, XamlRoot root)
	{
		if (root == XamlRoot)
		{
			XamlRootMap.Unregistered -= OnXamlRootUnregistered;
			StopAcceptancePoll();
			_device.RemoveDropTarget(this);
		}
	}

	private sealed class Session(WaylandDataOffer offer, uint serial)
		: WaylandDropSession(offer.WhenReadsCompleted, offer.FinishAndDestroy, offer.Destroy)
	{
		public WaylandDataOffer Offer { get; } = offer;
		public uint Serial { get; } = serial;
		public Point Position { get; set; }
		// Null until the first motion was answered, so the first answer is always sent.
		public DataPackageOperation? Accepted { get; set; }
	}

	private readonly struct DragEventSource(Point location, uint? frameId = null) : IDragEventSource
	{
		private static long _nextFrameId;

		public long Id => _fakePointerId;

		public uint FrameId { get; } = frameId ?? (uint)Interlocked.Increment(ref _nextFrameId);

		public (Point location, DragDropModifiers modifier) GetState() => (location, DragDropModifiers.None);

		public Point GetPosition(object? relativeTo)
		{
			if (relativeTo is null)
			{
				return location;
			}

			if (relativeTo is UIElement element)
			{
				return UIElement.GetTransform(element, null).Inverse().Transform(location);
			}

			throw new InvalidOperationException("The relative to must be a UIElement.");
		}
	}
}
