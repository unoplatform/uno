using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Samples.UITests.ImageTests
{
	[Sample("Image", IsManualTest = true, Description = "The sample showcases a list of images with a placeholder that is shown until the image loads. You should see the placeholder when you click the button until the new images are loaded. The old image should not stay there after the button is pressed.")]
	public sealed partial class Image_Placeholder : Page
	{
		private int _count;

		public Image_Placeholder()
		{
			this.InitializeComponent();

			lv.ItemsSource = Enumerable.Range(0, 10).Select(i => new ViewModel
			{
				Uri = $"https://picsum.photos/id/{_count++}/640/360",
				Label = $"Image {_count}"
			}).ToList();
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			((List<ViewModel>)lv.ItemsSource).ForEach(u => u.Uri = $"https://picsum.photos/id/{_count++}/640/360");
		}
	}

	public class ViewModel : INotifyPropertyChanged
	{
		private string _uri;
		private string _label;

		public event PropertyChangedEventHandler PropertyChanged;

		public string Label
		{
			get => _label;
			set
			{
				if (_label != value)
				{
					_label = value;
					OnPropertyChanged();
				}
			}
		}

		public string Uri
		{
			get => _uri;
			set
			{
				if (_uri != value)
				{
					_uri = value;
					OnPropertyChanged();
				}
			}
		}

		private void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
