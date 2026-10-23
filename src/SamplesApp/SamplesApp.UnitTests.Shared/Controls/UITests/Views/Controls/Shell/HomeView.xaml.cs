#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SampleControl.Entities;
using SampleControl.Presentation;
using Uno.UI.Common;
using Uno.UI.Samples.Entities;
using Uno.UI.Samples.Helper;

namespace Uno.UI.Samples.Controls;

/// <summary>The shell's landing page (DataContext = <see cref="SampleChooserViewModel"/>). It replaces the sample host while shown.</summary>
public sealed partial class HomeView : UserControl
{
	internal const int MaxCards = 8;

	// Below this the tags drop under the sample count, and the page uses the narrow padding.
	private const double NarrowWidth = 600;

	private SampleChooserViewModel? _viewModel;
	private ICommand? _recordCommand;

	public HomeView()
	{
		InitializeComponent();

		DataContextChanged += (_, _) => Attach(DataContext as SampleChooserViewModel);
		Loaded += (_, _) => Attach(DataContext as SampleChooserViewModel);
		Unloaded += (_, _) => Attach(null);
		SizeChanged += (_, e) => UpdateLayoutForWidth(e.NewSize.Width);
	}

	/// <summary>False on touch, where the shortcut captions mean nothing.</summary>
	public bool ShowShortcutHints
	{
		get => (bool)GetValue(ShowShortcutHintsProperty);
		set => SetValue(ShowShortcutHintsProperty, value);
	}

	public static DependencyProperty ShowShortcutHintsProperty { get; } =
		DependencyProperty.Register(nameof(ShowShortcutHints), typeof(bool), typeof(HomeView), new PropertyMetadata(true, OnShowShortcutHintsChanged));

	private static void OnShowShortcutHintsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((HomeView)d).UpdateAll();

	private void Attach(SampleChooserViewModel? viewModel)
	{
		if (ReferenceEquals(_viewModel, viewModel))
		{
			return;
		}

		if (_viewModel is not null)
		{
			_viewModel.PropertyChanged -= OnViewModelPropertyChanged;
		}

		_viewModel = viewModel;

		if (_viewModel is not null)
		{
			_viewModel.PropertyChanged += OnViewModelPropertyChanged;
		}

		UpdateAll();
	}

	private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			case nameof(SampleChooserViewModel.RecentSamples):
				UpdateRecents();
				break;

			case nameof(SampleChooserViewModel.FavoriteSamples):
				UpdateFavorites();
				break;

