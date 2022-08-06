using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Uno.Extensions;
using Windows.Foundation;
using Windows.UI.Composition;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Documents.TextFormatting;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls
{
	internal class SkiaTextBoxView : FrameworkElement, ITextBoxView
	{
		private readonly TextBox _textBox;
		private readonly bool _isPasswordBox;
		private bool _isPasswordRevealed;
		private readonly SkiaTextBoxVisual _textBoxVisual;

		private readonly Run _run = new();
		private readonly InlineCollection _inlines;

		public UIElement Content => this;

		public TextBox TextBox => _textBox;

		internal InlineCollection Inlines => _inlines;

		public SkiaTextBoxView(TextBox textBox)
		{
			_textBox = textBox;
			_isPasswordBox = textBox is PasswordBox;
			_inlines = new InlineCollection(textBox) { _run };
			_textBoxVisual = new SkiaTextBoxVisual(Visual.Compositor, this);
			Visual.Children.InsertAtBottom(_textBoxVisual);
		}

		public int GetSelectionLength() => 0;

		public int GetSelectionStart() => 0;

		public void InvalidateLayout() { }

		public void OnFocusStateChanged(FocusState focusState) { }

		public void OnForegroundChanged(Brush brush) { }

		public void Select(int start, int length) { }

		public void SetIsPassword(bool isPassword) { }

		public void SetText(string text)
		{
			_run.Text = text;
		}

		public void UpdateMaxLength() { }

		protected override Size MeasureOverride(Size availableSize)
		{
			var desiredSize = Inlines.Measure(availableSize);
			return desiredSize;
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			var arrangedSize = Inlines.Arrange(finalSize);
			_textBoxVisual.Size = new Vector2((float)arrangedSize.Width, (float)arrangedSize.Height);

			return base.ArrangeOverride(finalSize);
		}
	}
}
