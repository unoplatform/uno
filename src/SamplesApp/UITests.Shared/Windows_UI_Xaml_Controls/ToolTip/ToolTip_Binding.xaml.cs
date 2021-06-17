using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Windows.UI.Core;
using System.Windows.Input;

namespace UITests.Windows_UI_Xaml_Controls.ToolTip
{
	[Sample(ViewModelType = typeof(ToolTipBindingViewModel))]
	public sealed partial class ToolTip_Binding : Page
	{
		public ToolTip_Binding()
		{
			InitializeComponent();
			DataContextChanged += ToolTip_Binding_DataContextChanged;
		}

		public ToolTipBindingViewModel ViewModel { get; private set; }

		private void ToolTip_Binding_DataContextChanged(DependencyObject sender, DataContextChangedEventArgs args) =>
			ViewModel = args.NewValue as ToolTipBindingViewModel;
	}

	public class ToolTipBindingViewModel : ViewModelBase
	{
		private string _text = "Initial text";
		private string _toolTipContent = "Initial content";
		private Windows.UI.Xaml.Controls.ToolTip _toolTip = new Windows.UI.Xaml.Controls.ToolTip()
		{
			Content = "Initial ToolTip"
		};

		public ToolTipBindingViewModel(CoreDispatcher dispatcher) :
			base(dispatcher)
		{
		}

		public ICommand ChangeTextCommand => GetOrCreateCommand(ChangeText);

		public ICommand ChangeToolTipContentCommand => GetOrCreateCommand(ChangeToolTipContent);

		public ICommand ChangeToolTipCommand => GetOrCreateCommand(ChangeToolTip);

		public string Text
		{
			get => _text;
			set
			{
				_text = value;
				RaisePropertyChanged();
			}
		}

		public string ToolTipContent
		{
			get => _toolTipContent;
			set
			{
				_toolTipContent = value;
				RaisePropertyChanged();
			}
		}

		public Windows.UI.Xaml.Controls.ToolTip ToolTip
		{
			get => _toolTip;
			set
			{
				_toolTip = value;
				RaisePropertyChanged();
			}
		}

		private void ChangeText() => Text = "Changed text";

		private void ChangeToolTipContent() => ToolTipContent = "Chnaged content";

		private void ChangeToolTip() =>
			ToolTip = new Windows.UI.Xaml.Controls.ToolTip()
			{
				Content = "Changed ToolTip"
			};
	}
}
