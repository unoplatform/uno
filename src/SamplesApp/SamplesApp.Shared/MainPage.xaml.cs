using SampleControl.Presentation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SamplesApp
{
	public sealed partial class MainPage : Page
	{
		public MainPage()
		{
			this.InitializeComponent();

			sampleControl.DataContext = new SampleChooserViewModel(sampleControl);
		}

#if UNO_HAS_ENHANCED_LIFECYCLE || !HAS_UNO
		protected
#if HAS_UNO
			internal
#endif
			override void OnNavigatedTo(NavigationEventArgs e)
		{
			if (this.IsLoaded)
			{
				// https://github.com/unoplatform/uno/issues/1478
				throw new System.Exception("OnNavigatedTo should happen before Loaded.");
			}
		}
#endif
	}
}
