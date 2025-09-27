using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content.Res;
using Android.Graphics;
using Android.Runtime;
using Android.Text;
using Android.Text.Method;
using Android.Util;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using Uno.Disposables;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.DataBinding;
using Uno.UI.Extensions;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Text;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using static Android.Widget.TextView;
using Math = System.Math;
using Uno.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class TextBox : View.IOnFocusChangeListener, IOnEditorActionListener
	{
		private int _keyboardAccessDelay = 50;
		private TextBoxView _textBoxView;
		private readonly SerialDisposable _keyboardDisposable = new SerialDisposable();
		private Factory _editableFactory;

		/// <summary>
		/// If true, and <see cref="IsSpellCheckEnabled"/> is false, take vigorous measures to ensure that spell-check (ie predictive text) is
		/// really disabled.
		/// </summary>
		/// <remarks>
		/// Specifically, when true, and <see cref="IsSpellCheckEnabled"/> is false, this sets <see cref="InputTypes.TextVariationPassword"/> on
		/// the inner <see cref="TextBoxView"/>. This is required because a number of OEM keyboards (particularly on older devices?) ignore
		/// the <see cref="InputTypes.TextFlagNoSuggestions"/>. It's optional because setting the password InputType is a workaround which is
		/// known to cause issues in certain circumstances. See discussion here: https://stackoverflow.com/a/5188119/1902058
		/// </remarks>
		[Uno.UnoOnly]
		public bool ShouldForceDisableSpellCheck { get; set; } = true;

		internal TextBoxView TextBoxView => _textBoxView;

		/// <summary>
		/// Both IsReadOnly = true and IsTabStop = false make the native TextBoxView read-only.
		/// </summary>
		internal bool IsNativeViewReadOnly => IsReadOnly || !IsTabStop;

		public bool PreventKeyboardDisplayOnProgrammaticFocus
		{
			get => (bool)GetValue(PreventKeyboardDisplayOnProgrammaticFocusProperty);
			set => SetValue(PreventKeyboardDisplayOnProgrammaticFocusProperty, value);
		}

		public static DependencyProperty PreventKeyboardDisplayOnProgrammaticFocusProperty { get; } =
			DependencyProperty.Register(
				nameof(PreventKeyboardDisplayOnProgrammaticFocus),
				typeof(bool),
				typeof(TextBox),
				new FrameworkPropertyMetadata(false));

		partial void OnUnloadedPartial()
		{
			if (_textBoxView != null)
			{
				_textBoxView.OnFocusChangeListener = null;

				// We always force lose the focus when unloading the control.
				// This is required as the FocusChangedListener may not be called
				// when the unloaded propagation is done on the .NET side (see
				// FeatureConfiguration.FrameworkElement.AndroidUseManagedLoadedUnloaded for
				// more details.
				ProcessFocusChanged(false);
			}
		}

		partial void InitializePropertiesPartial()
		{
			OnImeOptionsChanged(ImeOptions);
		}

		private void UpdateTextBoxView()
		{
			if (_textBoxView == null && _contentElement != null)
			{
				_textBoxView = _contentElement.GetChildren().FirstOrDefault() as TextBoxView;

				if (_textBoxView == null)
				{
					_textBoxView = new TextBoxView(this);

					_contentElement.Content = _textBoxView;
					_textBoxView.SetTextNative(Text);

					_editableFactory = _editableFactory ?? new Factory(WeakReferencePool.RentSelfWeakReference(this));
					_textBoxView.SetEditableFactory(_editableFactory);
				}

				SetupTextBoxView();
			}
		}

		public ImeAction ImeOptions
		{
			get { return (ImeAction)this.GetValue(ImeOptionsProperty); }
			set { this.SetValue(ImeOptionsProperty, value); }
		}

		public static DependencyProperty ImeOptionsProperty { get; } =
			DependencyProperty.Register(
				nameof(ImeOptions),
				typeof(ImeAction),
				typeof(TextBox),
				new FrameworkPropertyMetadata(
					defaultValue: ImeAction.Unspecified,
					coerceValueCallback: CoerceImeOptions,
					propertyChangedCallback: (s, args) => ((TextBox)s)?.OnImeOptionsChanged((ImeAction)args.NewValue)
				)
			);

		private static object CoerceImeOptions(DependencyObject dependencyObject, object baseValue, DependencyPropertyValuePrecedences _)
		{
			return dependencyObject is TextBox textBox && textBox.InputScope?.GetFirstInputScopeNameValue() == InputScopeNameValue.Search
				? ImeAction.Search
				: baseValue;
		}

		private void OnImeOptionsChanged(ImeAction imeAction)
		{
			if (_textBoxView != null)
			{
				_textBoxView.ImeOptions = imeAction;
			}
		}

		partial void SetInputReturnTypePlatform(InputReturnType inputReturnType) =>
			OnImeOptionsChanged(inputReturnType.ToImeAction());

		partial void OnFocusStateChangedPartial(FocusState focusState, bool initial)
		{
			if (_textBoxView == null)
			{
				return;
			}

			if (focusState == FocusState.Unfocused)
			{
				if (_textBoxView.IsFocused)
				{
					_textBoxView.ClearFocus();
				}
			}
			else
			{
				if (!_textBoxView.IsFocused)
				{
					using (focusState == FocusState.Programmatic ? PreventKeyboardDisplayIfSet() : null)
					{
						_textBoxView.RequestFocus();

						var selectionStart = this.SelectionStart;

						if (selectionStart == 0)
						{
							int cursorPosition = selectionStart + _textBoxView?.Text?.Length ?? 0;

							this.Select(cursorPosition, 0);
						}
					}
				}
			}
		}

		public override bool RequestFocus(FocusSearchDirection direction, Rect previouslyFocusedRect)
		{
			using (PreventKeyboardDisplayIfSet())
			{
				var wantsFocus = (_textBoxView?.RequestFocus(direction, previouslyFocusedRect))
						.GetValueOrDefault(false);
				if (wantsFocus && FocusState == FocusState.Unfocused)
				{
					Focus(FocusState.Programmatic);
				}
				return wantsFocus;
			}
		}

		partial void SelectPartial(int start, int length)
			=> _textBoxView.SetSelection(start: start, stop: start + length);

		partial void SelectAllPartial() => _textBoxView.SelectAll();

		/// <summary>
		/// Applies PreventKeyboardDisplayOnProgrammaticFocus by temporarily disabling soft input display.
		/// </summary>
		private IDisposable PreventKeyboardDisplayIfSet()
		{
			if (!PreventKeyboardDisplayOnProgrammaticFocus || _textBoxView == null)
			{
				return null;
			}

			var before = _textBoxView.ShowSoftInputOnFocus;
			_textBoxView.ShowSoftInputOnFocus = false;
			return Disposable.Create(() => _textBoxView.ShowSoftInputOnFocus = before);
		}

		partial void OnInputScopeChangedPartial(InputScope newValue)
		{
			this.CoerceValue(ImeOptionsProperty);

			if (newValue != null)
			{
				UpdateInputScope(newValue);
			}
		}

		protected void SetInputScope(InputTypes types)
		{
			if (_textBoxView != null)
			{
				_textBoxView.SetInputTypes(types, types);

				if (!types.HasPasswordFlag())
				{
					UpdateFontPartial();
				}
			}
		}

		protected void SetSelection(int position)
		{
			if (_textBoxView != null)
			{
				_textBoxView.SetSelection(position);
			}
		}

		public int SelectionStart
		{
			get
			{
				return _textBoxView?.SelectionStart ?? 0;
			}
			set
			{
				if (_textBoxView != null)
				{
					if (FocusState == FocusState.Unfocused)
					{
						Focus(FocusState.Programmatic);
					}

					if (SelectionLength > 0)
					{
						_textBoxView.SetSelection(
							Math.Min(_textBoxView.Text.Length, value),
							Math.Min(_textBoxView.Text.Length, value + SelectionLength));
					}
					else
					{
						_textBoxView.SetSelection(
							Math.Min(_textBoxView.Text.Length, value)
						);
					}
				}
			}
		}

		public int SelectionLength
		{
			get
			{
				if (_textBoxView != null)
				{
					return _textBoxView.SelectionEnd - _textBoxView.SelectionStart;
				}
				return 0;
			}
			set
			{
				if (_textBoxView != null)
				{
					// A Windows Apps TextBox throws an ArgumentException when the value is negative (in Android native, only when SelectionEnd is negative)
					if (value < 0)
					{
						throw new ArgumentException("SelectionLength cannot be negative");
					}

					if (FocusState == FocusState.Unfocused)
					{
						Focus(FocusState.Programmatic);
					}

					_textBoxView.SetSelection(
						_textBoxView.SelectionStart,
						Math.Min(
							_textBoxView.Text.Length,
							_textBoxView.SelectionStart + value
						)
					);
				}
			}
		}

		private InputTypes AdjustInputTypes(InputTypes inputType, InputScope inputScope)
		{
			inputType = InputScopeHelper.ConvertToCapitalization(inputType, inputScope);

			if (!IsSpellCheckEnabled)
			{
				inputType = InputScopeHelper.ConvertToRemoveSuggestions(inputType, ShouldForceDisableSpellCheck);
			}

			if (AcceptsReturn)
			{
				inputType |= InputTypes.TextFlagMultiLine;
			}

			return inputType;
		}

		private void UpdateInputScope(InputScope inputScope)
		{
			if (_textBoxView != null)
			{
				var inputType = InputScopeHelper.ConvertInputScope(inputScope);
				inputType = AdjustInputTypes(inputType, inputScope);

				if (FeatureConfiguration.TextBox.UseLegacyInputScope)
				{
					_textBoxView.InputType = inputType;
				}
				else
				{
					// InputScopes like multi-line works on Android only for InputType property, not SetRawInputType.
					// For CurrencyAmount (and others), both works but there is a behavioral difference documented in UseLegacyInputScope.
					// The behavior that matches UWP is achieved by SetRawInputType.
					var adjustedInputType = AdjustInputTypes(InputTypes.ClassText, inputScope);
					_textBoxView.SetInputTypes(adjustedInputType, inputType);
				}
			}
		}

		partial void OnSelectionHighlightColorChangedPartial(SolidColorBrush brush)
		{
			if (_textBoxView != null)
			{
				var color = brush.ColorWithOpacity;
				_textBoxView.SetHighlightColor(color);
			}
		}

		partial void OnIsSpellCheckEnabledChangedPartial(bool newValue)
		{
			if (_textBoxView != null)
			{
				UpdateInputScope(InputScope);
			}
		}

		partial void OnAcceptsReturnChangedPartial(bool newValue)
		{
			_textBoxView?.SetHorizontallyScrolling(!newValue);
			_textBoxView?.UpdateSingleLineMode();

			UpdateInputScope(InputScope);
		}

		partial void OnTextWrappingChangedPartial()
		{
			_textBoxView?.UpdateSingleLineMode();
		}

		partial void UpdateFontPartial()
		{
			if (Parent != null && _textBoxView != null)
			{
				var typeface = FontHelper.FontFamilyToTypeFace(FontFamily, FontWeight, FontStyle, FontStretch);
				_textBoxView.Typeface = typeface;
				_textBoxView.SetTextSize(ComplexUnitType.Px, (float)Math.Round(ViewHelper.LogicalToPhysicalFontPixels((float)FontSize)));
			}
		}

		partial void OnIsEnabledChangedPartial(IsEnabledChangedEventArgs e)
		{
			if (_textBoxView != null)
			{
				_textBoxView.Enabled = e.NewValue;
			}
		}

		partial void OnIsReadonlyChangedPartial() => UpdateTextBoxViewReadOnly();

		partial void OnIsTabStopChangedPartial() => UpdateTextBoxViewReadOnly();


		private IKeyListener _listener;

		private void UpdateTextBoxViewReadOnly()
		{
			if (_textBoxView == null)
			{
				return;
			}

			// If read only state actually changes, we need to set or unset
			// the KeyListener to prevent the user from typing in the TextBox.
			// We also need to reset the InputTypes afterwards, as the KeyListener
			// will have changed them.
			if (IsNativeViewReadOnly && _textBoxView.KeyListener is not null)
			{
				_listener = _textBoxView.KeyListener;
				_textBoxView.KeyListener = null;
				_textBoxView.ResetInputTypes();
			}
			else if (!IsNativeViewReadOnly && _textBoxView.KeyListener is null)
			{
				_textBoxView.KeyListener = _listener;
				_textBoxView.ResetInputTypes();
			}

			_textBoxView.Focusable = IsTabStop;
			_textBoxView.FocusableInTouchMode = IsTabStop;
			_textBoxView.Clickable = IsTabStop;
			_textBoxView.LongClickable = IsTabStop;
			_textBoxView.Invalidate();
		}

		private void SetupTextBoxView()
		{
			if (_textBoxView != null)
			{
				_textBoxView.OnFocusChangeListener = this;
				_textBoxView.SetOnEditorActionListener(this);
			}
		}

		void IOnFocusChangeListener.OnFocusChange(View v, bool hasFocus)
		{
			//When a TextBox loses focus, we want to dismiss the keyboard if no other view requiring it is focused
			if (v == _textBoxView)
			{
				ProcessFocusChanged(hasFocus);
			}
		}

		private void ProcessFocusChanged(bool hasFocus)
		{
			//We get the view token early to avoid nullvalues when the view has already been detached
			var viewWindowToken = _textBoxView.WindowToken;

			_keyboardDisposable.Disposable = Uno.UI.Dispatching.NativeDispatcher.Main
				//The delay is required because the OnFocusChange method is called when the focus is being changed, not when it has changed.
				//If the focus is moved from one TextBox to another, the CurrentFocus will be null, meaning we would hide the keyboard when we shouldn't.
				.EnqueueOperation(
					async () =>
					{
						await Task.Delay(TimeSpan.FromMilliseconds(_keyboardAccessDelay));

						var activity = ContextHelper.Current as Activity;
						//In Android, the focus can be transferred to some controls not requiring the keyboard
						var needsKeyboard = activity?.CurrentFocus != null &&
						activity?.CurrentFocus is TextBoxView &&
							// Don't show keyboard if programmatically focused and PreventKeyboardDisplayOnProgrammaticFocus is true
							!(FocusState == FocusState.Programmatic && PreventKeyboardDisplayOnProgrammaticFocus);

						var inputManager = activity?.GetSystemService(AContext.InputMethodService) as InputMethodManager;

						//When a TextBox gains focus, we want to show the keyboard
						if (hasFocus && needsKeyboard)
						{
							inputManager?.ShowSoftInput(_textBoxView, ShowFlags.Implicit);
						}

						//When a TextBox loses focus, we want to dismiss the keyboard if no other view requiring it is focused
						if (!hasFocus && !needsKeyboard)
						{
							// Hide they keyboard for the activity's current focus instead of the view
							// because it may have already been assigned to another focused control

							//Seems like CurrentFocus can be null if the previously focused element is not part of the view anymore,
							//resulting in the keyboard not being closed.
							//We still try to get the Window token from it and if we fail, we get it from the TextBox we're currently unfocusing.
							inputManager?.HideSoftInputFromWindow(activity?.CurrentFocus?.WindowToken ?? viewWindowToken, HideSoftInputFlags.None);
						}

						if (hasFocus)
						{
							if (FocusState == FocusState.Unfocused)
							{
								// Using FocusState.Pointer by default until need to distinguish between Pointer, Programmatic and Keyboard.
								Focus(FocusState.Pointer);
							}
						}
						else
						{
							Unfocus();
						}
					}
				);
		}

		partial void OnTextAlignmentChangedPartial(TextAlignment newValue)
		{
			if (_textBoxView == null)
			{
				return;
			}

			// Only works if text direction is left-to-right
			switch (newValue)
			{
				case TextAlignment.Center:
					_textBoxView.Gravity = GravityFlags.CenterHorizontal;
					break;
				case TextAlignment.Left:
					_textBoxView.Gravity = GravityFlags.Start;
					break;
				case TextAlignment.Right:
					_textBoxView.Gravity = GravityFlags.End;
					break;
				case TextAlignment.Justify:
				case TextAlignment.DetectFromContent:
				default:
					this.Log().Warn($"TextBox doesn't support TextAlignment.{newValue}");
					_textBoxView.Gravity = GravityFlags.Start; // Defaulting to Left
					break;
			}
		}

		public bool OnEditorAction(TextView v, [GeneratedEnum] ImeAction actionId, KeyEvent e)
		{
			//We need to force a keypress event on editor action.
			//the key press event is not triggered if we press the enter key depending on the ime.options
			var modifiers = e is not null ? VirtualKeyHelper.FromModifiers(e.Modifiers) : VirtualKeyModifiers.None;
			RaiseEvent(KeyDownEvent, new KeyRoutedEventArgs(this, global::Windows.System.VirtualKey.Enter, modifiers));
			RaiseEvent(KeyUpEvent, new KeyRoutedEventArgs(this, global::Windows.System.VirtualKey.Enter, modifiers));

			// Action will be ImeNull if AcceptsReturn is true, in which case we return false to allow the new line to register.
			// Otherwise we return true to allow the focus to change correctly.
			return actionId != ImeAction.ImeNull;
		}

		partial void OnTextCharacterCasingChangedPartial(CharacterCasing newValue)
		{
			if (_textBoxView == null)
			{
				return;
			}

			UpdateCasing(newValue);
		}

		private void UpdateCasing(CharacterCasing characterCasing)
		{
			var currentFilters = _textBoxView.GetFilters()?.ToList() ?? new List<IInputFilter>();

			//Remove any casing filters before applying new ones.
			currentFilters.Remove(a => a is InputFilterAllLower || a is InputFilterAllCaps);

			switch (characterCasing)
			{
				case CharacterCasing.Lower:
					var lowerFilter = new List<IInputFilter>(currentFilters)
										{
											new InputFilterAllLower()
										};
					_textBoxView.SetFilters(lowerFilter.ToArray());

					break;
				case CharacterCasing.Upper:
					var upperFilter = new List<IInputFilter>(currentFilters)
										{
											new InputFilterAllCaps()
										};
					_textBoxView.SetFilters(upperFilter.ToArray());

					break;
				case CharacterCasing.Normal:
					break;
			}
		}
	}

	class InputFilterAllLower : InputFilterAllCaps
	{
		public override Java.Lang.ICharSequence FilterFormatted(Java.Lang.ICharSequence source, int start, int end, ISpanned dest, int dstart, int dend)
		{
			for (var x = start; x < end; x++)
			{
				if (char.IsUpper(source.ElementAt(x)))
				{
					var v = new char[end - start];
					TextUtils.GetChars(source.ToString(), start, end, v, 0);
					var s = new string(v).ToLower(CultureInfo.InvariantCulture);

					if (source is ISpanned sourceSpanned)
					{
						var sp = new SpannableString(s);
						TextUtils.CopySpansFrom(sourceSpanned, start, end, null, sp, 0);
						return sp;
					}
					else
					{
						return new Java.Lang.String(s);
					}
				}
			}

			return null;
		}
	}
}
