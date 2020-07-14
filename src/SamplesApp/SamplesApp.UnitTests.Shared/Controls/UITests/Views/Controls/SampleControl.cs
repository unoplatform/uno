#if !SILVERLIGHT
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if NETFX_CORE
using Windows.ApplicationModel.Store;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
#elif XAMARIN || NETSTANDARD2_0
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;
#else
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows;
#endif

namespace Uno.UI.Samples.Controls
{
	public partial class SampleControl : ContentControl
	{
		public SampleControl()
		{
#if !XAMARIN
			this.DefaultStyleKey = typeof(SampleControl);
#endif
		}

		public string SampleDescription
		{
			get { return (string)this.GetValue(SampleDescriptionProperty); }
			set { this.SetValue(SampleDescriptionProperty, value); }
		}

		public static DependencyProperty SampleDescriptionProperty { get ; } =
			DependencyProperty.Register("SampleDescription", typeof(string), typeof(SampleControl), new FrameworkPropertyMetadata(""));
		
		// This only exists as a proxy to ContentTemplate
		public DataTemplate SampleContent
		{
			get { return (DataTemplate)GetValue(SampleContentProperty); }
			set { SetValue(SampleContentProperty, value); }
		}

		public static DependencyProperty SampleContentProperty { get ; } =
			DependencyProperty.Register("SampleContent", typeof(DataTemplate), typeof(SampleControl), new FrameworkPropertyMetadata(null, OnSampleContentChanged));

		private static void OnSampleContentChanged(object dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			(dependencyObject as SampleControl).ContentTemplate = args.NewValue as DataTemplate;
		}

#if XAMARIN
		protected override void OnDataContextChanged()
		{
			base.OnDataContextChanged();

			// Workaround to #10396: The DataContext of ContentTemplate should be ContentControl.DataContext if ContentControl.Content is not set.
			this.SetValue(ContentProperty, DataContext, DependencyPropertyValuePrecedences.DefaultValue);
		}
#endif
	}
}
#endif