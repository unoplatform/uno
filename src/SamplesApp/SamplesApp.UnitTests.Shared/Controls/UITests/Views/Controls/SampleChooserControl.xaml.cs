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
		private bool _initialMeasure = true;
		private bool _initialArrange = true;
#if HAS_UNO
		private Rect? _initialBounds;
#endif

		public SampleChooserControl()
		{
			this.InitializeComponent();

#if HAS_UNO
			Loaded += OnLoaded;
#endif
		}

#if HAS_UNO
		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			_initialBounds = XamlRoot.Bounds;
			XamlRoot.Changed += OnXamlRootChanged;
		}

		private void OnXamlRootChanged(XamlRoot sender, XamlRootChangedEventArgs args)
		{
			// TODO MZ: Adjust
			//if (XamlRoot.Bounds != _initialBounds)
			//{
			//	XamlRoot.Changed -= OnXamlRootChanged;
			//}
			//else
			//{
			//	Assert.Fail("The XamlRoot's bounds should not have changed after Loaded.");
			//}
		}
#endif

		protected override Size MeasureOverride(Size availableSize)
		{
			Assert.IsTrue(IsLoaded, "The control should be loaded before the first measure.");
			Assert.IsNotNull(XamlRoot, "Child should not be null.");
#if HAS_UNO
			Assert.IsNotNull(XamlRoot?.VisualTree.ContentRoot.CompositionContent, "ContentIsland of the ContentRoot should have been set by now.");
#endif

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

		private void OnSearchEnterKey_KeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (e.Key == Windows.System.VirtualKey.Enter)
			{
				((SampleChooserViewModel)DataContext).TryOpenSample();
			}
		}
	}
}
