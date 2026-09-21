// Portions of this files are Copyright 2013 The Flutter Authors. All rights reserved.
// Use of these portions of source code is governed by a BSD-style license that can be
// found in the THIRD-PARTY-NOTICES.md file.

// Ported to C# from https://github.com/flutter/flutter/blob/ea4cdcf39e935bb643b1294abe52c45063597caf/engine/src/flutter/shell/platform/android/io/flutter/plugin/editing/InputConnectionAdaptor.java

#nullable enable

using System;
using Android.Content;
using Android.OS;
using Android.Text;
using Android.Views;
using Android.Views.InputMethods;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Skia.Android;

/// <summary>
/// This implementation of <see cref="BaseInputConnection"/> is used
/// to bridge the Android <see cref="IInputConnection"/> in order to handle 
/// all keyboard interactions properly. Specifically, some devices handle the
/// backspace key differently and the Activity KeyDown does not report it properly.
/// 
/// Note that this implementation does not handle space bar long press yet.
/// </summary>
class TextInputConnection : BaseInputConnection
{
	private readonly View _target;
	private readonly InputMethodManager _imm;
	private readonly ObservableEditingState _editable;
	private readonly KeyboardEventHandler _keyboardHandler;
	private readonly DynamicLayout _layout;
	private readonly EditorInfo _editorInfo;

	private IImeSessionHost? _activeHost;
	private bool _endingBatch;
	private bool _duringHostStateUpdate;
	private CursorAnchorInfo.Builder? _cursorAnchorInfoBuilder;
	private bool _monitorCursorUpdate;
	private ExtractedText _extractedText = new ExtractedText();
	private ExtractedTextRequest? _extractRequest;
	private int _batchEditNestDepth;
	private int _hostVersion;
	private bool _textChangedInBatch;
	private bool _composingRegionChangedInBatch;

	public delegate bool KeyboardEventHandler(TextInputConnection source, KeyEvent? keyEvent);

	/// <summary>
	/// Prepares composition bookkeeping before native text is applied, then reports its committed state.
	/// </summary>
	internal event EventHandler<TextInputCompositionEventArgs>? CompositionStateChanged;

	public TextInputConnection(View target, EditorInfo editorInfo, KeyboardEventHandler keyboardHandler) : base(target, true)
	{
		_target = target;
		_editorInfo = editorInfo;
		_imm = (InputMethodManager?)target.Context!.GetSystemService(Context.InputMethodService)!;
		_editable = new ObservableEditingState(null, _target);
		_keyboardHandler = keyboardHandler;

		_editable.AddEditingStateListener(DidChangeEditingState);

		// We create a dummy Layout with max width so that the selection
		// shifting acts as if all text were in one line.
#pragma warning disable CA1422 // Validate platform compatibility
		_layout =
			new DynamicLayout(
				_editable,
				new TextPaint(),
				int.MaxValue,
				global::Android.Text.Layout.Alignment.AlignNormal!,
				1.0f,
				0.0f,
				false);
#pragma warning restore CA1422 // Validate platform compatibility
	}

	public override IEditable? Editable
		=> _editable;

	internal IImeSessionHost? ActiveHost
	{
		get => _activeHost;
		set
		{
			if (ReferenceEquals(_activeHost, value))
			{
				return;
			}

			_hostVersion++;
			_activeHost = value;
			_editable.SetComposingRange(-1, -1);
			UpdateEditableStateFromHost();
			SendCursorAnchorInfo();
		}
	}

	internal void OnTextInputHostChanged()
	{
		if (_activeHost is not null && _editable is not null && !_endingBatch)
		{
			this.LogDebug()?.Debug($"OnActiveTextInputHostChanged: {_activeHost.Text}");

			UpdateEditableStateFromHost();
		}
	}

	internal (int Start, int End)? GetComposingRange()
		=> _editable.ComposingStart >= 0 && _editable.ComposingEnd > _editable.ComposingStart
			? (_editable.ComposingStart, _editable.ComposingEnd)
			: null;

	internal void RestoreComposingRange(int start, int end)
	{
		var textLength = _editable.Length();
		start = Math.Clamp(start, 0, textLength);
		end = Math.Clamp(end, start, textLength);
		if (end > start)
		{
			base.SetComposingRegion(start, end);
			SendCursorAnchorInfo();
		}
	}

