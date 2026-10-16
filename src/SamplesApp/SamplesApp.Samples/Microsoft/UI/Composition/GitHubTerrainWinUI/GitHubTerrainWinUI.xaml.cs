#nullable enable

using System;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Composition.GitHubTerrainWinUI;

[Sample(
	"Cool Graphics",
	Name = "GitHubTerrainWinUI",
	IsManualTest = true,
	Description =
		"The same landscape as the Skia sample, built only from public WinUI API: the projection is " +
		"affine, so every face of every box is a parallelogram, and each one is a Composition sprite " +
		"shape carrying a unit rectangle under a transform. The view toggle springs the camera " +
		"between the flat heatmap and the isometric landscape; the data choice swaps the smooth " +
		"field for a sparse year of real-looking activity.")]
public sealed partial class GitHubTerrainWinUI : Page
{
	private readonly Stopwatch _clock = Stopwatch.StartNew();

	private TerrainMesh? _mesh;
	private bool _rendering;
	private double _lastSeconds;

	public GitHubTerrainWinUI()
	{
		InitializeComponent();

		Loaded += OnLoaded;
		Unloaded += OnUnloaded;

		raiseToggle.Checked += (_, _) => SetRaised(true);
		raiseToggle.Unchecked += (_, _) => SetRaised(false);
		dataChoice.SelectionChanged += (_, _) => _mesh?.UseSparseData(dataChoice.SelectedIndex == 1);
	}

	private void SetRaised(bool raised)
	{
		raiseToggle.Content = raised ? "View: terrain" : "View: flat";
		if (_mesh is { } mesh)
		{
			mesh.RaiseTarget = raised ? 1f : 0f;
		}
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		_mesh ??= new TerrainMesh(host);

		if (!_rendering)
		{
			_rendering = true;
			_lastSeconds = _clock.Elapsed.TotalSeconds;
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

	private void OnRendering(object? sender, object e)
	{
		if (_mesh is not { } mesh)
		{
			return;
		}

		var seconds = _clock.Elapsed.TotalSeconds;
		var dt = (float)Math.Min(0.05, seconds - _lastSeconds);
		_lastSeconds = seconds;

		mesh.Update((float)ActualWidth, (float)ActualHeight, dt);
	}
}
