#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Hosting;
using Uno.UI.Xaml.Controls.Extensions;
using Uno.UI.Runtime.Wayland.Protocols;

namespace Uno.UI.Runtime.Wayland;

/// <summary>
/// zwp_text_input_v3 implementation of <see cref="IImeTextBoxExtension"/>. The compositor routes
/// enabled text input to its input method; keys the input method does not consume still arrive
/// through wl_keyboard, so keyboard text and committed text are never both delivered.
/// </summary>
internal sealed class WaylandImeTextBoxExtension : IImeTextBoxExtension
{
	private const int MaximumCommittedStates = 64;
	private const uint ChangeCauseInputMethod = 0;
	private const uint ChangeCauseOther = 1;

	private readonly IWaylandTextInput _textInput;
	private readonly Action<Action> _postToUI;

	// Under the display lock: double-buffered state, applied at done.
	private readonly WaylandTextInputBatch _pending = new();

	// Under the display lock: the state of each commit, keyed by its serial, so a done
	// applies to the session and surrounding text it was computed for.
	private readonly WaylandDisplay _display;
	private readonly WaylandTextInputStateHistory<IImeSessionHost> _committedStates = new(MaximumCommittedStates);
	private uint _commitSerial;
	private IImeSessionHost? _sessionHost;
	private long _session;

	// UI thread.
	private WlSurface? _focusedSurface;
	private IImeSessionHost? _host;
	private WlSurface? _hostSurface;
	private int _compositionStart;
	private int _compositionBaseLength;
	private int _compositionLength;
	private long _textRevision;
	private string _revisionText = string.Empty;
	private bool _enabled;
	private bool _supportsSurrounding;
	private bool _isComposing;
	private bool _applyingInputMethodChange;
	// State requests wait for a done that matches the last commit.
	private bool _deferState;
	private bool _stateDirty;
	private WaylandTextInputText.SurroundingText? _surroundingText;
	private (WaylandTextInputText.SurroundingText Surrounding, string Text, (int X, int Y, int Width, int Height)? Caret, (uint Hint, uint Purpose) Content)? _lastSentState;

	internal WaylandImeTextBoxExtension(IWaylandTextInput textInput, WaylandDisplay display, Action<Action> postToUI, Action? onTextInput = null)
	{
		_display = display;
		_postToUI = postToUI;
		using (display.Lock())
		{
			_textInput = textInput;
			_textInput.Enter += (_, e) => _postToUI(() => OnEnter(e.Surface));
			_textInput.Leave += (_, e) => _postToUI(() => OnLeave(e.Surface));
			_textInput.PreeditString += (_, e) =>
			{
				if (!string.IsNullOrEmpty(e.Text))
				{
					onTextInput?.Invoke();
				}
				_pending.Preedit = e.Text;
				_pending.PreeditCursor = e.CursorBegin;
			};
			_textInput.CommitString += (_, e) =>
			{
				if (!string.IsNullOrEmpty(e.Text))
				{
					onTextInput?.Invoke();
				}
				_pending.Commit = e.Text;
			};
			_textInput.DeleteSurroundingText += (_, e) =>
			{
				_pending.DeleteBefore = e.BeforeLength;
				_pending.DeleteAfter = e.AfterLength;
			};
			_textInput.Done += OnDone;
		}
	}

	internal static WaylandImeTextBoxExtension? TryCreate(WaylandConnection connection)
	{
		using (connection.Display.Lock())
		{
			return connection.TextInputManager is { } manager && connection.Seat is { } seat
				? new WaylandImeTextBoxExtension(new WaylandTextInput(manager.GetTextInput(seat)), connection.Display, connection.PostToUI, connection.NotifyTextInputReceived)
				: null;
		}
	}

	public bool IsComposing => _isComposing;

	public event EventHandler? CompositionStarted;
	public event EventHandler<ImeCompositionEventArgs>? CompositionUpdated;
	public event EventHandler<ImeCompositionEventArgs>? CompositionCompleted;
	public event EventHandler? CompositionEnded;

	public void StartImeSession(IImeSessionHost host, ImeSessionActivation activation)
	{
		if (_host is not null)
		{
			EndImeSession();
		}
		_host = host;
		_revisionText = host.Text ?? string.Empty;
		_hostSurface = host.XamlRoot is { } xamlRoot && XamlRootMap.GetHostForRoot(xamlRoot) is WaylandXamlRootHost rootHost ? rootHost.Surface : null;
		_isComposing = false;
		if (_hostSurface is not null && _hostSurface == _focusedSurface)
		{
			Enable();
		}
	}

	public void UpdateImeSession(IImeSessionHost host, ImeSessionUpdate update) => UpdateState(host);

