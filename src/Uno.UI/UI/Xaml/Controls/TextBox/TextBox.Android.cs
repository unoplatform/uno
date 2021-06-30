using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
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
using Uno.Logging;
using Uno.UI;
using Uno.UI.DataBinding;
using Uno.UI.Extensions;
using Windows.UI.Core;
using Windows.UI.Text;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using static Android.Widget.TextView;
using Math = System.Math;

namespace Windows.UI.Xaml.Controls
{
	public partial class TextBox : View.IOnFocusChangeListener, IOnEditorActionListener
	{
		private int _keyboardAccessDelay = 50;
		private TextBoxView _textBoxView;
		private readonly SerialDisposable _keyboardDisposable = new SerialDisposable();
		private Factory _editableFactory;
		private IKeyListener _listener;

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

		public bool PreventKeyboardDisplayOnProgrammaticFocus
		{
			get
			{
				return (bool)this.GetValue(PreventKeyboardDisplayOnProgrammaticFocusProperty);
			}
			set
			{
				this.SetValue(PreventKeyboardDisplayOnProgrammaticFocusProperty, value);
			}
		}

		public static DependencyProperty PreventKeyboardDisplayOnProgrammaticFocusProperty { get; } =
		DependencyProperty.Register(
			"PreventKeyboardDisplayOnProgrammaticFocus", typeof(bool),
			typeof(TextBox),
			new FrameworkPropertyMetadata(false));

