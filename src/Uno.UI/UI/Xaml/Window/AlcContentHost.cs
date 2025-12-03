#nullable enable

using System;
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

	public AlcContentHost()
	{
		HorizontalAlignment = HorizontalAlignment.Stretch;
		VerticalAlignment = VerticalAlignment.Stretch;
		HorizontalContentAlignment = HorizontalAlignment.Stretch;
		VerticalContentAlignment = VerticalAlignment.Stretch;

		Loaded += OnLoaded;
	}

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
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		UpdateMergedResources();
	}

	private void UpdateMergedResources()
	{
		var sourceApp = _sourceApplicationOverride
			?? _contentApplication
			?? Application.GetForInstance(Content)
			?? Application.Current;

		if (sourceApp?.Resources is null)
		{
			return;
		}

		// Clear existing merged dictionaries to avoid duplicates
		Resources.MergedDictionaries.Clear();

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
			}
		}

		// Copy direct resources (non-merged)
		// We need to be careful here to only copy resources that are directly defined
		// in the application resources, not those from merged dictionaries
		foreach (var key in sourceApp.Resources.Keys.Except(Resources.Keys).ToList())
		{
			Resources[key] = sourceApp.Resources[key];
		}
	}
}
