using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using SampleControl.Presentation;
using Windows.Foundation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Uno.UI.Extensions;
using Uno.UI.Xaml.Core;

#if WINAPPSDK
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
#elif XAMARIN || UNO_REFERENCE_API
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
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
			this.Loaded += (s, e) =>
			{
				var contentRoot = this.FindFirstDescendant<Grid>(x => x.Name == "ContentRoot");
				EventManager.DebugGate = fe => fe == contentRoot;
				contentRoot.SizeChanged += OnSplitViewMainContentSizeChanged;
			};
		}

		private void OnSplitViewMainContentSizeChanged(object sender, SizeChangedEventArgs args)
		{

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

		private void DebugVT(object sender, RoutedEventArgs e)
		{
			var sampleRoot =
				(SampleContentControl as ContentControl).Content as FrameworkElement ??
				((SampleContentControl as ContentControl).TemplatedRoot as ContentPresenter).Content as FrameworkElement ??
				SampleContentControl;
			var sut = sampleRoot?.FindFirstDescendant<FrameworkElement>(x => x.Name == "SUT") ?? sampleRoot;

			var tree = sut?.TreeGraph();
			var thisTree = this.TreeGraph();
			var rootTree = XamlRoot.Content?.TreeGraph();
		}
	}
}
