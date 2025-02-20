using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Microsoft.UI.Windowing;
using SamplesApp;
using SampleControl.Presentation;


namespace UITests.Microsoft_UI_Windowing;

[Sample("Windowing", Name = "OverlappedPresenter", IsManualTest = true, ViewModelType = typeof(OverlappedPresenterTestsViewModel),
	Description = "Playground for testing of OverlappedPresenter functionality.")]
public sealed partial class OverlappedPresenterTests : Page
{
	public OverlappedPresenterTests()
	{
		this.InitializeComponent();
		DataContextChanged += OverlappedPresenterTests_DataContextChanged;
	}

	internal OverlappedPresenterTestsViewModel ViewModel { get; private set; }

	private void OverlappedPresenterTests_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
	{
		ViewModel = args.NewValue as OverlappedPresenterTestsViewModel;
	}
}

internal class OverlappedPresenterTestsViewModel : ViewModelBase
{
	private OverlappedPresenter _currentPresenter;
	private bool _hasBorder;
	private bool _hasTitleBar;

	public OverlappedPresenterTestsViewModel()
	{
		SetDefault();
	}

	public void SetDefault() => SetPresenter(OverlappedPresenter.Create());

	public void SetDialog() => SetPresenter(OverlappedPresenter.CreateForDialog());

	public void SetContextMenu() => SetPresenter(OverlappedPresenter.CreateForContextMenu());

	public void SetToolWindow() => SetPresenter(OverlappedPresenter.CreateForToolWindow());

	private void SetPresenter(OverlappedPresenter presenter)
	{
		_currentPresenter = presenter;
		App.MainWindow.AppWindow.SetPresenter(_currentPresenter);
		_hasBorder = _currentPresenter.HasBorder;
		_hasTitleBar = _currentPresenter.HasTitleBar;
		RaisePropertyChanged("");
	}

	public bool ShouldActivateWindow { get; set; } = false;

	public OverlappedPresenterState State => _currentPresenter.State;

	public void Maximize()
	{
		_currentPresenter.Maximize();
		RaisePropertyChanged(nameof(State));
	}

	public void Minimize()
	{
		_currentPresenter.Minimize(ShouldActivateWindow);
		RaisePropertyChanged(nameof(State));
	}

	public void Restore()
	{
		_currentPresenter.Restore(ShouldActivateWindow);
		RaisePropertyChanged(nameof(State));
	}

	public void RestoreAfter2Seconds()
	{
		Dispatcher.RunAsync(async () =>
		{
			await Task.Delay(2000);
			_currentPresenter.Restore(ShouldActivateWindow);
			RaisePropertyChanged(nameof(State));
		});
	}

	public bool HasBorder
	{
		get => _hasBorder;
		set
		{
			_hasBorder = value;
			RaisePropertyChanged();
		}
	}

	public async void SetBorderAndTitleBar()
	{
		try
		{
			_currentPresenter.SetBorderAndTitleBar(_hasBorder, _hasTitleBar);
			RaisePropertyChanged("");
		}
		catch (Exception ex)
		{
			_hasBorder = _currentPresenter.HasBorder;
			_hasTitleBar = _currentPresenter.HasTitleBar;

			var dialog = new ContentDialog
			{
				Title = "Error",
				Content = ex.Message,
				CloseButtonText = "OK",
				XamlRoot = SampleChooserViewModel.Instance.Owner.XamlRoot
			};
			await dialog.ShowAsync();
		}
	}

	public bool HasTitleBar
	{
		get => _hasTitleBar;
		set
		{
			_hasTitleBar = value;
			RaisePropertyChanged();
		}
	}

	public bool IsAlwaysOnTop
	{
		get => _currentPresenter.IsAlwaysOnTop;
		set => _currentPresenter.IsAlwaysOnTop = value;
	}

	public bool IsMaximizable
	{
		get => _currentPresenter.IsMaximizable;
		set => _currentPresenter.IsMaximizable = value;
	}

	public bool IsMinimizable
	{
		get => _currentPresenter.IsMinimizable;
		set => _currentPresenter.IsMinimizable = value;
	}

	public bool IsResizable
	{
		get => _currentPresenter.IsResizable;
		set => _currentPresenter.IsResizable = value;
	}

	public bool IsModal
	{
		get => _currentPresenter.IsModal;
		set
		{
			try
			{
				_currentPresenter.IsModal = value;
			}
			catch (Exception ex)
			{
				var dialog = new ContentDialog
				{
					Title = "Error",
					Content = ex.Message,
					CloseButtonText = "OK",
					XamlRoot = SampleChooserViewModel.Instance.Owner.XamlRoot
				};
				_ = dialog.ShowAsync();
			}
			finally
			{
				RaisePropertyChanged();
			}
		}
	}
}
