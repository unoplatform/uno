using CoreGraphics;
using Uno.UI.Views.Controls;
using System;
using System.Collections.Generic;
using System.Text;
using UIKit;
using Uno.Extensions;
using System.Linq;
using Foundation;
using Uno.UI.Extensions;
using Windows.UI.Core;
using Uno.UI;
using Uno.UI.Helpers;
using Windows.UI.Xaml.Media;
using Uno.UI.Controls;
using Windows.UI;
using Uno.Disposables;
using ObjCRuntime;

namespace Windows.UI.Xaml.Controls
{
	public partial class MultilineTextBoxView : UITextView, ITextBoxView, DependencyObject, IFontScalable, IUIScrollView
	{
		private MultilineTextBoxDelegate _delegate;
		private readonly WeakReference<TextBox> _textBox;
		private WeakReference<Uno.UI.Controls.Window> _window;
		private Action _foregroundChanged;

		public override void Paste(NSObject sender) => HandlePaste(() => base.Paste(sender));

		public override void PasteAndGo(NSObject sender) => HandlePaste(() => base.PasteAndGo(sender));

		public override void PasteAndMatchStyle(NSObject sender) => HandlePaste(() => base.PasteAndMatchStyle(sender));

		public override void PasteAndSearch(NSObject sender) => HandlePaste(() => base.PasteAndSearch(sender));

		public override void Paste(NSItemProvider[] itemProviders) => HandlePaste(() => base.Paste(itemProviders));

		private void HandlePaste(Action baseAction)
		{
			var args = new TextControlPasteEventArgs();
			var textBox = _textBox.GetTarget();
			textBox?.RaisePaste(args);
			if (!args.Handled)
			{
				baseAction.Invoke();
			}
		}

		CGPoint IUIScrollView.UpperScrollLimit { get { return (CGPoint)(ContentSize - Frame.Size); } }

		void IUIScrollView.ApplyZoomScale(nfloat scale, bool animated)
		{
			if (animated)
			{
				SetZoomScale(scale, animated);
			}
			else
			{
				ZoomScale = scale;
			}
		}

		void IUIScrollView.ApplyContentOffset(CGPoint contentOffset, bool animated)
		{
			if (animated)
			{
				SetContentOffset(contentOffset, animated);
			}
			else
			{
				ContentOffset = contentOffset;
			}
		}

		public MultilineTextBoxView(TextBox textBox)
		{
			_textBox = new WeakReference<TextBox>(textBox);
			_window = new WeakReference<Uno.UI.Controls.Window>(textBox.FindFirstParent<Uno.UI.Controls.Window>());
			InitializeBinder();
			Initialize();
		}

		private void Initialize()
		{
			Delegate = _delegate = new MultilineTextBoxDelegate(_textBox);
			BackgroundColor = UIColor.Clear;
			TextContainer.LineFragmentPadding = 0;

			// Reset the default margin of 8px at the top
			TextContainerInset = new UIEdgeInsets();
		}

		public override string Text
		{
			get
			{
				return base.Text;
			}
			set
			{
				// The native control will ignore a value of null and retain an empty string. We coalesce the null to prevent a spurious empty string getting bounced back via two-way binding.
				value = value ?? string.Empty;
				if (base.Text != value)
				{
					base.Text = value;
					OnTextChanged();
				}
			}
		}

		public void SetTextNative(string text) => Text = text;

		internal void OnTextChanged()
		{
			var textBox = _textBox?.GetTarget();
			if (textBox != null)
			{
				var text = textBox.ProcessTextInput(Text);
				SetTextNative(text);
			}

			SetNeedsLayout();
			//We need to schedule the scrolling on the dispatcher so that we wait for the whole UI to be done before scrolling.
			//Because the multiline must have its height set so we can set properly the scrollviewer insets
			_ = CoreDispatcher.Main.RunAsync(
				CoreDispatcherPriority.Normal,
				() => ScrollToCursor()
			);
		}

