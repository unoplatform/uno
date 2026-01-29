using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;
using Query = System.Func<Uno.UITest.IAppQuery, Uno.UITest.IAppQuery>;

namespace SamplesApp.UITests.Microsoft_UI_Xaml_Controls.NumberBoxTests
{
	public partial class Given_TwoPaneView : SampleControlUITestBase
	{
		// Need to be the same as c_defaultMinWideModeWidth/c_defaultMinTallModeHeight in TwoPaneViewFactory.cpp
		private const double c_defaultMinWideModeWidth = 641.0;
		private const double c_defaultMinTallModeHeight = 641.0;

		// Need to be the same as c_simulatedPaneWidth/c_simulatedPaneHeight/c_simulatedMiddle in TwoPaneViewPage.xaml.cs
		private const double c_simulatedPaneWidth = 300.0;
		private const double c_simulatedPaneHeight = 400.0;
		private const double c_simulatedMiddle = 12.0;

		// Need to be the same as c_controlMargin in TwoPaneViewPage.xaml.cs
		private const double c_controlMargin_left = 40.0;
		private const double c_controlMargin_top = 10.0;
		private const double c_controlMargin_right = 30.0;
		private const double c_controlMargin_bottom = 20.0;

		enum ControlWidth { Default, Wide, Narrow }
		enum ControlHeight { Default, Tall, Short }
		enum WideModeConfiguration { LeftRight, RightLeft, SinglePane }
		enum TallModeConfiguration { TopBottom, BottomTop, SinglePane }
		enum PanePriority { Pane1, Pane2 }
		enum TwoPaneViewMode { SinglePane, Wide, Tall }

		enum ViewMode { Pane1Only, Pane2Only, LeftRight, RightLeft, TopBottom, BottomTop }

		[SetUp]
		public void TestSetup()
		{
			Run("UITests.Shared.Microsoft_UI_Xaml_Controls.TwoPaneViewTests.TwoPaneViewPage");

			Query myQuery = q => q.All().Marked("ControlWidthText");

			var controlWidthText = new QueryEx(myQuery);

			// _app.Repl();

			Assert.IsGreaterThan(641, controlWidthText.GetDependencyPropertyValue<int>("Text"), "The window size must be large enough for the control Width to be larger than 641");
		}

		[Test]
		[AutoRetry]
		[Ignore("May be fixed by https://github.com/unoplatform/uno/pull/2481, needs QueryEx.All for Wasm")]
		public void ViewModeTest()
		{
			{
				SetControlWidth(ControlWidth.Wide);
				SetControlHeight(ControlHeight.Tall);

				// Wait.ForSeconds(5);

				AssertViewMode(ViewMode.LeftRight);

				Console.WriteLine("Assert changing wide behavior splits right/left");
				SetWideModeConfiguration(WideModeConfiguration.RightLeft);
				AssertViewMode(ViewMode.RightLeft);

				Console.WriteLine("Assert narrow width splits top/bottom");
				SetControlWidth(ControlWidth.Narrow);
				AssertViewMode(ViewMode.TopBottom);

				Console.WriteLine("Assert changing tall behavior splits bottom/top");
				SetTallModeConfiguration(TallModeConfiguration.BottomTop);
				AssertViewMode(ViewMode.BottomTop);

				Console.WriteLine("Assert short height shows only priority pane");
				SetControlHeight(ControlHeight.Short);
				AssertViewMode(ViewMode.Pane1Only);

				Console.WriteLine("Assert changing priority switches panes");
				SetPanePriority(PanePriority.Pane2);
				AssertViewMode(ViewMode.Pane2Only);

				Console.WriteLine("Assert tall height with span shows only priority pane");
				SetControlHeight(ControlHeight.Tall);
				AssertViewMode(ViewMode.BottomTop);
				SetTallModeConfiguration(TallModeConfiguration.SinglePane);
				AssertViewMode(ViewMode.Pane2Only);

				Console.WriteLine("Assert wide width with span shows only priority pane");
				SetControlWidth(ControlWidth.Wide);
				AssertViewMode(ViewMode.RightLeft);
				SetWideModeConfiguration(WideModeConfiguration.SinglePane);
				AssertViewMode(ViewMode.Pane2Only);

				Console.WriteLine("Assert changing priority switches panes");
				SetPanePriority(PanePriority.Pane1);
				AssertViewMode(ViewMode.Pane1Only);
			}
		}

