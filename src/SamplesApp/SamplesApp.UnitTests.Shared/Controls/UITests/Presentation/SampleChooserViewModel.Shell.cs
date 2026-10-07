#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using SampleControl.Entities;
using SamplesApp.Samples.Help;
using Uno.UI.Common;
using Uno.UI.Samples.Entities;
using UITests.Playground;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

using ICommand = System.Windows.Input.ICommand;

namespace SampleControl.Presentation;

public partial class SampleChooserViewModel
{
	internal const string BenchmarksPageTypeName = "Benchmarks.Shared.Controls.BenchmarkDotNetTestsPage";
	private const string RuntimeTestsPageTypeName = "SamplesApp.Samples.UnitTests.UnitTestsPage";

	private const string ShellThemeKey = "Shell.Theme";
	private const string ShellStartupPageKey = "Shell.StartupPage";
	private const string ShellManualTestsOnlyKey = "Shell.ManualTestsOnly";
	private const string ShellDescriptionCollapsedKey = "Shell.DescriptionCollapsed";
	private const string ShellBrowserSectionKey = "Shell.BrowserSection";

	private bool _isAutomationRun;
	private bool _isShellChromeVisible = true;
	private bool _isHomeVisible;
	private bool _isDescriptionCollapsed;
	private BrowserView _browserView = BrowserView.Samples;
	private StartupPage _startupPage = StartupPage.Home;
	private ShellDestination _shellDestination = ShellDestination.Samples;
	private int? _totalSampleCount;
	private ElementTheme? _pendingRootTheme;

	/// <summary>
	/// Set by the app before the first view model is created, when it was launched by automation
	/// (runtime tests, screenshots, perf runs).
	/// </summary>
	internal static bool IsAutomationLaunch { get; set; }

	/// <summary>Set by the shell once it hosts the Home view; until then Home falls back to Playground.</summary>
	internal static bool SupportsHomeView { get; set; }

	public static bool CanRecordScreenshots =>
#if __SKIA__
		!OperatingSystem.IsBrowser();
#else
		false;
#endif

	public static bool CanCreateNewWindow =>
#if HAS_UNO
		Uno.UI.Xaml.Controls.NativeWindowFactory.SupportsMultipleWindows;
#elif WINAPPSDK
		true;
#else
		false;
#endif

	public AppInfo AppInfo { get; } = new();

	/// <summary>
	/// True when the app runs under automation. Persisted shell settings are neither restored
	/// nor written, the recent list is left alone and Home is never shown.
	/// </summary>
	public bool IsAutomationRun
	{
		get => _isAutomationRun;
		internal set
		{
			if (_isAutomationRun != value)
			{
				_isAutomationRun = value;
				RaisePropertyChanged();

				if (value)
				{
					IsHomeVisible = false;
				}
			}
		}
	}

	/// <summary>False while recording screenshots or in focus mode (F11).</summary>
	public bool IsShellChromeVisible
	{
		get => _isShellChromeVisible;
		set
		{
			if (_isShellChromeVisible != value)
			{
				_isShellChromeVisible = value;
				RaisePropertyChanged();
			}
		}
	}

	public bool IsHomeVisible
	{
		get => _isHomeVisible;
		set
		{
			value &= !IsAutomationRun;
			if (_isHomeVisible != value)
			{
				_isHomeVisible = value;
				RaisePropertyChanged();
			}
		}
	}

	public BrowserView BrowserView
	{
		get => _browserView;
		set
		{
			if (_browserView != value)
			{
				_browserView = value;
				RaisePropertyChanged();
			}
		}
	}

	public ShellDestination ShellDestination
	{
		get => _shellDestination;
		private set
		{
			if (_shellDestination != value)
			{
				_shellDestination = value;
				RaisePropertyChanged();
			}
		}
	}

	public StartupPage StartupPage
	{
		get => _startupPage;
		set
		{
			if (_startupPage != value)
			{
				_startupPage = value;
				PersistShellSetting(ShellStartupPageKey, value.ToString());
				RaisePropertyChanged();
			}
		}
	}

