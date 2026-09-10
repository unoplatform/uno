using System.Collections.Generic;
using System.Runtime.InteropServices;
using Android.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Uno.UI.Xaml.Controls.Extensions;

namespace Uno.UI.Runtime.Skia.Android;

internal sealed class AndroidSkiaTextBoxNotificationsProviderSingleton : ITextBoxNotificationsProviderSingleton
{
	internal List<TextBoxCore> LiveTextBoxes { get; } = new();
	internal Dictionary<int, TextBoxCore> LiveTextBoxesMap { get; } = new();

	public static AndroidSkiaTextBoxNotificationsProviderSingleton Instance { get; } = new AndroidSkiaTextBoxNotificationsProviderSingleton();

	private AndroidSkiaTextBoxNotificationsProviderSingleton()
	{
	}

	// Resolves the text input plugin of the render view owning the given XamlRoot's window.
	// TODO #13827: the foreground-activity fallback is ambiguous once multiple windows exist.
	private static TextInputPlugin? GetTextInputPlugin(XamlRoot? xamlRoot = null)
	{
		var activity = AndroidSkiaXamlRootHost.GetActivity(xamlRoot)
			?? BaseActivity.Current as ApplicationActivity;
		return activity?.RenderView?.TextInputPlugin;
	}

	public void OnFocused(TextBoxCore core)
	{
		if (GetTextInputPlugin(core.Owner.XamlRoot) is { } textInputPlugin)
		{
			if (CouldRequireKeyboard(core))
			{
				textInputPlugin.ShowTextInput(core);
			}
			textInputPlugin.NotifyViewEntered(core, core.GetHashCode());
		}
	}

	public void OnUnfocused(TextBoxCore core)
	{
		if (GetTextInputPlugin(core.Owner.XamlRoot) is { } textInputPlugin)
		{
			// Hide the keyboard only when the next element to be focused is not an Element that
			// could require the keyboard (TextBox, AutoSuggestBox, NumberBox, etc.).
			// This prevents the keyboard from flickering when switching between TextBoxes
			// https://github.com/unoplatform/uno-private/issues/1160
			if (!IsFocusingElementKeyboardActivator(core.Owner.XamlRoot))
			{
				textInputPlugin.HideTextInput();
			}

			textInputPlugin.NotifyViewExited(core.GetHashCode());
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

	public void OnEnteredVisualTree(TextBoxCore core)
	{
		LiveTextBoxes.Add(core);
		LiveTextBoxesMap.Add(core.GetHashCode(), core);
	}

	public void OnLeaveVisualTree(TextBoxCore core)
	{
		LiveTextBoxes.Remove(core);
		LiveTextBoxesMap.Remove(core.GetHashCode());
	}

	public void FinishAutofillContext(bool shouldSave)
	{
		if (GetTextInputPlugin() is { } textInputPlugin)
		{
			textInputPlugin.FinishAutofillContext(shouldSave);
		}
	}

	public void NotifyValueChanged(TextBoxCore core)
	{
		if (GetTextInputPlugin(core.Owner.XamlRoot) is { } textInputPlugin)
		{
			textInputPlugin.NotifyValueChanged(core.GetHashCode(), core.Text);
		}
	}

	public void NotifySelectionChanged(TextBoxCore core)
	{
	}

	private static bool CouldRequireKeyboard(object? element)
	{
		return element switch
		{
			TextBoxCore core => !core.IsReadOnly,
			ITextBoxHost host => !host.Core.IsReadOnly,
			AutoSuggestBox or NumberBox => true,
			_ => false,
		};
	}
}
