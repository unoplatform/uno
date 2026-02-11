using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

#if HAS_UNO
using Uno.UI.Runtime.Skia;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation
{
	/// <summary>
	/// Runtime tests for accessible slider behavior.
	/// Tests automation peer properties, range value pattern, and ARIA attribute mapping.
	/// </summary>
	[TestClass]
	public class Given_AccessibleSlider
	{
		/// <summary>
		/// T026: Verifies that a focused slider exposes its Value, Minimum, and Maximum
		/// via the IRangeValueProvider pattern. These map to aria-valuenow, aria-valuemin, aria-valuemax.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Slider_Focused_Then_Value_MinMax_Exposed()
		{
			// Arrange
			var slider = new Slider
			{
				Minimum = 0,
				Maximum = 100,
				Value = 50
			};

			await UITestHelper.Load(slider);

			// Act
			var peer = slider.GetOrCreateAutomationPeer();
			var rangeValueProvider = peer?.GetPattern(PatternInterface.RangeValue) as IRangeValueProvider;

			// Assert
			Assert.IsNotNull(peer, "Slider should have an automation peer");
			Assert.IsNotNull(rangeValueProvider, "Slider should support IRangeValueProvider");
			Assert.AreEqual(50.0, rangeValueProvider.Value, "Value should be 50");
			Assert.AreEqual(0.0, rangeValueProvider.Minimum, "Minimum should be 0");
			Assert.AreEqual(100.0, rangeValueProvider.Maximum, "Maximum should be 100");
		}

		/// <summary>
		/// T027: Verifies that changing the slider value via IRangeValueProvider.SetValue()
		/// updates the underlying Slider control. This is the path used when arrow keys are
		/// pressed on the semantic input[type=range] element.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_ArrowKey_Pressed_Then_Value_Changes()
		{
			// Arrange
			var slider = new Slider
			{
				Minimum = 0,
				Maximum = 100,
				Value = 50,
				StepFrequency = 1
			};

			await UITestHelper.Load(slider);

			var peer = slider.GetOrCreateAutomationPeer();
			var rangeValueProvider = peer?.GetPattern(PatternInterface.RangeValue) as IRangeValueProvider;

			Assert.IsNotNull(rangeValueProvider, "Slider should support IRangeValueProvider");

			// Act - Simulate arrow key changing value via automation peer
			rangeValueProvider.SetValue(55);

			// Assert
			Assert.AreEqual(55.0, slider.Value, "Slider value should be updated to 55");
			Assert.AreEqual(55.0, rangeValueProvider.Value, "RangeValueProvider should report updated value");
		}

		/// <summary>
		/// T028: Verifies that when the Slider value changes programmatically,
		/// the automation peer reports the new value. This ensures the semantic
		/// aria-valuenow attribute stays in sync.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Value_Changes_Then_AriaValueNow_Updates()
		{
			// Arrange
			var slider = new Slider
			{
				Minimum = 0,
				Maximum = 100,
				Value = 25
			};

			await UITestHelper.Load(slider);

			var peer = slider.GetOrCreateAutomationPeer();
			var rangeValueProvider = peer?.GetPattern(PatternInterface.RangeValue) as IRangeValueProvider;

			Assert.IsNotNull(rangeValueProvider, "Slider should support IRangeValueProvider");
			Assert.AreEqual(25.0, rangeValueProvider.Value, "Initial value should be 25");

			// Act - Change value programmatically
			slider.Value = 75;
			await TestServices.WindowHelper.WaitForIdle();

			// Assert - Peer should reflect the new value
			Assert.AreEqual(75.0, rangeValueProvider.Value, "RangeValueProvider should report updated value 75");

#if HAS_UNO
			// Verify AriaMapper produces correct attributes
			var attributes = AriaMapper.GetAriaAttributes(peer);
			Assert.AreEqual(75.0, attributes.ValueNow, "AriaMapper should report ValueNow=75");
			Assert.AreEqual(0.0, attributes.ValueMin, "AriaMapper should report ValueMin=0");
			Assert.AreEqual(100.0, attributes.ValueMax, "AriaMapper should report ValueMax=100");
#endif
		}

		/// <summary>
		/// Verifies that slider automation peer has correct control type.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Slider_Created_Then_Has_Slider_ControlType()
		{
			// Arrange
			var slider = new Slider { Value = 50 };
			await UITestHelper.Load(slider);

			// Act
			var peer = slider.GetOrCreateAutomationPeer();
			var controlType = peer?.GetAutomationControlType();

			// Assert
			Assert.AreEqual(AutomationControlType.Slider, controlType);
		}

		/// <summary>
		/// Verifies that a slider with custom range boundaries works correctly.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Slider_Has_Custom_Range_Then_Values_Are_Correct()
		{
			// Arrange
			var slider = new Slider
			{
				Minimum = -10,
				Maximum = 10,
				Value = 0,
				StepFrequency = 0.5
			};

			await UITestHelper.Load(slider);

			// Act
			var peer = slider.GetOrCreateAutomationPeer();
			var rangeValueProvider = peer?.GetPattern(PatternInterface.RangeValue) as IRangeValueProvider;

			// Assert
			Assert.IsNotNull(rangeValueProvider);
			Assert.AreEqual(0.0, rangeValueProvider.Value);
			Assert.AreEqual(-10.0, rangeValueProvider.Minimum);
			Assert.AreEqual(10.0, rangeValueProvider.Maximum);
		}

		/// <summary>
		/// Verifies that a read-only slider cannot be modified via automation.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Slider_Is_Disabled_Then_IsReadOnly()
		{
			// Arrange
			var slider = new Slider
			{
				Minimum = 0,
				Maximum = 100,
				Value = 50,
				IsEnabled = false
			};

			await UITestHelper.Load(slider);

			// Act
			var peer = slider.GetOrCreateAutomationPeer();
			var rangeValueProvider = peer?.GetPattern(PatternInterface.RangeValue) as IRangeValueProvider;

			// Assert
			Assert.IsNotNull(rangeValueProvider);
			Assert.IsFalse(peer.IsEnabled(), "Disabled slider's peer should report IsEnabled=false");
		}

		/// <summary>
		/// Verifies that slider with AutomationProperties.Name exposes correct name.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Slider_Has_AutomationName_Then_Name_Is_Exposed()
		{
			// Arrange
			var slider = new Slider { Value = 50 };
			AutomationProperties.SetName(slider, "Volume control");

			await UITestHelper.Load(slider);

			// Act
			var peer = slider.GetOrCreateAutomationPeer();
			var name = peer?.GetName();

			// Assert
			Assert.AreEqual("Volume control", name, "Automation name should be exposed");
		}

#if HAS_UNO
		/// <summary>
		/// Verifies that AriaMapper correctly identifies slider semantic element type.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Slider_Mapped_Then_SemanticElementType_Is_Slider()
		{
			// Arrange
			var slider = new Slider { Value = 50 };
			await UITestHelper.Load(slider);

			// Act
			var peer = slider.GetOrCreateAutomationPeer();
			var elementType = AriaMapper.GetSemanticElementType(peer);

			// Assert
			Assert.AreEqual(SemanticElementType.Slider, elementType);
		}

		/// <summary>
		/// Verifies that AriaMapper produces correct ARIA role for sliders.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Slider_Mapped_Then_AriaRole_Is_Slider()
		{
			// Arrange
			var slider = new Slider { Value = 50 };
			await UITestHelper.Load(slider);

			// Act
			var peer = slider.GetOrCreateAutomationPeer();
			var attributes = AriaMapper.GetAriaAttributes(peer);

			// Assert
			Assert.AreEqual("slider", attributes.Role);
		}

		/// <summary>
		/// Verifies that AriaMapper correctly detects range value capability.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Slider_Mapped_Then_PatternCapabilities_CanRangeValue_Is_True()
		{
			// Arrange
			var slider = new Slider { Value = 50 };
			await UITestHelper.Load(slider);

			// Act
			var peer = slider.GetOrCreateAutomationPeer();
			var capabilities = AriaMapper.GetPatternCapabilities(peer);

			// Assert
			Assert.IsTrue(capabilities.CanRangeValue, "Slider should have CanRangeValue capability");
		}
#endif
	}
}
