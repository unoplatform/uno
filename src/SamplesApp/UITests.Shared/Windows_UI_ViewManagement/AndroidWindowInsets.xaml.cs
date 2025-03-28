using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Uno.Extensions;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Private.Infrastructure;

namespace UITests.Windows_UI_ViewManagement
{
	[Sample("Windows.UI.ViewManagement", IsManualTest = true)]
	public sealed partial class AndroidWindowInsets : Page
	{
		public AndroidWindowInsets()
		{
			this.InitializeComponent();
		}
	}
}
