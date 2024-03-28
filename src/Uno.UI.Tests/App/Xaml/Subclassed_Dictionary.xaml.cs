using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Tests.App.Views;
using Uno.UI.Tests.ViewLibrary;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.Sockets;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Uno.UI.Tests.App.Xaml
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class Subclassed_Dictionary : ResourceDictionary
	{

		public Subclassed_Dictionary()
		{
			this.InitializeComponent();
		}
	}

	public class MyConverter : IValueConverter
	{
		public List<MyConverterItem> Values { get; } = new List<MyConverterItem>();
		public object Value { get; set; }

		public object Convert(object value, Type targetType, object parameter, string language) => Values[0].Value;
		public object ConvertBack(object value, Type targetType, object parameter, string language) => Values[0].Value;
	}

	public class MyConverterItem
	{
		public object Value { get; set; }
	}
}
