using Uno.UI.Samples.Controls;
using Windows.System.Profile;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Shared.Windows_System.Profile
{
	[Sample("Windows.System", "Profile.AnalyticsInfo",
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
				var v1 = (ushort)((v & 0xFFFF000000000000L) >> 48);
				var v2 = (ushort)((v & 0x0000FFFF00000000L) >> 32);
				var v3 = (ushort)((v & 0x00000000FFFF0000L) >> 16);
				var v4 = (ushort)((v & 0x000000000000FFFFL));
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
