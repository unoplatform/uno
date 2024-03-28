using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Uno.UI.RuntimeTests
{
	public sealed partial class When_xLoad_Visibility_While_Materializing : UserControl
	{
		public When_xLoad_Visibility_While_Materializing()
		{
			Model = new Model();

			Model.PropertyChanged += delegate
			{
				FindName("item1");
			};

			this.InitializeComponent();
		}

		public Model Model { get; }
	}

	public class Model : System.ComponentModel.INotifyPropertyChanged
	{
		public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

		private bool _isVisible;

		public bool IsVisible
		{
			get { return _isVisible; }
			set
			{
				_isVisible = value;

				PropertyChanged.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(IsVisible)));
			}
		}

	}

	public partial class When_xLoad_Visibility_While_Materializing_Content : UserControl
	{
		public When_xLoad_Visibility_While_Materializing_Content()
		{
			Instances++;
		}

		public static int Instances { get; set; }
	}
}
