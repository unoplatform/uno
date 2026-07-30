using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace Uno.UI.Tests.Windows_UI_Xaml_Markup.XUidTests.Controls
{
	public sealed partial class When_Empty_XUid : UserControl
	{
		public When_Empty_XUid()
		{
			this.InitializeComponent();
		}
	}

	public class TestEmptyConverter : IValueConverter
	{
		public TestEmptyConverter()
		{
			ValueIfNull = null;
			ValueIfNotNull = null;
		}

		public object ValueIfNull { get; set; }

		public object ValueIfNotNull { get; set; }

		public object Convert(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
		public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
	}
}
