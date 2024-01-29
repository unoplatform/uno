using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Private.Infrastructure;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Xaml_Media.ImageBrushTests
{
	[SampleControlInfo(category: "Brushes")]
	public sealed partial class ImageBrush_SameWithDelay : UserControl
	{
		BrushContext _ctx = new BrushContext();

		public ImageBrush_SameWithDelay()
		{
			this.InitializeComponent();

			DataContext = _ctx;

			var _ = UnitTestDispatcherCompat.From(this).RunAsync(UnitTestDispatcherCompat.Priority.Normal, () => _ctx.ImgSource = "https://lh5.ggpht.com/lxBMauupBiLIpgOgu5apeiX_YStXeHRLK1oneS4NfwwNt7fGDKMP0KpQIMwfjfL9GdHRVEavmg7gOrj5RYC4qwrjh3Y0jCWFDj83jzg");

			imageBrush.ImageOpened += (s, e) =>
			{
				_ctx.ImgSource2 = "https://lh5.ggpht.com/lxBMauupBiLIpgOgu5apeiX_YStXeHRLK1oneS4NfwwNt7fGDKMP0KpQIMwfjfL9GdHRVEavmg7gOrj5RYC4qwrjh3Y0jCWFDj83jzg";
			};
		}
	}

	public class BrushContext : System.ComponentModel.INotifyPropertyChanged
	{
		private string _imageSource;
		private string _imageSource2;

		public string ImgSource
		{
			get { return _imageSource; }
			set
			{
				_imageSource = value;
				PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(ImgSource)));
			}
		}

		public string ImgSource2
		{
			get { return _imageSource2; }
			set
			{
				_imageSource2 = value;
				PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(ImgSource2)));
			}
		}

		public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
	}
}
