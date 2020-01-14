using System;
using System.Collections.Generic;
using System.Text;
using Uno.UI;
using Uno.Extensions;
using Windows.UI.Xaml.Media;
using Uno.Logging;
using Windows.Foundation;
using System.Globalization;
using Uno.UI.UI.Xaml.Documents;

namespace Windows.UI.Xaml.Controls
{
	public partial class TextBoxView : FrameworkElement
	{
		private readonly TextBox _textBox;



		public Brush Foreground
		{
			get { return (Brush)GetValue(ForegroundProperty); }
			set { SetValue(ForegroundProperty, value); }
		}

		internal static readonly DependencyProperty ForegroundProperty =
			DependencyProperty.Register(
				name: "Foreground",
				propertyType: typeof(Brush),
				ownerType: typeof(TextBoxView),
				typeMetadata: new FrameworkPropertyMetadata(
					defaultValue: null,
					options: FrameworkPropertyMetadataOptions.Inherits,
					propertyChangedCallback: (s, e) => (s as TextBoxView)?.OnForegroundChanged(e)));

		private void OnForegroundChanged(DependencyPropertyChangedEventArgs e)
			=> this.SetForeground(e.NewValue);

		public TextBoxView(TextBox textBox, bool isMultiline) : base(isMultiline ? "textarea" : "input")
		{
			IsMultiline = isMultiline;
			_textBox = textBox;
			SetTextNative(_textBox.Text);

			SetStyle(
				("overflow-x", "visible"),
				("overflow-y", "visible")
			);

			if (FeatureConfiguration.TextBox.HideCaret)
			{
				SetStyle(
					("caret-color", "transparent !important")
				);
			}

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
			
			HtmlInput += OnInput;

			SetTextNative(_textBox.Text);
		}

		protected override void OnUnloaded()
		{
			base.OnUnloaded();
			
			HtmlInput -= OnInput;
		}

		private void OnInput(object sender, EventArgs eventArgs)
		{
			var text = GetProperty("value");

			var updatedText = _textBox.ProcessTextInput(text);

			if (updatedText != text)
			{
				SetTextNative(updatedText);
			}

			InvalidateMeasure();
		}

		internal void SetTextNative(string text)
		{
			SetProperty("value", text);
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			return MeasureView(availableSize);
		}

		internal void SetIsPassword(bool isPassword)
		{
			if (IsMultiline)
			{
				throw new NotSupportedException("A PasswordBox cannot have multiple lines.");
			}
			SetAttribute("type", isPassword ? "password" : "text");
		}

		internal void SetEnabled(bool newValue)
		{
			SetProperty("disabled", newValue ? "false" : "true");
		}

		public int SelectionStart
		{
			get => int.TryParse(GetProperty("selectionStart"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) ? result : 0;
			set => SetProperty("selectionStart", value.ToString());
		}

		public int SelectionEnd
		{
			get => int.TryParse(GetProperty("selectionEnd"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) ? result : 0;
			set => SetProperty("selectionEnd", value.ToString());
		}

		internal override bool IsViewHit()
		{
			return true;
		}
	}
}
