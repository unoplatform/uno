using System.Collections.Generic;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using SampleControl.Presentation;
using SamplesApp;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;

namespace UITests.Microsoft_UI_Windowing;

[Sample("Windowing", Name = "SystemBackdrop (Mica)", IsManualTest = true,
	ViewModelType = typeof(MicaBackdropTestsViewModel),
	Description =
		"Demonstrates Window.SystemBackdrop support for Mica and Desktop Acrylic materials. " +
		"On macOS, uses NSVisualEffectView with the matching vibrancy material. " +
		"The page Background should be Transparent to let the backdrop show through.")]
public sealed partial class MicaBackdropTests : Page
{
	public MicaBackdropTests()
	{
		this.InitializeComponent();
		DataContextChanged += OnDataContextChanged;
	}

	internal MicaBackdropTestsViewModel ViewModel { get; private set; }

	private void OnDataContextChanged(Microsoft.UI.Xaml.FrameworkElement sender, Microsoft.UI.Xaml.DataContextChangedEventArgs args)
	{
		ViewModel = args.NewValue as MicaBackdropTestsViewModel;
	}
}

internal class MicaBackdropTestsViewModel : ViewModelBase
{
	private readonly List<(Panel panel, Brush original)> _savedBackgrounds = new();

	public MicaBackdropTestsViewModel()
	{
	}

	public string IsMicaSupported => Microsoft.UI.Composition.SystemBackdrops.MicaController.IsSupported().ToString();

	public string CurrentBackdrop
	{
		get
		{
			var backdrop = App.MainWindow.SystemBackdrop;
			if (backdrop is MicaBackdrop mica)
			{
				return $"Mica ({mica.Kind})";
			}
			return backdrop?.GetType().Name ?? "None";
		}
	}

	public void SetMicaBase()
	{
		SetBackdrop(new MicaBackdrop { Kind = MicaKind.Base });
	}

	public void SetMicaBaseAlt()
	{
		SetBackdrop(new MicaBackdrop { Kind = MicaKind.BaseAlt });
	}

	public void SetDesktopAcrylic()
	{
		SetBackdrop(new DesktopAcrylicBackdrop());
	}

	public void ClearBackdrop()
	{
		App.MainWindow.SystemBackdrop = null;
		RestoreAncestorBackgrounds();
		RaisePropertyChanged(nameof(CurrentBackdrop));
	}

	private void SetBackdrop(SystemBackdrop backdrop)
	{
		App.MainWindow.SystemBackdrop = backdrop;
		MakeAncestorBackgroundsTransparent();
		RaisePropertyChanged(nameof(CurrentBackdrop));
	}

	/// <summary>
	/// Walks the visual tree from the Window content down to this page and makes all
	/// Panel backgrounds transparent so the system backdrop material can show through.
	/// This mirrors WinUI behavior where apps must use transparent backgrounds with Mica.
	/// </summary>
	private void MakeAncestorBackgroundsTransparent()
	{
		RestoreAncestorBackgrounds();

		var transparentBrush = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
		DependencyObject current = App.MainWindow.Content;
		while (current is not null)
		{
			if (current is Panel panel && panel.Background is SolidColorBrush scb && scb.Color.A > 0)
			{
				_savedBackgrounds.Add((panel, panel.Background));
				panel.Background = transparentBrush;
			}
			// Walk into the visual tree
			current = current is ContentControl cc ? cc.Content as DependencyObject
				: current is Panel p && p.Children.Count > 0 ? p.Children[0]
				: Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(current) > 0
					? Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(current, 0)
					: null;
		}
	}

	private void RestoreAncestorBackgrounds()
	{
		foreach (var (panel, original) in _savedBackgrounds)
		{
			panel.Background = original;
		}
		_savedBackgrounds.Clear();
	}
}
