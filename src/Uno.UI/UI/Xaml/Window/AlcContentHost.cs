#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Xaml.Controls;

/// <summary>
/// A specialized ContentControl that hosts content from a secondary AssemblyLoadContext,
/// inheriting resources from the secondary ALC's Application.Current.Resources.
/// </summary>
public sealed partial class AlcContentHost : ContentControl
{
	private Application? _sourceApplicationOverride;
	private Application? _contentApplication;

	// Keys this control copied into its own Resources / ThemeDictionaries from the source
	// application. Tracked so the next UpdateMergedResources removes exactly those entries:
	// when the secondary app's content is cleared (app unloading), the host must not keep
	// referencing the previous app's resource objects — they may be typed in the secondary
	// (collectible) AssemblyLoadContext and would otherwise pin it after unload.
	//
	// Direct resources are only ever copied for keys the host does not already have (see the
	// Except filter in UpdateMergedResources), so releasing them is a plain Remove. Theme
	// dictionaries, however, OVERWRITE the host's same-key entry, so each projection records
	// the host's previous value (and whether one existed) to restore it on release rather than
	// dropping a consumer-defined dictionary the projection had shadowed.
	private readonly List<object> _copiedDirectResourceKeys = new();
	private readonly List<ProjectedThemeDictionary> _copiedThemeDictionaries = new();

	// A theme-dictionary key this control projected from the source application, paired with the
	// host's own value for that key before the projection overwrote it.
	private readonly struct ProjectedThemeDictionary
	{
		public ProjectedThemeDictionary(object key, bool hadPreviousValue, object? previousValue)
		{
			Key = key;
			HadPreviousValue = hadPreviousValue;
			PreviousValue = previousValue;
		}

		public object Key { get; }

		public bool HadPreviousValue { get; }

		public object? PreviousValue { get; }
	}

	public AlcContentHost()
	{
		HorizontalAlignment = HorizontalAlignment.Stretch;
		VerticalAlignment = VerticalAlignment.Stretch;
		HorizontalContentAlignment = HorizontalAlignment.Stretch;
		VerticalContentAlignment = VerticalAlignment.Stretch;

		Loaded += OnLoaded;
		Unloaded += OnUnloaded;
	}

	/// <summary>
	/// Raised after <see cref="OnContentChanged"/> completes. Allows hosting code
	/// (e.g. an outer-app binary loader) to observe inner-app content
	/// transitions without subclassing.
	/// </summary>
	public event EventHandler<EventArgs>? ContentChanged;

	internal Application? SourceApplicationOverride
	{
		get => _sourceApplicationOverride;
		set
		{
			if (!ReferenceEquals(_sourceApplicationOverride, value))
			{
				_sourceApplicationOverride = value;
				UpdateMergedResources();
			}
		}
	}

	protected override void OnContentChanged(object oldContent, object newContent)
	{
		base.OnContentChanged(oldContent, newContent);

		_contentApplication = Application.GetForInstance(newContent);
		UpdateMergedResources();

		ContentChanged?.Invoke(this, EventArgs.Empty);
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		UpdateMergedResources();
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
		// When the host discards this control (e.g. the previewed app is torn down and the
		// host's reference is set to null / it is removed from the tree) without first setting
		// Content to null, release everything projected from the secondary application so its
		// resource objects — potentially typed in the collectible ALC — are not retained by this
		// control after it leaves the tree, which would otherwise pin the ALC after unload.
		// A subsequent re-Loaded re-projects via UpdateMergedResources.
		ClearProjectedResources();
		Resources.MergedDictionaries.Clear();
	}

	// Removes everything this control projected from the source application (direct resources and
	// theme-dictionary entries) and clears the tracking lists. Shared by UpdateMergedResources
	// (before recomputing the source) and OnUnloaded (on teardown).
	private void ClearProjectedResources()
	{
		foreach (var key in _copiedDirectResourceKeys)
		{
			Resources.Remove(key);
		}

		_copiedDirectResourceKeys.Clear();

		foreach (var projected in _copiedThemeDictionaries)
		{
			if (projected.HadPreviousValue)
			{
				// Restore the host's own dictionary that the projection overwrote, rather than
				// dropping a consumer-defined entry (e.g. a host "Light" dictionary).
				Resources.ThemeDictionaries[projected.Key] = projected.PreviousValue!;
			}
			else
			{
				Resources.ThemeDictionaries.Remove(projected.Key);
			}
		}

		_copiedThemeDictionaries.Clear();
	}

	private void UpdateMergedResources()
	{
		// Remove everything previously projected from the (old) source application before
		// recomputing the source. When the content is cleared on app unload the source falls
		// back to the host application, and the previous app's resource objects must not be
		// retained by this control (see the tracking fields for the rationale).
		ClearProjectedResources();

		// Clear existing merged dictionaries to avoid duplicates
		Resources.MergedDictionaries.Clear();

		var sourceApp = _sourceApplicationOverride
			?? _contentApplication
			?? Application.GetForInstance(Content)
			?? Application.Current;

		if (sourceApp?.Resources is null)
		{
			return;
		}

		// Merge all resource dictionaries from the source application
		foreach (var resourceDictionary in sourceApp.Resources.MergedDictionaries)
		{
			Resources.MergedDictionaries.Add(resourceDictionary);
		}

		// Copy theme dictionaries
		if (sourceApp.Resources.ThemeDictionaries.Count > 0)
		{
			foreach (var themeDictionary in sourceApp.Resources.ThemeDictionaries)
			{
				var hadPreviousValue = Resources.ThemeDictionaries.TryGetValue(themeDictionary.Key, out var previousValue);
				Resources.ThemeDictionaries[themeDictionary.Key] = themeDictionary.Value;
				_copiedThemeDictionaries.Add(new ProjectedThemeDictionary(themeDictionary.Key, hadPreviousValue, hadPreviousValue ? previousValue : null));
			}
		}

		// Copy direct resources (non-merged)
		// We need to be careful here to only copy resources that are directly defined
		// in the application resources, not those from merged dictionaries
		foreach (var key in sourceApp.Resources.Keys.Except(Resources.Keys).ToList())
		{
			Resources[key] = sourceApp.Resources[key];
			_copiedDirectResourceKeys.Add(key);
		}
	}
}