		[Test]
		[AutoRetry]
		[Ignore("May be fixed by https://github.com/unoplatform/uno/pull/2481, needs QueryEx.All for Wasm")]
		public void ThresholdTest()
		{
			{
				SetControlWidth(ControlWidth.Wide);
				SetControlHeight(ControlHeight.Tall);

				Console.WriteLine("Assert changing min wide width updates view mode");

				// UNO TODO: Default simulator sccreen Width is not enough for original test to pass
				//SetMinWideModeWidth(c_defaultMinWideModeWidth + 100);
				SetMinWideModeWidth(700);
				SetMinTallModeHeight(400);
				AssertViewMode(ViewMode.TopBottom);

				Console.WriteLine("Assert changing min tall height updates view mode");
				// UNO TODO: Default simulator sccreen height is not enough for original test to pass
				// SetMinTallModeHeight(c_defaultMinTallModeHeight + 100);
				SetMinTallModeHeight(700);
				AssertViewMode(ViewMode.Pane1Only);

				Console.WriteLine("Assert changing min wide width updates view mode");
				// UNO TODO: Default simulator sccreen Width is not enough for original test to pass
				// SetMinWideModeWidth(c_defaultMinWideModeWidth - 100);
				SetMinWideModeWidth(400);
				AssertViewMode(ViewMode.LeftRight);
			}
		}

		[Test]
		[AutoRetry]
		[Ignore("May be fixed by https://github.com/unoplatform/uno/pull/2481, needs QueryEx.All for Wasm")]
		public void RegionTest()
		{
			{
				Console.WriteLine("Assert horizontal split regions");
				SetComboBox("SimulateComboBox", "LeftRight");

				AssertViewMode(ViewMode.LeftRight);
				AssertPaneSize(1, c_simulatedPaneWidth, c_simulatedPaneHeight);
				AssertPaneSize(2, c_simulatedPaneWidth, c_simulatedPaneHeight);
				AssertPaneSpacing(c_simulatedMiddle);

				Console.WriteLine("Assert vertical split regions");
				SetComboBox("SimulateComboBox", "TopBottom");

				AssertViewMode(ViewMode.TopBottom);
				AssertPaneSize(1, c_simulatedPaneHeight, c_simulatedPaneWidth);
				AssertPaneSize(2, c_simulatedPaneHeight, c_simulatedPaneWidth);
				AssertPaneSpacing(c_simulatedMiddle);
			}
		}

		[Test]
		[AutoRetry]
		[Ignore("May be fixed by https://github.com/unoplatform/uno/pull/2481, needs QueryEx.All for Wasm")]
		public void RegionOffsetTest()
		{
			{
				Console.WriteLine("Assert horizontal split regions with control offset from simulated window");
				SetComboBox("SimulateComboBox", "LeftRight");

				var marginCheckbox = _app.Marked("AddMarginCheckBox");
				marginCheckbox.FastTap();
				// Wait.ForIdle();

				AssertViewMode(ViewMode.LeftRight);
				AssertPaneSize(1, c_simulatedPaneWidth - c_controlMargin_left, c_simulatedPaneHeight - (c_controlMargin_top + c_controlMargin_bottom));
				AssertPaneSize(2, c_simulatedPaneWidth - c_controlMargin_right, c_simulatedPaneHeight - (c_controlMargin_top + c_controlMargin_bottom));
				AssertPaneSpacing(c_simulatedMiddle);

				Console.WriteLine("Assert vertical split regions with control offset from simulated window");
				SetComboBox("SimulateComboBox", "TopBottom");

				AssertViewMode(ViewMode.TopBottom);
				AssertPaneSize(1, c_simulatedPaneHeight - (c_controlMargin_left + c_controlMargin_right), c_simulatedPaneWidth - c_controlMargin_top);
				AssertPaneSize(2, c_simulatedPaneHeight - (c_controlMargin_left + c_controlMargin_right), c_simulatedPaneWidth - c_controlMargin_bottom);
				AssertPaneSpacing(c_simulatedMiddle);
			}
		}

		[Test]
		[AutoRetry]
		[Ignore("May be fixed by https://github.com/unoplatform/uno/pull/2481, needs QueryEx.All for Wasm")]
		public void SingleRegionTest()
		{
			{
				Console.WriteLine("Assert control acts appropriately when there are multiple regions but the control is only in one of them");

				SetComboBox("SimulateComboBox", "LeftRight");

				var marginCheckbox = _app.Marked("OneSideCheckBox");
				marginCheckbox.FastTap();
				// Wait.ForIdle();

				AssertViewMode(ViewMode.Pane1Only);
				AssertPaneSize(1, c_simulatedPaneWidth, c_simulatedPaneHeight);

				Console.WriteLine("Assert vertical split when control is in a single region");
				SetMinTallModeHeight(c_simulatedPaneHeight - 10);

				AssertViewMode(ViewMode.TopBottom);
				AssertPaneSpacing(0);
			}
		}