	public Task<IReadOnlyList<string>> GetLinguisticAlternativesAsync(string compositionText, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
	}

	public event EventHandler<ImePartialCompositionEventArgs>? CompositionPartiallyCommitted
	{
		add { }
		remove { }
	}

	public event EventHandler<ImeCompositionEventArgs>? CompositionCanceled;

	public event EventHandler<ImeCandidateWindowBoundsChangedEventArgs>? CandidateWindowBoundsChanged
	{
		add { }
		remove { }
	}

	public void EndImeSession()
	{
		if (_isComposing)
		{
			// Losing focus keeps the composed text, as on WinUI.
			_isComposing = false;
			CompositionEnded?.Invoke(this, EventArgs.Empty);
		}
		Disable();
		_host = null;
		_hostSurface = null;
		_surroundingText = null;
	}

	// Sends the surrounding text and caret rectangle of the focused TextBox after it changed.
	internal void UpdateState(IImeSessionHost host)
	{
		if (host != _host || _applyingInputMethodChange)
		{
			return;
		}
		UpdateTextRevision(host);
		if (_isComposing && !host.IsComposing)
		{
			// An external edit canceled the host's preedit; its span no longer belongs to this session.
			Disable();
			if (_hostSurface == _focusedSurface && _focusedSurface is not null)
			{
				Enable();
			}
			return;
		}
		if (!host.CanAcceptTextInput)
		{
			Disable();
			return;
		}
		if (!_enabled && _hostSurface == _focusedSurface && _focusedSurface is not null)
		{
			Enable();
		}
		else
		{
			SendState(host, ChangeCauseOther);
		}
	}

	private void CommitState()
	{
		var state = _enabled && _host is { } host
			? new WaylandTextInputStateHistory<IImeSessionHost>.State(_session, host, _supportsSurrounding ? _surroundingText : null, GetTextWithoutPreedit(host), _textRevision)
			: (WaylandTextInputStateHistory<IImeSessionHost>.State?)null;
		using (_display.Lock())
		{
			_commitSerial = unchecked(_commitSerial + 1);
			if (state is { } committedState)
			{
				_committedStates.Record(_commitSerial, committedState);
			}
		}
		_textInput.Commit();
	}

	private void Disable()
	{
		using (_display.Lock())
		{
			_session++;
			_sessionHost = null;
			_committedStates.Clear();
			_pending.Disable();
		}
		if (_isComposing)
		{
			_isComposing = false;
			CompositionEnded?.Invoke(this, EventArgs.Empty);
		}
		_deferState = _stateDirty = false;
		_lastSentState = null;
		if (_enabled)
		{
			_enabled = false;
			_textInput.Disable();
			CommitState();
			WaylandDiagnostics.Trace("text-input: disabled");
		}
	}

	private void SendState(IImeSessionHost host, uint cause)
	{
		if (!_enabled || host != _host)
		{
			return;
		}

		if (_deferState)
		{
			_stateDirty = true;
			return;
		}
		var surrounding = CalculateSurrounding(host);
		if (_supportsSurrounding && !surrounding.IsSupported)
		{
			// Support is fixed for an enable/disable context; reset it rather than send a false anchor.
			Disable();
			Enable();
			return;
		}
		var caretRectangle = GetCaretRectangle(host);
		var content = WaylandTextInputContent.Get(
			host.InputScope?.Names.Count > 0 ? host.InputScope.Names[0].NameValue : Microsoft.UI.Xaml.Input.InputScopeNameValue.Default,
			host.IsTextPredictionEnabled, host.IsSpellCheckEnabled, host.AcceptsReturn);
		var state = (surrounding, GetTextWithoutPreedit(host), caretRectangle, content);
		if (_lastSentState == state)
		{
			_stateDirty = false;
			return;
		}
		_textInput.SetTextChangeCause(cause);
		_textInput.SetContentType(content.Hint, content.Purpose);
		_surroundingText = surrounding;
		if (_supportsSurrounding)
		{
			_textInput.SetSurroundingText(surrounding.Text, surrounding.Cursor, surrounding.Anchor);
		}

		if (caretRectangle is { } caret)
		{
			_textInput.SetCursorRectangle(caret.X, caret.Y, caret.Width, caret.Height);
		}
		CommitState();
		_lastSentState = state;
		_stateDirty = false;
	}

	private void Enable()
	{
		if (_host is not { CanAcceptTextInput: true } host)
		{
			return;
		}
		_supportsSurrounding = CalculateSurrounding(host).IsSupported;
		// Enabling again resets the input method state for the newly focused TextBox.
		_enabled = true;
		using (_display.Lock())
		{
			_pending.Begin(unchecked(_commitSerial + 1));
			_sessionHost = host;
		}
		_lastSentState = null;
		_textInput.Enable();
		WaylandDiagnostics.Trace("text-input: enabled");
		SendState(host, ChangeCauseOther);
	}