	public bool IsDescriptionCollapsed
	{
		get => _isDescriptionCollapsed;
		set
		{
			if (_isDescriptionCollapsed != value)
			{
				_isDescriptionCollapsed = value;
				PersistShellSetting(ShellDescriptionCollapsedKey, value);
				RaisePropertyChanged();
			}
		}
	}

	/// <summary>0 = System, 1 = Light, 2 = Dark.</summary>
	public int AppThemeIndex
	{
		get => GetRootTheme() switch
		{
			ElementTheme.Light => 1,
			ElementTheme.Dark => 2,
			_ => 0,
		};
		set
		{
			// A TwoWay SelectedIndex pushes -1 while its items reset.
			if (value is < 0 or > 2 || value == AppThemeIndex)
			{
				return;
			}

			switch (value)
			{
				case 1:
					IsAppThemeLight = true;
					break;
				case 2:
					IsAppThemeDark = true;
					break;
				default:
					IsAppThemeSystem = true;
					break;
			}
		}
	}

	public IReadOnlyList<string> CurrentBreadcrumb
	{
		get
		{
			var category = CurrentSelectedSample?.Categories?.FirstOrDefault();
			if (category is null)
			{
				return Array.Empty<string>();
			}

			return category.StartsWith('_') ? new[] { "Tools" } : new[] { "Library", category };
		}
	}

	public bool RecentsEnabled => _numberOfRecentSamplesVisible > 0;

	public int TotalSampleCount => _totalSampleCount ??=
		_allCategories?.SelectMany(c => c.SamplesContent).Distinct().Count() ?? 0;

	public int CategoryCount => Categories?.Count ?? 0;

	public int FavoritesCount => FavoriteSamples?.Count ?? 0;

	public bool HasFavorites => FavoritesCount > 0;

	public bool HasRecents => RecentSamples?.Any() ?? false;

	public bool HasBenchmarks => _allSamples.Any(s => s.Type?.FullName == BenchmarksPageTypeName);

	public ICommand ShowHomeCommand { get; private set; } = null!;
	public ICommand ShowSettingsCommand { get; private set; } = null!;
	public ICommand ShowLibraryCommand { get; private set; } = null!;
	public ICommand ShowCategoryCommand { get; private set; } = null!;
	public ICommand ShowSearchResultsCommand { get; private set; } = null!;
	public ICommand OpenSampleCommand { get; private set; } = null!;
	public ICommand OpenSampleInNewWindowCommand { get; private set; } = null!;
	public ICommand OpenBenchmarksCommand { get; private set; } = null!;
	public ICommand CopySampleLinkCommand { get; private set; } = null!;
	public ICommand CopyTypeNameCommand { get; private set; } = null!;
	public ICommand OpenSourceOnGitHubCommand { get; private set; } = null!;
	public ICommand ClearFavoritesCommand { get; private set; } = null!;
	public ICommand ClearRecentsCommand { get; private set; } = null!;
	public ICommand ToggleFocusModeCommand { get; private set; } = null!;

	private void InitializeShellCommands()
	{
		ShowHomeCommand = new DelegateCommand(ShowHome);
		ShowSettingsCommand = new DelegateCommand(ShowSettings);
		ShowLibraryCommand = new DelegateCommand(() => ShowBrowserSection(Section.Library));
		ShowCategoryCommand = new DelegateCommand<string>(ShowCategory);
		ShowSearchResultsCommand = new DelegateCommand(() => ShowBrowserSection(Section.Search));
		OpenSampleCommand = new DelegateCommand<SampleChooserContent>(sample =>
		{
			if (sample is not null)
			{
				_ = OpenSample(CancellationToken.None, sample);
			}
		});
		OpenSampleInNewWindowCommand = new DelegateCommand<SampleChooserContent>(OpenSampleInNewWindow);
		OpenBenchmarksCommand = new DelegateCommand(() => _ = SetSelectedSample(CancellationToken.None, BenchmarksPageTypeName));
		CopySampleLinkCommand = new DelegateCommand<SampleChooserContent>(sample => CopyToClipboard((sample ?? CurrentSelectedSample)?.QueryString));
		CopyTypeNameCommand = new DelegateCommand<SampleChooserContent>(sample => CopyToClipboard((sample ?? CurrentSelectedSample)?.ControlType?.FullName));
		OpenSourceOnGitHubCommand = new DelegateCommand<SampleChooserContent>(sample => _ = OpenSourceOnGitHub(sample ?? CurrentSelectedSample));
		ClearFavoritesCommand = new DelegateCommand(() => _ = ClearFavorites());
		ClearRecentsCommand = new DelegateCommand(() => _ = ClearRecents());
		ToggleFocusModeCommand = new DelegateCommand(ToggleFocusMode);
	}

