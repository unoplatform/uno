using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using Android.Content;
using Android.OS;
using Android.Text;
using Android.Util;
using Android.Views;
using Android.Views.Autofill;
using Android.Views.InputMethods;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.Extensions;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Skia.Android;

internal sealed class TextInputPlugin
{
	private readonly UnoSKCanvasView _view;
	private readonly InputMethodManager? _imm;
	private readonly AutofillManager? _afm;
	private InputTypes _inputTypes = InputTypes.TextVariationNormal;
	private TextInputConnection? _inputConnection;

	internal TextInputPlugin(UnoSKCanvasView view)
	{
		_view = view;
		_imm = (InputMethodManager?)view.Context!.GetSystemService(Context.InputMethodService);
		if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
		{
			_afm = (AutofillManager?)view.Context.GetSystemService(Java.Lang.Class.FromType(typeof(AutofillManager)));
		}
	}

	internal void NotifyViewEntered(TextBox textBox, int virtualId)
	{
		if (_afm == null || _inputConnection is null)
		{
			return;
		}

		_inputConnection.ActiveTextBox = textBox;

		var physicalRect = GetPhysicalRect(textBox);
		_afm.NotifyViewEntered(_view, virtualId, new((int)physicalRect.Left, (int)physicalRect.Top, (int)physicalRect.Right, (int)physicalRect.Bottom));
	}

	private Windows.Foundation.Rect GetPhysicalRect(TextBox textBox)
	{
		int[] offset = new int[2];
		_view.GetLocationOnScreen(offset);
		var transform = UIElement.GetTransform(from: textBox, to: null);
		var logicalRect = transform.Transform(new Windows.Foundation.Rect(default, new Windows.Foundation.Size(textBox.Visual.Size.X, textBox.Visual.Size.Y)));
		var physicalRect = logicalRect.LogicalToPhysicalPixels();
		return physicalRect.OffsetRect(offset[0], offset[1]);
	}

	internal void NotifyViewExited(int virtualId)
	{
		if (_afm == null || _inputConnection is null)
		{
			return;
		}

		_inputConnection.ActiveTextBox = null;

		_afm.NotifyViewExited(_view, virtualId);
	}

	internal void FinishAutofillContext(bool shouldSave)
	{
		if (_afm is null)
		{
			return;
		}

		if (shouldSave)
		{
			_afm.Commit();
		}
		else
		{
			_afm.Cancel();
		}
	}

	internal void NotifyValueChanged(int virtualId, string newValue)
	{
		if (_afm == null /*|| !NeedsAutofill()*/)
		{
			return;
		}

		this.LogDebug()?.Debug($"NotifyValueChanged: {newValue}");

		_afm.NotifyValueChanged(_view, virtualId, AutofillValue.ForText(newValue));
	}

	internal void ShowTextInput(InputTypes inputTypes)
	{
		_inputTypes = inputTypes;
		_view.RequestFocus();
		_imm?.ShowSoftInput(_view, 0);
	}

	internal void HideTextInput()
	{
		_imm?.HideSoftInputFromWindow(_view.ApplicationWindowToken, 0);
	}

	internal void OnProvideAutofillVirtualStructure(ViewStructure? structure)
	{
#if false // Removing temporarily. We'll need to add it back.
		var textBoxes = AndroidSkiaTextBoxNotificationsProviderSingleton.Instance.LiveTextBoxes;
		var index = structure!.AddChildCount(textBoxes.Count);
		var parentId = structure.AutofillId!;
		for (int i = 0; i < textBoxes.Count; i++)
		{
			var textBox = textBoxes[i];
			var child = structure.NewChild(index + i);
			child!.SetAutofillId(parentId, textBox.GetHashCode());
			child.SetDataIsSensitive(textBox is PasswordBox);

			// This is not really correct implementation.
			// We cannot determine ourselves the autofill hints.
			// Consider exposing a public API for the user to be able to specify this.
			child.SetAutofillHints([textBox is PasswordBox ? "password" : "username"]);
			child.SetAutofillType(AutofillType.Text);
			child.SetVisibility(ViewStates.Visible);
			// Do we need child.Hint? How to set it?

			var physicalRect = GetPhysicalRect(textBox);
			child.SetDimens((int)physicalRect.Left, (int)physicalRect.Top, 0, 0, (int)physicalRect.Width, (int)physicalRect.Height);
		}
#endif
	}

