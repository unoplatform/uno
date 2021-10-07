using Uno.UI.Samples.Controls;
using Windows.System.Profile;
using Windows.UI.Xaml.Controls;

namespace UITests.Shared.Windows_System.Profile
{
	[SampleControlInfo("Windows.System", "Profile.AnalyticsInfo",
		   description: "Shows properties of AnalyticsInfo")]
	public sealed partial class AnalyticsInfoTests : UserControl
    {
        public AnalyticsInfoTests()
        {
            this.InitializeComponent();
            DeviceFormTextBlock.Text = AnalyticsInfo.DeviceForm;
            DeviceFamilyTextBlock.Text = AnalyticsInfo.VersionInfo.DeviceFamily;
            DeviceFamilyVersionTextBlock.Text = AnalyticsInfo.VersionInfo.DeviceFamilyVersion;

			try
			{
				ulong v = ulong.Parse(AnalyticsInfo.VersionInfo.DeviceFamilyVersion);
				ulong v1 = (v & 0xFFFF000000000000L) >> 48;
				ulong v2 = (v & 0x0000FFFF00000000L) >> 32;
				ulong v3 = (v & 0x00000000FFFF0000L) >> 16;
				ulong v4 = (v & 0x000000000000FFFFL);
				var decodedVersion = $"{v1}.{v2}.{v3}.{v4}";
				DecodedDeviceFamilyVersionTextBlock.Text = decodedVersion;
			}
			catch
			{
				DecodedDeviceFamilyVersionTextBlock.Text = "Invalid version string";
			}
		}
    }
}
