using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using SampleControl.Presentation;
using Windows.Foundation;
using Windows.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Xaml.Input;
#if WINAPPSDK
using Windows.Foundation.Collections;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
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
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			Assert.IsNotNull(XamlRoot, "XamlRoot was not initialized before measure");
#if HAS_UNO
			Assert.IsTrue(XamlRoot.VisualTree.ContentRoot.CompositionContent.RasterizationScaleInitialized, "Rasterization scale was not initialized");
#endif

			if (_initialMeasure && availableSize == default)
			{
				Assert.Fail("Initial Measure should not be called with empty size");
			}

			_initialMeasure = false;
			return base.MeasureOverride(availableSize);
		}

		protected override Size ArrangeOverride(Size availableSize)
		{
			if (_initialArrange && availableSize == default)
			{
				Assert.Fail("Initial Arrange should not be called with empty size");
			}

			_initialArrange = false;
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