	internal IInputConnection? OnCreateInputConnection(EditorInfo? outAttrs)
	{
		if (outAttrs is not null)
		{
			outAttrs.InputType = _inputTypes;
		}

		return _inputConnection = new TextInputConnection(this, _view);
	}

	/// <summary>
	/// This implementation of <see cref="BaseInputConnection"/> is used
	/// to bridge the Android <see cref="IInputConnection"/> in order to handle 
	/// all keyboard interactions properly. Specifically, some devices handle the
	/// backspace key differently and the Activity KeyDown does not report it proeprly,
	/// 
	/// Note that this implementation does not handle space bar long press yet.
	/// </summary>
	private class TextInputConnection : BaseInputConnection
	{
		private TextInputPlugin _owner;
		private TextBox? _activeTextBox;
		private bool _endingBatch;
		private bool _duringTextBoxSelectionChanged;

		public TextInputConnection(TextInputPlugin owner, View target) : base(target, true)
		{
			_owner = owner;
		}

		public TextBox? ActiveTextBox
		{
			get => _activeTextBox;
			internal set
			{
				if (_activeTextBox is not null)
				{
					_activeTextBox.SelectionChanged -= OnActiveTextBoxSelectionChanged;
					_activeTextBox.TextChanged -= OnActiveTextBoxTextChanged;
				}

				_activeTextBox = value;
				Editable?.Clear();

				if (_activeTextBox is not null)
				{
					Editable?.Append(_activeTextBox.Text);
					Selection.SetSelection(Editable, _activeTextBox.SelectionStart, _activeTextBox.SelectionStart + _activeTextBox.SelectionLength);

					_activeTextBox.SelectionChanged += OnActiveTextBoxSelectionChanged;
					_activeTextBox.TextChanged += OnActiveTextBoxTextChanged;
				}
			}
		}

		private void OnActiveTextBoxTextChanged(object sender, Microsoft.UI.Xaml.Controls.TextChangedEventArgs e)
		{
			if (_activeTextBox is not null && Editable is not null)
			{
				this.LogDebug()?.Debug($"OnActiveTextBoxTextChanged: {_activeTextBox.Text}");

				Editable.Clear();
				Editable.Append(_activeTextBox.Text);
				SetSelection(_activeTextBox.SelectionStart, _activeTextBox.SelectionStart + _activeTextBox.SelectionLength);
			}
		}

		private void OnActiveTextBoxSelectionChanged(object sender, RoutedEventArgs e)
		{
			if (_activeTextBox is not null
				&& Editable is not null
				&& !_endingBatch)
			{
				this.LogDebug()?.Debug($"OnActiveTextBoxSelectionChanged: {_activeTextBox.SelectionStart}->{_activeTextBox.SelectionStart + _activeTextBox.SelectionLength}");

				var length = Editable.Length() + 1;

				_duringTextBoxSelectionChanged = true;
				SetSelection(
					Math.Min(length, _activeTextBox.SelectionStart),
					Math.Min(length, _activeTextBox.SelectionStart + _activeTextBox.SelectionLength));
				_duringTextBoxSelectionChanged = false;
			}
		}

		public override bool EndBatchEdit()
		{
			_endingBatch = true;

			var selectionStart = Selection.GetSelectionStart(Editable);
			var selectionEnd = Selection.GetSelectionEnd(Editable);

			this.LogDebug()?.Debug($"EndBatchEdit: {Editable?.ToString()} ({selectionStart}->{selectionEnd})");

			if (ActiveTextBox is not null)
			{
				ActiveTextBox.ProcessTextInput(Editable?.ToString() ?? string.Empty);
				ActiveTextBox.Select(selectionStart, selectionEnd - selectionStart);
			}

			_endingBatch = false;

			return false;
		}

		public override bool SetSelection(int start, int end)
		{
			var ret = base.SetSelection(start, end);

			if (ActiveTextBox is not null
				&& !_duringTextBoxSelectionChanged)
			{
				this.LogDebug()?.Debug($"SetSelection: {Editable?.ToString()} ({start}->{end})");

				var selectionStart = Selection.GetSelectionStart(Editable);
				var selectionEnd = Selection.GetSelectionEnd(Editable);

				ActiveTextBox.Select(selectionStart, selectionEnd - selectionStart);
			}

			return ret;
		}

