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
	private readonly List<object> _copiedDirectResourceKeys = new();
	private readonly List<object> _copiedThemeDictionaryKeys = new();

	public AlcContentHost()
	{
		HorizontalAlignment = HorizontalAlignment.Stretch;
		VerticalAlignment = VerticalAlignment.Stretch;
		HorizontalContentAlignment = HorizontalAlignment.Stretch;
		VerticalContentAlignment = VerticalAlignment.Stretch;

		Loaded += OnLoaded;
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

	private void UpdateMergedResources()
	{
		// Remove everything previously projected from the (old) source application before
		// recomputing the source. When the content is cleared on app unload the source falls
		// back to the host application, and the previous app's resource objects must not be
		// retained by this control (see the tracking fields for the rationale).
		foreach (var key in _copiedDirectResourceKeys)
		{
			Resources.Remove(key);
		}

		_copiedDirectResourceKeys.Clear();

		foreach (var key in _copiedThemeDictionaryKeys)
		{
			Resources.ThemeDictionaries.Remove(key);
		}

		_copiedThemeDictionaryKeys.Clear();

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
				Resources.ThemeDictionaries[themeDictionary.Key] = themeDictionary.Value;
				_copiedThemeDictionaryKeys.Add(themeDictionary.Key);
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