			case nameof(SampleChooserViewModel.Categories):
			case nameof(SampleChooserViewModel.IsSampleIndexLoaded):
				UpdateSummary();
				UpdateCategories();
				UpdateQuickActions();
				break;
		}
	}

	private void UpdateAll()
	{
		UpdateSummary();
		UpdateQuickActions();
		UpdateRecents();
		UpdateFavorites();
		UpdateCategories();
	}

	private void UpdateSummary()
	{
		var appInfo = _viewModel?.AppInfo;
		ShellHomePlatformTag.Text = appInfo?.Platform ?? "";
		ShellHomeFrameworkTag.Text = appInfo?.TargetFramework ?? "";
		ShellHomeConfigurationTag.Text = appInfo?.Configuration ?? "";

		ShellHomeSummary.Text = _viewModel is { IsSampleIndexLoaded: true } vm
			? SummaryText(vm.TotalSampleCount, vm.CategoryCount)
			: "Indexing samples…";
	}

	internal static string SummaryText(int samples, int categories)
		=> $"{samples:N0} {(samples == 1 ? "sample" : "samples")} in {categories:N0} {(categories == 1 ? "category" : "categories")}";

	private void UpdateQuickActions()
	{
		if (_viewModel is not { } vm)
		{
			ShellHomeQuickActions.ItemsSource = null;
			return;
		}

		List<HomeCard> actions =
		[
			Action("ShellHomeRuntimeTests", "Runtime tests", "\uE9D5", vm.OpenRuntimeTestsCommand, ShellCommands.OpenRuntimeTests),
			Action("ShellHomePlayground", "Playground", "\uE943", vm.OpenPlaygroundCommand, ShellCommands.OpenPlayground),
		];

		if (vm.HasBenchmarks)
		{
			actions.Add(Action("ShellHomeBenchmarks", "Benchmarks", "\uE9D2", vm.OpenBenchmarksCommand, commandId: null));
		}

		if (SampleChooserViewModel.CanRecordScreenshots)
		{
			_recordCommand ??= new DelegateCommand(ConfirmRecording);
			actions.Add(Action("ShellHomeRecordScreenshots", "Record screenshots", "\uE722", _recordCommand, commandId: null));
		}

		ShellHomeQuickActions.ItemsSource = actions;

		HomeCard Action(string automationId, string title, string glyph, ICommand command, string? commandId)
		{
			var shortcut = commandId is null ? "" : ShellCommands.Describe(commandId);
			return new HomeCard(title, "", glyph, command, null, automationId)
			{
				Shortcut = shortcut,
				ShortcutText = ShowShortcutHints ? shortcut : "",
				ToolTip = shortcut.Length > 0 ? $"{title} ({shortcut})" : title,
			};
		}
	}

	private void UpdateRecents()
	{
		var recents = SampleCards(_viewModel?.RecentSamples, "ShellHomeRecent");
		ShellHomeRecentList.ItemsSource = recents;
		ShellHomeRecentList.Visibility = ShellFunctions.Visible(recents.Count > 0);
		ShellHomeRecentShowAll.Visibility = ShellFunctions.Visible(recents.Count > 0);
		ShellHomeRecentEmptyCard.Visibility = ShellFunctions.Visible(recents.Count == 0);
		ShellHomeRecentEmpty.Text = _viewModel is { RecentsEnabled: false }
			? "History is off in this build."
			: "Samples you open show up here.";
	}

	private void UpdateFavorites()
	{
		var favorites = SampleCards(_viewModel?.FavoriteSamples, "ShellHomeFavorite");
		ShellHomeFavoritesList.ItemsSource = favorites;
		ShellHomeFavoritesList.Visibility = ShellFunctions.Visible(favorites.Count > 0);
		ShellHomeFavoritesShowAll.Visibility = ShellFunctions.Visible(favorites.Count > 0);
		ShellHomeFavoritesEmptyCard.Visibility = ShellFunctions.Visible(favorites.Count == 0);
		ShellHomeFavoritesEmpty.Text = ShowShortcutHints
			? $"Star a sample ({ShellCommands.Describe(ShellCommands.ToggleFavorite)}) to pin it here."
			: "Star a sample to pin it here.";
	}

	private void UpdateCategories()
	{
		var categories = TopCategories(_viewModel?.Categories)
			.Select(c => new HomeCard(c.Category, SampleCountText(c.Count), CategoryGlyph(c.Category), _viewModel!.ShowCategoryCommand, c.Category, "ShellHomeCategory")
			{
				ToolTip = $"Browse {c.Category}",
			})
			.ToList();

		ShellHomeCategoriesList.ItemsSource = categories;
		ShellHomeCategoriesSection.Visibility = ShellFunctions.Visible(categories.Count > 0);
	}

	private List<HomeCard> SampleCards(IEnumerable<SampleChooserContent>? samples, string automationId)
	{
		if (_viewModel is not { } vm || samples is null)
		{
			return [];
		}

		// The quick actions already cover the tool pages.
		return samples
			.Where(s => s is not null && !SampleChooserViewModel.IsToolPage(s))
			.Take(MaxCards)
			.Select(s =>
			{
				var category = SampleCategory(s);
				return new HomeCard(s.ControlName, category, CategoryGlyph(category), vm.OpenSampleCommand, s, automationId)
				{
					ToolTip = s.ControlName,
				};
			})
			.ToList();
	}

	/// <summary>The largest categories, alphabetically; the library has the rest.</summary>
	internal static IEnumerable<SampleChooserCategory> TopCategories(IEnumerable<SampleChooserCategory>? categories)
		=> categories?
			.Where(c => c is not null && !string.IsNullOrEmpty(c.Category))
			.OrderByDescending(c => c.Count)
			.ThenBy(c => c.Category, StringComparer.OrdinalIgnoreCase)
			.Take(MaxCards)
			.OrderBy(c => c.Category, StringComparer.OrdinalIgnoreCase)
		?? Enumerable.Empty<SampleChooserCategory>();

	internal static string SampleCountText(int count) => count == 1 ? "1 sample" : $"{count:N0} samples";

	private static string SampleCategory(SampleChooserContent sample)
		=> sample.Categories?.FirstOrDefault() is { } category && !category.StartsWith('_') ? category : "Tools";

	// A rough hint of what a category is about; anything unknown gets the generic grid glyph.
	internal static string CategoryGlyph(string? category)
	{
		var name = category ?? "";
		return Glyphs.FirstOrDefault(g => name.Contains(g.Keyword, StringComparison.OrdinalIgnoreCase)).Glyph ?? "\uF0E2";
	}

	private static readonly (string Keyword, string Glyph)[] Glyphs =
	[
		("Tools", "\uE90F"),
		("Button", "\uE7C9"),
		("AutoSuggest", "\uE721"),
		("Search", "\uE721"),
		("Text", "\uE8D2"),
		("Font", "\uE8D2"),
		("Scroll", "\uE8CB"),
		("List", "\uE8FD"),
		("Grid", "\uE80A"),
		("Repeater", "\uE8FD"),
		("Tree", "\uE8FD"),
		("Navigation", "\uE700"),
		("Menu", "\uE700"),
		("Flyout", "\uE8A1"),
		("Popup", "\uE8A1"),
		("Dialog", "\uE8A1"),
		("Animation", "\uE916"),
		("Storyboard", "\uE916"),
		("Composition", "\uE916"),
		("Image", "\uEB9F"),
		("Media", "\uE714"),
		("Brush", "\uE790"),
		("Color", "\uE790"),
		("Input", "\uE765"),
		("Keyboard", "\uE765"),
		("Pointer", "\uE7C9"),
		("Storage", "\uE8B7"),
		("File", "\uE8B7"),
		("Layout", "\uE80A"),
		("Panel", "\uE80A"),
		("Window", "\uE8A7"),
		("Date", "\uE787"),
		("Calendar", "\uE787"),
		("Time", "\uE916"),
		("Web", "\uE774"),
		("Map", "\uE707"),
	];

	private void UpdateLayoutForWidth(double width)
	{
		var narrow = width < NarrowWidth;
		ShellHomeContent.Padding = narrow ? new Thickness(16, 12, 16, 12) : new Thickness(24, 16, 24, 16);

		Grid.SetRow(ShellHomeTags, narrow ? 1 : 0);
		Grid.SetColumn(ShellHomeTags, narrow ? 0 : 1);
		Grid.SetColumnSpan(ShellHomeTags, narrow ? 2 : 1);
	}

	private void RecentShowAll_Click(object sender, RoutedEventArgs e) => _viewModel?.ShowBrowserSection(Section.Recents);

	private void FavoritesShowAll_Click(object sender, RoutedEventArgs e) => _viewModel?.ShowBrowserSection(Section.Favorites);

	private void BrowseLibrary_Click(object sender, RoutedEventArgs e) => _viewModel?.ShowLibraryCommand.Execute(null);

	private void ConfirmRecording()
	{
		var index = (ShellHomeQuickActions.ItemsSource as List<HomeCard>)?.FindIndex(a => a.AutomationId == "ShellHomeRecordScreenshots") ?? -1;
		if (index < 0 || ShellHomeQuickActions.TryGetElement(index) is not FrameworkElement anchor)
		{
			return;
		}

		ShellConfirmFlyout.Show(
			anchor,
			"This walks through every sample and takes a while. The shell stays hidden until it ends.",
			"Start recording",
			"ShellHomeRecordConfirm",
			() => _viewModel?.RecordAllTestsCommand.Execute(null));
	}
}

/// <summary>One Home card: a quick action, a sample or a category.</summary>
public sealed class HomeCard
{
	public HomeCard(string title, string caption, string glyph, ICommand command, object? commandParameter, string automationId)
	{
		Title = title;
		Caption = caption;
		Glyph = glyph;
		Command = command;
		CommandParameter = commandParameter;
		AutomationId = automationId;
		ToolTip = title;
	}

	public string Title { get; }

	public string Caption { get; }

	public string Glyph { get; }

	public ICommand Command { get; }

	public object? CommandParameter { get; }

	public string AutomationId { get; }

	public string AutomationName => Caption.Length > 0 ? $"{Title}, {Caption}" : Title;

	public string ToolTip { get; init; }

	public string Shortcut { get; init; } = "";

	public string ShortcutText { get; init; } = "";
}