	// Old batches still belong to this focus session, but their deletion offsets are stale.
	private void ResynchronizeState(IImeSessionHost host)
	{
		var supportsSurrounding = CalculateSurrounding(host).IsSupported;
		using (_display.Lock())
		{
			_committedStates.Clear();
			// Reserve the disable commit before publishing the new surrounding-text boundary.
			_commitSerial = unchecked(_commitSerial + 1);
			_pending.Resynchronize(unchecked(_commitSerial + 1));
		}
		_deferState = _stateDirty = false;
		_lastSentState = null;
		_enabled = false;
		_textInput.Disable();
		_textInput.Commit();
		_supportsSurrounding = supportsSurrounding;
		_enabled = true;
		_textInput.Enable();
		SendState(host, ChangeCauseOther);
	}

	private void OnEnter(WlSurface surface)
	{
		_focusedSurface = surface;
		WaylandDiagnostics.Trace("text-input: enter");
		if (_hostSurface == surface)
		{
			Enable();
		}
	}

	private void OnLeave(WlSurface surface)
	{
		if (_focusedSurface != surface)
		{
			return;
		}
		WaylandDiagnostics.Trace("text-input: leave");
		_focusedSurface = null;
		// Leave ends the enabled state; the compositor ignores requests until the next enter.
		_enabled = false;
		OnPreeditChanged(null, 0);
		Disable();
		_surroundingText = null;
	}

	// Dispatch thread.
	private void OnDone(object? sender, ZwpTextInputV3.DoneEventArgs args)
	{
		Action? apply = null;
		using (_display.Lock())
		{
			var batch = _pending.Take(args.Serial, _commitSerial);
			if (batch.Disposition != WaylandTextInputBatchDisposition.Reject && _sessionHost is { } host)
			{
				var state = _committedStates.GetStateOrCurrent(args.Serial, _session, host);
				apply = () => Apply(args.Serial, state.Session, state.Host, state.Surrounding, state.Text, state.Revision,
					batch.Preedit, batch.PreeditCursor, batch.Commit, batch.DeleteBefore, batch.DeleteAfter);
			}
		}
		if (apply is not null)
		{
			_postToUI(apply);
		}
	}

	// Applies a done event in the order the protocol specifies: delete surrounding text (dropping the preedit
	// first), insert the commit string, then show the new preedit. A preedit update alone keeps the composition.
	private void Apply(uint serial, long session, IImeSessionHost host, WaylandTextInputText.SurroundingText? surrounding, string text, long revision, string? preedit, int preeditCursor, string? commit, uint deleteBefore, uint deleteAfter)
	{
		if (!_enabled || session != _session || host != _host || !host.CanAcceptTextInput)
		{
			return;
		}
		bool oldContext;
		using (_display.Lock())
		{
			// A resync after done was posted also makes this batch's deletion stale.
			oldContext = _pending.GetDisposition(serial, _commitSerial) == WaylandTextInputBatchDisposition.ApplyWithoutDeletion;
			if (oldContext)
			{
				deleteBefore = deleteAfter = 0;
			}
			if (_committedStates.TryGetValue(serial, out var state))
			{
				surrounding = state.Surrounding;
				text = state.Text;
				revision = state.Revision;
			}
		}
		var deleting = deleteBefore != 0 || deleteAfter != 0;
		var reconcileDeletion = deleting && (surrounding is not { IsSupported: true } || revision != _textRevision || text != GetTextWithoutPreedit(host));
		var wasComposing = _isComposing;
		_applyingInputMethodChange = true;
		try
		{
			if (reconcileDeletion)
			{
				// Only deletion needs the old surrounding-text snapshot; commit and preedit still apply.
				WaylandDiagnostics.Trace("text-input: stale deletion, resetting");
				deleting = false;
				OnPreeditChanged(null, 0);
				ResynchronizeState(host);
			}
			_deferState = !oldContext && !reconcileDeletion && serial != _commitSerial;
			if (deleting && _isComposing)
			{
				OnPreeditChanged(null, 0);
			}
			if (deleting && surrounding is { IsSupported: true } snapshot)
			{
				DeleteSurroundingText(host, snapshot, deleteBefore, deleteAfter);
			}
			if (commit is not null)
			{
				OnCommittedText(commit);
			}
			// A new preedit replaces the selection; the surrounding text excludes it and keeps the caret at its start.
			OnPreeditChanged(preedit, preeditCursor);
			_surroundingText = CalculateSurrounding(host);
		}
		finally
		{
			_revisionText = host.Text ?? string.Empty;
			_applyingInputMethodChange = false;
		}
		var currentText = GetTextWithoutPreedit(host);
		using (_display.Lock())
		{
			if (serial == _commitSerial)
			{
				_committedStates.RetainOnly(serial);
			}
			if (_committedStates.TryGetValue(serial, out _))
			{
				_committedStates.Record(serial, new(session, host, _supportsSurrounding ? _surroundingText : null, currentText, _textRevision));
			}
		}
		// A preedit-only update leaves the surrounding text unchanged; its start and end move the caret.
		if (deleting || commit is not null || wasComposing != _isComposing || _stateDirty)
		{
			SendState(host, ChangeCauseInputMethod);
		}
	}

