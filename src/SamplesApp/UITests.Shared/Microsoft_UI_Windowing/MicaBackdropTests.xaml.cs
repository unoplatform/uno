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
		"On Windows 11 (22621+), uses DwmSetWindowAttribute with DWMWA_SYSTEMBACKDROP_TYPE. " +
		"The page Background should be Transparent to let the backdrop show through.")]
public sealed partial class MicaBackdropTests : Page
{
	public MicaBackdropTests()
	{
		this.InitializeComponent();
		DataContextChanged += OnDataContextChanged;
		Unloaded += OnUnloaded;
	}

	internal MicaBackdropTestsViewModel ViewModel { get; private set; }

	private void OnDataContextChanged(Microsoft.UI.Xaml.FrameworkElement sender, Microsoft.UI.Xaml.DataContextChangedEventArgs args)
	{
		ViewModel = args.NewValue as MicaBackdropTestsViewModel;
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
		ViewModel?.ClearBackdrop();
	}
}

internal class MicaBackdropTestsViewModel : ViewModelBase
{
	private readonly List<(DependencyObject element, Brush original)> _savedBackgrounds = new();

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
		RestoreSavedBackgrounds();
		RaisePropertyChanged(nameof(CurrentBackdrop));
	}

	private void SetBackdrop(SystemBackdrop backdrop)
	{
		App.MainWindow.SystemBackdrop = backdrop;
		MakeVisualTreeBackgroundsTransparent();
		RaisePropertyChanged(nameof(CurrentBackdrop));
	}

	/// <summary>
	/// Walks the visible visual tree hosted by SamplesApp and makes opaque container
	/// backgrounds transparent so the system backdrop material can show through.
	/// </summary>
	private void MakeVisualTreeBackgroundsTransparent()
	{
		RestoreSavedBackgrounds();

		var transparentBrush = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
		if (App.MainWindow.Content is not DependencyObject root)
		{
			return;
		}

		var pending = new Stack<DependencyObject>();
		var visited = new HashSet<DependencyObject>();
		pending.Push(root);

		while (pending.Count > 0)
		{
			var current = pending.Pop();
			if (!visited.Add(current))
			{
				continue;
			}

			if (TryGetOpaqueBackground(current, out var originalBrush))
			{
				_savedBackgrounds.Add((current, originalBrush));
				SetBackground(current, transparentBrush);
			}

			for (var i = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(current) - 1; i >= 0; i--)
			{
				pending.Push(Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(current, i));
			}
		}
	}

	private static bool TryGetOpaqueBackground(DependencyObject element, out Brush originalBrush)
	{
		switch (element)
		{
			case Panel panel when panel.Background is SolidColorBrush panelBrush && panelBrush.Color.A > 0:
				originalBrush = panel.Background;
				return true;
			case Border border when border.Background is SolidColorBrush borderBrush && borderBrush.Color.A > 0:
				originalBrush = border.Background;
				return true;
			default:
				originalBrush = null;
				return false;
		}
	}

	private static void SetBackground(DependencyObject element, Brush background)
	{
		switch (element)
		{
			case Panel panel:
				panel.Background = background;
				break;
			case Border border:
				border.Background = background;
				break;
		}
	}

	private void RestoreSavedBackgrounds()
	{
		foreach (var (element, original) in _savedBackgrounds)
		{
			SetBackground(element, original);
		}

		_savedBackgrounds.Clear();
	}
}