	/// <summary>
	/// Sends the current cursor anchor info to the IME immediately.
	/// Called when ActiveHost is set to ensure the candidate window
	/// is positioned correctly before any editing occurs.
	/// </summary>
	internal void SendCursorAnchorInfo()
	{
		var info = GetCursorAnchorInfo();
		if (info is not null)
		{
			_imm.UpdateCursorAnchorInfo(_target, info);
		}
	}

	private (int Start, int End)? UpdateEditableStateFromHost()
	{
		// In managed, we only use \r and convert all \n's to \r's to
		// match WinUI, so when copying from managed to native, we convert
		// \r to \n so that in the case of typing two newlines in a row,
		// we get \n\n from native and not \r\n (the first converted by
		// managed, the second was just typed
		// before conversion) which looks like a single newline.
		// cf. https://github.com/unoplatform/uno-private/issues/965
		var host = _activeHost;
		var text = host?.Text.Replace('\r', '\n') ?? string.Empty;
		var nativeText = _editable.ToString();
		var composingRange = host?.IsComposing == true ? GetComposingRange() : null;

		_duringHostStateUpdate = true;
		_editable.BeginBatchEdit();
		try
		{
			if (!string.Equals(text, nativeText, StringComparison.Ordinal))
			{
				var prefix = 0;
				var suffix = 0;
				if (composingRange is { } nativeRange && HasUnchangedCompositionContext(nativeText, text, nativeRange))
				{
					// Repeated surrounding text must not be mistaken for accepted preedit.
					prefix = nativeRange.Start;
					suffix = nativeText.Length - nativeRange.End;
				}
				else
				{
					var commonLength = Math.Min(text.Length, nativeText.Length);
					while (prefix < commonLength && text[prefix] == nativeText[prefix])
					{
						prefix++;
					}
					while (suffix < commonLength - prefix
						&& text[text.Length - suffix - 1] == nativeText[nativeText.Length - suffix - 1])
					{
						suffix++;
					}
				}
				var nativeEnd = nativeText.Length - suffix;
				var correctedEnd = text.Length - suffix;
				if (composingRange is { } range)
				{
					composingRange = (
						RebaseComposingPosition(range.Start, prefix, nativeEnd, correctedEnd),
						RebaseComposingPosition(range.End, prefix, nativeEnd, correctedEnd));
				}

				_editable.Replace(prefix, nativeEnd, text.Substring(prefix, correctedEnd - prefix));
				if (composingRange is { } correctedRange)
				{
					// Replacing an exclusive composing span removes it, even for casing/length corrections.
					_editable.SetComposingRange(correctedRange.Start, correctedRange.End);
				}
			}

			if (host is not null)
			{
				var length = _editable.Length();
				Selection.SetSelection(
					_editable,
					Math.Clamp(host.SelectionStart, 0, length),
					Math.Clamp(host.SelectionStart + host.SelectionLength, 0, length));
			}
			else
			{
				Selection.RemoveSelection(_editable);
			}

			if (!TextInputPlugin.CanAcceptTextInput(host))
			{
				_editable.SetComposingRange(-1, -1);
				composingRange = null;
			}
		}
		finally
		{
			_editable.EndBatchEdit();
			_duringHostStateUpdate = false;
		}

		return composingRange;
	}

	private static bool HasUnchangedCompositionContext(string nativeText, string correctedText, (int Start, int End) range)
	{
		var suffixLength = nativeText.Length - range.End;
		return correctedText.Length >= nativeText.Length - (range.End - range.Start)
			&& correctedText.AsSpan(0, range.Start).SequenceEqual(nativeText.AsSpan(0, range.Start))
			&& correctedText.AsSpan(correctedText.Length - suffixLength).SequenceEqual(nativeText.AsSpan(range.End));
	}

	private static int RebaseComposingPosition(int position, int start, int nativeEnd, int correctedEnd)
		=> position <= start
			? position
			: position >= nativeEnd
				? position + correctedEnd - nativeEnd
				: start + Math.Min(position - start, correctedEnd - start);

