using SampleControl.Presentation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SamplesApp
{
	public sealed partial class MainPage : Page
	{
		public MainPage()
		{
			this.InitializeComponent();

			ViewModel = new SampleChooserViewModel(sampleControl);
			sampleControl.DataContext = ViewModel;
		}

		internal SampleChooserViewModel ViewModel { get; }

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
