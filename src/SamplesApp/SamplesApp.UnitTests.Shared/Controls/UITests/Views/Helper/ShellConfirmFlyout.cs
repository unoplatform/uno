#nullable enable

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Uno.UI.Samples.Helper;

/// <summary>A small confirm flyout (message, confirm button, optional cancel) for actions that are costly or lose work.</summary>
public static class ShellConfirmFlyout
{
	public static Flyout Show(FrameworkElement anchor, string message, string confirmText, string automationId, Action onConfirm, string? cancelText = null)
	{
		Flyout flyout = new() { Placement = FlyoutPlacementMode.Bottom };

		Button confirm = new() { Content = confirmText };
		AutomationProperties.SetAutomationId(confirm, automationId);
		confirm.Click += (_, _) =>
		{
			flyout.Hide();
			onConfirm();
		};

		StackPanel buttons = new() { Orientation = Orientation.Horizontal, Spacing = 8 };
		buttons.Children.Add(confirm);

		if (cancelText is not null)
		{
			Button cancel = new() { Content = cancelText };
			AutomationProperties.SetAutomationId(cancel, automationId + "Cancel");
			cancel.Click += (_, _) => flyout.Hide();
			buttons.Children.Add(cancel);
		}

		StackPanel content = new() { MaxWidth = 280, Spacing = 12 };
		content.Children.Add(new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap });
		content.Children.Add(buttons);

		flyout.Content = content;
		flyout.ShowAt(anchor);
		return flyout;
	}
}
