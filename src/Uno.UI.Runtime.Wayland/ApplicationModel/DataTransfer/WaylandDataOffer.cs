#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Uno.Foundation.Logging;
using Uno.UI.Runtime.Wayland.Native;
using Uno.UI.Runtime.Wayland.Protocols;

namespace Uno.UI.Runtime.Wayland;

// A selection or drag-and-drop offer. Same-MIME getters share a pipe transfer on a worker thread.
internal sealed class WaylandDataOffer
{
	// Longest pause the source may make between writes.
	private static readonly TimeSpan _readTimeout = TimeSpan.FromSeconds(5);

	private readonly WlDataOffer _offer;
	private readonly List<string> _mimeTypes = new();
	private readonly WaylandDataOfferReads _reads;
	private readonly WaylandDropNegotiation _negotiation = new();
	private bool _destroyed;

	// Created by wl_data_device.data_offer on the dispatch thread; the offer events that follow are subscribed here.
	internal WaylandDataOffer(WlDataOffer offer)
	{
		_offer = offer;
		_reads = new(Read);
		offer.Offer += (_, e) =>
		{
			lock (_mimeTypes)
			{
				_mimeTypes.Add(e.MimeType);
			}
		};
		offer.Action += (_, e) => _negotiation.SelectedAction = e.DndAction;
		offer.SourceActions += (_, e) => SourceActions = e.SourceActions;
	}

	internal uint MarkDropped()
	{
		lock (_offer.Display.SyncRoot)
		{
			_negotiation.Dropped = true;
			return _offer.Version < 3 ? (_negotiation.AcceptedMime ? 1u : 0u)
				: _negotiation.CanDrop ? _negotiation.SelectedAction : 0u;
		}
	}

	internal uint Version => _offer.Version;

	// wl_data_device_manager.dnd_action bits offered by a drag source (wl_data_offer v3).
	internal uint SourceActions { get; private set; }

	internal IReadOnlyList<string> MimeTypes
	{
		get
		{
			lock (_mimeTypes)
			{
				return _mimeTypes.ToArray();
			}
		}
	}

	/// <summary>Receives the offered data for <paramref name="mimeType"/>; null if the offer is gone or the source fails to send it.</summary>
	internal Task<byte[]?> ReadAsync(string mimeType, CancellationToken cancellationToken)
		=> _reads.ReadAsync(mimeType, cancellationToken);

	/// <summary>Completes once every read started so far has finished.</summary>
	internal Task<bool> WhenReadsCompleted() => _reads.WhenReadsCompleted();

	private byte[]? Read(string mimeType, CancellationToken cancellationToken)
	{
		var read = -1;
		var write = -1;
		try
		{
			(read, write) = WaylandPipe.Create();
			lock (_offer.Display.SyncRoot)
			{
				if (_destroyed || cancellationToken.IsCancellationRequested)
				{
					return null;
				}
				_offer.Receive(mimeType, write);
			}
			_ = Posix.close(write);
			write = -1;
			var ownedRead = read;
			read = -1;
			var data = WaylandPipe.ReadToEnd(ownedRead, _readTimeout, cancellationToken: cancellationToken);
			if (data is null && !cancellationToken.IsCancellationRequested && this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn($"The Wayland data source for {mimeType} stalled or exceeded {WaylandPipe.MaximumTransferSize} bytes.");
			}
			return data;
		}
		catch (Exception error)
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn($"Failed to receive Wayland data for {mimeType}.", error);
			}
			return null;
		}
		finally
		{
			if (read >= 0)
			{
				_ = Posix.close(read);
			}
			if (write >= 0)
			{
				_ = Posix.close(write);
			}
		}
	}

	// Drag-and-drop feedback: the accepted MIME type (null rejects the drop) and the accepted actions.
	internal void Accept(uint serial, string? mimeType, uint actions, uint preferredAction)
	{
		lock (_offer.Display.SyncRoot)
		{
			if (_destroyed)
			{
				return;
			}
			_offer.Accept(serial, mimeType);
			_negotiation.AcceptedMime = mimeType is not null;
			_negotiation.AcceptedActions = actions;
			if (_offer.Version >= 3)
			{
				_offer.SetActions(actions, preferredAction);
			}
		}
	}

	// Ends a successful drop: the source may now delete moved data.
	internal void FinishAndDestroy(uint completedAction)
	{
		lock (_offer.Display.SyncRoot)
		{
			if (!_destroyed && _offer.Version >= 3 && _negotiation.CanFinish(completedAction))
			{
				_offer.Finish();
			}
			Destroy();
		}
	}

	internal void Destroy()
	{
		lock (_offer.Display.SyncRoot)
		{
			if (!_destroyed)
			{
				_destroyed = true;
				_reads.Dispose();
				_offer.Destroy();
			}
		}
	}
}
