using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using SampleControl.Presentation;
using Windows.Foundation;
using Windows.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
#if NETFX_CORE
using Windows.Foundation.Collections;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
#elif XAMARIN || UNO_REFERENCE_API
using Windows.UI.Xaml.Controls;
using System.Globalization;
#endif

namespace Uno.UI.Samples.Controls
{
	public sealed partial class SampleChooserControl : UserControl
	{
		private bool _initialMeasure = true;
		private bool _initialArrange = true;

		public SampleChooserControl()
		{
			this.InitializeComponent();
			RTLCheckBox.Checked += (_, _) => this.FlowDirection = FlowDirection.RightToLeft;
			RTLCheckBox.Unchecked += (_, _) => this.FlowDirection = FlowDirection.LeftToRight;
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			if (_initialMeasure && availableSize == default)
			{
				_initialMeasure = false;
				Assert.Fail("Initial Measure should not be called with empty size");
			}
			return base.MeasureOverride(availableSize);
		}

		protected override Size ArrangeOverride(Size availableSize)
		{
			if (_initialArrange && availableSize == default)
			{
				_initialArrange = false;
				Assert.Fail("Initial Arrange should not be called with empty size");
			}
			return base.ArrangeOverride(availableSize);
		}
	}
}