	public override bool EndBatchEdit()
	{
		if (_batchEditNestDepth == 0)
		{
			return false;
		}

		_batchEditNestDepth -= 1;
		_editable.EndBatchEdit();

		if (_batchEditNestDepth != 0)
		{
			return true;
		}

		var compositionChanged = _composingRegionChangedInBatch || _textChangedInBatch;
		var textChanged = _textChangedInBatch;
		_textChangedInBatch = false;
		_composingRegionChangedInBatch = false;
		var host = _activeHost;
		var hostVersion = _hostVersion;
		if (host is null || _duringHostStateUpdate || _endingBatch)
		{
			return false;
		}

		_endingBatch = true;
		try
		{
			var selectionStart = _editable.SelectionStart;
			var selectionEnd = _editable.SelectionEnd;
			this.LogDebug()?.Debug($"EndBatchEdit: {_editable.ToString()} ({selectionStart}->{selectionEnd})");

			if (TextInputPlugin.CanAcceptTextInput(host))
			{
				if (compositionChanged)
				{
					NotifyCompositionStateChanged(textChanged, textApplied: false, GetComposingRange());
				}

				if (IsCurrentHost(host, hostVersion) && TextInputPlugin.CanAcceptTextInput(host))
				{
					host.UpdateTextFromNative(_editable.ToString(), selectionStart, selectionEnd - selectionStart);
				}

				if (IsCurrentHost(host, hostVersion) && TextInputPlugin.CanAcceptTextInput(host))
				{
					var correctedComposition = UpdateEditableStateFromHost();
					if (correctedComposition is { } range
						&& IsCurrentHost(host, hostVersion) && TextInputPlugin.CanAcceptTextInput(host))
					{
						host.ReconcileCompositionFromNative(range.Start, range.End - range.Start);
					}
					if (compositionChanged && IsCurrentHost(host, hostVersion) && TextInputPlugin.CanAcceptTextInput(host))
					{
						NotifyCompositionStateChanged(textChanged, textApplied: true, correctedComposition);
					}
				}
			}
			else if (string.Equals(_editable.ToString(), host.Text.Replace('\r', '\n'), StringComparison.Ordinal))
			{
				// A read-only host still participates in native selection and copy.
				host.SelectFromNative(selectionStart, selectionEnd - selectionStart);
			}
		}
		finally
		{
			_endingBatch = false;
			if (IsCurrentHost(host, hostVersion))
			{
				UpdateEditableStateFromHost();
			}
		}

		return false;
	}

	private bool IsCurrentHost(IImeSessionHost host, int version)
		=> ReferenceEquals(_activeHost, host)
			&& _hostVersion == version
			&& !TextInputPlugin.IsSupersededByFocusedHost(host);

	private void NotifyCompositionStateChanged(bool textChanged, bool textApplied, (int Start, int End)? composingRange)
		=> CompositionStateChanged?.Invoke(this, new TextInputCompositionEventArgs(
			composingRange?.Start ?? -1,
			composingRange?.End ?? -1,
			_editable.ToString(),
			textChanged,
			textApplied));

	public override bool SetSelection(int start, int end)
	{
		if (_editable.SelectionStart != start || _editable.SelectionEnd != end)
		{
			BeginBatchEdit();
			bool ret = base.SetSelection(start, end);
			EndBatchEdit();
			return ret;
		}
		else
		{
			this.LogDebug()?.Debug($"SetSelection: Skipping for unchanged selection ({start}->{end})");
			return true;
		}
	}

	public override bool SendKeyEvent(KeyEvent? e)
	{
		this.LogDebug()?.Debug($"SendKeyEvent {e?.Action} {e?.KeyCode}");

		return _activeHost is not null && _keyboardHandler(this, e);
	}

	public override bool RequestCursorUpdates(int cursorUpdateMode)
	{
		if (_imm is null)
		{
			return false;
		}

		this.LogDebug()?.Debug($"RequestCursorUpdates {cursorUpdateMode}");

		if (((CursorUpdate)cursorUpdateMode & CursorUpdate.Immediate) != 0)
		{
			_imm.UpdateCursorAnchorInfo(_target, GetCursorAnchorInfo());
		}

		bool updated = ((CursorUpdate)cursorUpdateMode & CursorUpdate.Monitor) != 0;
		if (updated != _monitorCursorUpdate)
		{
			this.LogDebug()?.Debug($"The input method toggled cursor monitoring " + (updated ? "on" : "off"));
		}

		// Enables cursor monitoring. See InputConnectionAdaptor#didChangeEditingState.
		_monitorCursorUpdate = updated;
		return true;
	}

