using System;
using System.Linq;
using System.Windows.Input;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UITests.Windows_UI_Xaml.FocusTests
{
	[Sample("Focus", ViewModelType = typeof(Focus_FocusVisualViewModel))]
	internal sealed partial class Focus_FocusVisual : Page
	{
		public Focus_FocusVisual()
		{
			InitializeComponent();
			DataContextChanged += Focus_FocusVisual_DataContextChanged;
		}

		public Focus_FocusVisualViewModel ViewModel { get; private set; }

		private void Focus_FocusVisual_DataContextChanged(DependencyObject sender, Windows.UI.Xaml.DataContextChangedEventArgs args)
		{
			ViewModel = args.NewValue as Focus_FocusVisualViewModel;
		}
	}

	internal class Focus_FocusVisualViewModel : ViewModelBase
	{
		public Focus_FocusVisualViewModel(Private.Infrastructure.UnitTestDispatcherCompat dispatcher) : base(dispatcher)
		{
		}

		public FocusState[] FocusStates { get; } = Enum.GetValues<FocusState>();

		public FocusState SelectedFocusState { get; set; } = FocusState.Unfocused;

		public ICommand FocusCommand => GetOrCreateCommand<Control>(Focus);

		public void Focus(Control element)
		{
			element.Focus(SelectedFocusState);
		}
	}
}
