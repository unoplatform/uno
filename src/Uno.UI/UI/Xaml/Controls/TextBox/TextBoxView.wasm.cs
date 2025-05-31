﻿using System;
using System.Collections.Generic;
using System.Text;
using Uno.UI;
using Uno.Extensions;
using Microsoft.UI.Xaml.Media;
using Uno.Foundation.Logging;
using Windows.Foundation;
using System.Globalization;
using Uno.Disposables;
using Uno.Foundation;
using Uno.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Uno.UI.Helpers;
using Uno.UI.Xaml.Input;

namespace Microsoft.UI.Xaml.Controls
{
	internal partial class TextBoxView : FrameworkElement
	{
		private readonly TextBox _textBox;
		private Action _foregroundChanged;
		private IDisposable _foregroundBrushChangedSubscription;

		private bool _browserContextMenuEnabled = true;
		private bool _isReadOnly;

		public Brush Foreground
		{
			get => (Brush)GetValue(ForegroundProperty);
			set => SetValue(ForegroundProperty, value);
		}

		internal static DependencyProperty ForegroundProperty { get; } =
			DependencyProperty.Register(
				name: "Foreground",
				propertyType: typeof(Brush),
				ownerType: typeof(TextBoxView),
				typeMetadata: new FrameworkPropertyMetadata(
					defaultValue: null,
					options: FrameworkPropertyMetadataOptions.Inherits,
					propertyChangedCallback: (s, e) => (s as TextBoxView)?.OnForegroundChanged(e)));

		private void OnForegroundChanged(DependencyPropertyChangedEventArgs e)
		{
			if (e.NewValue is SolidColorBrush scb)
			{
				_foregroundBrushChangedSubscription?.Dispose();
				_foregroundBrushChangedSubscription = Brush.SetupBrushChanged(scb, ref _foregroundChanged, () => SetForeground(scb));
			}
		}

		public TextBoxView(TextBox textBox, bool isMultiline)
			// We need to use textarea regardless of isMultiline
			// because "input" native HTML element can't have its text top-aligned.
			// For PasswordBox, it must be input. So, for now we can't match WinUI and it will
			// remain center-aligned instead of top-aligned.
			: base(textBox is PasswordBox ? "input" : "textarea")
		{
			IsMultiline = isMultiline;
			_textBox = textBox;
			SetTextNative(_textBox.Text);

			if (FeatureConfiguration.TextBox.HideCaret)
			{
				SetStyle(
					("caret-color", "transparent !important")
				);
			}

			SetAttribute("tabindex", "0");
			UpdateContextMenuEnabling();
		}

		private event EventHandler HtmlInput
		{
			add => RegisterEventHandler("input", value, GenericEventHandlers.RaiseEventHandler);
			remove => UnregisterEventHandler("input", value, GenericEventHandlers.RaiseEventHandler);
		}

		private event RoutedEventHandlerWithHandled HtmlPaste
		{
			add => RegisterEventHandler("paste", value, GenericEventHandlers.RaiseRoutedEventHandlerWithHandled);
			remove => UnregisterEventHandler("paste", value, GenericEventHandlers.RaiseRoutedEventHandlerWithHandled);
		}

		internal bool IsMultiline { get; }

		private protected override void OnLoaded()
		{
			base.OnLoaded();

			HtmlInput += OnInput;
			HtmlPaste += OnPaste;

			if (!IsMultiline)
			{
				WindowManagerInterop.SetSingleLine(this);
			}

			SetTextNative(_textBox.Text);
		}

		private bool OnPaste(object sender, RoutedEventArgs e)
		{
			var args = new TextControlPasteEventArgs();
			_textBox.RaisePaste(args);
			return args.Handled;
		}

		private protected override void OnUnloaded()
		{
			base.OnUnloaded();

			HtmlInput -= OnInput;
			HtmlPaste -= OnPaste;
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

			InvalidateMeasure();
		}

		internal void Select(int start, int length)
			=> WindowManagerInterop.SelectInputRange(HtmlId, start, length);

		protected override Size MeasureOverride(Size availableSize) => MeasureView(availableSize);

		protected override Size ArrangeOverride(Size finalSize)
			=> ArrangeFirstChild(finalSize);

		internal void SetPasswordRevealState(PasswordRevealState revealState)
		{
			if (IsMultiline)
			{
				throw new NotSupportedException("A PasswordBox cannot have multiple lines.");
			}
			SetAttribute("type", revealState == PasswordRevealState.Obscured ? "password" : "text");
		}

		internal void SetEnabled(bool newValue) => SetProperty("disabled", newValue ? "false" : "true");

		internal void SetIsReadOnly(bool isReadOnly)
		{
			if (_isReadOnly == isReadOnly)
			{
				// Avoid JS call if the actual value didn't change.
				return;
			}

			_isReadOnly = isReadOnly;
			if (isReadOnly)
			{
				SetAttribute("readonly", "readonly");
			}
			else
			{
				RemoveAttribute("readonly");
			}
		}

		internal void UpdateContextMenuEnabling()
		{
			// _browserContextMenuEnabled flag is used to avoid unnecessary round-trips
			// to JS when the value didn't change.

			if (_textBox?.ContextFlyout is not null && _browserContextMenuEnabled)
			{
				SetCssClasses("context-menu-disabled");
				_browserContextMenuEnabled = false;
			}
			else if (_textBox?.ContextFlyout is null && !_browserContextMenuEnabled)
			{
				UnsetCssClasses("context-menu-disabled");
				_browserContextMenuEnabled = true;
			}
		}

		internal void SetInputScope(InputScope scope) =>
			SetAttribute("inputmode", scope.ToInputModeValue());

		private void SetInputReturnType(InputReturnType inputReturnType) =>
			SetAttribute("enterkeyhint", inputReturnType.ToEnterKeyHintValue());

		public int SelectionStart
		{
			get => int.TryParse(GetProperty("selectionStart"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) ? result : 0;
			set => SetProperty("selectionStart", value.ToString(CultureInfo.InvariantCulture));
		}

		public int SelectionEnd
		{
			get => int.TryParse(GetProperty("selectionEnd"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) ? result : 0;
			set => SetProperty("selectionEnd", value.ToString(CultureInfo.InvariantCulture));
		}

		internal override bool IsViewHit() => true;
	}
}
