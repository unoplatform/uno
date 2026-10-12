#nullable enable

using System;
using System.Windows.Input;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SampleControl.Presentation;
using Uno.UI.Samples.Helper;

namespace Uno.UI.Samples.Controls;

/// <summary>The browser pane's settings view (DataContext = <see cref="SampleChooserViewModel"/>).</summary>
public sealed partial class SettingsView : UserControl
{
	private const double TouchTargetSize = 40;
	private static readonly TimeSpan CopiedFeedbackDuration = TimeSpan.FromSeconds(2);

	private DispatcherQueueTimer? _copiedTimer;
	private SampleChooserViewModel? _observedViewModel;

	public SettingsView()
	{
		InitializeComponent();

		ShellSettingsMicaCard.Visibility = ShellFunctions.Visible(SampleChooserViewModel.CanUseMica);
		ShellSettingsRecordCard.Visibility = ShellFunctions.Visible(SampleChooserViewModel.CanRecordScreenshots);
		ShellSettingsLogViewDumpCard.Visibility = ShellFunctions.Visible(SampleChooserViewModel.IsDebug);

		// Phones and browsers have no caption to extend into.
		ShellSettingsTitleBarCard.Visibility = ShellFunctions.Visible(!(OperatingSystem.IsAndroid() || OperatingSystem.IsIOS() || OperatingSystem.IsBrowser()));

		Loaded += (_, _) =>
		{
			ObserveViewModel(ViewModel);
			UpdateTouchSizing();
		};
		Unloaded += (_, _) =>
		{
			ObserveViewModel(null);
			ResetCopiedFeedback();
		};
	}

	// Simulate touch can flip while the view is loaded, from here or from the header's quick settings.
	private void ObserveViewModel(SampleChooserViewModel? viewModel)
	{
		if (_observedViewModel is not null)
		{
			_observedViewModel.PropertyChanged -= OnViewModelPropertyChanged;
		}

		_observedViewModel = viewModel;

		if (viewModel is not null)
		{
			viewModel.PropertyChanged += OnViewModelPropertyChanged;
		}
	}

	private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		if (e.PropertyName == "SimulateTouch")
		{
			UpdateTouchSizing();
		}
	}

	private void UpdateTouchSizing()
	{
		var touch = ShellFunctions.IsTouchShell;
		foreach (var control in new Control[] { ShellSettingsThemeComboBox, ShellSettingsStartupPageComboBox, ShellSettingsClearFavoritesButton, ShellSettingsClearHistoryButton, ShellSettingsRecordButton, ShellSettingsLogViewDumpButton, ShellCopyDiagnosticsButton })
		{
			if (touch)
			{
				control.MinHeight = TouchTargetSize;
			}
			else
			{
				control.ClearValue(MinHeightProperty);
			}
		}
	}

	private SampleChooserViewModel? ViewModel => DataContext as SampleChooserViewModel;

	private void ClearFavoritesConfirm_Click(object sender, RoutedEventArgs e)
	{
		ShellSettingsClearFavoritesFlyout.Hide();
		Execute(ViewModel?.ClearFavoritesCommand);
	}

	private void ClearHistoryConfirm_Click(object sender, RoutedEventArgs e)
	{
		ShellSettingsClearHistoryFlyout.Hide();
		Execute(ViewModel?.ClearRecentsCommand);
	}

	private void RecordConfirm_Click(object sender, RoutedEventArgs e)
	{
		ShellSettingsRecordFlyout.Hide();
		Execute(ViewModel?.RecordAllTestsCommand);
	}

	private void CopyDiagnostics_Click(object sender, RoutedEventArgs e)
	{
		if (ViewModel?.CopyDiagnostics() != true)
		{
			ResetCopiedFeedback();
			return;
		}

		ShellCopyDiagnosticsIcon.Glyph = "\uE73E";
		ShellCopyDiagnosticsText.Text = "Copied";

		_copiedTimer ??= CreateCopiedTimer();
		_copiedTimer.Stop();
		_copiedTimer.Start();
	}

	private DispatcherQueueTimer CreateCopiedTimer()
	{
		var timer = DispatcherQueue.CreateTimer();
		timer.Interval = CopiedFeedbackDuration;
		timer.IsRepeating = false;
		timer.Tick += (_, _) => ResetCopiedFeedback();
		return timer;
	}

	private void ResetCopiedFeedback()
	{
		_copiedTimer?.Stop();
		ShellCopyDiagnosticsIcon.Glyph = "\uE8C8";
		ShellCopyDiagnosticsText.Text = "Copy diagnostics";
	}

	private static void Execute(ICommand? command)
	{
		if (command?.CanExecute(null) == true)
		{
			command.Execute(null);
		}
	}
}
