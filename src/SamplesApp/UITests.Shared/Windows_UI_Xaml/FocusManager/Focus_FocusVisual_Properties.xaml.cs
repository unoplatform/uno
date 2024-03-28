using System;
using System.Linq;
using Uno.Disposables;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UITests.Windows_UI_Xaml.FocusTests
{
	[Sample("Focus", ViewModelType = typeof(FocusVisualPropertiesViewModel))]
	public sealed partial class Focus_FocusVisual_Properties : Page
	{
		private readonly Button[] _buttons;
		private Button _currentFocusButton;

		public Focus_FocusVisual_Properties()
		{
			InitializeComponent();
			DataContextChanged += OnDataContextChanged;
			_buttons = FocusPanel.Children.OfType<Button>().ToArray();
		}

		internal FocusVisualPropertiesViewModel ViewModel { get; private set; }

		private void OnDataContextChanged(DependencyObject sender, DataContextChangedEventArgs args)
		{
			ViewModel = args.NewValue as FocusVisualPropertiesViewModel;
			if (ViewModel != null)
			{
				ViewModel.OnCycle = OnCycle;
			}
		}

		private void OnCycle()
		{
			var currentIndex = Array.IndexOf(_buttons, _currentFocusButton);
			currentIndex++;
			currentIndex %= _buttons.Length;
			_currentFocusButton = _buttons[currentIndex];
			_currentFocusButton.Focus(FocusState.Keyboard);
		}
	}

	internal class FocusVisualPropertiesViewModel : ViewModelBase
	{
		private DispatcherTimer _timer = new DispatcherTimer()
		{
			Interval = TimeSpan.FromSeconds(1)
		};

		private bool _isCycling = false;

		public FocusVisualPropertiesViewModel(Private.Infrastructure.UnitTestDispatcherCompat dispatcher) : base(dispatcher)
		{
			_timer.Tick += CycleFocus;

			Disposables.Add(Disposable.Create(() =>
			{
				OnCycle = null;
				_timer.Stop();
			}));
		}

		public Action OnCycle { get; set; }

		private void CycleFocus(object sender, object e) => OnCycle?.Invoke();

		public bool IsCycling
		{
			get => _isCycling;
			set
			{
				Set(ref _isCycling, value);
				UpdateCycling();
			}
		}

		private void UpdateCycling()
		{
			if (IsCycling)
			{
				_timer.Start();
			}
			else
			{
				_timer.Stop();
			}
		}
	}
}
