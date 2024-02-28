using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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


namespace UITests.Microsoft_UI_Windowing;

[Sample("Microsoft.UI.Windowing", "OverlappedPresenter", IsManualTest = true, ViewModelType = typeof(OverlappedPresenterTestsViewModel), Description = "Playground for testing of OverlappedPresenter functionality.")]
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

	public OverlappedPresenterTestsViewModel()
	{
		SetDefault();
	}

	public void SetDefault()
	{
		App.MainWindow.AppWindow.SetPresenter(_currentPresenter = OverlappedPresenter.Create());
		RaisePropertyChanged("");
	}

	public void SetDialog()
	{
		App.MainWindow.AppWindow.SetPresenter(_currentPresenter = OverlappedPresenter.CreateForDialog());
		RaisePropertyChanged("");
	}

	public void SetContextMenu()
	{
		App.MainWindow.AppWindow.SetPresenter(_currentPresenter = OverlappedPresenter.CreateForContextMenu());
		RaisePropertyChanged("");
	}

	public void SetToolWindow()
	{
		App.MainWindow.AppWindow.SetPresenter(_currentPresenter = OverlappedPresenter.CreateForToolWindow());
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

	public bool HasBorder
	{
		get => _currentPresenter.HasBorder;
		set
		{
			_currentPresenter.SetBorderAndTitleBar(value, _currentPresenter.HasTitleBar);
			RaisePropertyChanged("");
		}
	}

	public bool HasTitleBar
	{
		get => _currentPresenter.HasTitleBar;
		set
		{
			_currentPresenter.SetBorderAndTitleBar(_currentPresenter.HasBorder, value);
			RaisePropertyChanged("");
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
		set => _currentPresenter.IsModal = value;
	}
}