	private void UpdateTextRevision(IImeSessionHost host)
	{
		var text = host.Text ?? string.Empty;
		if (!_applyingInputMethodChange && text != _revisionText)
		{
			_revisionText = text;
			_textRevision++;
		}
	}

	private string GetTextWithoutPreedit(IImeSessionHost host)
	{
		var text = host.Text ?? string.Empty;
		return _isComposing ? text.Remove(_compositionStart, _compositionLength) : text;
	}

	private WaylandTextInputText.SurroundingText CalculateSurrounding(IImeSessionHost host)
	{
		var text = host.Text ?? string.Empty;
		if (_isComposing)
		{
			var start = _compositionStart;
			text = text.Remove(start, _compositionLength);
			return WaylandTextInputText.GetSurroundingText(text, start, start);
		}
		var end = host.SelectionStart + host.SelectionLength;
		var (cursor, anchor) = host.IsBackwardSelection ? (host.SelectionStart, end) : (end, host.SelectionStart);
		return WaylandTextInputText.GetSurroundingText(text, cursor, anchor);
	}

	private static void DeleteSurroundingText(IImeSessionHost host, WaylandTextInputText.SurroundingText surrounding, uint beforeLength, uint afterLength)
	{
		var (before, after) = WaylandTextInputText.GetDeletionRanges(surrounding, beforeLength, afterLength);
		var text = host.Text ?? string.Empty;
		if (!host.CanAcceptTextInput || after.Start + after.Length > text.Length)
		{
			return;
		}
		var selectionLength = after.Start - before.Start - before.Length;
		host.UpdateTextFromNative(text.Remove(after.Start, after.Length).Remove(before.Start, before.Length), before.Start, selectionLength);
	}

	internal bool TryCommitKeyboardText(IImeSessionHost host, string text)
	{
		if (host != _host)
		{
			return false;
		}
		_applyingInputMethodChange = true;
		try
		{
			OnCommittedText(text);
			_surroundingText = CalculateSurrounding(host);
		}
		finally
		{
			_revisionText = host.Text ?? string.Empty;
			_applyingInputMethodChange = false;
		}
		SendState(host, ChangeCauseOther);
		return true;
	}

	private void OnCommittedText(string text)
	{
		if (!_isComposing)
		{
			CompositionStarted?.Invoke(this, EventArgs.Empty);
		}
		CompositionCompleted?.Invoke(this, new ImeCompositionEventArgs(text));
		_isComposing = false;
		CompositionEnded?.Invoke(this, EventArgs.Empty);
	}

	// Uno's composition has a caret but no selection, as on macOS: a preedit selection shows its start
	// and a hidden cursor (-1) the end of the preedit.
	private void OnPreeditChanged(string? text, int cursorBegin)
	{
		if (!string.IsNullOrEmpty(text))
		{
			if (!_isComposing)
			{
				_compositionStart = _host!.SelectionStart;
				_compositionBaseLength = _host.Text.Length - _host.SelectionLength;
				_isComposing = true;
				CompositionStarted?.Invoke(this, EventArgs.Empty);
			}
			CompositionUpdated?.Invoke(this, new ImeCompositionEventArgs(text, cursorBegin < 0 ? -1 : WaylandTextInputText.ByteOffsetToCharIndex(text, cursorBegin)));
			_compositionLength = _host!.Text.Length - _compositionBaseLength;
		}
		else if (_isComposing)
		{
			// The input method cleared the preedit without committing it.
			CompositionCanceled?.Invoke(this, new ImeCompositionEventArgs(string.Empty));
			_isComposing = false;
			CompositionEnded?.Invoke(this, EventArgs.Empty);
		}
	}

	// text-input-v3 rectangles use surface-local logical coordinates.
	private static (int X, int Y, int Width, int Height)? GetCaretRectangle(IImeSessionHost host)
	{
		return host.TryGetCandidateWindowRect(out var rect)
			? ((int)Math.Round(rect.X), (int)Math.Round(rect.Y), Math.Max(1, (int)Math.Ceiling(rect.Width)), Math.Max(1, (int)Math.Ceiling(rect.Height)))
			: null;
	}
}
