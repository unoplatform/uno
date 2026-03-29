using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Input.ContextMenu;

/// <summary>
/// Repro sample for TextCommandBarFlyout compositor pattern.
/// Replicates dynamically adding custom AppBarButtons with MenuFlyout submenus
/// to SecondaryCommands via Opening/Closed events.
///
/// UI elements are cached per-TextBox and reused across flyout open/close cycles,
/// mirroring how TextCommandBarFlyout itself caches system buttons in m_buttons.
/// Items must NOT be static/shared across flyout instances — that causes
/// "Element is already the child of another element" on WinUI and visual tree
/// corruption on Uno/Skia.
/// </summary>
[Sample("Input", Name = "TextBox ContextMenu Compositor",
	Description = "TextCommandBarFlyout compositor pattern: custom items + submenus in SecondaryCommands",
	IsManualTest = true)]
public sealed partial class TextBox_ContextMenu_Compositor : Page
{
	private int _eventCounter;

	// Per-TextBox cached items. Each TextBox gets its own set via the dictionary,
	// reused across open/close cycles. UpdateButtons() calls SecondaryCommands.Clear()
	// on every Opening which cleanly detaches them, then we re-add the same instances.
	private readonly Dictionary<string, CachedCompositorItems> _itemsByTextBox = new();

	private sealed class CachedCompositorItems
	{
		public AppBarSeparator Separator { get; set; }
		public AppBarButton PasteSpecialButton { get; set; }
		public MenuFlyout PasteSpecialMenuFlyout { get; set; }
		public MenuFlyoutItem PasteAsPlainTextItem { get; set; }
		public MenuFlyoutItem PasteToCellsItem { get; set; }
		public MenuFlyoutItem PasteFormattedItem { get; set; }
	}

	public TextBox_ContextMenu_Compositor()
	{
		this.InitializeComponent();

		// Scenario 1, 2, 3, 5: subscribe via Loaded (single TextBox per scenario)
		SubscribeViaLoaded(PasteSpecialTextBox, ComposePasteSpecial);
		SubscribeViaLoaded(OverflowTextBox, ComposeManyItems);
		SubscribeViaLoaded(SeparatorTextBox, ComposeSeparatedItems);
		SubscribeViaLoaded(ShadowCleanupTextBox, ComposePasteSpecial);

		// Scenario 4: subscribe via GettingFocus/LosingFocus (matching client pattern)
		SubscribeViaFocus(RapidSwitchA);
		SubscribeViaFocus(RapidSwitchB);
		SubscribeViaFocus(RapidSwitchC);
		SubscribeViaFocus(RapidSwitchD);
	}

	#region Per-TextBox Item Cache

	private CachedCompositorItems GetOrCreateItems(TextBox textBox)
	{
		var key = textBox.Name;
		if (!_itemsByTextBox.TryGetValue(key, out var items))
		{
			var menuFlyout = new MenuFlyout();
			items = new CachedCompositorItems
			{
				Separator = new AppBarSeparator(),
				PasteSpecialMenuFlyout = menuFlyout,
				PasteAsPlainTextItem = new MenuFlyoutItem { Text = "Paste as Plain Text" },
				PasteToCellsItem = new MenuFlyoutItem { Text = "Paste to Cells" },
				PasteFormattedItem = new MenuFlyoutItem { Text = "Paste Formatted" },
				PasteSpecialButton = new AppBarButton
				{
					Label = "Paste Special",
					Flyout = menuFlyout
				}
			};

			items.PasteAsPlainTextItem.Click += OnSubmenuItemClick;
			items.PasteToCellsItem.Click += OnSubmenuItemClick;
			items.PasteFormattedItem.Click += OnSubmenuItemClick;

			_itemsByTextBox[key] = items;
		}
		return items;
	}

	#endregion

	#region Subscription Patterns

