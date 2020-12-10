using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions;
using Uno.UI.Tests.Windows_UI_XAML_Controls.PopupTests.Controls;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace Uno.UI.Tests.PivotTests
{
	[TestClass]
	public class Given_Popup
	{
		[TestMethod]
		public void When_Popup()
		{
			var SUT = new When_Popup();

			Assert.IsTrue(SUT.FindName("myPopup")?.GetType() == typeof(Windows.UI.Xaml.Controls.Primitives.Popup));
		}

		[TestMethod]
		public void When_Popup_Forwarding_Legacy_To_Official()
		{
			var SUT = new Windows.UI.Xaml.Controls.Popup();
			Windows.UI.Xaml.Controls.Primitives.Popup primitiveSUT = SUT;

			void ValidateEquality()
			{
				Assert.AreEqual(SUT.PopupPanel, primitiveSUT.PopupPanel);
				Assert.AreEqual(SUT.Child, primitiveSUT.Child);
				Assert.AreEqual(SUT.VerticalOffset, primitiveSUT.VerticalOffset);
				Assert.AreEqual(SUT.HorizontalOffset, primitiveSUT.HorizontalOffset);
				Assert.AreEqual(SUT.LightDismissOverlayMode, primitiveSUT.LightDismissOverlayMode);
				Assert.AreEqual(SUT.IsOpen, primitiveSUT.IsOpen);
				Assert.AreEqual(SUT.IsLightDismissEnabled, primitiveSUT.IsLightDismissEnabled);
			}

			SUT.PopupPanel = new PopupPanel(SUT);
			ValidateEquality();
			SUT.Child = new TextBlock();
			ValidateEquality();
			SUT.VerticalOffset = 42.42;
			ValidateEquality();
			SUT.HorizontalOffset = 42.42;
			ValidateEquality();
			SUT.LightDismissOverlayMode = LightDismissOverlayMode.Off;
			ValidateEquality();
			SUT.IsOpen = !SUT.IsOpen;
			ValidateEquality();
			SUT.IsLightDismissEnabled = !SUT.IsLightDismissEnabled;
			ValidateEquality();
		}

		[TestMethod]
		public void When_Popup_Forwarding_Official_To_Legacy()
		{
			var SUT = new Windows.UI.Xaml.Controls.Popup();
			Windows.UI.Xaml.Controls.Primitives.Popup primitiveSUT = SUT;

			void ValidateEquality()
			{
				Assert.AreEqual(SUT.PopupPanel, primitiveSUT.PopupPanel);
				Assert.AreEqual(SUT.Child, primitiveSUT.Child);
				Assert.AreEqual(SUT.VerticalOffset, primitiveSUT.VerticalOffset);
				Assert.AreEqual(SUT.HorizontalOffset, primitiveSUT.HorizontalOffset);
				Assert.AreEqual(SUT.LightDismissOverlayMode, primitiveSUT.LightDismissOverlayMode);
				Assert.AreEqual(SUT.IsOpen, primitiveSUT.IsOpen);
				Assert.AreEqual(SUT.IsLightDismissEnabled, primitiveSUT.IsLightDismissEnabled);
			}

			primitiveSUT.PopupPanel = new PopupPanel(SUT);
			ValidateEquality();
			primitiveSUT.Child = new TextBlock();
			ValidateEquality();
			primitiveSUT.VerticalOffset = 42.42;
			ValidateEquality();
			primitiveSUT.HorizontalOffset = 42.42;
			ValidateEquality();
			primitiveSUT.LightDismissOverlayMode = LightDismissOverlayMode.Off;
			ValidateEquality();
			primitiveSUT.IsOpen = !SUT.IsOpen;
			ValidateEquality();
			primitiveSUT.IsLightDismissEnabled = !SUT.IsLightDismissEnabled;
			ValidateEquality();
		}
	}
}