	private void ObserveShellChanges()
	{
		PropertyChanged += OnShellPropertyChanged;
		UpdateShellDestination();
	}

	private void OnShellPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			case nameof(CurrentSelectedSample):
				RaisePropertyChanged(nameof(CurrentBreadcrumb));
				RaisePropertyChanged(nameof(KeyboardShortcutsEnabled));
				UpdateShellDestination();
				break;

			case nameof(IsHomeVisible):
			case nameof(BrowserView):
			case nameof(IsSplitVisible):
				UpdateShellDestination();
				break;

			case nameof(IsRecordAllTests):
				IsShellChromeVisible = !IsRecordAllTests;
				break;

			case nameof(Categories):
				RaisePropertyChanged(nameof(CategoryCount));
				break;

			case nameof(FavoriteSamples):
				RaisePropertyChanged(nameof(FavoritesCount));
				RaisePropertyChanged(nameof(HasFavorites));
				SyncFavoritedSample();
				break;

			case nameof(RecentSamples):
				RaisePropertyChanged(nameof(HasRecents));
				break;

			case nameof(ManualTestsOnly):
				PersistShellSetting(ShellManualTestsOnlyKey, ManualTestsOnly);
				break;

			case nameof(CategoryVisibility) when CategoryVisibility:
			case nameof(SampleVisibility) when SampleVisibility:
				PersistShellSetting(ShellBrowserSectionKey, nameof(Section.Library));
				break;

			case nameof(RecentsVisibility) when RecentsVisibility:
				PersistShellSetting(ShellBrowserSectionKey, nameof(Section.Recents));
				break;