	private ExtractedText? GetExtractedText(ExtractedTextRequest? request)
	{
		if (request is not null)
		{
			_extractedText.StartOffset = 0;
			_extractedText.PartialStartOffset = -1;
			_extractedText.PartialEndOffset = -1;
			_extractedText.SelectionStart = _editable.SelectionStart;
			_extractedText.SelectionEnd = _editable.SelectionEnd;

			// convert _editable to an ICharSequence

			_extractedText.Text =
				request == null || (request.Flags & GetTextFlags.WithStyles) == 0
					? new Java.Lang.String(_editable.ToString())
					: _editable;
			return _extractedText;
		}

		return null;
	}

	public bool handleKeyEvent(KeyEvent evt)
	{
		if (evt.Action == KeyEventActions.Down)
		{
			if (evt.KeyCode == Keycode.DpadLeft)
			{
				return HandleHorizontalMovement(true, evt.IsShiftPressed);
			}
			else if (evt.KeyCode == Keycode.DpadRight)
			{
				return HandleHorizontalMovement(false, evt.IsShiftPressed);
			}
			else if (evt.KeyCode == Keycode.DpadUp)
			{
				return HandleVerticalMovement(true, evt.IsShiftPressed);
			}
			else if (evt.KeyCode == Keycode.DpadDown)
			{
				return HandleVerticalMovement(false, evt.IsShiftPressed);
				// When the enter key is pressed on a non-multiline field, consider it a
				// submit instead of a newline.
			}
			else if ((evt.KeyCode == Keycode.Enter
					|| evt.KeyCode == Keycode.NumpadEnter)
				// Uno Doc: Enter is handled in _keyboardHandler (TextInputPlugin) regardless of InputTypes.TextFlagMultiLine
				/* && (InputTypes.TextFlagMultiLine & _editorInfo.InputType) == 0 */)
			{
				// editor actions are not supported yet
				// PerformEditorAction((ImeAction)_editorInfo.ImeOptions & ImeAction.ImeMaskAction);
				return false;
			}
			else if (!TextInputPlugin.CanAcceptTextInput(_activeHost))
			{
				return false;
			}
			else if (evt.KeyCode == Keycode.Del)
			{
				// For cases where the backspace key is not handled by the system, we need to
				// handle it ourselves. Also, useful for when using a hardware keyboard.
				// related to: https://github.com/unoplatform/uno-private/issues/1121
				int selStart = ClampIndexToEditable(Selection.GetSelectionStart(_editable));
				int selEnd = ClampIndexToEditable(Selection.GetSelectionEnd(_editable));
				if (selStart == selEnd && selStart > 0)
				{
					// Extend selection to left of the last character
					selStart = TextUtils.GetOffsetBefore(_editable, selStart);
				}

				if (selEnd > selStart)
				{
					// Delete the selection.
					BeginBatchEdit();
					_editable.Delete(selStart, selEnd);
					EndBatchEdit();
					return true;
				}

				return false;
			}
			// Handles the [DEL] key on virtual keyboards. Found on very few devices / keyboards.
			else if (evt.KeyCode == Keycode.ForwardDel)
			{
				int selStart = ClampIndexToEditable(Selection.GetSelectionStart(_editable));
				int selEnd = ClampIndexToEditable(Selection.GetSelectionEnd(_editable));

				if (selStart < selEnd)
				{
					BeginBatchEdit();
					_editable.Delete(selStart, selEnd);
					EndBatchEdit();
					return true;
				}

				if (selStart == selEnd && selStart < _editable.Length())
				{
					BeginBatchEdit();
					_editable.Delete(selStart, selStart + 1);
					EndBatchEdit();
					return true;
				}

				return false;
			}
			else
			{
				// Enter a character.
				int selStart = Selection.GetSelectionStart(_editable);
				int selEnd = Selection.GetSelectionEnd(_editable);
				int character = evt.GetUnicodeChar(evt.MetaState); // original code does not have a parameter here
				if (selStart < 0 || selEnd < 0 || character == 0)
				{
					return false;
				}

				int selMin = Math.Min(selStart, selEnd);
				int selMax = Math.Max(selStart, selEnd);
				BeginBatchEdit();
				if (selMin != selMax) { _editable.Delete(selMin, selMax); }
				_editable.Insert(selMin, new Java.Lang.String(((char)character).ToString()));
				SetSelection(selMin + 1, selMin + 1);
				EndBatchEdit();
				return true;
			}
		}
		return false;
	}