		[Test]
		[AutoRetry]
		[Ignore("May be fixed by https://github.com/unoplatform/uno/pull/2481, needs QueryEx.All for Wasm")]
		public void InitialPanePriorityTest()
		{
			{
				Console.WriteLine("Assert when pane priority is set to pane 2 on the small split panel, it only loads pane 2 (bug 14486142)");

				// Assert panes are actually being shown correctly
				Query paneContent1Query = q => q.All().Marked("TwoPaneViewSmall").Descendant().Marked("PART_Pane1ScrollViewer");
				Query paneContent2Query = q => q.All().Marked("TwoPaneViewSmall").Descendant().Marked("PART_Pane2ScrollViewer");

				_app.WaitForDependencyPropertyValue(paneContent1Query, "Visibility", "Collapsed");
				_app.WaitForDependencyPropertyValue(paneContent2Query, "Visibility", "Visible");
			}
		}

		[Test]
		[AutoRetry]
		[Ignore("May be fixed by https://github.com/unoplatform/uno/pull/2481, needs QueryEx.All for Wasm")]
		public void PaneLengthTest()
		{
			{
				SetControlWidth(ControlWidth.Wide);
				SetControlHeight(ControlHeight.Tall);

				int controlWidth = GetInt("ControlWidthText");
				int controlHeight = GetInt("ControlHeightText");
				Console.WriteLine("TwoPaneView size is " + controlWidth + ", " + controlHeight);

				Console.WriteLine("Assert changing pane 1 length to star sizing");
				SetLength(1, 1, "Star");

				AssertPaneSize(1, controlWidth / 2.0, 0);
				AssertPaneSize(2, controlWidth / 2.0, 0);

				Console.WriteLine("Assert changing pane 2 length to pixel sizing");
				SetLength(2, 199, "Pixel");

				AssertPaneSize(1, controlWidth - 199, 0);
				AssertPaneSize(2, 199, 0);

				Console.WriteLine("Assert column sizes stay the same when switching pane order");
				SetWideModeConfiguration(WideModeConfiguration.RightLeft);

				AssertPaneSize(1, controlWidth - 199, 0);
				AssertPaneSize(2, 199, 0);

				Console.WriteLine("Assert lengths apply to top/bottom configuration");
				SetControlWidth(ControlWidth.Narrow);

				// UNO TODO: Default simulator sccreen height is not enough for original test to pass
				SetMinTallModeHeight(400);

				AssertPaneSize(1, 0, controlHeight - 199);
				AssertPaneSize(2, 0, 199);
			}
		}

		private void AssertViewMode(ViewMode mode)
		{
			// Assert configuration is correct for mode
			TwoPaneViewMode expectedConfiguration = TwoPaneViewMode.SinglePane;
			switch (mode)
			{
				case ViewMode.LeftRight:
				case ViewMode.RightLeft:
					expectedConfiguration = TwoPaneViewMode.Wide;
					break;

				case ViewMode.TopBottom:
				case ViewMode.BottomTop:
					expectedConfiguration = TwoPaneViewMode.Tall;
					break;
			}

			Query configurationTextBlock = q => q.All().Marked("ConfigurationTextBlock");
			Assert.AreEqual(expectedConfiguration.ToString(), new QueryEx(configurationTextBlock).GetText());

			// Assert panes are actually being shown correctly
			QueryEx paneContent1Query = new QueryEx(q => q.All().Marked("TwoPaneView").Descendant().Marked("PART_Pane1ScrollViewer"));
			QueryEx paneContent2Query = new QueryEx(q => q.All().Marked("TwoPaneView").Descendant().Marked("PART_Pane2ScrollViewer"));

			var paneContent1 = _app.Query(paneContent1Query).FirstOrDefault();
			var paneContent2 = _app.Query(paneContent2Query).FirstOrDefault();

			var paneContent1BoundingRectangle = _app.Query(paneContent1Query).First().Rect;
			var paneContent2BoundingRectangle = _app.Query(paneContent2Query).First().Rect;

			var pane1Visibility = paneContent1Query.GetDependencyPropertyValue<string>("Visibility");
			var pane2Visibility = paneContent2Query.GetDependencyPropertyValue<string>("Visibility");

			if (mode != ViewMode.Pane2Only)
			{
				Assert.AreEqual("Visible", paneContent1Query.GetDependencyPropertyValue<string>("Visibility"), "Expected to find pane1");
				Console.WriteLine("Content 1 dimensions: " + paneContent1BoundingRectangle.ToString());
			}

			if (mode != ViewMode.Pane1Only)
			{
				Assert.AreEqual("Visible", paneContent2Query.GetDependencyPropertyValue<string>("Visibility"), "Expected to find pane2");
				Console.WriteLine("Content 2 dimensions: " + paneContent2BoundingRectangle.ToString());
			}

			if (mode == ViewMode.Pane2Only)
			{
				Assert.AreEqual("Collapsed", paneContent1Query.GetDependencyPropertyValue<string>("Visibility"), "Expected not to find pane1");
			}

			if (mode == ViewMode.Pane1Only)
			{
				Assert.AreEqual("Collapsed", paneContent2Query.GetDependencyPropertyValue<string>("Visibility"), "Expected not to find pane2");
			}

			switch (mode)
			{
				case ViewMode.LeftRight:
				case ViewMode.RightLeft:
					Assert.AreEqual(paneContent1BoundingRectangle.Y, paneContent2BoundingRectangle.Y, "Assert panes are horizontally aligned");
					if (mode == ViewMode.LeftRight) Assert.IsGreaterThanOrEqual(paneContent1BoundingRectangle.Right, paneContent2BoundingRectangle.X, "Assert left/right pane placement");
					else Assert.IsGreaterThanOrEqual(-paneContent2BoundingRectangle.Right, paneContent1BoundingRectangle.X, "Assert right/left pane placement");
					break;

				case ViewMode.TopBottom:
				case ViewMode.BottomTop:
					Assert.AreEqual(paneContent1BoundingRectangle.X, paneContent2BoundingRectangle.X, "Assert panes are vertically aligned");
					if (mode == ViewMode.TopBottom) Assert.IsGreaterThanOrEqual(paneContent1BoundingRectangle.Bottom, paneContent2BoundingRectangle.Y, "Assert top/bottom pane placement");
					else Assert.IsGreaterThanOrEqual(paneContent2BoundingRectangle.Bottom, paneContent1BoundingRectangle.Y, "Assert bottom/top pane placement");
					break;
			}
		}

