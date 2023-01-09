using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.Runtime.Skia.UI.Xaml.Controls;
using Uno.UI.Runtime.Skia.WPF.Controls;
using Windows.UI.Xaml.Controls;
using WpfElement = System.Windows.UIElement;
using WpfCanvas = System.Windows.Controls.Canvas;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace Uno.UI.Runtime.Skia.Wpf.Extensions.UI.Xaml.Controls
{
	internal class TextTextBoxView : WpfTextBoxView
	{
		private readonly WpfTextViewTextBox _textBox = new();

		public TextTextBoxView()
		{			
		}

		public override string Text { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

		protected override WpfElement RootElement => _textBox;

		protected override WpfElement InputElement => _textBox;


		public override (int start, int end) GetSelectionBounds() => throw new NotImplementedException();
		public override bool IsCompatible(TextBox textBox) => throw new NotImplementedException();
		public override IDisposable ObserveTextChanges(EventHandler onChanged)
		{
			InputElement.TextChanged += WpfTextViewTextChanged;
				disposable.Add(Disposable.Create(() => _currentTextBoxInputWidget.TextChanged -= WpfTextViewTextChanged));
			}

			if (_currentPasswordBoxInputWidget is not null)
			{
				_currentPasswordBoxInputWidget.PasswordChanged += PasswordBoxViewPasswordChanged;
				disposable.Add(Disposable.Create(() => _currentPasswordBoxInputWidget.PasswordChanged -= PasswordBoxViewPasswordChanged));
			}
			_textChangedDisposable.Disposable = disposable;
		}
		public override void SetSelectionBounds(int start, int end) => throw new NotImplementedException();

		private WpfTextViewTextBox CreateInputControl()
		{
			var textView = new WpfTextViewTextBox();
			return textView;
		}
	}
}