	private int ClampIndexToEditable(int index)
	{
		int clamped = Math.Max(0, Math.Min(_editable.Length(), index));
		return clamped;
	}

	private bool HandleHorizontalMovement(bool isLeft, bool isShiftPressed)
	{
		int selStart = Selection.GetSelectionStart(_editable);
		int selEnd = Selection.GetSelectionEnd(_editable);

		if (selStart < 0 || selEnd < 0)
		{
			return false;
		}

		int newSelectionEnd =
			isLeft
				? Math.Max(TextUtils.GetOffsetBefore(_editable, selEnd), 0)
				: Math.Min(TextUtils.GetOffsetAfter(_editable, selEnd), _editable.Length());

		bool shouldCollapse = selStart == selEnd && !isShiftPressed;

		if (shouldCollapse)
		{
			SetSelection(newSelectionEnd, newSelectionEnd);
		}
		else
		{
			SetSelection(selStart, newSelectionEnd);
		}

		return true;
	}

	private bool HandleVerticalMovement(bool isUp, bool isShiftPressed)
	{
		int selStart = Selection.GetSelectionStart(_editable);
		int selEnd = Selection.GetSelectionEnd(_editable);

		if (selStart < 0 || selEnd < 0)
		{
			return false;
		}

		bool shouldCollapse = selStart == selEnd && !isShiftPressed;

		BeginBatchEdit();
		if (shouldCollapse)
		{
			if (isUp)
			{
				Selection.MoveUp(_editable, _layout);
			}
			else
			{
				Selection.MoveDown(_editable, _layout);
			}
			int newSelection = Selection.GetSelectionStart(_editable);
			SetSelection(newSelection, newSelection);
		}
		else
		{
			if (isUp)
			{
				Selection.ExtendUp(_editable, _layout);
			}
			else
			{
				Selection.ExtendDown(_editable, _layout);
			}
			SetSelection(Selection.GetSelectionStart(_editable), Selection.GetSelectionEnd(_editable));
		}
		EndBatchEdit();
		return true;
	}


	private CursorAnchorInfo? GetCursorAnchorInfo()
	{
		if (_editable is null || _imm is null)
		{
			return null;
		}

		_cursorAnchorInfoBuilder?.Reset();
		_cursorAnchorInfoBuilder ??= new CursorAnchorInfo.Builder();

		_cursorAnchorInfoBuilder.SetSelectionRange(
			Selection.GetSelectionStart(_editable),
			Selection.GetSelectionEnd(_editable));

		int composingStart = _editable.ComposingStart;
		int composingEnd = _editable.ComposingEnd;
		if (composingStart >= 0 && composingEnd > composingStart)
		{
			_cursorAnchorInfoBuilder.SetComposingText(
				composingStart, _editable.SubSequence(composingStart, composingEnd));
		}
		else
		{
			_cursorAnchorInfoBuilder.SetComposingText(-1, "");
		}

		// Set insertion marker location so the IME candidate window
		// appears near the caret rather than at a default position.
		if (_activeHost is { TextBoxView.DisplayBlock.ParsedText: { } parsedText, XamlRoot: { } xamlRoot })
		{
			var selEnd = _activeHost.SelectionStart + _activeHost.SelectionLength;
			var caretRect = parsedText.GetRectForIndex(selEnd);
			var transform = _activeHost.TextBoxView.DisplayBlock.TransformToVisual(null);
			var caretPoint = transform.TransformPoint(
				new Windows.Foundation.Point(caretRect.Left, caretRect.Top));
			var caretBottom = transform.TransformPoint(
				new Windows.Foundation.Point(caretRect.Left, caretRect.Top + caretRect.Height));
			var scale = xamlRoot.RasterizationScale;

			// Coordinates are in screen pixels. We use an identity matrix
			// since we already transform to absolute screen coordinates.
			_cursorAnchorInfoBuilder.SetMatrix(new global::Android.Graphics.Matrix());

			int[] viewOffset = new int[2];
			_target.GetLocationOnScreen(viewOffset);

			var markerX = (float)(caretPoint.X * scale) + viewOffset[0];
			var markerTop = (float)(caretPoint.Y * scale) + viewOffset[1];
			var markerBottom = (float)(caretBottom.Y * scale) + viewOffset[1];

			_cursorAnchorInfoBuilder.SetInsertionMarkerLocation(
				markerX, markerTop, markerBottom, markerBottom,
				CursorAnchorFlags.HasVisibleRegion);
		}

		return _cursorAnchorInfoBuilder.Build();
	}

