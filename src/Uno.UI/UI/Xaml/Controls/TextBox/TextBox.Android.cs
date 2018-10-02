using System;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Graphics;
using Android.Runtime;
using Android.Text;
using Android.Util;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using Uno.Disposables;
using Uno.Extensions;
using Uno.Logging;
using Uno.UI;
using Uno.UI.Extensions;
using Windows.UI.Core;
using Windows.UI.Text;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using static Android.Widget.TextView;

namespace Windows.UI.Xaml.Controls
{
	public partial class TextBox : View.IOnFocusChangeListener, IOnEditorActionListener
	{
		private int _keyboardAccessDelay = 50;
		private TextBoxView _textBoxView;
		//private readonly SerialDisposable _keyPressDisposable = new SerialDisposable();
		private readonly SerialDisposable _keyboardDisposable = new SerialDisposable();

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

		protected override void OnUnloaded()
		{
			base.OnUnloaded();

			//_keyPressDisposable.Disposable = null;

			if (_textBoxView != null)
			{
				_textBoxView.OnFocusChangeListener = null;
			}
		}

		protected override void OnLoaded()
		{
			base.OnLoaded();
			SetupTextBoxView();
			UpdateCommonStates();
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
					_textBoxView = new TextBoxView()
						.Binding("BindableText", new Data.Binding()
						{
							Path = "Text",
							RelativeSource = RelativeSource.TemplatedParent,
							Mode = BindingMode.TwoWay
						});

					_contentElement.Content = _textBoxView;
				}

				SetupTextBoxView();
			}
		}

		public ImeAction ImeOptions
		{
			get { return (ImeAction)this.GetValue(ImeOptionsProperty); }
			set { this.SetValue(ImeOptionsProperty, value); }
		}

		public static readonly DependencyProperty ImeOptionsProperty =
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
			return dependencyObject is TextBox textBox && textBox.InputScope.GetFirstInputScopeNameValue() == InputScopeNameValue.Search
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
					UpdateFontPartial(this);
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
					inputType = InputScopeHelper.ConvertToRemoveSuggestions(inputType);
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

		partial void OnMaxLengthChangedPartial(DependencyPropertyChangedEventArgs e)
		{
			if (_textBoxView != null)
			{
				if (e.NewValue != null)
				{
					var maxValue = (int)e.NewValue;

					if (maxValue != 0)
					{
						_textBoxView.SetFilters(new IInputFilter[] { new InputFilterLengthFilter(maxValue) });
					}
					else
					{
						// Remove length filter
						_textBoxView.SetFilters(new IInputFilter[0]);
					}
				}
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

		partial void UpdateFontPartial(object sender)
		{
			var textBox = sender as TextBox;
			if (textBox != null && textBox.Parent != null && _textBoxView != null)
			{
				var style = GetTypefaceStyle(textBox.FontStyle, textBox.FontWeight);
				var typeface = FontHelper.FontFamilyToTypeFace(textBox.FontFamily, textBox.FontWeight);

				_textBoxView.SetTypeface(typeface, style);
				_textBoxView.SetTextSize(ComplexUnitType.Px, (float)Math.Round(ViewHelper.LogicalToPhysicalFontPixels((float)textBox.FontSize)));
			}
		}

		protected override void OnIsEnabledChanged(bool oldValue, bool newValue)
		{
			if (_textBoxView != null)
			{
				_textBoxView.Enabled = newValue;
			}
			base.OnIsEnabledChanged(oldValue, newValue);
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
				_textBoxView.InputType = InputTypes.Null;
			}
		}

		private void SetupTextBoxView()
		{
			if (_textBoxView != null)
			{
				//_keyPressDisposable.Disposable = _textBoxView.RegisterKeyPress(OnKeyPress);
				_textBoxView.OnFocusChangeListener = this;
				_textBoxView.SetOnEditorActionListener(this);
			}
		}

		void IOnFocusChangeListener.OnFocusChange(View v, bool hasFocus)
		{
			//When a TextBox loses focus, we want to dismiss the keyboard if no other view requiring it is focused
			if (v == _textBoxView)
			{
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
								// Don't show keyboard if programmatically focussed and PreventKeyboardDisplayOnProgrammaticFocus is true
								!(FocusState == FocusState.Programmatic && PreventKeyboardDisplayOnProgrammaticFocus);

							var inputManager = activity?.GetSystemService(Android.Content.Context.InputMethodService) as Android.Views.InputMethods.InputMethodManager;

							//When a TextBox gains focus, we want to show the keyboard
							if (hasFocus && needsKeyboard)
							{
								inputManager?.ShowSoftInput(v, Android.Views.InputMethods.ShowFlags.Forced);
								inputManager?.ToggleSoftInput(Android.Views.InputMethods.ShowFlags.Forced, Android.Views.InputMethods.HideSoftInputFlags.ImplicitOnly);
							}

							//When a TextBox loses focus, we want to dismiss the keyboard if no other view requiring it is focused
							if (!hasFocus && !needsKeyboard)
							{
								// Hide they keyboard for the activity's current focus instead of the view
								// because it may have already been assigned to another focused control
								inputManager?.HideSoftInputFromWindow(activity?.CurrentFocus?.WindowToken, Android.Views.InputMethods.HideSoftInputFlags.None);
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
#if !DEBUG
#error !!!
#endif

			// Action will be ImeNull if AcceptsReturn is true, in which case we return false to allow the new line to register.
			// Otherwise we return true to allow the focus to change correctly.
			return actionId != ImeAction.ImeNull;
		}
	}
}
