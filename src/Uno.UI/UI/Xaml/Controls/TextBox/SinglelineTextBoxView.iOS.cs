using System;
using System.Runtime.InteropServices;
using CoreGraphics;
using Foundation;
using ObjCRuntime;
using UIKit;
using Uno.Extensions;
using Uno.UI.Controls;
using Uno.UI.Extensions;
using Windows.UI.Xaml.Media;
using Windows.UI;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI;
using static Uno.UI.FeatureConfiguration;

namespace Windows.UI.Xaml.Controls
{
	public partial class SinglelineTextBoxView : UITextField, ITextBoxView, DependencyObject, IFontScalable
	{
		private SinglelineTextBoxDelegate _delegate;
		private readonly WeakReference<TextBox> _textBox;
		private Action _foregroundChanged;

		public SinglelineTextBoxView(TextBox textBox)
		{
			_textBox = new WeakReference<TextBox>(textBox);

			InitializeBinder();
			Initialize();
		}

		public override void Paste(NSObject sender) => HandlePaste(() => base.Paste(sender));

		public override void PasteAndGo(NSObject sender) => HandlePaste(() => base.PasteAndGo(sender));

		public override void PasteAndMatchStyle(NSObject sender) => HandlePaste(() => base.PasteAndMatchStyle(sender));

		public override void PasteAndSearch(NSObject sender) => HandlePaste(() => base.PasteAndSearch(sender));

		public override void Paste(NSItemProvider[] itemProviders) => HandlePaste(() => base.Paste(itemProviders));

		private void HandlePaste(Action baseAction)
		{
			var args = new TextControlPasteEventArgs();
			TextBox?.RaisePaste(args);
			if (!args.Handled)
			{
				baseAction.Invoke();
			}
		}

		internal TextBox TextBox => _textBox.GetTarget();

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
			OnTextChanged();
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
		}

		partial void OnLoadedPartial()
		{
			this.EditingChanged += OnEditingChanged;
			this.EditingDidEnd += OnEditingChanged;
		}

		partial void OnUnloadedPartial()
		{
			this.EditingChanged -= OnEditingChanged;
			this.EditingDidEnd -= OnEditingChanged;
		}

		//Forces the secure UITextField to maintain its current value upon regaining focus
		public override bool BecomeFirstResponder()
		{
			var result = base.BecomeFirstResponder();

			if (SecureTextEntry)
			{
				var text = Text;
				Text = string.Empty;
				InsertText(text);
			}

			return result;
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

		public static DependencyProperty ForegroundProperty { get; } =
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
			var textBox = _textBox.GetTarget();
			if (textBox != null)
			{
				if (newValue is SolidColorBrush scb)
				{
					Brush.SetupBrushChanged(oldValue, newValue, ref _foregroundChanged, () => ApplyColor());

					void ApplyColor()
					{
						TextColor = scb.Color;
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
			=> SelectedTextRange = this.GetTextRange(start: start, end: start + length).GetHandle();

		/// <summary>
		/// Workaround for https://github.com/unoplatform/uno/issues/9430
		/// </summary>
		[DllImport(Constants.ObjectiveCLibrary, EntryPoint = "objc_msgSendSuper")]
		static internal extern IntPtr IntPtr_objc_msgSendSuper(IntPtr receiver, IntPtr selector);

		[DllImport(Constants.ObjectiveCLibrary, EntryPoint = "objc_msgSendSuper")]
		static internal extern void void_objc_msgSendSuper(IntPtr receiver, IntPtr selector, IntPtr arg);

		[Export("selectedTextRange")]
		public new IntPtr SelectedTextRange
		{
			get
			{
				return IntPtr_objc_msgSendSuper(SuperHandle, Selector.GetHandle("selectedTextRange"));
			}
			set
			{
				var textBox = TextBox;

				if (textBox != null && SelectedTextRange != value)
				{
					void_objc_msgSendSuper(SuperHandle, Selector.GetHandle("setSelectedTextRange:"), value);
					textBox.OnSelectionChanged();
				}
			}
		}
	}
}
