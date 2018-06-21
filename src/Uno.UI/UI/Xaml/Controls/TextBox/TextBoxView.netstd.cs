using System;
using System.Collections.Generic;
using System.Text;
using Uno.UI;
using Uno.Extensions;
using Windows.UI.Xaml.Media;
using Uno.Logging;
using Windows.Foundation;

namespace Windows.UI.Xaml.Controls
{
	public partial class TextBoxView : FrameworkElement
	{
		private readonly TextBox _textBox;

		public TextBoxView(TextBox textBox, bool isMultiline) : base(isMultiline ? "textarea" : "input")
		{
			IsMultiline = isMultiline;
			_textBox = textBox;
			OnTextChanged(textBox, new TextChangedEventArgs()); // TODO

			SetStyle(
				("overflow-x", "visible"),
				("overflow-y", "visible")
			);
			SetAttribute("tabindex", "0");
		}

		private event EventHandler HtmlInput
		{
			add => RegisterEventHandler("input", value);
			remove => UnregisterEventHandler("input", value);
		}

		internal bool IsMultiline { get; }

		protected override void OnLoaded()
		{
			base.OnLoaded();

			_textBox.TextChanged += OnTextChanged;
			HtmlInput += OnInput;
		}

		protected override void OnUnloaded()
		{
			base.OnUnloaded();

			_textBox.TextChanged -= OnTextChanged;
			HtmlInput -= OnInput;
		}

		private void OnInput(object sender, EventArgs eventArgs)
		{
			_textBox.Text = GetProperty("value");
		}

		private void OnTextChanged(object sender, TextChangedEventArgs e)
		{
			SetProperty("value", _textBox.Text);
		}

		internal void SetIsPassword(bool isPassword)
		{
			if (IsMultiline)
			{
				throw new NotSupportedException("A PasswordBox cannot have multiple lines.");
			}
			SetAttribute("type", isPassword ? "password" : "text");
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			return MeasureView(availableSize);
		}

		protected override void OnIsEnabledChanged(bool oldValue, bool newValue)
		{
			base.OnIsEnabledChanged(oldValue, newValue);

			SetProperty("disabled", newValue ? "true" : "false");
		}

		public int SelectionStart
		{
			get => int.Parse(GetProperty("selectionStart"));
			set => SetProperty("selectionStart", value.ToString());
		}

		public int SelectionEnd
		{
			get => int.Parse(GetProperty("selectionEnd"));
			set => SetProperty("selectionEnd", value.ToString());
		}

		internal override bool IsViewHit()
		{
			return true;
		}
	}
}
