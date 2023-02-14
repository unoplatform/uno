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
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

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
