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

// Drops from other clients onto one window, and drags from it to other clients. All members run on the UI thread.
internal sealed class WaylandDragDropExtension : IImmediateDragDropExtension
{
	private static readonly long _fakePointerId = Pointer.CreateUniqueIdForUnknownPointer();
	private static readonly TimeSpan _acceptancePollInterval = TimeSpan.FromMilliseconds(100);
	// Reported to Uno's drag when our own drag leaves this window again: outside the root's bounds, like a pointer would be.
	private static readonly Point _outsidePosition = new(-1, -1);

	private readonly DragDropManager _manager;
	private readonly WaylandDataDevice _device;
	private Session? _session;
	// Wayland sends motion only while the pointer moves, so a DragOver deferral that completes later is polled.
	private Timer? _acceptancePoll;
	private uint _frameId;

	internal WaylandDragDropExtension(DragDropManager manager, WaylandDataDevice device)
	{
		_manager = manager;
		_device = device;
		device.AddDropTarget(this);
		XamlRootMap.Unregistered += OnXamlRootUnregistered;
	}

	private XamlRoot XamlRoot => _manager.ContentRoot.GetOrCreateXamlRoot();

	private WaylandXamlRootHost? Host => XamlRootMap.GetHostForRoot(XamlRoot) as WaylandXamlRootHost;

	internal WlSurface? Surface => Host?.Surface;

	private CoreDragDropManager CoreDragDropManager => XamlRoot.GetCoreDragDropManager(XamlRoot);

	internal void Enter(WaylandDataOffer offer, uint serial, Point position)
	{
		// Offers before wl_data_offer v3 cannot negotiate actions and always copy.
		var operations = offer.Version >= 3 ? WaylandDataFormats.ToOperations(offer.SourceActions) : DataPackageOperation.Copy;
		Session session;
		// A compositor runs one drag at a time, so while ours is active the offer is ours: the drag Uno already
		// runs for it continues with its original data instead of a copy read back through pipes.
		if (_device.OutgoingDrag is { IsCompleted: false } own && own.XamlRoot == XamlRoot)
		{
			session = new Session(offer, serial, own.Info.SourceId);
			own.Info.RegisterCompletedCallback(session.Complete);
			WaylandDiagnostics.Trace("dnd: own drag entered the origin window");
		}
		else
		{
			var package = new DataPackage();
			WaylandDataFormats.Fill(package, offer);
			session = new Session(offer, serial, _fakePointerId);

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"Drag entered with operations {operations} and formats {string.Join(", ", offer.MimeTypes)}.");
			}

