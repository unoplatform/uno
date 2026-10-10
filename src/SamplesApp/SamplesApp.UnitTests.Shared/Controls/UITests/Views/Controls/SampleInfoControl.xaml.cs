#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using SampleControl.Entities;
using SampleControl.Presentation;
using Uno.UI.Samples.Helper;
using Windows.ApplicationModel.DataTransfer;

namespace Uno.UI.Samples.Controls;

public sealed partial class SampleInfoControl : UserControl
{
	private const double MaxContentWidth = 400;
	private const double TouchTargetSize = 40;
	private const double DesktopTargetSize = 32;
	private const double LinksStackWidth = 320;
	private const string CopyGlyph = "\uE8C8";
	private const string CopiedGlyph = "\uE73E";

	private static readonly TimeSpan CopiedGlyphDuration = TimeSpan.FromMilliseconds(1500);

	private readonly DispatcherQueueTimer _copiedTimer;
	private FontIcon? _copiedIcon;

	public SampleInfoControl()
	{
		this.InitializeComponent();

		_copiedTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
		_copiedTimer.Interval = CopiedGlyphDuration;
		_copiedTimer.IsRepeating = false;
		_copiedTimer.Tick += (_, _) => RestoreCopyGlyph();

		DataContextChanged += (_, _) => UpdateFromSample();
		Unloaded += (_, _) => RestoreCopyGlyph();
		UpdateFromSample();
	}

	/// <summary>Raised when an action in the panel makes the hosting flyout pointless.</summary>
	internal event EventHandler? CloseRequested;

	public void CopyContentClick(object sender, RoutedEventArgs e)
	{
		if (sender is not Button { CommandParameter: string { Length: > 0 } content } button)
		{
			return;
		}

		DataPackage dataPackage = new();
		dataPackage.SetText(content);
		try
		{
			SampleChooserViewModel.SetClipboardContent(dataPackage);
		}
		catch (Exception ex)
		{
			// WinAppSDK throws CLIPBRD_E_CANT_OPEN while another process holds the clipboard.
			ShellLog.Warn("Could not copy to the clipboard.", ex);
			return;
		}

		ShowCopied(button);
	}

	public async void OpenGitHubClick(object sender, RoutedEventArgs e)
	{
		if (DataContext is not SampleChooserContent sample || string.IsNullOrEmpty(sample.GitHubSourceUrl))
		{
			return;
		}

		try
		{
			await Windows.System.Launcher.LaunchUriAsync(new Uri(sample.GitHubSourceUrl));
		}
		catch (Exception ex)
		{
			ShellLog.Error($"Could not open {sample.GitHubSourceUrl}.", ex);
		}
	}

	private void OpenNewWindowClick(object sender, RoutedEventArgs e)
	{
		if (DataContext is SampleChooserContent sample)
		{
			CloseRequested?.Invoke(this, EventArgs.Empty);
			SampleChooserViewModel.Instance.OpenSampleInNewWindowCommand.Execute(sample);
		}
	}

	/// <summary>Keeps the panel inside the window: 400 wide at most, scrolling when it is taller than the window.</summary>
	internal void FitToWindow(Windows.Foundation.Size windowSize)
	{
		// The flyout presenter pads 16 DIP on each side and keeps a margin to the window edge.
		Width = Math.Clamp(windowSize.Width - 64, 240, MaxContentWidth);
		ShellInfoScroll.MaxHeight = Math.Max(160, windowSize.Height - 128);

		var stackLinks = Width < LinksStackWidth;
		ShellInfoLinks.Orientation = stackLinks ? Orientation.Vertical : Orientation.Horizontal;
		ShellInfoLinks.Spacing = stackLinks ? 0 : 16;
		UpdateTouchSizing();
	}

	/// <summary>Zero-width spaces after each dot let a long type name wrap.</summary>
	internal static string WithBreakOpportunities(string text) => text.Replace(".", ".​");

	internal static string GetFileName(string? path)
		=> string.IsNullOrEmpty(path) ? string.Empty : path.Substring(path.LastIndexOfAny(['/', '\\']) + 1);

	/// <summary>
	/// A dotnet run argument that survives a shell. The app cuts the sample at the first space, so a name with
	/// spaces or shell characters goes by its type name; without one it is quoted.
	/// </summary>
	internal static string GetLaunchArgument(string? queryString, string? typeName = null)
	{
		if (string.IsNullOrEmpty(queryString))
		{
			return string.Empty;
		}

		var value = queryString.TrimStart('?');
		if (!IsShellSafe(value) && typeName is { Length: > 0 } && IsShellSafe(typeName))
		{
			value = $"sample={typeName}";
		}

		return IsShellSafe(value) ? $"-- {value}" : $"-- \"{value}\"";
	}

	private static bool IsShellSafe(string value)
		=> value.All(c => char.IsAsciiLetterOrDigit(c) || c is '_' or '.' or '/' or '-' or '=' or '+' or ':');

