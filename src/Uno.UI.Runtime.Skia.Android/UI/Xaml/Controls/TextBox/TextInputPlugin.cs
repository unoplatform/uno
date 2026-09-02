using System;
using Android.Content;
using Android.OS;
using Android.Text;
using Android.Views;
using Android.Views.Autofill;
using Android.Views.InputMethods;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Controls;
using Uno.UI.Xaml.Controls.Extensions;

namespace Uno.UI.Runtime.Skia.Android;

internal sealed class TextInputPlugin
{
	private readonly View _view;
	private readonly InputMethodManager? _imm;
	private readonly AutofillManager? _afm;
	private InputTypes _inputTypes = InputTypes.TextVariationNormal;
	private ImeAction _imeAction;
	private TextInputConnection? _inputConnection;
	private EditorInfo? _editorInfo;
	private IImeSessionHost? _activeHost;
	private (int Start, int End)? _pendingCompositionRange;

	/// <summary>
	/// Gets the currently active <see cref="TextInputConnection"/>, if any.
	/// Used by <see cref="AndroidImeTextBoxExtension"/> to subscribe to composition state changes.
	/// </summary>
	internal TextInputConnection? ActiveInputConnection => _inputConnection;

	/// <summary>
	/// Raised when a new <see cref="TextInputConnection"/> is created (e.g., when the system
	/// calls <c>OnCreateInputConnection</c>). The <see cref="AndroidImeTextBoxExtension"/>
	/// uses this to re-subscribe to composition state changes on the new connection.
	/// </summary>
	internal event EventHandler<TextInputConnectionCreatedEventArgs>? InputConnectionCreated;

	internal TextInputPlugin(View view)
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
		SetActiveHost(textBox);

		if (_afm == null || _imm is null)
		{
			return;
		}

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

	internal void NotifyViewExited(TextBox textBox, int virtualId)
	{
		ClearActiveHost(textBox);

		if (_afm == null)
		{
			return;
		}

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
		this.LogDebug()?.Debug($"NotifyValueChanged: {newValue}");

		_afm?.NotifyValueChanged(_view, virtualId, AutofillValue.ForText(newValue));
		_inputConnection?.OnTextInputHostChanged();
	}

	internal void NotifySelectionChanged(TextBox textBox)
	{
		if (ReferenceEquals(_activeHost, textBox))
		{
			_inputConnection?.OnTextInputHostChanged();
		}
	}

	internal void StartImeSession(IImeSessionHost host, ImeSessionActivation activation)
	{
		SetActiveHost(host);
		if (!activation.IsSoftwareKeyboardSuppressed)
		{
			ShowTextInput(host);
		}
	}

	internal void EndImeSession(IImeSessionHost host) => ClearActiveHost(host);

	internal void UpdateImeSession(IImeSessionHost host, ImeSessionUpdate update)
	{
		if (!ReferenceEquals(_activeHost, host))
		{
			return;
		}

		if ((update & (
			ImeSessionUpdate.InputScope |
			ImeSessionUpdate.TextPrediction |
			ImeSessionUpdate.AcceptsReturn |
			ImeSessionUpdate.SpellCheck)) != 0)
		{
			UpdateInputOptions(host);
			_pendingCompositionRange = _inputConnection?.GetComposingRange();
			_imm?.RestartInput(_view);
		}

		if ((update & ImeSessionUpdate.TextAndSelection) != 0)
		{
			_inputConnection?.OnTextInputHostChanged();
			_inputConnection?.SendCursorAnchorInfo();
		}

		if ((update & ImeSessionUpdate.CandidateWindowAlignment) != 0)
		{
			_inputConnection?.SendCursorAnchorInfo();
		}
	}

	internal void ShowTextInput(IImeSessionHost host)
	{
		UpdateInputOptions(host);
		SetActiveHost(host);
		_view.RequestFocus();
		_imm?.ShowSoftInput(_view, 0);
	}

	private void SetActiveHost(IImeSessionHost host)
	{
		UpdateInputOptions(host);
		var changed = !ReferenceEquals(_activeHost, host);
		_activeHost = host;
		if (_inputConnection is { } connection)
		{
			connection.ActiveHost = host;
		}

		if (changed)
		{
			_imm?.RestartInput(_view);
		}
	}

	private void ClearActiveHost(IImeSessionHost host)
	{
		if (!ReferenceEquals(_activeHost, host))
		{
			return;
		}

		_activeHost = null;
		_pendingCompositionRange = null;
		if (_inputConnection is { } connection && ReferenceEquals(connection.ActiveHost, host))
		{
			connection.ActiveHost = null;
		}
	}

	private void UpdateInputOptions(IImeSessionHost host)
	{
		_inputTypes = ConvertInputScope(host);
		_imeAction = host switch
		{
			TextBox textBox => TextBoxExtensions.GetInputReturnType(textBox).ToImeAction(),
			{ AcceptsReturn: false } => ImeAction.Done,
			_ => ImeAction.None,
		};

		if (_editorInfo is not null)
		{
			ApplyInputOptions(_editorInfo);
		}
	}

