#nullable disable
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.UI.Windowing;
using SamplesApp;
using Uno.UI.Samples.UITests.Helpers;
using Windows.Graphics;

namespace UITests.Microsoft_UI_Windowing
{
	internal class TitleBarAndBorderViewModel : ViewModelBase
	{
		private OverlappedPresenter _presenter = OverlappedPresenter.Create();

		private bool _windowExtends;
		private bool _appWindowExtends;
		private bool _hasBorder;
		private bool _hasTitleBar;
		private string _presenterDescription = string.Empty;
		private string _lastError = string.Empty;
		private double _dragRectX;
		private double _dragRectY;
		private double _dragRectWidth;
		private double _dragRectHeight;

		public TitleBarAndBorderViewModel()
		{
			// Initialize from current window/presenter state
			UpdateState();
			_presenterDescription = $"Presenter: {_presenter.GetType().Name}";
		}

		public bool WindowExtends
		{
			get => _windowExtends;
			set
			{
				App.MainWindow.ExtendsContentIntoTitleBar = value;
				UpdateState();
			}
		}

		public bool AppWindowExtends
		{
			get => _appWindowExtends;
			set
			{
				App.MainWindow.AppWindow.TitleBar.ExtendsContentIntoTitleBar = value;
				UpdateState();
			}
		}

		private void UpdateState()
		{
			_windowExtends = App.MainWindow.ExtendsContentIntoTitleBar;
			_appWindowExtends = App.MainWindow.AppWindow.TitleBar.ExtendsContentIntoTitleBar;
			_hasBorder = _presenter.HasBorder;
			_hasTitleBar = _presenter.HasTitleBar;

			RaisePropertyChanged(nameof(WindowExtends));
			RaisePropertyChanged(nameof(AppWindowExtends));
			RaisePropertyChanged(nameof(HasBorder));
			RaisePropertyChanged(nameof(HasTitleBar));
		}

		public bool HasBorder
		{
			get => _hasBorder;
			set => Set(ref _hasBorder, value);
		}

		public bool HasTitleBar
		{
			get => _hasTitleBar;
			set => Set(ref _hasTitleBar, value);
		}

		public string PresenterDescription
		{
			get => _presenterDescription;
			private set => Set(ref _presenterDescription, value);
		}

		public string LastError
		{
			get => _lastError;
			private set => Set(ref _lastError, value);
		}

		public double DragRectX
		{
			get => _dragRectX;
			set => Set(ref _dragRectX, value);
		}

		public double DragRectY
		{
			get => _dragRectY;
			set => Set(ref _dragRectY, value);
		}

		public double DragRectWidth
		{
			get => _dragRectWidth;
			set => Set(ref _dragRectWidth, value);
		}

		public double DragRectHeight
		{
			get => _dragRectHeight;
			set => Set(ref _dragRectHeight, value);
		}

		public ICommand SetDragRectanglesCommand => GetOrCreateCommand(() =>
		{
			try
			{
				var rect = new Windows.Graphics.RectInt32(
					(int)DragRectX,
					(int)DragRectY,
					(int)DragRectWidth,
					(int)DragRectHeight);
				App.MainWindow.AppWindow.TitleBar.SetDragRectangles(new[] { rect });
				LastError = string.Empty;
			}
			catch (Exception ex)
			{
				LastError = ex.Message;
			}
		});

		public ICommand ClearDragRectanglesCommand => GetOrCreateCommand(() =>
		{
			try
			{
				App.MainWindow.AppWindow.TitleBar.SetDragRectangles(Array.Empty<RectInt32>());
				LastError = string.Empty;
			}
			catch (Exception ex)
			{
				LastError = ex.Message;
			}
		});

		public ICommand ApplyCommand => GetOrCreateCommand(ApplySetBorderAndTitleBar);

		public ICommand ResetCommand => GetOrCreateCommand(ResetPresenter);

		private void ApplySetBorderAndTitleBar()
		{
			try
			{
				_presenter.SetBorderAndTitleBar(HasBorder, HasTitleBar);
				App.MainWindow.AppWindow.SetPresenter(_presenter);
				PresenterDescription = $"Presenter: {_presenter.GetType().Name} (HasBorder={_presenter.HasBorder}, HasTitleBar={_presenter.HasTitleBar})";
				LastError = string.Empty;

				UpdateState();
			}
			catch (Exception ex)
			{
				LastError = ex.Message;
			}
		}

		private void ResetPresenter()
		{
			_presenter = OverlappedPresenter.Create();
			App.MainWindow.AppWindow.SetPresenter(_presenter);
			HasBorder = _presenter.HasBorder;
			HasTitleBar = _presenter.HasTitleBar;
			PresenterDescription = $"Presenter: {_presenter.GetType().Name}";
			LastError = string.Empty;

			UpdateState();
		}
	}
}