	internal static IReadOnlyList<string> GetChips(SampleChooserContent sample)
	{
		List<string> chips = new();
		if (sample.Categories is { } categories)
		{
			foreach (var category in categories)
			{
				if (!string.IsNullOrWhiteSpace(category))
				{
					chips.Add(category);
				}
			}
		}

		if (sample.IsManualTest)
		{
			chips.Add("Manual");
		}

		if (sample.UsesFrame)
		{
			chips.Add("Uses Frame");
		}

		if (sample.DisableKeyboardShortcuts)
		{
			chips.Add("Shortcuts disabled");
		}

		if (sample.IgnoreInSnapshotTests)
		{
			chips.Add("Not in snapshots");
		}

		return chips;
	}

	private void UpdateFromSample()
	{
		RestoreCopyGlyph();

		if (DataContext is not SampleChooserContent sample)
		{
			ShellInfoContent.Visibility = Visibility.Collapsed;
			ShellInfoEmptyText.Visibility = Visibility.Visible;
			return;
		}

		ShellInfoContent.Visibility = Visibility.Visible;
		ShellInfoEmptyText.Visibility = Visibility.Collapsed;

		ShellInfoTitle.Text = sample.ControlName ?? string.Empty;
		ShellInfoChips.ItemsSource = GetChips(sample);

		var hasDescription = !string.IsNullOrWhiteSpace(sample.Description);
		ShellInfoDescription.Text = sample.Description ?? string.Empty;
		ShellInfoDescription.Visibility = hasDescription ? Visibility.Visible : Visibility.Collapsed;
		ShellInfoNoDescription.Visibility = hasDescription ? Visibility.Collapsed : Visibility.Visible;

		var typeName = sample.ControlType?.FullName;
		var link = sample.Categories is null ? null : sample.QueryString;
		var launch = GetLaunchArgument(link, typeName);
		var fileName = GetFileName(sample.SourceFilePath);

		SetRow(ShellInfoTypeLabel, ShellInfoTypeValue, ShellCopyTypeButton, typeName, typeName is null ? null : WithBreakOpportunities(typeName));
		SetRow(ShellInfoLinkLabel, ShellInfoLinkValue, ShellCopyLinkInfoButton, link, link);
		SetRow(ShellInfoLaunchLabel, ShellInfoLaunchValue, ShellCopyLaunchButton, launch, WithBreakOpportunities(launch));
		SetRow(ShellInfoSourceLabel, ShellInfoSourceValue, ShellCopySourceButton, sample.SourceFilePath, fileName);
		ToolTipService.SetToolTip(ShellInfoSourceValue, string.IsNullOrEmpty(sample.SourceFilePath) ? null : sample.SourceFilePath);

		GitHubLinkButton.Visibility = string.IsNullOrEmpty(sample.GitHubSourceUrl) ? Visibility.Collapsed : Visibility.Visible;
		ShellInfoOpenNewWindowButton.Visibility = SampleChooserViewModel.CanCreateNewWindow ? Visibility.Visible : Visibility.Collapsed;
		UpdateTouchSizing();
	}

	private static void SetRow(TextBlock label, TextBlock value, Button copyButton, string? copyText, string? displayText)
	{
		var visibility = string.IsNullOrEmpty(copyText) ? Visibility.Collapsed : Visibility.Visible;
		label.Visibility = value.Visibility = copyButton.Visibility = visibility;
		value.Text = displayText ?? string.Empty;
		copyButton.CommandParameter = copyText;
	}

	private void UpdateTouchSizing()
	{
		var touch = ShellFunctions.IsTouchShell;
		var size = touch ? TouchTargetSize : DesktopTargetSize;
		foreach (var button in new[] { ShellCopyTypeButton, ShellCopyLinkInfoButton, ShellCopyLaunchButton, ShellCopySourceButton })
		{
			button.Width = button.Height = size;
		}

		GitHubLinkButton.MinHeight = ShellInfoOpenNewWindowButton.MinHeight = touch ? TouchTargetSize : 0;
	}

	private void ShowCopied(Button button)
	{
		RestoreCopyGlyph();

		if (button.Content is FontIcon icon)
		{
			icon.Glyph = CopiedGlyph;
			_copiedIcon = icon;
			_copiedTimer.Start();
		}

		ShellInfoCopiedAnnouncement.Text = "Copied";

		// Neither WinUI nor Uno raises LiveRegionChanged by itself when the text changes.
		var peer = FrameworkElementAutomationPeer.FromElement(ShellInfoCopiedAnnouncement)
			?? FrameworkElementAutomationPeer.CreatePeerForElement(ShellInfoCopiedAnnouncement);
		peer?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
	}

	private void RestoreCopyGlyph()
	{
		_copiedTimer.Stop();
		if (_copiedIcon is not null)
		{
			_copiedIcon.Glyph = CopyGlyph;
			_copiedIcon = null;
		}

		ShellInfoCopiedAnnouncement.Text = string.Empty;
	}
}
