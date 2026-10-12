#nullable enable

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Samples.Controls;
using Uno.WinUI.Graphics2DSK;

namespace UITests.Shared.Windows_UI_Composition.GitHubTerrain;

[Sample(
	"Cool Graphics",
	Name = "GitHubTerrain",
	IsManualTest = true,
	Description =
		"Nine years of contributions as a landscape: 53 weeks across, nine years of weekdays deep, " +
		"each cell a box drawn as sorted faces, with walls only where the surface steps. The view " +
		"toggle springs the camera between the flat heatmap and the isometric landscape; the data " +
		"choice swaps the smooth field for a sparse year of real-looking activity.")]
public sealed partial class GitHubTerrain : Page
{
	private TerrainCanvas? _canvas;
	private bool _rendering;

	public GitHubTerrain()
	{
		InitializeComponent();

		if (!SKCanvasElement.IsSupportedOnCurrentPlatform())
		{
			host.Children.Add(new TextBlock { Text = "This sample is not supported on this platform." });
			return;
		}

		Loaded += OnLoaded;
		Unloaded += OnUnloaded;

		// Each toggle names the state it is in, so both axes are readable at a glance rather than
		// leaving the off state to be guessed from a checked/unchecked border.
		raiseToggle.Checked += (_, _) => SetRaised(true);
		raiseToggle.Unchecked += (_, _) => SetRaised(false);
		dataChoice.SelectionChanged += (_, _) => SetSparse(dataChoice.SelectedIndex == 1);
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		if (_canvas is null)
		{
			_canvas = new TerrainCanvas { RaiseTarget = 1f, IsHitTestVisible = false };
			host.Children.Insert(0, _canvas);
		}

		SetRaised(raiseToggle.IsChecked == true);
		SetSparse(dataChoice.SelectedIndex == 1);

		if (!_rendering)
		{
			_rendering = true;
			CompositionTarget.Rendering += OnRendering;
		}
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
		if (_rendering)
		{
			_rendering = false;
			CompositionTarget.Rendering -= OnRendering;
		}
	}

	private void SetRaised(bool raised)
	{
		raiseToggle.Content = raised ? "View: terrain" : "View: flat";
		if (_canvas is { } c)
		{
			c.RaiseTarget = raised ? 1f : 0f;
		}
	}

	private void SetSparse(bool sparse) => _canvas?.UseSparseData(sparse);

	private void OnRendering(object? sender, object e) => _canvas?.Invalidate();
}