		internal void ScrollToCursor()
		{
			var window = _window.GetTarget();

			if (window == null)
			{
				// TextBox may not yet have been attached to window when it was templated
				window = _textBox.GetTarget().FindFirstParent<Uno.UI.Controls.Window>();
				_window = new WeakReference<Uno.UI.Controls.Window>(window);
			}

			if (this.IsFirstResponder)
			{
				window.MakeVisible(this, BringIntoViewMode.BottomRightOfViewPort);
			}
		}


		public override CGSize SizeThatFits(CGSize size)
		{
			var textBox = _textBox.GetTarget();

			if (textBox != null)
			{
				//bug in base.SizeThatFits(size) where we get stuck and size is never return for value NaN
				if (nfloat.IsNaN(size.Width))
				{
					size = new CGSize(nfloat.PositiveInfinity, nfloat.PositiveInfinity);
				}

				//at this point size returned represent all the space available
				//to have the same behavior of Windows, we need to call UIView.SizeThatFits() to return a size that best fits
				var expectedSize = base.SizeThatFits(size);

				//Disable the scroll if you are given enough space. Else framework will use the scrollview to move text up
				ScrollEnabled = expectedSize.Height >= size.Height;

				var canTakeAllSpace = expectedSize.Height < size.Height && !nfloat.IsInfinity(size.Height) && size.Height != textBox.MaxHeight;
				// if textBox.Height is set, size.Height will be the same
				var shouldTakeAllSpace = !double.IsNaN(textBox.Height) || textBox.VerticalAlignment == VerticalAlignment.Stretch;
				if (canTakeAllSpace && shouldTakeAllSpace)
				{
					expectedSize.Height = size.Height;//Take all the space given
				}

				canTakeAllSpace = expectedSize.Width < size.Width && !nfloat.IsInfinity(size.Width) && size.Width != textBox.MaxWidth;
				// if textBox.Width is set, size.Width will be the same
				shouldTakeAllSpace = !double.IsNaN(textBox.Width) || textBox.HorizontalAlignment == HorizontalAlignment.Stretch;
				if (canTakeAllSpace && shouldTakeAllSpace)
				{
					expectedSize.Width = size.Width;//Take all the space given
				}

				var result = IFrameworkElementHelper.SizeThatFits(this, expectedSize);//Adjust for NaN and MaxHeight..

				return result;
			}
			else
			{
				return CGSize.Empty;
			}
		}

		public void UpdateFont()
		{
			var textBox = _textBox.GetTarget();

			if (textBox != null)
			{
				var newFont = UIFontHelper.TryGetFont((nfloat)textBox.FontSize, textBox.FontWeight, textBox.FontStyle, textBox.FontFamily);

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
				typeof(MultilineTextBoxView),
				new FrameworkPropertyMetadata(
					defaultValue: SolidColorBrushHelper.Black,
					options: FrameworkPropertyMetadataOptions.Inherits,
					propertyChangedCallback: (s, e) => ((MultilineTextBoxView)s).OnForegroundChanged((Brush)e.OldValue, (Brush)e.NewValue)
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

		/// <summary>
		/// Workaround for https://github.com/unoplatform/uno/issues/9430
		/// </summary>
		[Export("selectedTextRange")]
		public new IntPtr SelectedTextRange
		{
			get
			{
				return SinglelineTextBoxView.IntPtr_objc_msgSendSuper(SuperHandle, Selector.GetHandle("selectedTextRange"));
			}
			set
			{
				if (SelectedTextRange != value)
				{
					SinglelineTextBoxView.void_objc_msgSendSuper(SuperHandle, Selector.GetHandle("setSelectedTextRange:"), value);
					_textBox.GetTarget()?.OnSelectionChanged();
				}
			}
		}

		public void RefreshFont()
		{
			UpdateFont();
		}

		public void Select(int start, int length)
			=> SelectedTextRange = this.GetTextRange(start: start, end: start + length).GetHandle();
	}
}
