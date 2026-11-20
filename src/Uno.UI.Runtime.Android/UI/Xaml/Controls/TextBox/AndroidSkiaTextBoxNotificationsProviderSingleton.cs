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

	// Visibility last reported to the autofill framework, per live TextBox.
	private readonly Dictionary<TextBoxCore, bool> _reportedVisibility = new();
	private FrameworkElement? _layoutUpdatedSource;

	public static AndroidSkiaTextBoxNotificationsProviderSingleton Instance { get; } = new AndroidSkiaTextBoxNotificationsProviderSingleton();

	private AndroidSkiaTextBoxNotificationsProviderSingleton()
	{
	}

	// Resolves the text input plugin of the render view owning the given XamlRoot's window.
	// TODO #8341: the foreground-activity fallback is ambiguous once multiple windows exist.
	internal static TextInputPlugin? GetTextInputPlugin(XamlRoot? xamlRoot = null)
	{
		var activity = AndroidSkiaXamlRootHost.GetActivity(xamlRoot)
			?? BaseActivity.Current as ApplicationActivity;
		return activity?.RenderView?.TextInputPlugin;
	}

	public void OnFocused(TextBoxCore textBox)
	{
		if (GetTextInputPlugin(textBox.Owner.XamlRoot) is { } textInputPlugin)
		{
			if (CouldRequireKeyboard(textBox.Owner) && !textBox.IsSoftwareKeyboardSuppressed)
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
		// Unloaded runs before the next Loaded (also when re-parenting), but never register an instance twice.
		if (LiveTextBoxesMap.TryAdd(textBox.GetHashCode(), textBox))
		{
			LiveTextBoxes.Add(textBox);
			UpdateLayoutUpdatedSubscription();
		}

		ReportVisibility(textBox, IsEffectivelyVisible(textBox));
	}

	public void OnLeaveVisualTree(TextBoxCore textBox)
	{
		LiveTextBoxes.Remove(textBox);
		LiveTextBoxesMap.Remove(textBox.GetHashCode());
		UpdateLayoutUpdatedSubscription();

		// Lets the autofill framework offer to save the credentials once every field of the form is gone.
		ReportVisibility(textBox, isVisible: false);
		_reportedVisibility.Remove(textBox);
	}

	// A TextBox hidden by a collapsed ancestor or a page being navigated away from.
	internal static bool IsEffectivelyVisible(TextBoxCore textBox)
		=> textBox.Owner.Visibility == Visibility.Visible && textBox.Owner.AreAllAncestorsVisible();

	private void ReportVisibility(TextBoxCore textBox, bool isVisible)
	{
		if (_reportedVisibility.TryGetValue(textBox, out var reported) && reported == isVisible)
		{
			return;
		}

		_reportedVisibility[textBox] = isVisible;
		GetTextInputPlugin(textBox.Owner.XamlRoot)?.NotifyViewVisibilityChanged(textBox.GetHashCode(), isVisible);
	}

	// LayoutUpdated is raised for every layout pass whichever element subscribed, so one live TextBox
	// is enough to catch the visibility changes (e.g. a login panel collapsed instead of unloaded).
	private void UpdateLayoutUpdatedSubscription()
	{
		var source = LiveTextBoxes.Count > 0 ? LiveTextBoxes[0].Owner : null;
		if (ReferenceEquals(source, _layoutUpdatedSource))
		{
			return;
		}

		if (_layoutUpdatedSource is not null)
		{
			_layoutUpdatedSource.LayoutUpdated -= OnLayoutUpdated;
		}

		_layoutUpdatedSource = source;
		if (source is not null)
		{
			source.LayoutUpdated += OnLayoutUpdated;
		}
	}

	private void OnLayoutUpdated(object? sender, object e)
	{
		foreach (var textBox in LiveTextBoxes)
		{
			ReportVisibility(textBox, IsEffectivelyVisible(textBox));
		}
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