	public override bool BeginBatchEdit()
	{
		this.LogDebug()?.Debug($"BeginBatchEdit");

		_editable.BeginBatchEdit();
		_batchEditNestDepth += 1;

		return true;
	}

	public override bool ClearMetaKeyStates(MetaKeyStates states)
	{
		this.LogDebug()?.Debug($"ClearMetaKeyStates {states}");

		return base.ClearMetaKeyStates(states);
	}

	public override void CloseConnection()
	{
		this.LogDebug()?.Debug($"CloseConnection");

		// BaseInputConnection closes composition through our virtual batch-edit callbacks.
		// A retired connection must not replay its old buffer or end the replacement host's composition.
		_activeHost = null;
		_hostVersion++;
		_editable.RemoveEditingStateListener(DidChangeEditingState);
		base.CloseConnection();

		while (_batchEditNestDepth > 0)
		{
			EndBatchEdit();
		}
	}

	public override bool CommitCompletion(CompletionInfo? text)
	{
		this.LogDebug()?.Debug($"CommitCompletion {text?.Label}");

		return base.CommitCompletion(text);
	}

	public override bool CommitContent(InputContentInfo? inputContentInfo, InputContentFlags flags, Bundle? opts)
	{
		this.LogDebug()?.Debug($"CommitContent {flags}");

		return base.CommitContent(inputContentInfo, flags, opts);
	}

	public override bool CommitCorrection(CorrectionInfo? correctionInfo)
	{
		this.LogDebug()?.Debug($"CommitCorrection {correctionInfo?.NewText}");
		return base.CommitCorrection(correctionInfo);
	}

	public override bool CommitText(Java.Lang.ICharSequence? text, int newCursorPosition)
	{
		this.LogDebug()?.Debug($"CommitText {text}");

		if (!TextInputPlugin.CanAcceptTextInput(_activeHost))
		{
			return false;
		}

		_composingRegionChangedInBatch |= _activeHost?.IsComposing == true;
		return base.CommitText(text, newCursorPosition);
	}

	public override bool DeleteSurroundingText(int beforeLength, int afterLength)
	{
		this.LogDebug()?.Debug($"DeleteSurroundingText {beforeLength}->{afterLength}");

		if (!TextInputPlugin.CanAcceptTextInput(_activeHost))
		{
			return false;
		}

		if (_editable.SelectionStart == -1)
		{
			return true;
		}

		return base.DeleteSurroundingText(beforeLength, afterLength);
	}

	public override bool DeleteSurroundingTextInCodePoints(int beforeLength, int afterLength)
	{
		this.LogDebug()?.Debug($"DeleteSurroundingTextInCodePoints {beforeLength}->{afterLength}");

		return TextInputPlugin.CanAcceptTextInput(_activeHost) && base.DeleteSurroundingTextInCodePoints(beforeLength, afterLength);
	}

	public override bool FinishComposingText()
	{
		this.LogDebug()?.Debug($"FinishComposingText");

		// A fully rejected preedit has no native span left to generate a removal notification.
		_composingRegionChangedInBatch |= _activeHost?.IsComposing == true;
		return base.FinishComposingText();
	}

	public override CapitalizationMode GetCursorCapsMode(CapitalizationMode reqModes)
	{
		this.LogDebug()?.Debug($"GetCursorCapsMode {reqModes}");

		return base.GetCursorCapsMode(reqModes);
	}

	public override ExtractedText? GetExtractedText(ExtractedTextRequest? request, GetTextFlags flags)
	{
		this.LogDebug()?.Debug($"GetExtractedText [{request?.Token}], {flags}");

		bool textMonitor = ((int)flags & 1 /* GET_EXTRACTED_TEXT_MONITOR */) != 0;
		if (textMonitor == (_extractRequest == null))
		{
			this.LogDebug()?.Debug("The input method toggled text monitoring " + (textMonitor ? "on" : "off"));
		}
		// Enables text monitoring if the relevant flag is set. See
		// InputConnectionAdaptor#didChangeEditingState.
		_extractRequest = textMonitor ? request : null;

		return GetExtractedText(request);
	}