		public override bool SendKeyEvent(KeyEvent? e)
		{
			this.LogDebug()?.Debug($"SendKeyEvent {e?.Action} {e?.KeyCode}");

			return base.SendKeyEvent(e);
		}

		public override bool RequestCursorUpdates(int cursorUpdateMode)
		{
			this.LogDebug()?.Debug($"RequestCursorUpdates {cursorUpdateMode}");

			return base.RequestCursorUpdates(cursorUpdateMode);
		}

		public override bool BeginBatchEdit()
		{
			this.LogDebug()?.Debug($"BeginBatchEdit");

			return base.BeginBatchEdit();
		}

		public override bool ClearMetaKeyStates(MetaKeyStates states)
		{
			this.LogDebug()?.Debug($"ClearMetaKeyStates {states}");

			return base.ClearMetaKeyStates(states);
		}

		public override void CloseConnection()
		{
			this.LogDebug()?.Debug($"CloseConnection");

			base.CloseConnection();
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

			return base.CommitText(text, newCursorPosition);
		}

		public override bool DeleteSurroundingText(int beforeLength, int afterLength)
		{
			this.LogDebug()?.Debug($"DeleteSurroundingText {beforeLength}->{afterLength}");

			return base.DeleteSurroundingText(beforeLength, afterLength);
		}

		public override bool DeleteSurroundingTextInCodePoints(int beforeLength, int afterLength)
		{
			this.LogDebug()?.Debug($"DeleteSurroundingTextInCodePoints {beforeLength}->{afterLength}");

			return base.DeleteSurroundingTextInCodePoints(beforeLength, afterLength);
		}

		public override bool FinishComposingText()
		{
			this.LogDebug()?.Debug($"FinishComposingText");

			return base.FinishComposingText();
		}

		public override CapitalizationMode GetCursorCapsMode(CapitalizationMode reqModes)
		{
			this.LogDebug()?.Debug($"GetCursorCapsMode {reqModes}");

			return base.GetCursorCapsMode(reqModes);
		}

		public override ExtractedText? GetExtractedText(ExtractedTextRequest? request, GetTextFlags flags)
		{
			var ret = base.GetExtractedText(request, flags);

			this.LogDebug()?.Debug($"GetExtractedText [{request?.Token}], {flags}: {ret?.SelectionStart ?? -1}->{ret?.SelectionEnd ?? -1} {ret?.PartialStartOffset ?? -1}->{ret?.PartialEndOffset ?? -1}");

			return ret;
		}

		public override Java.Lang.ICharSequence? GetSelectedTextFormatted(GetTextFlags flags)
		{
			var ret = base.GetSelectedTextFormatted(flags);

			_endingBatch = true;

			var selectionStart = Selection.GetSelectionStart(Editable);
			var selectionEnd = Selection.GetSelectionEnd(Editable);

			this.LogDebug()?.Debug($"GetSelectedTextFormatted {flags} = {selectionStart}->{selectionEnd}");

			if (ActiveTextBox is not null)
			{
				ActiveTextBox.Select(selectionStart, selectionEnd - selectionStart);
			}

			_endingBatch = false;

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

			return base.PerformContextMenuAction(id);
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

			return base.ReplaceText(start, end, text, newCursorPosition, textAttribute);
		}

		public override bool ReportFullscreenMode(bool enabled)
		{
			this.LogDebug()?.Debug($"ReportFullscreenMode {enabled}");

			return base.ReportFullscreenMode(enabled);
		}

		public override bool SetComposingRegion(int start, int end)
		{
			this.LogDebug()?.Debug($"SetComposingRegion {start}->{end}");

			return base.SetComposingRegion(start, end);
		}

		public override bool SetComposingText(Java.Lang.ICharSequence? text, int newCursorPosition)
		{
			this.LogDebug()?.Debug($"SetComposingText {text}, {newCursorPosition}");

			return base.SetComposingText(text, newCursorPosition);
		}

		public override TextSnapshot? TakeSnapshot()
		{
			this.LogDebug()?.Debug($"TakeSnapshot");

			return base.TakeSnapshot();
		}
	}
}