			var info = new CoreDragInfo(Source(session, position), package.GetView(), operations);
			info.RegisterCompletedCallback(session.Complete);
			CoreDragDropManager.DragStarted(info);
		}
		_session = session;
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
		UpdateAcceptance(session, CoreDragDropManager.ProcessMoved(Source(session, position)));
	}

	private void PollAcceptance()
	{
		if (_session is not { } session)
		{
			return;
		}
		// Uno answers the first move over an element with DragEnter and only the next one with DragOver, which is
		// where WinUI apps set the accepted operation. A pointer supplies that next move; the sparse motion events
		// of a compositor drag may not, so while nothing is accepted the poll moves again at the same position.
		// Once accepted, an already processed frame id only reads the operation of the latest DragOver.
		var source = session.Accepted is null or DataPackageOperation.None
			? Source(session, session.Position)
			: new DragEventSource(session.SourceId, 0, session.Position);
		UpdateAcceptance(session, CoreDragDropManager.ProcessMoved(source));
	}

	// Frame ids must grow past the pointer frames Uno's drag already consumed, or the move is ignored as stale.
	private DragEventSource Source(Session session, Point position)
	{
		_frameId = Math.Max(_frameId, Host?.PointerSource.LastFrameId ?? 0) + 1;
		return new DragEventSource(session.SourceId, _frameId, position);
	}

	private void UpdateAcceptance(Session session, DataPackageOperation accepted)
	{
		if (accepted != session.Accepted)
		{
			session.Accepted = accepted;
			// A single action is offered, so the action the compositor selects is the one the target accepted.
			var sourceActions = session.Offer.Version >= 3 ? session.Offer.SourceActions : WaylandDataFormats.DndActionCopy;
			var action = session.IsOwnDrag
				? WaylandDropNegotiation.ChooseOwnDragAction(WaylandDataFormats.ToDndActions(accepted), sourceActions, accepted != DataPackageOperation.None)
				: WaylandDropNegotiation.ChooseAction(WaylandDataFormats.ToDndActions(accepted), sourceActions);
			session.Offer.Accept(session.Serial, action != 0 && session.Offer.MimeTypes is [var mime, ..] ? mime : null, action, action);
			if (WaylandDiagnostics.IsEnabled)
			{
				WaylandDiagnostics.Trace($"dnd: target accepts {accepted} at {session.Position}, offering action {action}");
			}
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
			WaylandDiagnostics.Trace(session.IsOwnDrag ? "dnd: own drag left the window" : "dnd: drag left the window");
			_session = null;
			StopAcceptancePoll();
			session.Leave();
			if (session.IsOwnDrag)
			{
				// Our drag goes on elsewhere and may come back; Uno treats a move outside the window as a recoverable leave.
				CoreDragDropManager.ProcessMoved(Source(session, _outsidePosition));
			}
			else
			{
				CoreDragDropManager.ProcessAborted(session.SourceId);
			}
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
			CoreDragDropManager.ProcessAborted(session.SourceId);
			return;
		}
		var accepted = CoreDragDropManager.ProcessDropped(Source(session, session.Position));

		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"Drop processed, last accepted operation {accepted}.");
		}
	}

	// Called by Uno as soon as its drag starts: the implicit grab is converted into a compositor drag, which is
	// the only way a window covering this one can be dropped on, and onCompleted gets the action the target
	// performed. Content no other client could take (Uno-private formats) stays a managed drag.
	public bool TryStartNativeDrag(CoreDragInfo info, Action<DataPackageOperation> onCompleted)
	{
		// Drags of other clients entering this window run a drag operation too; they already are compositor drags.
		if (info.SourceId == _fakePointerId || Host is not { } host)
		{
			return false;
		}
		var grabSerial = host.Input?.GrabSerial;
		string? reason;
		try
		{
			reason = !_device.SupportsDragOutcome ? "wl_data_device_manager below v3"
				: grabSerial is null ? "no implicit grab"
				: WaylandDataFormats.GetDragMimeTypes(info.Data).Count == 0 ? "nothing to offer"
				: null;
		}
		catch (Exception error)
		{
			// Called while Uno's drag operation is constructed: a failure here must not prevent the managed drag.
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn("The drag content could not be inspected; the drag stays internal.", error);
			}
			return false;
		}
		if (reason is not null)
		{
			if (WaylandDiagnostics.IsEnabled)
			{
				WaylandDiagnostics.Trace($"dnd: drag stays internal ({reason})");
			}
			return false;
		}
		// A drag that never completed (a target holding its offer) would block the compositor's single drag slot.
		_device.OutgoingDrag?.Cancel();
		var drag = new WaylandOutgoingDrag(_device, host, XamlRoot, info, grabSerial!.Value, onCompleted);
		_device.OutgoingDrag = drag;
		// A compositor may silently ignore start_drag when the grab ended just before it, and the drag then ends
		// with Uno's own release; ending the source with Uno's operation keeps the drag from staying registered.
		info.RegisterCompletedCallback(_ => drag.Cancel());
		drag.Start();
		return true;
	}

	// Only reached for drags TryStartNativeDrag declined, which keep their implicit grab and can come back.
	public void StartNativeDrag(CoreDragInfo info, Action<DataPackageOperation> onCompleted) { }

	private void OnXamlRootUnregistered(object? sender, XamlRoot root)
	{
		if (root == XamlRoot)
		{
			XamlRootMap.Unregistered -= OnXamlRootUnregistered;
			StopAcceptancePoll();
			_device.RemoveDropTarget(this);
			if (_device.OutgoingDrag is { } drag && drag.XamlRoot == root)
			{
				drag.Cancel();
			}
		}
	}

	private sealed class Session(WaylandDataOffer offer, uint serial, long sourceId)
		: WaylandDropSession(offer.WhenReadsCompleted, offer.FinishAndDestroy, offer.Destroy)
	{
		public WaylandDataOffer Offer { get; } = offer;
		public uint Serial { get; } = serial;
		// The pointer id Uno's drag operation is keyed by: the fake external pointer, or our own drag's source.
		public long SourceId { get; } = sourceId;
		public bool IsOwnDrag => SourceId != _fakePointerId;
		public Point Position { get; set; }
		// Null until the first motion was answered, so the first answer is always sent.
		public DataPackageOperation? Accepted { get; set; }
	}

	private readonly struct DragEventSource(long id, uint frameId, Point location) : IDragEventSource
	{
		public long Id => id;

		public uint FrameId => frameId;

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
