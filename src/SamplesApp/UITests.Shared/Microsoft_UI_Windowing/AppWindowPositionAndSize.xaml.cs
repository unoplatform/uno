using System;
using System.Threading.Tasks;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SamplesApp;
using Uno.Disposables;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Windows.Graphics;

namespace UITests.Microsoft_UI_Windowing;

[Sample(
	"Windowing",
	Name = "AppWindowPositionAndSize",
	IsManualTest = true,
	ViewModelType = typeof(AppWindowPositionAndSizeViewModel),
	Description = "Playground for window position and size changes.")]
public sealed partial class AppWindowPositionAndSize : Page
{
	public AppWindowPositionAndSize()
	{
		this.InitializeComponent();
		DataContextChanged += AppWindowPositionAndSize_DataContextChanged;
	}

	internal AppWindowPositionAndSizeViewModel ViewModel { get; private set; }

	private void AppWindowPositionAndSize_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
	{
		ViewModel = args.NewValue as AppWindowPositionAndSizeViewModel;
		if (ViewModel is not null)
		{
			ViewModel.XamlRoot = XamlRoot;
		}
	}
}

internal class AppWindowPositionAndSizeViewModel : ViewModelBase
{
	private AppWindow _appWindow;
	private PointInt32 _position;
	private SizeInt32 _size;

	public AppWindowPositionAndSizeViewModel()
	{
		_appWindow = App.MainWindow.AppWindow;

		_appWindow.Changed += OnAppWindowChanged;
		Disposables.Add(Disposable.Create(() => _appWindow.Changed -= OnAppWindowChanged));

		UpdateProperties();
	}

	private void OnAppWindowChanged(AppWindow sender, AppWindowChangedEventArgs args) => UpdateProperties();

	private void UpdateProperties()
	{
		_position = _appWindow.Position;
		_size = _appWindow.Size;
		RaisePropertyChanged(nameof(Position));
		RaisePropertyChanged(nameof(Size));
		RaisePropertyChanged(nameof(X));
		RaisePropertyChanged(nameof(Y));
		RaisePropertyChanged(nameof(Width));
		RaisePropertyChanged(nameof(Height));
	}

	internal XamlRoot XamlRoot { get; set; }

	internal int X
	{
		get => _position.X;
		set
		{
			if (_position.X != value)
			{
				_position.X = value;
				RaisePropertyChanged();
			}
		}
	}

	internal int Y
	{
		get => _position.Y;
		set
		{
			if (_position.Y != value)
			{
				_position.Y = value;
				RaisePropertyChanged();
			}
		}
	}

	internal PointInt32 Position => _position;

	internal int Width
	{
		get => _size.Width;
		set
		{
			if (_size.Width != value)
			{
				_size.Width = value;
				RaisePropertyChanged();
			}
		}
	}

	internal int Height
	{
		get => _size.Height;
		set
		{
			if (_size.Height != value)
			{
				_size.Height = value;
				RaisePropertyChanged();
			}
		}
	}

	internal SizeInt32 Size => _size;

	internal async void Move()
	{
		try
		{
			_appWindow.Move(Position);
		}
		catch (Exception ex)
		{
			await ShowMessage(ex.Message);
		}
	}

	internal async void Resize()
	{
		try
		{
			_appWindow.Resize(Size);
		}
		catch (Exception ex)
		{
			await ShowMessage(ex.Message);
		}
	}

	private async Task ShowMessage(string message)
	{
		var contentDialog = new ContentDialog();
		contentDialog.Content = message;
		contentDialog.XamlRoot = XamlRoot;
		contentDialog.PrimaryButtonText = "OK";
		await contentDialog.ShowAsync();
	}
}