		private protected override void OnUnloaded()
		{
			base.OnUnloaded();

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

		private protected override void OnLoaded()
		{
			base.OnLoaded();
			SetupTextBoxView();
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

		public static DependencyProperty ImeOptionsProperty { get ; } =
			DependencyProperty.Register("ImeOptions",
				typeof(ImeAction),
				typeof(TextBox),
				new FrameworkPropertyMetadata(
					defaultValue: ImeAction.Unspecified,
					coerceValueCallback: CoerceImeOptions,
					propertyChangedCallback: (s, args) => ((TextBox)s)?.OnImeOptionsChanged((ImeAction)args.NewValue)
				)
			);

		private static object CoerceImeOptions(DependencyObject dependencyObject, object baseValue)
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

		partial void OnFocusStateChangedPartial(FocusState focusState)
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

		partial void OnForegroundColorChangedPartial(Brush newValue)
		{
		}

		partial void OnInputScopeChangedPartial(DependencyPropertyChangedEventArgs e)
		{
			this.CoerceValue(ImeOptionsProperty);

			if (e.NewValue != null)
			{
				var inputScope = (InputScope)e.NewValue;

				UpdateInputScope(inputScope);
			}
		}

		protected void SetInputScope(InputTypes types)
		{
			if (_textBoxView != null)
			{
				_textBoxView.InputType = types;

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

		private void UpdateInputScope(InputScope inputScope)
		{
			if (_textBoxView != null)
			{
				var inputType = InputScopeHelper.ConvertInputScope(inputScope ?? InputScope);

				inputType = InputScopeHelper.ConvertToCapitalization(inputType, inputScope ?? InputScope);

				if (!IsSpellCheckEnabled)
				{
					inputType = InputScopeHelper.ConvertToRemoveSuggestions(inputType, ShouldForceDisableSpellCheck);
				}

				if (AcceptsReturn)
				{
					inputType |= InputTypes.TextFlagMultiLine;
				}

				_textBoxView.InputType = inputType;
			}
		}

		partial void OnIsSpellCheckEnabledChangedPartial(DependencyPropertyChangedEventArgs e)
		{
			if (_textBoxView != null)
			{
				UpdateInputScope(InputScope);
			}
		}

		partial void OnAcceptsReturnChangedPartial(DependencyPropertyChangedEventArgs e)
		{
			var acceptsReturn = (bool)e.NewValue;
			_textBoxView?.SetHorizontallyScrolling(!acceptsReturn);
			_textBoxView?.SetMaxLines(acceptsReturn ? int.MaxValue : 1);

			UpdateInputScope(InputScope);
		}

		partial void OnTextWrappingChangedPartial(DependencyPropertyChangedEventArgs e)
		{
			//TODO : see bug #8178
		}

		partial void UpdateFontPartial()
		{
			if (Parent != null && _textBoxView != null)
			{
				var style = GetTypefaceStyle(FontStyle, FontWeight);
				var typeface = FontHelper.FontFamilyToTypeFace(FontFamily, FontWeight);

				_textBoxView.SetTypeface(typeface, style);
				_textBoxView.SetTextSize(ComplexUnitType.Px, (float)Math.Round(ViewHelper.LogicalToPhysicalFontPixels((float)FontSize)));
			}
		}

		private protected override void OnIsEnabledChanged(IsEnabledChangedEventArgs e)
		{
			if (_textBoxView != null)
			{
				_textBoxView.Enabled = e.NewValue;
			}
		}

		private static TypefaceStyle GetTypefaceStyle(FontStyle fontStyle, FontWeight fontWeight)
		{
			var style = TypefaceStyle.Normal;

			if (fontWeight.Weight > 500)
			{
				style |= TypefaceStyle.Bold;
			}

			if (fontStyle == FontStyle.Italic)
			{
				style |= TypefaceStyle.Italic;
			}

			return style;
		}

		partial void OnIsReadonlyChangedPartial(DependencyPropertyChangedEventArgs e)
		{
			if (_textBoxView != null)
			{
				var isReadOnly = IsReadOnly;

				_textBoxView.Focusable = !isReadOnly;
				_textBoxView.FocusableInTouchMode = !isReadOnly;
				_textBoxView.Clickable = !isReadOnly;
				_textBoxView.LongClickable = !isReadOnly;
				_textBoxView.SetCursorVisible(!isReadOnly);

				if (isReadOnly)
				{
					_listener = _textBoxView.KeyListener;
					_textBoxView.KeyListener = null;
				}
				else
				{
					if (_listener != null)
					{
						_textBoxView.KeyListener = _listener;
					}
				}
			}
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

			_keyboardDisposable.Disposable = CoreDispatcher.Main
				//The delay is required because the OnFocusChange method is called when the focus is being changed, not when it has changed.
				//If the focus is moved from one TextBox to another, the CurrentFocus will be null, meaning we would hide the keyboard when we shouldn't.
				.RunAsync(
					CoreDispatcherPriority.Normal,
					async () =>
					{
						await Task.Delay(TimeSpan.FromMilliseconds(_keyboardAccessDelay));

						var activity = ContextHelper.Current as Activity;
						//In Android, the focus can be transferred to some controls not requiring the keyboard
						var needsKeyboard = activity?.CurrentFocus != null &&
						activity?.CurrentFocus is TextBoxView &&
							// Don't show keyboard if programmatically focused and PreventKeyboardDisplayOnProgrammaticFocus is true
							!(FocusState == FocusState.Programmatic && PreventKeyboardDisplayOnProgrammaticFocus);

						var inputManager = activity?.GetSystemService(Android.Content.Context.InputMethodService) as Android.Views.InputMethods.InputMethodManager;

						//When a TextBox gains focus, we want to show the keyboard
						if (hasFocus && needsKeyboard)
						{
							inputManager?.ShowSoftInput(_textBoxView, Android.Views.InputMethods.ShowFlags.Implicit);
						}

						//When a TextBox loses focus, we want to dismiss the keyboard if no other view requiring it is focused
						if (!hasFocus && !needsKeyboard)
						{
							// Hide they keyboard for the activity's current focus instead of the view
							// because it may have already been assigned to another focused control

							//Seems like CurrentFocus can be null if the previously focused element is not part of the view anymore,
							//resulting in the keyboard not being closed.
							//We still try to get the Window token from it and if we fail, we get it from the TextBox we're currently unfocusing.
							inputManager?.HideSoftInputFromWindow(activity?.CurrentFocus?.WindowToken ?? viewWindowToken, Android.Views.InputMethods.HideSoftInputFlags.None);
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

		partial void OnTextAlignmentChangedPartial(DependencyPropertyChangedEventArgs e)
		{
			if (_textBoxView == null)
			{
				return;
			}

			var textAlignment = (TextAlignment)e.NewValue;

			// Only works if text direction is left-to-right
			switch (textAlignment)
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
					this.Log().Warn($"TextBox doesn't support TextAlignment.{textAlignment}");
					_textBoxView.Gravity = GravityFlags.Start; // Defaulting to Left
					break;
			}
		}

		public bool OnEditorAction(TextView v, [GeneratedEnum] ImeAction actionId, KeyEvent e)
		{
			//We need to force a keypress event on editor action.
			//the key press event is not triggered if we press the enter key depending on the ime.options

			OnKeyPress(v, new KeyEventArgs(true, Keycode.Enter, new KeyEvent(KeyEventActions.Up, Keycode.Enter)));

			// Action will be ImeNull if AcceptsReturn is true, in which case we return false to allow the new line to register.
			// Otherwise we return true to allow the focus to change correctly.
			return actionId != ImeAction.ImeNull;
		}

		partial void OnTextCharacterCasingChangedPartial(DependencyPropertyChangedEventArgs e)
		{
			if (_textBoxView == null)
			{
				return;
			}

			var casing = (CharacterCasing)e.NewValue;

			UpdateCasing(casing);
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