	private void SubscribeViaLoaded(TextBox textBox, Action<TextBox, TextCommandBarFlyout> composeAction)
	{
		textBox.Loaded += (s, e) =>
		{
			var flyout = textBox.ContextFlyout;
			if (flyout is TextCommandBarFlyout tcbf)
			{
				tcbf.Opening += (sender, args) => OnFlyoutOpening(textBox, tcbf, composeAction);
				tcbf.Closed += (sender, args) => OnFlyoutClosed(textBox, tcbf);
				LogEvent($"{textBox.Name}: Subscribed via Loaded (TextCommandBarFlyout)");
			}
			else
			{
				LogEvent($"{textBox.Name}: ContextFlyout is {flyout?.GetType().Name ?? "null"} (not TextCommandBarFlyout)");
			}
		};
	}

	/// <summary>
	/// Subscribe via GettingFocus/LosingFocus — matches the client's UnifiedTextOperationsBehavior pattern.
	/// </summary>
	private void SubscribeViaFocus(TextBox textBox)
	{
		textBox.GettingFocus += (sender, args) =>
		{
			var flyout = textBox.ContextFlyout;
			if (flyout is TextCommandBarFlyout tcbf)
			{
				tcbf.Opening += RapidSwitch_Opening;
				tcbf.Closed += RapidSwitch_Closed;
			}
		};

		textBox.LosingFocus += (sender, args) =>
		{
			var flyout = textBox.ContextFlyout;
			if (flyout is TextCommandBarFlyout tcbf)
			{
				tcbf.Opening -= RapidSwitch_Opening;
				tcbf.Closed -= RapidSwitch_Closed;
			}
		};
	}

	private void RapidSwitch_Opening(object sender, object e)
	{
		if (sender is TextCommandBarFlyout tcbf)
		{
			var textBox = FindTextBoxForFlyout(tcbf);
			if (textBox != null)
			{
				OnFlyoutOpening(textBox, tcbf, ComposePasteSpecial);
			}
		}
	}

	private void RapidSwitch_Closed(object sender, object e)
	{
		if (sender is TextCommandBarFlyout tcbf)
		{
			var textBox = FindTextBoxForFlyout(tcbf);
			if (textBox != null)
			{
				OnFlyoutClosed(textBox, tcbf);
			}
		}
	}

	private TextBox FindTextBoxForFlyout(TextCommandBarFlyout flyout)
	{
		if (RapidSwitchA.ContextFlyout == flyout) return RapidSwitchA;
		if (RapidSwitchB.ContextFlyout == flyout) return RapidSwitchB;
		if (RapidSwitchC.ContextFlyout == flyout) return RapidSwitchC;
		if (RapidSwitchD.ContextFlyout == flyout) return RapidSwitchD;
		return null;
	}

	#endregion

	#region Compositor: Opening / Closed

	private void OnFlyoutOpening(TextBox textBox, TextCommandBarFlyout flyout, Action<TextBox, TextCommandBarFlyout> composeAction)
	{
		LogEvent($"Opening: {textBox.Name} | Primary={flyout.PrimaryCommands.Count}, Secondary={flyout.SecondaryCommands.Count}");

		composeAction(textBox, flyout);

		LogEvent($"After compose: {textBox.Name} | Primary={flyout.PrimaryCommands.Count}, Secondary={flyout.SecondaryCommands.Count}");
	}

	private void OnFlyoutClosed(TextBox textBox, TextCommandBarFlyout flyout)
	{
		// Remove cached items from SecondaryCommands (but keep them for reuse)
		if (_itemsByTextBox.TryGetValue(textBox.Name, out var items))
		{
			flyout.SecondaryCommands.Remove(items.PasteSpecialButton);
			flyout.SecondaryCommands.Remove(items.Separator);
			items.PasteSpecialMenuFlyout.Items.Clear();
			LogEvent($"Closed: {textBox.Name} | Removed cached items, Secondary={flyout.SecondaryCommands.Count}");
		}
		else
		{
			// Scenarios 2, 3 use fresh items tracked in _lastAddedItems
			var removedCount = 0;
			foreach (var item in _lastAddedItems)
			{
				if (flyout.SecondaryCommands.Remove(item))
				{
					removedCount++;
				}
			}
			LogEvent($"Closed: {textBox.Name} | Removed {removedCount} fresh items, Secondary={flyout.SecondaryCommands.Count}");
			_lastAddedItems.Clear();
		}
	}

