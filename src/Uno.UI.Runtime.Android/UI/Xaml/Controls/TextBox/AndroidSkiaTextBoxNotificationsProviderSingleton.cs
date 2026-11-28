using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Android.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Controls.Extensions;

namespace Uno.UI.Runtime.Android;

internal sealed class AndroidSkiaTextBoxNotificationsProviderSingleton : ITextBoxNotificationsProviderSingleton
{
	// Loaded TextBoxes by virtual id. Weak, so that a TextBox whose Unloaded never comes can't keep its page alive.
	private readonly Dictionary<int, WeakReference<TextBoxCore>> _liveTextBoxes = new();

	// Visibility last sent to the autofill framework, per virtual id.
	private readonly Dictionary<int, bool> _reportedVisibility = new();

	// The plugin of the window a TextBox loaded in, so its Unloaded reaches that window even once its XamlRoot is gone.
	private readonly Dictionary<int, WeakReference<TextInputPlugin>> _owners = new();

	// The root of a live TextBox's visual tree, which lives as long as its window.
	private FrameworkElement? _layoutUpdatedSource;

	// Unique per TextBox for its lifetime (GetHashCode() can collide), shared by the structure, the notifications and Autofill().
	private static readonly ConditionalWeakTable<TextBoxCore, object> _virtualIds = new();
	private static int _lastVirtualId;

	internal static int GetVirtualId(TextBoxCore textBox)
		=> (int)_virtualIds.GetValue(textBox, static _ => Interlocked.Increment(ref _lastVirtualId));

	public static AndroidSkiaTextBoxNotificationsProviderSingleton Instance { get; } = new AndroidSkiaTextBoxNotificationsProviderSingleton();

	private AndroidSkiaTextBoxNotificationsProviderSingleton()
	{
	}

	// The text input plugin of the render view owning the given XamlRoot's window, null when that window is gone.
	internal static TextInputPlugin? GetOwningTextInputPlugin(XamlRoot? xamlRoot)
		=> AndroidSkiaXamlRootHost.GetActivity(xamlRoot)?.RenderView?.TextInputPlugin;

	// For notifications, which must still reach the foreground activity when the XamlRoot is already gone (e.g. on Unloaded).
	// TODO #8341: the foreground-activity fallback is ambiguous once multiple windows exist.
	internal static TextInputPlugin? GetTextInputPlugin(XamlRoot? xamlRoot = null)
		=> GetOwningTextInputPlugin(xamlRoot) ?? (BaseActivity.Current as ApplicationActivity)?.RenderView?.TextInputPlugin;

	public void OnFocused(TextBoxCore textBox)
	{
		if (GetTextInputPlugin(textBox.Owner.XamlRoot) is { } textInputPlugin)
		{
			if (CouldRequireKeyboard(textBox.Owner) && !textBox.IsSoftwareKeyboardSuppressed)
			{
				textInputPlugin.ShowTextInput(textBox);
			}
			textInputPlugin.NotifyViewEntered(textBox, GetVirtualId(textBox));
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

			textInputPlugin.NotifyViewExited(textBox, GetVirtualId(textBox));
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
		var id = GetVirtualId(textBox);
		if (!TryGetLiveTextBox(id, out _))
		{
			_liveTextBoxes[id] = new(textBox);
			if (GetOwningTextInputPlugin(textBox.Owner.XamlRoot) is { } owner)
			{
				_owners[id] = new(owner);
			}

			UpdateLayoutUpdatedSubscription();
		}

		ReportVisibility(textBox, IsEffectivelyVisible(textBox));
	}

	public void OnLeaveVisualTree(TextBoxCore textBox)
	{
		var id = GetVirtualId(textBox);
		if (TryGetLiveTextBox(id, out var registered) && ReferenceEquals(registered, textBox))
		{
			_liveTextBoxes.Remove(id);
			UpdateLayoutUpdatedSubscription();
		}

		// Lets the autofill framework offer to save the credentials once every field of the form is gone.
		ReportVisibility(textBox, isVisible: false);
		_reportedVisibility.Remove(id);
		_owners.Remove(id);
	}

	internal bool TryGetLiveTextBox(int id, [NotNullWhen(true)] out TextBoxCore? textBox)
	{
		textBox = null;
		return _liveTextBoxes.TryGetValue(id, out var entry) && entry.TryGetTarget(out textBox);
	}

	// Also drops the entries of TextBoxes collected without being unloaded.
	internal List<TextBoxCore> GetLiveTextBoxes()
	{
		var textBoxes = new List<TextBoxCore>(_liveTextBoxes.Count);
		List<int>? collected = null;
		foreach (var (id, entry) in _liveTextBoxes)
		{
			if (entry.TryGetTarget(out var textBox))
			{
				textBoxes.Add(textBox);
			}
			else
			{
				(collected ??= new()).Add(id);
			}
		}

		if (collected is not null)
		{
			foreach (var id in collected)
			{
				_liveTextBoxes.Remove(id);
				_reportedVisibility.Remove(id);
				_owners.Remove(id);
			}

			UpdateLayoutUpdatedSubscription();
		}

		return textBoxes;
	}

	// Collapsed itself or through an ancestor. The fields of a page navigated away from become Gone by unloading, not through this.
	internal static bool IsEffectivelyVisible(TextBoxCore textBox)
		=> textBox.Owner.Visibility == Visibility.Visible && textBox.Owner.AreAllAncestorsVisible();

	private void ReportVisibility(TextBoxCore textBox, bool isVisible)
	{
		var id = GetVirtualId(textBox);
		if (_reportedVisibility.TryGetValue(id, out var reported) && reported == isVisible)
		{
			return;
		}

		var plugin = _owners.TryGetValue(id, out var owner) && owner.TryGetTarget(out var ownerPlugin)
			? ownerPlugin
			: GetTextInputPlugin(textBox.Owner.XamlRoot);
		if (plugin is null)
		{
			return;
		}

		_reportedVisibility[id] = isVisible;
		plugin.NotifyViewVisibilityChanged(id, isVisible);
	}

	internal void ReportAllVisibility()
	{
		foreach (var textBox in GetLiveTextBoxes())
		{
			ReportVisibility(textBox, IsEffectivelyVisible(textBox));
		}
	}

	// LayoutUpdated is raised for every layout pass whichever element subscribed, so one subscription is enough
	// to catch the visibility changes (e.g. a login panel collapsed instead of unloaded). It is on the root rather
	// than a TextBox: the subscription keeps its element alive, which would defeat the weak registration.
	private void UpdateLayoutUpdatedSubscription()
	{
		FrameworkElement? source = null;
		foreach (var entry in _liveTextBoxes.Values)
		{
			if (entry.TryGetTarget(out var textBox) && textBox.Owner.XamlRoot?.VisualTree.RootElement is FrameworkElement root)
			{
				source = root;
				break;
			}
		}

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
		// Skips the walk (and the list allocation) when no autofill service could use the result.
		if (GetOwningTextInputPlugin(_layoutUpdatedSource?.XamlRoot) is not { IsTrackingAutofillVisibility: true })
		{
			return;
		}

		// Called by the layout pass: an AutofillManager failure must not break the other LayoutUpdated handlers.
		try
		{
			ReportAllVisibility();
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"{nameof(OnLayoutUpdated)} failed", ex);
			}

			Application.Current?.RaiseRecoverableUnhandledException(ex);
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
			textInputPlugin.NotifyValueChanged(GetVirtualId(textBox), textBox.Text);
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
