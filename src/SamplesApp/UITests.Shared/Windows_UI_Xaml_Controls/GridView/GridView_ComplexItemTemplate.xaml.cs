using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Threading;
using Uno.Extensions;
using Windows.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using System.Globalization;
using Uno.UI.Samples.Controls;

#if WINAPPSDK
using System.Runtime.InteropServices.WindowsRuntime;
#elif XAMARIN
#endif

namespace Uno.UI.Samples.Content.UITests.GridView
{
	[Sample("GridView", ViewModelType = typeof(GridView_ComplexViewModel), IgnoreInSnapshotTests = true)]
	public sealed partial class GridView_ComplexItemTemplate : UserControl
	{
		public GridView_ComplexItemTemplate()
		{
			this.InitializeComponent();
		}
	}
}