			case nameof(FavoritesVisibility) when FavoritesVisibility:
				PersistShellSetting(ShellBrowserSectionKey, nameof(Section.Favorites));
				break;
		}
	}

	private void UpdateShellDestination()
	{
		if (BrowserView == BrowserView.Settings && IsSplitVisible)
		{
			ShellDestination = ShellDestination.Settings;
		}
		else if (IsHomeVisible)
		{
			ShellDestination = ShellDestination.Home;
		}
		else
		{
			ShellDestination = CurrentSelectedSample?.ControlType?.FullName switch
			{
				RuntimeTestsPageTypeName => ShellDestination.RuntimeTests,
				BenchmarksPageTypeName => ShellDestination.Benchmarks,
				var name when name == typeof(Playground).FullName => ShellDestination.Playground,
				var name when name == typeof(HelpPage).FullName => ShellDestination.Help,
				_ => ShellDestination.Samples,
			};
		}
	}

	/// <summary>
	/// Opens what <see cref="StartupPage"/> asks for. Called by the app only when the launch selected no sample.
	/// </summary>
	public async Task ApplyStartupPageAsync(CancellationToken ct)
	{
		var page = IsAutomationRun ? StartupPage.Playground : StartupPage;

		if (page == StartupPage.LastSample)
		{
			var recents = RecentSamples?.ToList() is { Count: > 0 } loaded ? loaded : await GetRecentSamples(ct);
			if (recents.FirstOrDefault() is { ControlType: not null } last)
			{
				await SetSelectedSample(ct, last.ControlType.FullName);
				return;
			}

			page = StartupPage.Home;
		}

		if (page == StartupPage.Home && SupportsHomeView)
		{
			ShowHome();
			return;
		}

		SetSelectedSample(ct, "_None", "Playground");
	}

	private void ShowHome()
	{
		if (IsAutomationRun)
		{
			return;
		}

		IsSplitVisible = false;
		IsHomeVisible = true;
	}

	private void ShowSettings()
	{
		BrowserView = BrowserView.Settings;
		IsSplitVisible = true;
	}

	internal void ShowBrowserSection(Section section)
	{
		BrowserView = BrowserView.Samples;
		ShowNewSection(CancellationToken.None, section);
		RecentsSelected = section == Section.Recents;
		FavoritesSelected = section == Section.Favorites;
		IsSplitVisible = true;
	}

	private void ShowCategory(string? categoryName)
	{
		var category = Categories?.FirstOrDefault(c => c.Category == categoryName)
			?? _allCategories?.FirstOrDefault(c => c.Category == categoryName);

		if (category is null)
		{
			return;
		}

		SelectedCategory = category;
		ShowBrowserSection(Section.Samples);
	}

	private void OpenSampleInNewWindow(SampleChooserContent? sample)
	{
		sample ??= CurrentSelectedSample;
		if (!CanCreateNewWindow)
		{
			return;
		}

		CreateNewWindow();

		// CreateNewWindow replaced Instance with the new window's view model.
		if (sample?.ControlType is { } type && !ReferenceEquals(Instance, this))
		{
			Instance.TrySelectSample(CancellationToken.None, type.FullName!);
		}
	}

	private void ToggleFocusMode()
	{
		if (!IsRecordAllTests)
		{
			IsShellChromeVisible = !IsShellChromeVisible;
		}
	}

	private static void CopyToClipboard(string? text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return;
		}

		DataPackage dataPackage = new();
		dataPackage.SetText(text);
		Clipboard.SetContent(dataPackage);
	}

	// Commands discard these tasks, so failures are logged here.
	private static async Task OpenSourceOnGitHub(SampleChooserContent? sample)
	{
		try
		{
			if (sample?.GitHubSourceUrl is { Length: > 0 } url)
			{
				await Windows.System.Launcher.LaunchUriAsync(new Uri(url));
			}
		}
		catch (Exception e)
		{
			ShellLog.Error($"Could not open the source of {sample?.ControlName}.", e);
		}
	}

	private async Task ClearFavorites()
	{
		try
		{
			foreach (var sample in FavoriteSamples ?? new List<SampleChooserContent>())
			{
				UpdateFavoriteForSample(sample, false);
			}

			await SetFile(SampleChooserFavoriteConstant, Array.Empty<string>());

			FavoriteSamples = new List<SampleChooserContent>();
			OnSelectedCategoryChanged();
		}
		catch (Exception e)
		{
			ShellLog.Error("Could not clear the favorites.", e);
		}
	}

	private async Task ClearRecents()
	{
		try
		{
			await SetFile(SampleChooserLRUConstant, Array.Empty<string>());

			RecentSamples = new List<SampleChooserContent>();
		}
		catch (Exception e)
		{
			ShellLog.Error("Could not clear the recent samples.", e);
		}
	}

	/// <summary>
	/// Called when UI tests drive the app through the RunTest backdoor: stops persistence and recents,
	/// and drops any restored theme so results do not depend on local settings.
	/// </summary>
	internal void EnterUITestAutomation()
	{
		if (IsAutomationRun)
		{
			return;
		}

		IsAutomationRun = true;
		ApplyTransientTheme(ElementTheme.Default);
	}

	/// <summary>Applies a theme without persisting it (launch argument, restored setting).</summary>
	internal void ApplyTransientTheme(ElementTheme theme)
	{
		if (Owner.XamlRoot?.Content is FrameworkElement)
		{
			_pendingRootTheme = null;
			SetRootTheme(theme);
			RaiseThemeFlagsChanged();
			return;
		}

		// The view model is created before its control enters the tree.
		if (_pendingRootTheme is null)
		{
			Owner.Loaded += ApplyPendingRootTheme;
		}

		_pendingRootTheme = theme;
	}

	private void ApplyPendingRootTheme(object sender, RoutedEventArgs e)
	{
		Owner.Loaded -= ApplyPendingRootTheme;

		if (_pendingRootTheme is { } theme)
		{
			_pendingRootTheme = null;
			SetRootTheme(theme);
			RaiseThemeFlagsChanged();
		}
	}

	private void SetAppTheme(ElementTheme theme)
	{
		_pendingRootTheme = null;
		SetRootTheme(theme);
		PersistShellSetting(ShellThemeKey, theme.ToString());
		RaiseThemeFlagsChanged();
	}

	private bool IsPersistedFavorite(SampleChooserContent sample) => FavoriteSamples?.Contains(sample) ?? false;

	private bool _isPersistingFavorite;

	internal bool IsPersistingFavorite => _isPersistingFavorite;

	// Clicks faster than a write are folded into the next pass, so a few passes always reach the last request.
	private const int MaxFavoritePersistPasses = 8;

	// One write at a time: a click that lands mid-write is picked up by the next loop pass instead of being undone.
	private async Task PersistRequestedFavoriteAsync(SampleChooserContent sample)
	{
		if (_isPersistingFavorite)
		{
			return;
		}

		_isPersistingFavorite = true;
		try
		{
			for (var pass = 0; ReferenceEquals(CurrentSelectedSample, sample) && _isFavoritedSample != IsPersistedFavorite(sample); pass++)
			{
				if (pass == MaxFavoritePersistPasses)
				{
					ShellLog.Warn($"Gave up saving favorite {sample.ControlName} after {pass} attempts.");
					break;
				}

				// Sets rather than toggles: a stored list that failed to load must not turn a removal into an add.
				if (!await ToggleFavorite(CancellationToken.None, sample, isFavorite: _isFavoritedSample))
				{
					ShellLog.Warn($"Favorite {sample.ControlName} already reads as {(_isFavoritedSample ? "set" : "cleared")} in storage; keeping that.");
					break;
				}
			}
		}
		catch (Exception e)
		{
			ShellLog.Error($"Failed to update favorite {sample.ControlName}.", e);
		}
		finally
		{
			_isPersistingFavorite = false;
			SyncFavoritedSample();
		}
	}

	private void SyncFavoritedSample()
	{
		if (!_isPersistingFavorite)
		{
			IsFavoritedSample = CurrentSelectedSample is { } sample && IsPersistedFavorite(sample);
		}
	}

	private bool ShouldTrackRecents => !IsAutomationRun && !IsRecordAllTests;

	private void RestoreShellSettings()
	{
		if (IsAutomationRun)
		{
			return;
		}

		if (ReadShellSetting<string>(ShellThemeKey) is { } theme && Enum.TryParse(theme, out ElementTheme elementTheme))
		{
			ApplyTransientTheme(elementTheme);
		}

		if (ReadShellSetting<string>(ShellStartupPageKey) is { } startupPage && Enum.TryParse(startupPage, out StartupPage parsedStartupPage))
		{
			_startupPage = parsedStartupPage;
		}

		if (ReadShellSetting<bool?>(ShellDescriptionCollapsedKey) is { } descriptionCollapsed)
		{
			_isDescriptionCollapsed = descriptionCollapsed;
		}

		if (ReadShellSetting<bool?>(ShellManualTestsOnlyKey) is true)
		{
			ManualTestsOnly = true;
		}

		RestoreLatestCategory();

		if (ReadShellSetting<string>(ShellBrowserSectionKey) is { } section
			&& Enum.TryParse(section, out Section parsedSection)
			&& parsedSection is Section.Recents or Section.Favorites)
		{
			ShowNewSection(CancellationToken.None, parsedSection);
			RecentsSelected = parsedSection == Section.Recents;
			FavoritesSelected = parsedSection == Section.Favorites;
		}
	}

	private void RestoreLatestCategory()
	{
		try
		{
			if (ApplicationData.Current.LocalSettings.Values.ContainsKey(SampleChooserLatestCategoryConstant)
				&& GetLatestCategory(CancellationToken.None) is { } category)
			{
				SelectedCategory = category;
			}
		}
		catch (Exception e)
		{
			ShellLog.Warn("Could not restore the latest category.", e);
		}
	}

	private void PersistShellSetting(string key, object value)
	{
		if (IsAutomationRun)
		{
			return;
		}

		try
		{
			ApplicationData.Current.LocalSettings.Values[key] = value;
		}
		catch (Exception e)
		{
			ShellLog.Warn($"Could not persist shell setting {key}.", e);
		}
	}

	private static T? ReadShellSetting<T>(string key)
	{
		try
		{
			return ApplicationData.Current.LocalSettings.Values.TryGetValue(key, out var value) && value is T typed ? typed : default;
		}
		catch (Exception e)
		{
			ShellLog.Warn($"Could not read shell setting {key}.", e);
			return default;
		}
	}
}
