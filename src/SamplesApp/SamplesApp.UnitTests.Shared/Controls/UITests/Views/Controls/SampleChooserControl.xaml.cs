using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using SampleControl.Presentation;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml.Input;
#if WINAPPSDK
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
#elif XAMARIN || UNO_REFERENCE_API
using Microsoft.UI.Xaml.Controls;
using System.Globalization;
#endif

namespace Uno.UI.Samples.Controls
{
	public sealed partial class SampleChooserControl : UserControl
	{
		//private bool _initialMeasure = true;
		//private bool _initialArrange = true;

		public SampleChooserControl()
		{
			this.InitializeComponent();
			RTLCheckBox.Checked += (_, _) => this.FlowDirection = FlowDirection.RightToLeft;
			RTLCheckBox.Unchecked += (_, _) => this.FlowDirection = FlowDirection.LeftToRight;
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			//TODO:MZ: Uncomment and enable
			//if (_initialMeasure && availableSize == default)
			//{
			//	_initialMeasure = false;
			//	Assert.Fail("Initial Measure should not be called with empty size");
			//}
			return base.MeasureOverride(availableSize);
		}

		protected override Size ArrangeOverride(Size availableSize)
		{
			//TODO:MZ: Uncomment and enable
			//if (_initialArrange && availableSize == default)
			//{
			//	_initialArrange = false;
			//	Assert.Fail("Initial Arrange should not be called with empty size");
			//}
			return base.ArrangeOverride(availableSize);
		}

		private void OnSearchEnterKey_KeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (e.Key == Windows.System.VirtualKey.Enter)
			{
				((SampleChooserViewModel)DataContext).TryOpenSample();
			}
		}
	}
}
