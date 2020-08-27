using Uno.UI.Samples.Controls;
using Uno.UI.Toolkit.Helpers;
using Windows.UI.Xaml.Controls;

namespace UITests.Toolkit
{
	[Sample("Toolkit", Name = "LaunchTracker")]
    public sealed partial class LaunchTrackerTests : Page
    {
        public LaunchTrackerTests()
        {
            this.InitializeComponent();
        }

		public LaunchTracker Tracker => LaunchTracker.Current;
    }
}