	private void ApplyInputOptions(EditorInfo editorInfo)
	{
		editorInfo.InputType = _inputTypes;
		editorInfo.ImeOptions = ImeFlags.NoFullscreen;

		if (_imeAction != ImeAction.None)
		{
			editorInfo.ImeOptions |= (ImeFlags)_imeAction;
		}
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

	internal static InputTypes ConvertInputScope(IImeSessionHost host)
	{
		var firstInputScope = host.InputScope.GetFirstInputScopeNameValue();

		if (firstInputScope is InputScopeNameValue.DateDayNumber or InputScopeNameValue.DateMonthNumber or InputScopeNameValue.DateYear)
		{
			return InputTypes.ClassDatetime;
		}
		else if (firstInputScope is InputScopeNameValue.Number or InputScopeNameValue.NumericPin)
		{
			return InputTypes.ClassNumber;
		}
		else if (firstInputScope is InputScopeNameValue.CurrencyAmount)
		{
			return InputTypes.ClassNumber | InputTypes.NumberFlagDecimal;
		}
		else if (firstInputScope is InputScopeNameValue.NumberFullWidth)
		{
			// Android has no InputType that accepts numbers and punctuation other than "Phone" and "Text".
			// "Phone" is the closest one to what we're looking for here, but phone-specific keys could be confusing in some cases.
			return InputTypes.ClassPhone;
		}
		else if (firstInputScope == InputScopeNameValue.TelephoneNumber)
		{
			return InputTypes.ClassPhone;
		}

		var textType = InputTypes.ClassText;
		if (host.AcceptsReturn)
		{
			textType |= InputTypes.TextFlagMultiLine;
		}
		else if (firstInputScope is InputScopeNameValue.EmailNameOrAddress or InputScopeNameValue.EmailSmtpAddress)
		{
			textType |= InputTypes.TextVariationEmailAddress;
		}
		else if (firstInputScope == InputScopeNameValue.Url
			|| firstInputScope == InputScopeNameValue.Search)
		{
			textType |= InputTypes.TextVariationUri;
		}
		else if (host is PasswordBox { PasswordRevealMode: PasswordRevealMode.Visible })
		{
			textType |= InputTypes.TextVariationVisiblePassword;
		}
		else if (firstInputScope == InputScopeNameValue.PersonalFullName)
		{
			textType |= InputTypes.TextVariationPersonName;
		}
		else if (firstInputScope == InputScopeNameValue.Maps)
		{
			textType |= InputTypes.TextVariationPostalAddress;
		}

		if (host is PasswordBox)
		{
			// Note: both required. Some devices ignore TYPE_TEXT_FLAG_NO_SUGGESTIONS.
			textType |= InputTypes.TextFlagNoSuggestions;
			textType |= InputTypes.TextVariationPassword;
		}
		else
		{
			if (host.IsSpellCheckEnabled && host.IsTextPredictionEnabled)
			{
				textType |= InputTypes.TextFlagAutoCorrect;
			}

			// Not yet supported
			//if (!enableSuggestions)
			//{
			//	// Note: both required. Some devices ignore TYPE_TEXT_FLAG_NO_SUGGESTIONS.
			//	textType |= InputTypes.TYPE_TEXT_FLAG_NO_SUGGESTIONS;
			//	textType |= InputTypes.TYPE_TEXT_VARIATION_VISIBLE_PASSWORD;
			//}
		}

		if (host.CharacterCasing == CharacterCasing.Upper)
		{
			textType |= InputTypes.TextFlagCapCharacters;
		}
		// Not yet supported
		//else if (textCapitalization == TextInputChannel.TextCapitalization.WORDS)
		//{
		//	textType |= InputTypes.TYPE_TEXT_FLAG_CAP_WORDS;
		//}
		//else if (textCapitalization == TextInputChannel.TextCapitalization.SENTENCES)
		//{
		//	textType |= InputTypes.TYPE_TEXT_FLAG_CAP_SENTENCES;
		//}

		return textType;
	}

	internal void HideTextInput()
	{
		_imm?.HideSoftInputFromWindow(_view.ApplicationWindowToken, 0);
	}

	internal IInputConnection? OnCreateInputConnection(EditorInfo? editorInfo)
	{
		if (editorInfo is not null)
		{
			_editorInfo = editorInfo;
			ApplyInputOptions(_editorInfo);
		}

		_inputConnection = new TextInputConnection(_view, editorInfo ?? new(), HandleKeyEvent);
		_inputConnection.ActiveHost = _activeHost;
		if (_pendingCompositionRange is { } compositionRange && _activeHost?.IsComposing == true)
		{
			_inputConnection.RestoreComposingRange(compositionRange.Start, compositionRange.End);
		}
		_pendingCompositionRange = null;
		InputConnectionCreated?.Invoke(this, new TextInputConnectionCreatedEventArgs(_inputConnection));
		return _inputConnection;
	}

	public bool HandleKeyEvent(KeyEvent? keyEvent)
	{
		if (!(_imm?.IsAcceptingText ?? false) || _inputConnection == null)
		{
			return false;
		}

		// Send the KeyEvent as an IME KeyEvent. If the input connection is an
		// InputConnectionAdaptor then call its handleKeyEvent method (because
		// this method will be called by the keyboard manager, and
		// InputConnectionAdaptor#sendKeyEvent forwards the key event back to the
		// keyboard manager).

		if (keyEvent is not null
			&& _inputConnection is TextInputConnection inputConnection)
		{
			var handled = inputConnection.handleKeyEvent(keyEvent);

			if (!handled && (keyEvent.KeyCode == Keycode.Enter || keyEvent.KeyCode == Keycode.NumpadEnter))
			{
				// When using soft keyboards, we don't get KeyUp/KeyDown events. Instead,
				// the input method sends us updates with the updated text values. We
				// add this hack here specifically to support InputExtensions in
				// Toolkit, which listens to KeyUp/KeyDown events to operate, but it would
				// be more reasonable to treat Enter like any other key.
				return ApplicationActivity.Instance.DispatchKeyEvent(keyEvent);
			}
		}

		return false;
	}
}

internal sealed class TextInputConnectionCreatedEventArgs : EventArgs
{
	internal TextInputConnectionCreatedEventArgs(TextInputConnection connection) => Connection = connection;

	internal TextInputConnection Connection { get; }
}
