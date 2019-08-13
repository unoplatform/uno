using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Xaml_Controls.FlipViewTests
{
	[SampleControlInfo("FlipView", nameof(FlipView_Inlines), typeof(FlipView_InlinesViewModel))]
	public sealed partial class FlipView_Inlines : UserControl
	{
		public FlipView_Inlines()
		{
			this.InitializeComponent();
		}
	}

	[Bindable]
	public class FlipView_InlinesViewModel : ViewModelBase
	{
		private string _page1;
		private string _page2;
		private string _page3;

		public FlipView_InlinesViewModel(CoreDispatcher dispatcher) : base(dispatcher)
		{
			_page1 = "Page 1";
			_page2 = "Page 2";
			_page3 = "Page 3";
		}

		public string Page1
		{
			get => _page1;
			set
			{
				_page1 = value;
				RaisePropertyChanged();
			}
		}

		public string Page2
		{
			get => _page2;
			set
			{
				_page2 = value;
				RaisePropertyChanged();
			}
		}

		public string Page3
		{
			get => _page3;
			set
			{
				_page3 = value;
				RaisePropertyChanged();
			}
		}
	}
}
