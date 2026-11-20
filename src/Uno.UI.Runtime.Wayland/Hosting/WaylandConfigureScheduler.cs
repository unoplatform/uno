#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;

namespace Uno.UI.Runtime.Wayland;

internal sealed record WaylandWindowConfiguration(
	long Generation, WaylandDecorationLayout Layout, double Scale, uint PreferredScale, int IntegerScale,
	WaylandToplevelStates States, string Title, bool NotifyActivation, uint? Serial, long SerialSequence, bool Configured = true)
{
	internal bool CanPresent => Configured && Serial is not null;
}

// Geometry advances at frame opportunities; state notifications have an independent coalesced UI turn.
internal sealed class WaylandConfigureScheduler(Action<Action> postToUI, Action<WaylandWindowConfiguration> apply, Action<WaylandWindowConfiguration>? applyState = null)
{
	private readonly object _gate = new();
	private WaylandWindowConfiguration? _pending;
	private WaylandWindowConfiguration? _current;
	private bool _posted;
	private bool _inFlight;
	private bool _statePosted;
	private WaylandWindowConfiguration? _pendingState;
	private long? _heldPresentation;
	private long _acknowledgedSequence;

	internal WaylandWindowConfiguration? Current
	{
		get
		{
			lock (_gate)
			{
				return _current;
			}
		}
	}

	internal void Enqueue(WaylandWindowConfiguration configuration)
	{
		bool post;
		bool postState;
		lock (_gate)
		{
			_pendingState = configuration with { NotifyActivation = configuration.NotifyActivation || _pendingState?.NotifyActivation == true };
			postState = applyState is not null && !_statePosted;
			_statePosted |= postState;
			_pending = configuration with { NotifyActivation = configuration.NotifyActivation || _pending?.NotifyActivation == true };
			post = Schedule();
		}
		if (post)
		{
			postToUI(ApplyPending);
		}
		if (postState)
		{
			postToUI(ApplyState);
		}
	}

	// UI thread: applies what is posted now rather than on its UI turn, which then finds nothing left.
	internal void ApplyQueued()
	{
		ApplyPending();
		if (applyState is not null)
		{
			ApplyState();
		}
	}

	private void ApplyState()
	{
		WaylandWindowConfiguration configuration;
		lock (_gate)
		{
			if (!_statePosted)
			{
				return;
			}
			configuration = _pendingState!;
			_pendingState = null;
			_statePosted = false;
		}
		applyState!(configuration);
	}

	private bool Schedule()
	{
		if (!_posted && !_inFlight && _heldPresentation is null && _pending is not null)
		{
			_posted = true;
			return true;
		}
		return false;
	}

	private void ApplyPending()
	{
		WaylandWindowConfiguration configuration;
		lock (_gate)
		{
			if (!_posted)
			{
				return;
			}
			_posted = false;
			if (_heldPresentation is not null || _pending is null)
			{
				return;
			}
			configuration = _pending;
			_pending = null;
			_current = configuration;
			_inFlight = true;
		}
		apply(configuration);
	}

	// A synchronized decoration buffer must be paired with this configure's content, even after a fallback.
	internal void HoldPresentation(long generation)
	{
		lock (_gate)
		{
			if (_current?.Generation == generation)
			{
				_heldPresentation = generation;
			}
		}
	}

	internal void Complete(long generation, bool presented = true)
	{
		bool post;
		lock (_gate)
		{
			if (_current?.Generation != generation || (!presented && _heldPresentation == generation))
			{
				return;
			}
			_heldPresentation = null;
			_inFlight = false;
			post = Schedule();
		}
		if (post)
		{
			postToUI(ApplyPending);
		}
	}

	// State-only acks must not overtake the serial of an unpresented resize.
	internal void AcknowledgeState(WaylandWindowConfiguration configuration, long presentedGeneration, Action<uint> acknowledge)
	{
		lock (_gate)
		{
			if (_current is null || (_current.Generation <= presentedGeneration && _current.SerialSequence <= _acknowledgedSequence))
			{
				Acknowledge(configuration, acknowledge);
			}
		}
	}

	internal bool PreparePresentation(long generation, Action<uint> acknowledge, Func<WaylandWindowConfiguration, bool> prepare,
		[NotNullWhen(true)] out WaylandWindowConfiguration? configuration)
	{
		configuration = Current;
		if (configuration is null || !configuration.CanPresent || configuration.Generation != generation)
		{
			return false;
		}
		// Preparation can attach a synchronized CSD buffer before the content buffer is submitted.
		Acknowledge(configuration, acknowledge);
		return prepare(configuration);
	}

	// Sequence numbers follow receipt order, independent of uint serial wraparound and local scale changes.
	internal void Acknowledge(WaylandWindowConfiguration configuration, Action<uint> acknowledge)
	{
		lock (_gate)
		{
			if (configuration.Serial is { } serial && configuration.SerialSequence > _acknowledgedSequence)
			{
				acknowledge(serial);
				_acknowledgedSequence = configuration.SerialSequence;
			}
		}
	}
}
