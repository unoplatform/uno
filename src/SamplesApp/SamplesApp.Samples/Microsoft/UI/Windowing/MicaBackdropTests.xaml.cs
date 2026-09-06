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
		"The framework makes the visual tree transparent so the backdrop shows through. " +
		"Use the 'Extend content into title bar' toggle to verify the material also fills the title bar.")]
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
		ViewModel?.Reset();
	}
}

internal class MicaBackdropTestsViewModel : ViewModelBase
{
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

	/// <summary>
	/// Mirrors <see cref="Window.ExtendsContentIntoTitleBar"/> so the sample can verify the backdrop
	/// material also fills the title bar region when the content is extended into it.
	/// </summary>
	public bool ExtendsContentIntoTitleBar
	{
		get => App.MainWindow.ExtendsContentIntoTitleBar;
		set
		{
			App.MainWindow.ExtendsContentIntoTitleBar = value;
			RaisePropertyChanged();
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
		RaisePropertyChanged(nameof(CurrentBackdrop));
	}

	/// <summary>
	/// Restores the window to its default state when leaving the sample.
	/// </summary>
	public void Reset()
	{
		App.MainWindow.SystemBackdrop = null;
		ExtendsContentIntoTitleBar = false;
		RaisePropertyChanged(nameof(CurrentBackdrop));
	}

	private void SetBackdrop(SystemBackdrop backdrop)
	{
		// Setting Window.SystemBackdrop is enough: the framework transparentizes the visual tree so
		// the material shows through. No manual background walking is required here anymore.
		App.MainWindow.SystemBackdrop = backdrop;
		RaisePropertyChanged(nameof(CurrentBackdrop));
	}
}
