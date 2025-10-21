#nullable disable
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.UI.Windowing;
using SamplesApp;
using Uno.UI.Samples.UITests.Helpers;

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
