using System;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls;

public partial class PasswordBox
{
	partial void OnPasswordCharChangedPartial(DependencyPropertyChangedEventArgs e)
	{
		if (string.IsNullOrEmpty(PasswordChar) || PasswordChar.Length != 1)
		{
			throw new ArgumentException("PasswordChar must be a single character string.");
		}

		// Force display update to refresh the password character
		_core.TextBoxView?.UpdateDisplayBlockText(Password);
	}

	partial void SetPasswordRevealState(PasswordRevealState state) => _core.TextBoxView?.SetPasswordRevealState(state);

	#region SelectionFlyout DependencyProperty

	public FlyoutBase SelectionFlyout
	{
		get => (FlyoutBase)GetValue(SelectionFlyoutProperty);
		set => SetValue(SelectionFlyoutProperty, value);
	}

	public static DependencyProperty SelectionFlyoutProperty { get; } =
		DependencyProperty.Register(
			nameof(SelectionFlyout),
			typeof(FlyoutBase),
			typeof(PasswordBox),
			new FrameworkPropertyMetadata(defaultValue: null));

	#endregion

	#region CanPasteClipboardContent DependencyProperty

	public bool CanPasteClipboardContent => (bool)GetValue(CanPasteClipboardContentProperty);

	public static DependencyProperty CanPasteClipboardContentProperty { get; } =
		DependencyProperty.Register(
			nameof(CanPasteClipboardContent),
			typeof(bool),
			typeof(PasswordBox),
			new FrameworkPropertyMetadata(defaultValue: false));

	bool ITextBoxHost.CanPasteClipboardContent
	{
		get => CanPasteClipboardContent;
		set => SetValue(CanPasteClipboardContentProperty, value);
	}

	#endregion

	public event ContextMenuOpeningEventHandler ContextMenuOpening;

	internal TextBoxCore.TouchTextSelectionConvention TouchSelectionConvention
	{
		get => _core.TouchSelectionConvention;
		set => _core.TouchSelectionConvention = value;
	}
}
