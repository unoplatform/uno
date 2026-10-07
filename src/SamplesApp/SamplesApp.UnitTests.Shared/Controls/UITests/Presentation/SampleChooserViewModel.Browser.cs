#nullable enable

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using SampleControl.Entities;
using Uno.UI.Samples.Entities;

namespace SampleControl.Presentation;

public partial class SampleChooserViewModel
{
	private bool _isSyncingBrowserSelection;
	private bool _isShowingBrowserSection;
	private bool _isBrowserSelectionSyncQueued;
	private bool _wasSplitVisible;
	private bool _areSavedSamplesLoaded;

	/// <summary>False until favorites and recent samples have been read from storage.</summary>
	public bool AreSavedSamplesLoaded
	{
		get => _areSavedSamplesLoaded;
		private set
		{
			if (_areSavedSamplesLoaded != value)
			{
				_areSavedSamplesLoaded = value;
				RaisePropertyChanged();
			}
		}
	}

	/// <summary>The sample the browser should point at: a library sample that is shown, not a tool page or Home.</summary>
	private SampleChooserContent? ShownLibrarySample
		=> !IsHomeVisible && CurrentSelectedSample is { } sample && CurrentBreadcrumb is ["Library", _] ? sample : null;

	private void ObserveBrowserChanges()
	{
		_wasSplitVisible = IsSplitVisible;
		PropertyChanged += OnBrowserPropertyChanged;
	}

	private void OnBrowserPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			case nameof(IsSplitVisible):
				// Opening the browser lands on what is shown; explicit section commands pick their own place.
				if (IsSplitVisible && !_wasSplitVisible && !_isShowingBrowserSection && BrowserView == BrowserView.Samples)
				{
					RevealCurrentSample();
				}

				_wasSplitVisible = IsSplitVisible;
				break;

			case nameof(Categories):
				RemapSelectedCategory();
				break;

			case nameof(SearchTerm) when string.IsNullOrWhiteSpace(SearchTerm):
				CloseSearchResults();
				break;

			// The lists pick up their new items through bindings that may run after this handler.
			case nameof(CurrentSelectedSample):
			case nameof(IsHomeVisible):
			case nameof(SampleContents):
			case nameof(FavoriteSamples):
			case nameof(RecentSamples):
				QueueBrowserSelectionSync();
				break;
		}
	}

	/// <summary>Shows the shown sample's row; in the library this drills into its category first.</summary>
	internal void RevealCurrentSample()
	{
		if (CategoriesSelected && ShownLibrarySample is { } sample)
		{
			RevealInLibrary(sample);
		}
		else
		{
			SyncBrowserSelection();
		}
	}

	/// <summary>Drills the library into <paramref name="sample"/>'s category and selects its row, without reloading it.</summary>
	internal bool RevealInLibrary(SampleChooserContent sample)
	{
		var category = SelectedCategory is { } current && (Categories?.Any(c => ReferenceEquals(c, current)) ?? false) && current.SamplesContent.Contains(sample)
			? current
			: FindLibraryCategory(sample);

		if (category is null)
		{
			return false;
		}

		if (!ReferenceEquals(SelectedCategory, category))
		{
			SelectedCategory = category;
		}

		if (!SampleVisibility)
		{
			ShowNewSection(CancellationToken.None, Section.Samples);
		}

		SyncBrowserSelection(sample);
		return true;
	}

	// The manual-tests filter swaps in other category instances; keep pointing at the same category,
	// or back out to the category list when the new list does not have it.
	private void RemapSelectedCategory()
	{
		if (SelectedCategory is not { } selected
			|| Categories is not { } categories
			|| categories.Any(c => ReferenceEquals(c, selected)))
		{
			return;
		}

		if (categories.FirstOrDefault(c => c.Category == selected.Category) is { } match)
		{
			SelectedCategory = match;
			return;
		}

		SelectedCategory = null!;
		SampleContents = [];
		if (SampleVisibility)
		{
			// Replaces the drilled-in view rather than stacking on it, so Back does not return to the dropped category.
			ShowSelectedList(CancellationToken.None, Section.Library);
		}
	}

	// The manual-tests filter swaps the category list, so only categories that are listed count.
	private SampleChooserCategory? FindLibraryCategory(SampleChooserContent sample)
		=> (sample.Categories ?? [])
			.Select(name => Categories?.FirstOrDefault(c => c.Category == name))
			.FirstOrDefault(category => category is not null && category.SamplesContent.Contains(sample));

	private void QueueBrowserSelectionSync()
	{
		if (_isBrowserSelectionSyncQueued)
		{
			return;
		}

		_isBrowserSelectionSyncQueued = true;
		_ = _dispatcher.RunAsync(() =>
		{
			_isBrowserSelectionSyncQueued = false;
			SyncBrowserSelection();
		});
	}

	/// <summary>
	/// Selects the shown sample's row in every browser list (or clears stale rows) without loading it again:
	/// the Selected*Sample setters load a sample even when it is already shown, which UI tests rely on.
	/// </summary>
	internal void SyncBrowserSelection(SampleChooserContent? sample = null)
	{
		if (_isRecordAllTests)
		{
			return;
		}

		sample ??= IsHomeVisible ? null : CurrentSelectedSample;

		_isSyncingBrowserSelection = true;
		try
		{
			if (!Equals(SelectedLibrarySample, Find(SampleContents)))
			{
				SelectedLibrarySample = Find(SampleContents)!;
			}

			if (!Equals(SelectedFavoriteSample, Find(FavoriteSamples)))
			{
				SelectedFavoriteSample = Find(FavoriteSamples)!;
			}

			if (!Equals(SelectedRecentSample, Find(RecentSamples)))
			{
				SelectedRecentSample = Find(RecentSamples)!;
			}
		}
		finally
		{
			_isSyncingBrowserSelection = false;
		}

		SampleChooserContent? Find(IEnumerable<SampleChooserContent>? items)
			=> sample is null ? null : items?.FirstOrDefault(item => item.Equals(sample));
	}

	/// <summary>Shows every search result in the browser pane.</summary>
	internal void ShowSearchResults()
	{
		RunSearchNow();

		if (!SearchVisibility)
		{
			ShowBrowserSection(Section.Search);
		}
		else
		{
			IsSplitVisible = true;
		}
	}

	/// <summary>Lets tests that drive the singleton put the browser back where they found it.</summary>
	internal (Section Last, Section[] History, SampleChooserCategory? Category, BrowserView View) SaveBrowserState()
		=> (_lastSection, _previousSections.ToArray(), SelectedCategory, BrowserView);

	internal void RestoreBrowserState((Section Last, Section[] History, SampleChooserCategory? Category, BrowserView View) state)
	{
		_previousSections.Clear();
		foreach (var section in Enumerable.Reverse(state.History))
		{
			_previousSections.Push(section);
		}

		BrowserView = state.View;
		ShowSelectedList(CancellationToken.None, state.Last);
		if (!ReferenceEquals(SelectedCategory, state.Category))
		{
			SelectedCategory = state.Category!;
		}
	}

	/// <summary>Leaves the search results for the section that was shown before them.</summary>
	internal void CloseSearchResults()
	{
		while (SearchVisibility)
		{
			ShowPreviousSection(CancellationToken.None);
		}
	}
}