	// For scenarios 2, 3 which use fresh items
	private readonly List<ICommandBarElement> _lastAddedItems = new();

	#endregion

	#region Compose Strategies

	/// <summary>
	/// Scenario 1 + 4 + 5: Adds cached "Paste Special" AppBarButton with MenuFlyout submenu.
	/// Items are created once per TextBox and reused across open/close cycles.
	/// </summary>
	private void ComposePasteSpecial(TextBox textBox, TextCommandBarFlyout flyout)
	{
		var items = GetOrCreateItems(textBox);

		// Re-populate the cached submenu (items cleared in OnFlyoutClosed)
		items.PasteSpecialMenuFlyout.Items.Add(items.PasteAsPlainTextItem);
		items.PasteSpecialMenuFlyout.Items.Add(items.PasteToCellsItem);
		items.PasteSpecialMenuFlyout.Items.Add(items.PasteFormattedItem);

		// Re-add cached items to SecondaryCommands (cleared by UpdateButtons)
		flyout.SecondaryCommands.Add(items.Separator);
		flyout.SecondaryCommands.Add(items.PasteSpecialButton);
	}

	/// <summary>
	/// Scenario 2: Adds 6 fresh custom AppBarButtons to overflow SecondaryCommands.
	/// </summary>
	private void ComposeManyItems(TextBox textBox, TextCommandBarFlyout flyout)
	{
		_lastAddedItems.Clear();

		var separator = new AppBarSeparator();
		flyout.SecondaryCommands.Add(separator);
		_lastAddedItems.Add(separator);

		for (int i = 1; i <= 6; i++)
		{
			var index = i;
			var button = new AppBarButton { Label = $"Custom Action {index}" };
			button.Click += (s, e) => LogEvent($"Custom Action {index} clicked on {textBox.Name}");
			flyout.SecondaryCommands.Add(button);
			_lastAddedItems.Add(button);
		}
	}

	/// <summary>
	/// Scenario 3: Adds separator + "Paste Special" with submenu + another separator + "Custom Format".
	/// </summary>
	private void ComposeSeparatedItems(TextBox textBox, TextCommandBarFlyout flyout)
	{
		_lastAddedItems.Clear();

		var sep1 = new AppBarSeparator();
		flyout.SecondaryCommands.Add(sep1);
		_lastAddedItems.Add(sep1);

		var pasteSpecial = new AppBarButton
		{
			Label = "Paste Special",
			Flyout = CreateSubmenuFlyout()
		};
		flyout.SecondaryCommands.Add(pasteSpecial);
		_lastAddedItems.Add(pasteSpecial);

		var sep2 = new AppBarSeparator();
		flyout.SecondaryCommands.Add(sep2);
		_lastAddedItems.Add(sep2);

		var customFormat = new AppBarButton { Label = "Custom Format" };
		customFormat.Click += (s, e) => LogEvent($"Custom Format clicked on {textBox.Name}");
		flyout.SecondaryCommands.Add(customFormat);
		_lastAddedItems.Add(customFormat);
	}

	private MenuFlyout CreateSubmenuFlyout()
	{
		var flyout = new MenuFlyout();

		var item1 = new MenuFlyoutItem { Text = "Paste as Plain Text" };
		item1.Click += OnSubmenuItemClick;
		flyout.Items.Add(item1);

		var item2 = new MenuFlyoutItem { Text = "Paste to Cells" };
		item2.Click += OnSubmenuItemClick;
		flyout.Items.Add(item2);

		var item3 = new MenuFlyoutItem { Text = "Paste Formatted" };
		item3.Click += OnSubmenuItemClick;
		flyout.Items.Add(item3);

		return flyout;
	}