	public override Java.Lang.ICharSequence? GetSelectedTextFormatted(GetTextFlags flags)
	{
		var ret = base.GetSelectedTextFormatted(flags);

		var wasEndingBatch = _endingBatch;
		_endingBatch = true;
		try
		{
			var selectionStart = Selection.GetSelectionStart(Editable);
			var selectionEnd = Selection.GetSelectionEnd(Editable);
			this.LogDebug()?.Debug($"GetSelectedTextFormatted {flags} = {selectionStart}->{selectionEnd}");
			ActiveHost?.SelectFromNative(selectionStart, selectionEnd - selectionStart);
		}
		finally
		{
			_endingBatch = wasEndingBatch;
		}

		return ret;
	}

	public override SurroundingText? GetSurroundingText(int beforeLength, int afterLength, int flags)
	{
		this.LogDebug()?.Debug($"GetSurroundingText {beforeLength}, {afterLength}, {flags}");

		return base.GetSurroundingText(beforeLength, afterLength, flags);
	}

	public override Java.Lang.ICharSequence? GetTextAfterCursorFormatted(int length, GetTextFlags flags)
	{
		this.LogDebug()?.Debug($"GetTextAfterCursorFormatted {length}, {flags}");

		return base.GetTextAfterCursorFormatted(length, flags);
	}

	public override Java.Lang.ICharSequence? GetTextBeforeCursorFormatted(int length, GetTextFlags flags)
	{
		this.LogDebug()?.Debug($"GetTextBeforeCursorFormatted {length}, {flags}");

		return base.GetTextBeforeCursorFormatted(length, flags);
	}

	public override bool PerformContextMenuAction(int id)
	{
		this.LogDebug()?.Debug($"PerformContextMenuAction {id}");

		BeginBatchEdit();
		bool result = DoPerformContextMenuAction(id);
		EndBatchEdit();

		return result;
	}

	private bool DoPerformContextMenuAction(int id)
	{
		if ((id == global::Android.Resource.Id.Cut || id == global::Android.Resource.Id.Paste)
			&& !TextInputPlugin.CanAcceptTextInput(_activeHost))
		{
			return false;
		}

		if (id == global::Android.Resource.Id.SelectAll)
		{
			SetSelection(0, _editable.Length());
			return true;
		}
		else if (id == global::Android.Resource.Id.Cut)
		{
			int selStart = Selection.GetSelectionStart(_editable);
			int selEnd = Selection.GetSelectionEnd(_editable);
			if (selStart != selEnd)
			{
				int selMin = Math.Min(selStart, selEnd);
				int selMax = Math.Max(selStart, selEnd);
				var textToCut = _editable.SubSequence(selMin, selMax);
				var clipboard =
				(global::Android.Content.ClipboardManager)
					_target.Context!.GetSystemService(Context.ClipboardService)!;
				var clip = ClipData.NewPlainText("text label?", textToCut);
				clipboard.PrimaryClip = clip;
				_editable.Delete(selMin, selMax);
				SetSelection(selMin, selMin);
			}
			return true;
		}
		else if (id == global::Android.Resource.Id.Copy)
		{
			int selStart = Selection.GetSelectionStart(_editable);
			int selEnd = Selection.GetSelectionEnd(_editable);
			if (selStart != selEnd)
			{
				var textToCopy =
					_editable.SubSequence(Math.Min(selStart, selEnd), Math.Max(selStart, selEnd));
				var clipboard =
					(global::Android.Content.ClipboardManager)
					_target.Context!.GetSystemService(Context.ClipboardService)!;
				clipboard.PrimaryClip = ClipData.NewPlainText("text label?", textToCopy);
			}
			return true;
		}
		else if (id == global::Android.Resource.Id.Paste)
		{
			var clipboard =
				(global::Android.Content.ClipboardManager)
				_target.Context!.GetSystemService(Context.ClipboardService)!;

			var clip = clipboard.PrimaryClip;
			if (clip != null)
			{
				var textToPaste = clip.GetItemAt(0)?.CoerceToText(_target.Context);
				if (textToPaste is not null)
				{
					int selStart = Math.Max(0, Selection.GetSelectionStart(_editable));
					int selEnd = Math.Max(0, Selection.GetSelectionEnd(_editable));
					int selMin = Math.Min(selStart, selEnd);
					int selMax = Math.Max(selStart, selEnd);
					if (selMin != selMax) _editable.Delete(selMin, selMax);
					_editable.Insert(selMin, textToPaste);
					int newSelStart = selMin + textToPaste.Length;
					SetSelection(newSelStart, newSelStart);
				}
			}
			return true;
		}

		return false;
	}

