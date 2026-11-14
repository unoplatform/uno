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
	private const uint ChangeCauseInputMethod = 0;
	private const uint ChangeCauseOther = 1;

	private readonly IWaylandTextInput _textInput;
	private readonly Action<Action> _postToUI;
	private readonly WaylandDisplay _display;

	// Under the display lock: double-buffered state, applied at done, and the session it applies to.
	private readonly PendingBatch _pending = new();
	private uint _commitSerial;
	private IImeSessionHost? _sessionHost;
	private long _session;

	// Written on the UI thread, read at done: changes of the text not made by the input method.
	private long _textRevision;

	// UI thread.
	private WlSurface? _focusedSurface;
	private IImeSessionHost? _host;
	private WlSurface? _hostSurface;
	private int _compositionStart;
	private int _compositionBaseLength;
	private int _compositionLength;
	private string _revisionText = string.Empty;
	private bool _enabled;
	private bool _supportsSurrounding;
	private bool _isComposing;
	private bool _applyingInputMethodChange;
	// After a done for an older commit, state requests wait for the done that matches the last one.
	private bool _deferState;
	private bool _stateDirty;
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
		using (_display.Lock())
		{
			_commitSerial = unchecked(_commitSerial + 1);
		}
		_textInput.Commit();
	}

	private void Disable()
	{
		using (_display.Lock())
		{
			_session++;
			_sessionHost = null;
			_pending.Reset();
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
			_pending.Reset();
			_sessionHost = host;
		}
		_lastSentState = null;
		_textInput.Enable();
		WaylandDiagnostics.Trace("text-input: enabled");
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
	}

	// Dispatch thread. Every done is applied in order; only state requests wait for the one matching the last commit.
	private void OnDone(object? sender, ZwpTextInputV3.DoneEventArgs args)
	{
		Action? apply = null;
		using (_display.Lock())
		{
			var batch = _pending.Take();
			if (_sessionHost is { } host)
			{
				var session = _session;
				var revision = Interlocked.Read(ref _textRevision);
				var serial = args.Serial;
				apply = () => Apply(session, host, serial, revision, batch);
			}
		}
		if (apply is not null)
		{
			_postToUI(apply);
		}
	}

	// Applies a done event in the order the protocol specifies: delete surrounding text (dropping the preedit
	// first), insert the commit string, then show the new preedit. A preedit update alone keeps the composition.
	private void Apply(long session, IImeSessionHost host, uint serial, long revision, Batch batch)
	{
		if (!_enabled || session != _session || host != _host || !host.CanAcceptTextInput)
		{
			return;
		}
		var surrounding = CalculateSurrounding(host);
		var deleting = batch.DeleteBefore != 0 || batch.DeleteAfter != 0;
		if (deleting && (revision != Interlocked.Read(ref _textRevision) || !_supportsSurrounding || !surrounding.IsSupported))
		{
			// The deletion was computed for text that has changed since; the commit and preedit still apply.
			WaylandDiagnostics.Trace("text-input: stale deletion dropped");
			deleting = false;
		}
		var wasComposing = _isComposing;
		_applyingInputMethodChange = true;
		try
		{
			if (deleting && _isComposing)
			{
				OnPreeditChanged(null, 0);
				surrounding = CalculateSurrounding(host);
			}
			if (deleting)
			{
				DeleteSurroundingText(host, surrounding, batch.DeleteBefore, batch.DeleteAfter);
			}
			if (batch.Commit is not null)
			{
				OnCommittedText(batch.Commit);
			}
			// A new preedit replaces the selection; the surrounding text excludes it and keeps the caret at its start.
			OnPreeditChanged(batch.Preedit, batch.PreeditCursor);
		}
		finally
		{
			_revisionText = host.Text ?? string.Empty;
			_applyingInputMethodChange = false;
		}
		// Compared here, not at dispatch: a commit sent in between makes this done stale.
		bool current;
		using (_display.Lock())
		{
			current = serial == _commitSerial;
		}
		_deferState = !current;
		// A preedit-only update leaves the surrounding text unchanged; its start and end move the caret.
		if (deleting || batch.Commit is not null || wasComposing != _isComposing || _stateDirty)
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
			Interlocked.Increment(ref _textRevision);
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
		}
		finally
		{
			_revisionText = host.Text ?? string.Empty;
			_applyingInputMethodChange = false;
		}
		// Typed text is not the input method's: a deletion it computed earlier no longer applies.
		Interlocked.Increment(ref _textRevision);
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

	private readonly record struct Batch(string? Preedit, int PreeditCursor, string? Commit, uint DeleteBefore, uint DeleteAfter);

	private sealed class PendingBatch
	{
		internal string? Preedit;
		internal int PreeditCursor;
		internal string? Commit;
		internal uint DeleteBefore;
		internal uint DeleteAfter;

		internal Batch Take()
		{
			var batch = new Batch(Preedit, PreeditCursor, Commit, DeleteBefore, DeleteAfter);
			Reset();
			return batch;
		}

		internal void Reset()
		{
			Preedit = Commit = null;
			PreeditCursor = 0;
			DeleteBefore = DeleteAfter = 0;
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