		private void AssertPaneSize(int paneIndex, double width, double height)
		{
			if (width > 0)
			{
				AssertIsPrettyClose((int)width, GetInt("WidthText" + paneIndex), "Assert pane" + paneIndex + " width");
			}
			if (height > 0)
			{
				AssertIsPrettyClose((int)height, GetInt("HeightText" + paneIndex), "Assert pane" + paneIndex + " height");
			}
		}

		private void AssertPaneSpacing(double spacing)
		{
			AssertIsPrettyClose((int)spacing, GetInt("SpacingTextBox"), "Assert spacing");
		}

		private void AssertIsPrettyClose(int a, int b, string info)
		{
			// Due to MITA only reporting whole numbers, and some rounding on phone builds, just make sure widths/heights are pretty close.
			Assert.IsTrue(a <= b + 1 && a >= b - 1, info + ": expected " + a + ", actual " + b);
		}

		private int GetInt(string textBlockName)
		{
			Query tb = q => q.All().Marked(textBlockName);
			var documentText = new QueryEx(tb).GetText();
			Console.WriteLine("Parsing string '" + documentText + "' into an int");
			return int.Parse(documentText);
		}

		private void SetControlWidth(ControlWidth width)
		{
			SetComboBox("WidthComboBox", width.ToString());
		}

		private void SetControlHeight(ControlHeight height)
		{
			SetComboBox("HeightComboBox", height.ToString());
		}

		private void SetWideModeConfiguration(WideModeConfiguration behavior)
		{
			SetComboBox("WideModeConfigurationComboBox", behavior.ToString());
		}

		private void SetTallModeConfiguration(TallModeConfiguration behavior)
		{
			SetComboBox("TallModeConfigurationComboBox", behavior.ToString());
		}

		private void SetPanePriority(PanePriority priority)
		{
			SetComboBox("PanePriorityComboBox", priority.ToString());
		}

		private void SetMinWideModeWidth(double width)
		{
			Console.WriteLine("Setting min wide width to " + width);
			var widthTextBox = _app.Marked("MinWideModeWidthTextBox");
			var value = widthTextBox.SetDependencyPropertyValue("Text", width.ToString());
			// Wait.ForIdle();
		}

		private void SetMinTallModeHeight(double height)
		{
			Console.WriteLine("Setting min tall height to " + height);
			var heightTextBox = _app.Marked("MinTallModeHeightTextBox");
			var value = heightTextBox.SetDependencyPropertyValue("Text", height.ToString());
			// Wait.ForIdle();
		}

		private void SetComboBox(string comboBoxName, string item)
		{
			Console.WriteLine("Setting '" + comboBoxName + "' to '" + item + "'");
			var comboBox = _app.Marked(comboBoxName);
			var value = comboBox.SetDependencyPropertyValue("SelectedItem", item);
			// Wait.ForIdle();
		}

		private void SetLength(int pane, double value, string type)
		{
			Console.WriteLine("Setting pane " + pane + " length to " + value + ", " + type.ToString());

			var valueTextBox = _app.Marked("Pane" + pane + "LengthTextBox");
			valueTextBox.SetDependencyPropertyValue("Text", value.ToString());

			SetComboBox("Pane" + pane + "LengthComboBox", type);

			// Wait.ForIdle();
		}
	}
}
