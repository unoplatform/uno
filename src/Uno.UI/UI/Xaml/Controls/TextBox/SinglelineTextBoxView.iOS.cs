using CoreGraphics;
using Uno.UI.DataBinding;
using Uno.UI.Views.Controls;
using System;
using System.Collections.Generic;
using System.Text;
using UIKit;
using Uno.Extensions;
using Uno.UI.Extensions;
using Windows.UI.Xaml.Media;
using Uno.UI.Controls;
using Windows.UI;
using Uno.Disposables;

namespace Windows.UI.Xaml.Controls
{
	public partial class SinglelineTextBoxView : UITextField, ITextBoxView, DependencyObject, IFontScalable
	{
		private SinglelineTextBoxDelegate _delegate;
		private readonly WeakReference<TextBox> _textBox;
		private readonly SerialDisposable _foregroundChanged = new();

		private string _restoreOnNextKeyStroke;

		public SinglelineTextBoxView(TextBox textBox)
		{
			_textBox = new WeakReference<TextBox>(textBox);

			InitializeBinder();
			Initialize();
		}

		/// <inheritdoc />
		public override bool SecureTextEntry
		{
			get => base.SecureTextEntry;
			set
			{
				if (base.SecureTextEntry != value)
				{
					if (value)
					{
						// When we enable the "secure" mode, iOS will auto-magically clear the value on next key stroke
						// (Without invoking the "ShouldClear" nor any callback except "DidChangeSelection" multiple times).
						// The only way is to keep ref of the current text and restore it on next text change (expected to be an empty string).
						_restoreOnNextKeyStroke = base.Text;
					}

					base.SecureTextEntry = value;
				}
			}
		}

		public override string Text
		{
			get => base.Text;
			set
			{
				// The native control will ignore a value of null and retain an empty string. We coalesce the null to prevent a spurious empty string getting bounced back via two-way binding.
				value ??= string.Empty;
				if (base.Text != value)
				{
					base.Text = value;
					OnTextChanged();
				}
			}
		}

		private void OnEditingChanged(object sender, EventArgs e)
		{
			if (_restoreOnNextKeyStroke is { Length: > 0 } text)
			{
				base.Text = text + base.Text;
				_restoreOnNextKeyStroke = default;
			}
			else
			{
				OnTextChanged();
			}
		}

		private void OnTextChanged()
		{
			var textBox = _textBox?.GetTarget();
			if (textBox != null)
			{
				var text = textBox.ProcessTextInput(Text);
				SetTextNative(text);
			}
		}

		public void SetTextNative(string text) => Text = text;

		private void Initialize()
		{
			//Set native VerticalAlignment to top-aligned (default is center) to match Windows text placement
			base.VerticalAlignment = UIControlContentVerticalAlignment.Top;

			Delegate = _delegate = new SinglelineTextBoxDelegate(_textBox)
			{
				IsKeyboardHiddenOnEnter = true
			};

			RegisterLoadActions(
				() =>
				{
					this.EditingChanged += OnEditingChanged;
					this.EditingDidEnd += OnEditingChanged;
				},
				() =>
				{
					this.EditingChanged -= OnEditingChanged;
					this.EditingDidEnd -= OnEditingChanged;
				}
			);
		}

		public override CGSize SizeThatFits(CGSize size)
		{
			return IFrameworkElementHelper.SizeThatFits(this, base.SizeThatFits(size));
		}

		public override CGRect TextRect(CGRect forBounds)
		{
			return GetTextRect(forBounds);
		}

		public override CGRect PlaceholderRect(CGRect forBounds)
		{
			return GetTextRect(forBounds);
		}

		public override CGRect EditingRect(CGRect forBounds)
		{
			return GetTextRect(forBounds);
		}

		private CGRect GetTextRect(CGRect forBounds)
		{
			if (IsStoreInitialized)
			{
				// This test is present because most virtual methods are
				// called before the ctor has finished executing.

				return new CGRect(
					forBounds.X,
					forBounds.Y,
					forBounds.Width,
					forBounds.Height
				);
			}
			else
			{
				return CGRect.Empty;
			}
		}

		public void UpdateFont()
		{
			var textBox = _textBox.GetTarget();

			if (textBox != null)
			{
				var newFont = UIFontHelper.TryGetFont((float)textBox.FontSize, textBox.FontWeight, textBox.FontStyle, textBox.FontFamily);

				if (newFont != null)
				{
					base.Font = newFont;
					this.InvalidateMeasure();
				}
			}
		}

		public Brush Foreground
		{
			get { return (Brush)GetValue(ForegroundProperty); }
			set { SetValue(ForegroundProperty, value); }
		}

		public static DependencyProperty ForegroundProperty { get ; } =
			DependencyProperty.Register(
				"Foreground",
				typeof(Brush),
				typeof(SinglelineTextBoxView),
				new FrameworkPropertyMetadata(
					defaultValue: SolidColorBrushHelper.Black,
					options: FrameworkPropertyMetadataOptions.Inherits,
					propertyChangedCallback: (s, e) => ((SinglelineTextBoxView)s).OnForegroundChanged((Brush)e.OldValue, (Brush)e.NewValue)
				)
			);

		public void OnForegroundChanged(Brush oldValue, Brush newValue)
		{
			_foregroundChanged.Disposable = null;
			var textBox = _textBox.GetTarget();

			if (textBox != null)
			{
				var scb = newValue as SolidColorBrush;

				if (scb != null)
				{
					_foregroundChanged.Disposable = Brush.AssignAndObserveBrush(scb, _ => ApplyColor());
					ApplyColor();

					void ApplyColor()
					{
						TextColor = scb.Color;
						TintColor = scb.Color;
					}
				}
			}
		}

		public void UpdateTextAlignment()
		{
			var textBox = _textBox.GetTarget();

			if (textBox != null)
			{
				TextAlignment = textBox.TextAlignment.ToNativeTextAlignment();
			}
		}

		public void RefreshFont()
		{
			UpdateFont();
		}

		public void Select(int start, int length)
			=> SelectedTextRange = this.GetTextRange(start: start, end: start + length);

		public override UITextRange SelectedTextRange
		{
			get
			{
				return base.SelectedTextRange;
			}
			set
			{
				var textBox = _textBox.GetTarget();

				if (textBox != null && base.SelectedTextRange != value)
				{
					base.SelectedTextRange = value;
					textBox.OnSelectionChanged();
				}
			}
		}
	}
}
