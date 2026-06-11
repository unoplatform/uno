using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Tests.Windows_UI_Xaml.CreateFromStringTests;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace Uno.UI.Tests.App.Xaml
{
	public sealed partial class Test_CreateFromString : UserControl
	{
		public MyLocationPointControl TestLocationPoint => TestLocationControl;
		public MyLocationPointControl TestLocationPoint2 => TestLocationControl2;
		public MyLocationPointControl TestLocationPoint3 => TestLocationControl3;

		public Test_CreateFromString()
		{
			this.InitializeComponent();
		}
	}
}
