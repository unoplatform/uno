
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Core
{
	[TestClass]
	public class Given_Color
	{
#if HAS_UNO
		[TestMethod]
		[DynamicData(nameof(GetData), DynamicDataSourceType.Method)]
		public void When_FromArgb(byte a, byte r, byte g, byte b, uint result)
			=> Assert.AreEqual(result, Color.FromArgb(a, r, g, b).AsUInt32());

		[TestMethod]
		[DynamicData(nameof(GetData), DynamicDataSourceType.Method)]
		public void When_GetHashCode(byte a, byte r, byte g, byte b, uint result)
			=> Assert.AreEqual(result, (uint)Color.FromArgb(a, r, g, b).GetHashCode());

		public static IEnumerable<object[]> GetData()
		{
			yield return new object[] { (byte)0x00, (byte)0x00, (byte)0x00, (byte)0x00, (uint)0x00000000 };
			yield return new object[] { (byte)0xFF, (byte)0x00, (byte)0x00, (byte)0x00, (uint)0xFF000000 };
			yield return new object[] { (byte)0x00, (byte)0xFF, (byte)0x00, (byte)0x00, (uint)0x00FF0000 };
			yield return new object[] { (byte)0xFF, (byte)0x00, (byte)0xFF, (byte)0x00, (uint)0xFF00FF00 };
			yield return new object[] { (byte)0xFF, (byte)0x00, (byte)0x00, (byte)0xFF, (uint)0xFF0000FF };
			yield return new object[] { (byte)0x7F, (byte)0x00, (byte)0x00, (byte)0x00, (uint)0x7F000000 };
			yield return new object[] { (byte)0xFF, (byte)0x7F, (byte)0x00, (byte)0x00, (uint)0xFF7F0000 };
			yield return new object[] { (byte)0xFF, (byte)0x00, (byte)0x7F, (byte)0x00, (uint)0xFF007F00 };
			yield return new object[] { (byte)0xFF, (byte)0x00, (byte)0x00, (byte)0x7F, (uint)0xFF00007F };
			yield return new object[] { (byte)0xFF, (byte)0xFF, (byte)0xFF, (byte)0xFF, (uint)0xFFFFFFFF };
		}
#endif

		[TestMethod]
		[DynamicData(nameof(GetCompare), DynamicDataSourceType.Method)]
		public void When_Equals(Color left, Color right, bool result)
			=> Assert.AreEqual(result, left.Equals(right));

		[TestMethod]
		[DynamicData(nameof(GetCompare), DynamicDataSourceType.Method)]
		public void When_op_Equals(Color left, Color right, bool result)
			=> Assert.AreEqual(result, left == right);

		public static IEnumerable<object[]> GetCompare()
		{
			yield return new object[] { ColorHelper.FromArgb(0xFF, 0xFF, 0xFF, 0xFF), ColorHelper.FromArgb(0xFF, 0xFF, 0xFF, 0xFF), true };
			yield return new object[] { ColorHelper.FromArgb(0xFF, 0x7F, 0xFF, 0xFF), ColorHelper.FromArgb(0xFF, 0xFF, 0xFF, 0xFF), false };
			yield return new object[] { ColorHelper.FromArgb(0xFF, 0xFF, 0x7F, 0xFF), ColorHelper.FromArgb(0xFF, 0xFF, 0xFF, 0xFF), false };
			yield return new object[] { ColorHelper.FromArgb(0xFF, 0xFF, 0xFF, 0x7F), ColorHelper.FromArgb(0xFF, 0xFF, 0xFF, 0xFF), false };
			yield return new object[] { ColorHelper.FromArgb(0x00, 0x00, 0x00, 0x00), ColorHelper.FromArgb(0xFF, 0xFF, 0xFF, 0xFF), false };
			yield return new object[] { ColorHelper.FromArgb(0x00, 0x00, 0x00, 0x00), ColorHelper.FromArgb(0x00, 0x00, 0x00, 0x00), true };
			yield return new object[] { ColorHelper.FromArgb(0x7F, 0xFF, 0xFF, 0xFF), ColorHelper.FromArgb(0x7F, 0xFF, 0xFF, 0xFF), true };
			yield return new object[] { ColorHelper.FromArgb(0xFF, 0x7F, 0xFF, 0xFF), ColorHelper.FromArgb(0xFF, 0x7F, 0xFF, 0xFF), true };
			yield return new object[] { ColorHelper.FromArgb(0xFF, 0xFF, 0x7F, 0xFF), ColorHelper.FromArgb(0xFF, 0xFF, 0x7F, 0xFF), true };
			yield return new object[] { ColorHelper.FromArgb(0xFF, 0xFF, 0xFF, 0x7F), ColorHelper.FromArgb(0xFF, 0xFF, 0xFF, 0x7F), true };
		}

#if __MACOS__
		[TestMethod]
		[RunsOnUIThread]
		public void When_User_Change_macOS_System_Colors()
		{
			var _uiSettings = new Windows.UI.ViewManagement.UISettings();

			Color SUT_1 = _uiSettings.GetColorValue(Windows.UI.ViewManagement.UIColorType.Accent);
			Color SUT_2 = _uiSettings.GetColorValue(Windows.UI.ViewManagement.UIColorType.Background);

			var accent = new Windows.UI.Xaml.Media.SolidColorBrush(AppKit.NSColor.ControlAccent).Color;
			var background = new Windows.UI.Xaml.Media.SolidColorBrush(AppKit.NSColor.ControlBackground).Color;

			Assert.AreEqual(SUT_1, accent);
			Assert.AreEqual(SUT_2, background);
		}

#endif
	}
}
