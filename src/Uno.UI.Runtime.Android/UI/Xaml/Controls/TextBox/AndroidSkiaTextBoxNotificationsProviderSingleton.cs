using System.Collections.Generic;
using System.Runtime.InteropServices;
using Android.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Uno.UI.Xaml.Controls.Extensions;

namespace Uno.UI.Runtime.Android;

internal sealed class AndroidSkiaTextBoxNotificationsProviderSingleton : ITextBoxNotificationsProviderSingleton
{
	internal List<TextBoxCore> LiveTextBoxes { get; } = new();
	internal Dictionary<int, TextBoxCore> LiveTextBoxesMap { get; } = new();

	public static AndroidSkiaTextBoxNotificationsProviderSingleton Instance { get; } = new AndroidSkiaTextBoxNotificationsProviderSingleton();

	private AndroidSkiaTextBoxNotificationsProviderSingleton()
	{
	}

	// Resolves the text input plugin of the render view owning the given XamlRoot's window.
	// TODO #8341: the foreground-activity fallback is ambiguous once multiple windows exist.
	private static TextInputPlugin? GetTextInputPlugin(XamlRoot? xamlRoot = null)
	{
		var activity = AndroidSkiaXamlRootHost.GetActivity(xamlRoot)
			?? BaseActivity.Current as ApplicationActivity;
		return activity?.RenderView?.TextInputPlugin;
	}

	public void OnFocused(TextBoxCore textBox)
	{
		if (GetTextInputPlugin(textBox.Owner.XamlRoot) is { } textInputPlugin)
		{
			if (CouldRequireKeyboard(textBox.Owner))
			{
				textInputPlugin.ShowTextInput(textBox);
			}
			textInputPlugin.NotifyViewEntered(textBox, textBox.GetHashCode());
		}
	}

	public void OnUnfocused(TextBoxCore textBox)
	{
		if (GetTextInputPlugin(textBox.Owner.XamlRoot) is { } textInputPlugin)
		{
			// Hide the keyboard only when the next element to be focused is not an Element that
			// could require the keyboard (TextBox, AutoSuggestBox, NumberBox, etc.).
			// This prevents the keyboard from flickering when switching between TextBoxes
			// https://github.com/unoplatform/uno-private/issues/1160
			if (!IsFocusingElementKeyboardActivator(textBox.Owner.XamlRoot))
			{
				textInputPlugin.HideTextInput();
			}

			textInputPlugin.NotifyViewExited(textBox, textBox.GetHashCode());
		}

		static bool IsFocusingElementKeyboardActivator(XamlRoot? xamlRoot)
		{
			if (xamlRoot is null)
			{
				return true;
			}

			var focusingElement = FocusManager.GetFocusingElement(xamlRoot) as FrameworkElement;
			return CouldRequireKeyboard(focusingElement);
		}
	}

	public void OnEnteredVisualTree(TextBoxCore textBox)
	{
		LiveTextBoxes.Add(textBox);
		LiveTextBoxesMap.Add(textBox.GetHashCode(), textBox);
	}

	public void OnLeaveVisualTree(TextBoxCore textBox)
	{
		LiveTextBoxes.Remove(textBox);
		LiveTextBoxesMap.Remove(textBox.GetHashCode());
	}

	public void FinishAutofillContext(bool shouldSave)
	{
		if (GetTextInputPlugin() is { } textInputPlugin)
		{
			textInputPlugin.FinishAutofillContext(shouldSave);
		}
	}

	public void NotifyValueChanged(TextBoxCore textBox)
	{
		if (GetTextInputPlugin(textBox.Owner.XamlRoot) is { } textInputPlugin)
		{
			textInputPlugin.NotifyValueChanged(textBox.GetHashCode(), textBox.Text);
		}
	}

	public void NotifySelectionChanged(TextBoxCore textBox)
	{
		if (GetTextInputPlugin(textBox.Owner.XamlRoot) is { } textInputPlugin)
		{
			textInputPlugin.NotifySelectionChanged(textBox);
		}
	}

	private static bool CouldRequireKeyboard(FrameworkElement? element)
	{
		return element switch
		{
			ITextBoxHost { Core: { } core } => !core.IsReadOnly,
			RichEditBox richEditBox => !richEditBox.IsReadOnly,
			AutoSuggestBox or NumberBox => true,
			_ => false,
		};
	}
}
