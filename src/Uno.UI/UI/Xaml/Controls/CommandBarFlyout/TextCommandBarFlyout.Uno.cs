#nullable enable

using System;
using System.Collections.Generic;
using Uno.Foundation.Extensibility;
using Uno.UI.Xaml.Controls.Extensions;

namespace Microsoft.UI.Xaml.Controls;

// Uno specific additions to the ported TextCommandBarFlyout: the iOS "Select" command and the commands a
// platform contributes through ITextCommandBarFlyoutCommandsExtension.
partial class TextCommandBarFlyout
{
	private static readonly Lazy<ITextCommandBarFlyoutCommandsExtension?> s_platformCommands = new(() =>
		ApiExtensibility.CreateInstance<ITextCommandBarFlyoutCommandsExtension>(typeof(TextCommandBarFlyout), out var extension)
			? extension
			: null);

	// Lets runtime tests stand in a platform provider on hosts that register none (Skia Desktop).
	internal static ITextCommandBarFlyoutCommandsExtension? PlatformCommandsOverrideForTesting { get; set; }

	// Platform command buttons are reused across openings, keyed by command id. Each click runs the command
	// captured on the latest opening, so a single Click handler per button is enough.
	private readonly Dictionary<string, AppBarButton> m_platformButtons = new();
	private readonly Dictionary<string, Action> m_platformActions = new();

	// UIKit offers "Select" (the word at the caret) on the insertion-caret edit menu; only there.
	private static bool ShouldOfferSelectCommand(TextBox textBox, bool prefersPrimaryCommands)
		=> prefersPrimaryCommands
			&& textBox.SelectionLength == 0
			&& textBox.Core.TouchSelectionConvention == TextBoxCore.TouchTextSelectionConvention.iOS
			&& !string.IsNullOrWhiteSpace(textBox.Text);

	private void ExecuteSelectCommand()
	{
		if (Target is TextBox textBox)
		{
			textBox.Core.SelectWordAtCaretForTouch();
		}

		if (IsButtonInPrimaryCommands(TextControlButtons.Select))
		{
			// The selection now has a word in it: rebuild the bar (Cut / Copy / Paste…), as UIKit does.
			UpdateButtons();
		}
	}

	private void AddPlatformCommands()
	{
		if (!InputDevicePrefersPrimaryCommands
			|| Target is not TextBox textBox
			|| (PlatformCommandsOverrideForTesting ?? s_platformCommands.Value) is not { } provider)
		{
			return;
		}

		m_platformActions.Clear();

		foreach (var command in provider.GetCommands(textBox))
		{
			if (!m_platformButtons.TryGetValue(command.Id, out var button))
			{
				button = new AppBarButton();
				var id = command.Id;
				button.Click += (_, _) =>
				{
					if (m_platformActions.TryGetValue(id, out var action))
					{
						Hide();
						action();
					}
				};
				m_platformButtons[command.Id] = button;
			}

			button.Label = command.Label;
			m_platformActions[command.Id] = command.Execute;
			SecondaryCommands.Add(button);
		}
	}
}
