#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using SampleControl.Entities;

namespace SampleControl.Presentation;

public partial class SampleChooserViewModel
{
	internal static readonly TimeSpan SearchDebounceDelay = TimeSpan.FromMilliseconds(150);

	private IList<IGrouping<string, SampleChooserContent>> _searchResultsGrouped = Array.Empty<IGrouping<string, SampleChooserContent>>();
	private int _searchResultCount;

	/// <summary>Ranked search results grouped by first category, capped at <see cref="SampleSearch.MaxGroupedResults"/>.</summary>
	public IList<IGrouping<string, SampleChooserContent>> SearchResultsGrouped
	{
		get => _searchResultsGrouped;
		private set
		{
			_searchResultsGrouped = value;
			RaisePropertyChanged();
		}
	}

	/// <summary>Total number of search matches (not capped).</summary>
	public int SearchResultCount
	{
		get => _searchResultCount;
		private set
		{
			if (_searchResultCount != value)
			{
				_searchResultCount = value;
				RaisePropertyChanged();
			}
		}
	}

	/// <summary>Runs the search for the current <see cref="SearchTerm"/> synchronously, bypassing the debounce.</summary>
	public void RunSearchNow()
	{
		CancelPendingSearch();

		var results = _allCategories is { } categories
			? SampleSearch.Rank(SearchTerm, categories.SelectMany(c => c.SamplesContent))
			: new List<SampleChooserContent>();

		ApplySearchResults(results);
	}

	/// <summary>Opens the best-ranked result for the current <see cref="SearchTerm"/>, if any.</summary>
	public bool TryOpenTopSearchResult()
	{
		RunSearchNow();

		if (FilteredSamples is { Count: > 0 } samples)
		{
			SelectedSearchSample = samples[0];
			return true;
		}

		return false;
	}

	private void ApplySearchResults(List<SampleChooserContent> results)
	{
		FilteredSamples = results;
		SearchResultsGrouped = SampleSearch.Group(results).ToArray<IGrouping<string, SampleChooserContent>>();
		SearchResultCount = results.Count;
	}
}
