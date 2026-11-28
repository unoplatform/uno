using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Android.Content;
using Android.OS;
using Android.Text;
using Android.Util;
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

namespace Uno.UI.Runtime.Android;

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
	private bool _isAutofillStateLogged;
	private bool _hasAutofillSession;

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

	// Visibility changes only matter once an autofill service is selected and a field was focused (API 27 for the virtual overload).
	internal bool IsTrackingAutofillVisibility
		=> _hasAutofillSession && OperatingSystem.IsAndroidVersionAtLeast(27) && _afm is { IsEnabled: true };

	internal void NotifyViewEntered(TextBoxCore textBox, int virtualId)
	{
		SetActiveHost(textBox);
		LogAutofillStateOnce();

		if (_afm == null || _imm is null)
		{
			return;
		}

		if (!_hasAutofillSession)
		{
			_hasAutofillSession = true;
			// Layout passes weren't walked until now: report the fields collapsed in the meantime.
			AndroidSkiaTextBoxNotificationsProviderSingleton.Instance.ReportAllVisibility();
		}

		NotifyAutofillViewEntered(_afm, textBox, virtualId);
	}

	// Android expires the fill response when the window loses focus (e.g. Home and back), and a native view
	// announces itself again when it regains it. The focus never moved inside the app, so do it for the focused TextBox.
	internal void OnWindowFocusGained()
	{
		try
		{
			if (_afm is not null && _hasAutofillSession && _activeHost is TextBoxCore textBox)
			{
				NotifyAutofillViewEntered(_afm, textBox, AndroidSkiaTextBoxNotificationsProviderSingleton.GetVirtualId(textBox));
			}
		}
		catch (Exception e)
		{
			ReportAutofillFailure(e, nameof(OnWindowFocusGained));
		}
	}

	private void NotifyAutofillViewEntered(AutofillManager afm, TextBoxCore textBox, int virtualId)
	{
		var physicalRect = GetPhysicalRect(textBox.Owner);
		var offset = new int[2];
		_view.GetLocationOnScreen(offset);
		physicalRect = physicalRect.OffsetRect(offset[0], offset[1]);
		afm.NotifyViewEntered(_view, virtualId, new((int)physicalRect.Left, (int)physicalRect.Top, (int)physicalRect.Right, (int)physicalRect.Bottom));
	}

	// Otherwise every autofill call silently does nothing, which looks the same as a broken structure.
	private void LogAutofillStateOnce()
	{
		if (_isAutofillStateLogged || !this.Log().IsEnabled(LogLevel.Debug))
		{
			return;
		}

		_isAutofillStateLogged = true;
		if (_afm is null)
		{
			this.Log().Debug("Autofill unavailable: no AutofillManager (requires API 26)");
		}
		else if (!_afm.IsEnabled)
		{
			this.Log().Debug("Autofill disabled: no autofill service is selected on the device");
		}
	}

	// Bounds in physical pixels, relative to the render view.
	private static Windows.Foundation.Rect GetPhysicalRect(Control textBox)
	{
		var transform = UIElement.GetTransform(from: textBox, to: null);
		var logicalRect = transform.Transform(new Windows.Foundation.Rect(default, new Windows.Foundation.Size(textBox.Visual.Size.X, textBox.Visual.Size.Y)));
		return logicalRect.LogicalToPhysicalPixels();
	}

	internal void NotifyViewExited(TextBoxCore textBox, int virtualId)
	{
		ClearActiveHost(textBox);

		if (_afm == null)
		{
			return;
		}

		_afm.NotifyViewExited(_view, virtualId);
	}

	internal void NotifyViewVisibilityChanged(int virtualId, bool isVisible)
	{
		// Unlike the rest of AutofillManager (API 26), the virtual-view overload was added in API 27.
		if (OperatingSystem.IsAndroidVersionAtLeast(27))
		{
			_afm?.NotifyViewVisibilityChanged(_view, virtualId, isVisible);
		}
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
		// Never log the value: it can be a password.
		this.LogDebug()?.Debug($"NotifyValueChanged: {virtualId}");

		_afm?.NotifyValueChanged(_view, virtualId, AutofillValue.ForText(newValue));
		_inputConnection?.OnTextInputHostChanged();
	}

	internal void NotifySelectionChanged(TextBoxCore textBox)
	{
		if (ReferenceEquals(_activeHost, textBox))
		{
			_inputConnection?.OnTextInputHostChanged();
		}
	}

	internal void StartImeSession(IImeSessionHost host, ImeSessionActivation activation)
	{
		SetActiveHost(host);
		if (!activation.IsSoftwareKeyboardSuppressed && host.CanAcceptTextInput)
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
			TextBoxCore core => TextBoxExtensions.GetInputReturnType(core.Owner).ToImeAction(),
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

	// Called by Android: an exception must not cross back into Java, an autofill service would crash the app.
	internal void OnProvideAutofillVirtualStructure(ViewStructure? structure)
	{
		if (structure?.AutofillId is not { } parentId)
		{
			return;
		}

		try
		{
			ProvideAutofillVirtualStructure(structure, parentId);
		}
		catch (Exception e)
		{
			ReportAutofillFailure(e, nameof(OnProvideAutofillVirtualStructure));
		}
	}

	internal void Autofill(SparseArray values)
	{
		try
		{
			for (var i = 0; i < values.Size(); i++)
			{
				if (TryGetOwnedVisibleTextBox(values.KeyAt(i), out var textBox)
					&& textBox.Owner.IsEnabled
					&& !textBox.IsReadOnly
					&& values.ValueAt(i) is AutofillValue { IsText: true } value)
				{
					try
					{
						textBox.Text = value.TextValue ?? string.Empty;
					}
					catch (Exception e)
					{
						// Setting Text runs app code (TextChanging, bindings): don't skip the remaining fields (e.g. the password).
						if (this.Log().IsEnabled(LogLevel.Error))
						{
							this.Log().Error($"{nameof(Autofill)} failed for a field", e);
						}

						Application.Current?.RaiseRecoverableUnhandledException(e);
					}
				}
			}
		}
		catch (Exception e)
		{
			ReportAutofillFailure(e, nameof(Autofill));
		}
	}

	private void ReportAutofillFailure(Exception e, string callback)
	{
		if (this.Log().IsEnabled(LogLevel.Error))
		{
			this.Log().Error($"{callback} failed", e);
		}

		Application.Current?.RaiseRecoverableUnhandledException(e);
	}

	private bool TryGetOwnedVisibleTextBox(int virtualId, [NotNullWhen(true)] out TextBoxCore? textBox)
		=> AndroidSkiaTextBoxNotificationsProviderSingleton.Instance.TryGetLiveTextBox(virtualId, out textBox)
			&& ReferenceEquals(AndroidSkiaTextBoxNotificationsProviderSingleton.GetOwningTextInputPlugin(textBox.Owner.XamlRoot), this)
			&& AndroidSkiaTextBoxNotificationsProviderSingleton.IsEffectivelyVisible(textBox);

	// Android re-reads the visibility of its tracked virtual fields on resume, and the default says "visible".
	internal bool IsVisibleToUserForAutofill(int virtualId)
	{
		try
		{
			return TryGetOwnedVisibleTextBox(virtualId, out _);
		}
		catch (Exception e)
		{
			ReportAutofillFailure(e, nameof(IsVisibleToUserForAutofill));
			return true;
		}
	}

	private void ProvideAutofillVirtualStructure(ViewStructure structure, AutofillId parentId)
	{
		var fields = GetAutofillFields();
		var usernameFields = GetUsernameFieldIndexes(fields);
		var loggedHints = this.Log().IsEnabled(LogLevel.Debug) ? new string[fields.Count] : null;

		// Everything that can throw is computed before AddChildCount: a failure later would leave promised children unfilled.
		var hintsPerField = new string[]?[fields.Count];
		var inputTypes = new int[fields.Count];
		for (var i = 0; i < fields.Count; i++)
		{
			hintsPerField[i] = GetAutofillHints(fields[i].TextBox, usernameFields.Contains(i));
			inputTypes[i] = (int)ConvertInputScope(fields[i].TextBox);
		}

		var index = structure.AddChildCount(fields.Count);
		for (var i = 0; i < fields.Count; i++)
		{
			var (textBox, physicalRect, isVisible) = fields[i];
			var child = structure.NewChild(index + i);
			if (child is null)
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"Autofill structure: NewChild({index + i}) returned null");
				}

				continue;
			}

			// Must match the ids used by NotifyViewEntered/NotifyValueChanged and View.Autofill.
			child.SetAutofillId(parentId, AndroidSkiaTextBoxNotificationsProviderSingleton.GetVirtualId(textBox));
			child.SetClassName("android.widget.EditText");
			// User-entered text is sensitive (like a native EditText): stripped from fill requests, still sent for saving.
			child.SetDataIsSensitive(true);
			child.SetAutofillType(AutofillType.Text);
			child.SetAutofillValue(AutofillValue.ForText(textBox.Text ?? string.Empty));
			child.SetInputType(inputTypes[i]);
			child.SetEnabled(textBox.Owner.IsEnabled && !textBox.IsReadOnly);
			child.SetVisibility(isVisible ? ViewStates.Visible : ViewStates.Gone);

			var hints = hintsPerField[i];
			if (hints is not null)
			{
				child.SetAutofillHints(hints);
			}

			if (loggedHints is not null)
			{
				loggedHints[i] = (isVisible ? "" : "gone:") + (hints is not null ? string.Join("|", hints) : "none");
			}

			child.SetDimens((int)physicalRect.Left, (int)physicalRect.Top, 0, 0, (int)physicalRect.Width, (int)physicalRect.Height);
		}

		// Never log the values, only the shape the autofill service gets.
		if (loggedHints is not null)
		{
			this.Log().Debug($"Autofill structure: {fields.Count} fields [{string.Join(", ", loggedHints)}]");
		}
	}

	private readonly record struct AutofillField(TextBoxCore TextBox, Windows.Foundation.Rect PhysicalRect, bool IsVisible);

	// The TextBoxes of this view's window in screen order (top to bottom, then left to right), hidden ones last.
	private List<AutofillField> GetAutofillFields()
	{
		var fields = new List<AutofillField>();
		foreach (var textBox in AndroidSkiaTextBoxNotificationsProviderSingleton.Instance.GetLiveTextBoxes())
		{
			if (!ReferenceEquals(AndroidSkiaTextBoxNotificationsProviderSingleton.GetOwningTextInputPlugin(textBox.Owner.XamlRoot), this))
			{
				continue;
			}

			var isVisible = AndroidSkiaTextBoxNotificationsProviderSingleton.IsEffectivelyVisible(textBox);
			fields.Add(new(textBox, isVisible ? GetPhysicalRect(textBox.Owner) : default, isVisible));
		}

		return fields
			.OrderBy(f => !f.IsVisible)
			.ThenBy(f => f.PhysicalRect.Top)
			.ThenBy(f => f.PhysicalRect.Left)
			.ToList();
	}

	// The username of a login form: the single visible field right above a password field and overlapping it horizontally.
	// Nothing is guessed when several fields share that row (e.g. first name | last name) or when the field above is a password.
	private static HashSet<int> GetUsernameFieldIndexes(List<AutofillField> fields)
	{
		var usernames = new HashSet<int>();
		for (var p = 0; p < fields.Count; p++)
		{
			if (fields[p] is not { IsVisible: true, TextBox.IsPassword: true, PhysicalRect: var password })
			{
				continue;
			}

			var above = -1;
			for (var i = 0; i < fields.Count; i++)
			{
				if (i == p || !fields[i].IsVisible)
				{
					continue;
				}

				var rect = fields[i].PhysicalRect;
				if (rect.Bottom <= password.Top + 1
					&& rect.Left < password.Right
					&& rect.Right > password.Left
					&& (above < 0 || rect.Bottom > fields[above].PhysicalRect.Bottom))
				{
					above = i;
				}
			}

			if (above < 0 || fields[above].TextBox.IsPassword)
			{
				continue;
			}

			var candidate = fields[above].PhysicalRect;
			var isAloneInItsRow = true;
			for (var i = 0; i < fields.Count && isAloneInItsRow; i++)
			{
				var rect = fields[i].PhysicalRect;
				isAloneInItsRow = i == above || !fields[i].IsVisible || rect.Top >= candidate.Bottom || rect.Bottom <= candidate.Top;
			}

			if (isAloneInItsRow)
			{
				usernames.Add(above);
			}
		}

		return usernames;
	}

	// No public API to set autofill hints yet: derived from the password mode and input scope, like the iOS TextContentType.
	private static string[]? GetAutofillHints(TextBoxCore textBox, bool isUsernameField)
	{
		if (textBox.IsPassword)
		{
			return [View.AutofillHintPassword];
		}

		return textBox.InputScope.GetFirstInputScopeNameValue() switch
		{
			InputScopeNameValue.EmailNameOrAddress or InputScopeNameValue.EmailSmtpAddress => [View.AutofillHintEmailAddress],
			InputScopeNameValue.TelephoneNumber => [View.AutofillHintPhone],
			InputScopeNameValue.PersonalFullName => [View.AutofillHintName],
			// Some services (Google) don't recognize the username of a login form without the hint.
			InputScopeNameValue.Default or InputScopeNameValue.Text when isUsernameField => [View.AutofillHintUsername],
			// Left to the autofill service heuristics (inputType).
			_ => null,
		};
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
		else if (host is TextBoxCore { IsPassword: true, IsPasswordRevealed: true })
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

		if (host is TextBoxCore { IsPassword: true })
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
				return (_view.Context as global::Android.App.Activity)?.DispatchKeyEvent(keyEvent) ?? false;
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
