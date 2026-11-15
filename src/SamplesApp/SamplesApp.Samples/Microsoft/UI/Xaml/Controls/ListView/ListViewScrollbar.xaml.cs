using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;
using SamplesApp.Windows_UI_Xaml_Controls.Models;
using Microsoft.UI.Xaml;

namespace SamplesApp.Windows_UI_Xaml_Controls.ListView
{
	[Sample("ListView", Name = "ListViewScrollBar", ViewModelType = typeof(ListViewViewModel), IsManualTest = true)]
	public sealed partial class ListViewScrollBar : UserControl
	{
		DataTemplate _headerTemplate;
		DataTemplate _footerTemplate;

		public ListViewScrollBar()
		{
			this.InitializeComponent();
		}

		private bool _headerChecked = true;

		public bool HeaderChecked
		{
			get => _headerChecked;
			set
			{
				_headerChecked = value;

				if (value)
				{
					list01.HeaderTemplate = _headerTemplate;
				}
				else
				{
					_headerTemplate = list01.HeaderTemplate;
					list01.HeaderTemplate = null;
				}
			}
		}

		private bool _footerChecked = true;

		public bool FooterChecked
		{
			get => _footerChecked;
			set
			{
				_footerChecked = value;

				if (value)
				{
					list01.FooterTemplate = _footerTemplate;
				}
				else
				{
					_footerTemplate = list01.FooterTemplate;
					list01.FooterTemplate = null;
				}
			}
		}
	}
}