	private void DidChangeEditingState(bool textChanged, bool selectionChanged, bool composingRegionChanged)
	{
		if (!_duringHostStateUpdate)
		{
			_textChangedInBatch |= textChanged;
			_composingRegionChangedInBatch |= composingRegionChanged;
		}

		if (_imm is null)
		{
			return;
		}

		// This method notifies the input method that the editing state has changed.
		// updateSelection is mandatory. updateExtractedText and updateCursorAnchorInfo
		// are on demand (if the input method set the corresponding monitoring
		// flags). See getExtractedText and requestCursorUpdates.

		// Always send selection update. InputMethodManager#updateSelection skips
		// sending the message if none of the parameters have changed since the last
		// time we called it.
		_imm.UpdateSelection(
			_target,
			_editable.SelectionStart,
			_editable.SelectionEnd,
			_editable.ComposingStart,
			_editable.ComposingEnd);

		if (_extractRequest != null)
		{
			_imm.UpdateExtractedText(
				_target, _extractRequest.Token, GetExtractedText(_extractRequest));
		}
		// Always send cursor anchor info so the IME candidate window
		// tracks the caret position, even if the IME didn't explicitly
		// request cursor monitoring via RequestCursorUpdates.
		{
			var info = GetCursorAnchorInfo();
			_imm.UpdateCursorAnchorInfo(_target, info);
		}
	}

	public override bool PerformEditorAction(ImeAction actionCode)
	{
		this.LogDebug()?.Debug($"PerformEditorAction {actionCode}");

		return base.PerformEditorAction(actionCode);
	}

	public override bool PerformPrivateCommand(string? action, Bundle? data)
	{
		this.LogDebug()?.Debug($"PerformEditorAction {action}");

		return base.PerformPrivateCommand(action, data);
	}

	public override bool ReplaceText(int start, int end, Java.Lang.ICharSequence text, int newCursorPosition, TextAttribute? textAttribute)
	{
		this.LogDebug()?.Debug($"ReplaceText {start}->{end}, [{text}], {newCursorPosition}, {textAttribute}");

		return TextInputPlugin.CanAcceptTextInput(_activeHost) && base.ReplaceText(start, end, text, newCursorPosition, textAttribute);
	}

	public override bool ReportFullscreenMode(bool enabled)
	{
		this.LogDebug()?.Debug($"ReportFullscreenMode {enabled}");

		return base.ReportFullscreenMode(enabled);
	}

	public override bool SetComposingRegion(int start, int end)
	{
		this.LogDebug()?.Debug($"SetComposingRegion {start}->{end}");

		return TextInputPlugin.CanAcceptTextInput(_activeHost) && base.SetComposingRegion(start, end);
	}

	public override bool SetComposingText(Java.Lang.ICharSequence? text, int newCursorPosition)
	{
		this.LogDebug()?.Debug($"SetComposingText {text}, {newCursorPosition}");

		if (!TextInputPlugin.CanAcceptTextInput(_activeHost))
		{
			return false;
		}

		bool result;

		BeginBatchEdit();

		if (text?.Length() == 0)
		{
			_composingRegionChangedInBatch |= _activeHost?.IsComposing == true;
			result = base.CommitText(text, newCursorPosition);
		}
		else
		{
			result = base.SetComposingText(text, newCursorPosition);
		}

		EndBatchEdit();
		return result;
	}

	public override TextSnapshot? TakeSnapshot()
	{
		this.LogDebug()?.Debug($"TakeSnapshot");

		return base.TakeSnapshot();
	}
}

internal sealed class TextInputCompositionEventArgs(
	int composingStart,
	int composingEnd,
	string text,
	bool textChanged,
	bool textApplied) : EventArgs
{
	internal int ComposingStart { get; } = composingStart;
	internal int ComposingEnd { get; } = composingEnd;
	internal string Text { get; } = text;
	internal bool TextChanged { get; } = textChanged;
	internal bool TextApplied { get; } = textApplied;
	// A correction may reject the entire preedit without ending the native composition session.
	internal bool IsComposing => ComposingStart >= 0 && ComposingEnd >= ComposingStart && ComposingEnd <= Text.Length;
	internal string CompositionText => IsComposing ? Text.Substring(ComposingStart, ComposingEnd - ComposingStart) : string.Empty;
}