	#endregion

	#region Submenu Item Handlers

	private void OnSubmenuItemClick(object sender, RoutedEventArgs e)
	{
		var itemName = sender switch
		{
			MenuFlyoutItem mfi => mfi.Text,
			AppBarButton abb => abb.Label,
			_ => sender?.GetType().Name ?? "unknown"
		};

		LogEvent($"Submenu item clicked: '{itemName}'");
	}

	#endregion

	#region Inspect Flyout

	private void OnInspectPasteSpecialClick(object sender, RoutedEventArgs e)
	{
		PasteSpecialInspectResult.Text = InspectTextBoxFlyout(PasteSpecialTextBox);
	}

	private void OnInspectOverflowClick(object sender, RoutedEventArgs e)
	{
		OverflowInspectResult.Text = InspectTextBoxFlyout(OverflowTextBox);
	}

	private string InspectTextBoxFlyout(TextBox textBox)
	{
		var flyout = textBox.ContextFlyout;
		if (flyout == null)
		{
			var result = "ContextFlyout: NULL";
			LogEvent(result);
			return result;
		}

		var sb = new StringBuilder();
		sb.AppendLine($"ContextFlyout Type: {flyout.GetType().Name}");

		if (flyout is TextCommandBarFlyout tcbf)
		{
			sb.AppendLine($"PrimaryCommands: {tcbf.PrimaryCommands.Count}");
			sb.AppendLine($"SecondaryCommands: {tcbf.SecondaryCommands.Count}");

			if (tcbf.PrimaryCommands.Count == 0)
			{
				sb.AppendLine("*** BUG: PrimaryCommands is 0 (Cut/Copy/Paste missing)! ***");
			}

			foreach (var cmd in tcbf.PrimaryCommands)
			{
				if (cmd is AppBarButton abb)
				{
					sb.AppendLine($"  Primary: '{abb.Label}' Enabled={abb.IsEnabled}");
				}
			}

			foreach (var cmd in tcbf.SecondaryCommands)
			{
				if (cmd is AppBarButton abb)
				{
					var hasFlyout = abb.Flyout != null ? " [has submenu]" : "";
					sb.AppendLine($"  Secondary: '{abb.Label}' Enabled={abb.IsEnabled}{hasFlyout}");

					if (abb.Flyout is MenuFlyout mf)
					{
						foreach (var subItem in mf.Items)
						{
							if (subItem is MenuFlyoutItem mfi)
							{
								sb.AppendLine($"    Sub: '{mfi.Text}'");
							}
						}
					}
				}
				else if (cmd is AppBarSeparator)
				{
					sb.AppendLine("  Secondary: ---separator---");
				}
			}
		}
		else
		{
			sb.AppendLine($"Not a TextCommandBarFlyout");
		}

		var text = sb.ToString().TrimEnd();
		LogEvent(text.Replace("\n", " | ").Replace("\r", ""));
		return text;
	}

	#endregion

	#region Event Log

	private void LogEvent(string message)
	{
		_eventCounter++;
		var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
		var logEntry = $"[{_eventCounter}] {timestamp}: {message}\n";

		EventLogText.Text = logEntry + EventLogText.Text;

		if (EventLogText.Text.Length > 5000)
		{
			EventLogText.Text = EventLogText.Text.Substring(0, 4000);
		}
	}

	private void OnClearLogClick(object sender, RoutedEventArgs e)
	{
		_eventCounter = 0;
		EventLogText.Text = "Events will be logged here...";
		PasteSpecialInspectResult.Text = "Click 'Inspect Flyout' after right-clicking...";
		OverflowInspectResult.Text = "Click 'Inspect Flyout' after right-clicking...";
	}

	#endregion
}
