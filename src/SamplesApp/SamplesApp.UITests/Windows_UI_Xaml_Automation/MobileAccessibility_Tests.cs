#nullable enable

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers.Queries;
using Query = System.Func<Uno.UITest.IAppQuery, Uno.UITest.IAppQuery>;

namespace SamplesApp.UITests.Windows_UI_Xaml_Automation
{
	[ActivePlatforms(Platform.iOS)]
	[NonParallelizable]
	public partial class MobileAccessibility_Tests : SampleControlUITestBase
	{
		private const string PageName =
			"UITests.Shared.Windows_UI.Xaml_Automation.AutomationProperties_AutomationId";

		private static Query ResultQuery => q => q.Marked("MobileAutomationResult");

		private void RunPage()
			=> Run(PageName, waitForSampleControl: false, skipInitialScreenshot: true);

		[Test]
		[AutoRetry]
		public void When_PageLoads_Then_InvokeButton_IsLocatable_By_AutomationId()
		{
			RunPage();

			_app.WaitForElement("MobileAutomationInvoke");
		}

		[Test]
		[AutoRetry]
		public void When_InvokeButton_IsTapped_Then_ResultText_Changes()
		{
			RunPage();
			_app.WaitForElement("MobileAutomationInvoke");

			_app.FastTap("MobileAutomationInvoke");

			_app.WaitForDependencyPropertyValue(ResultQuery, "Text", "Fixture action invoked.");
		}

		[Test]
		[AutoRetry]
		public void When_CheckBox_IsTapped_Then_State_Changes()
		{
			RunPage();
			Query checkBox = q => q.Marked("MobileAutomationToggle");
			_app.WaitForElement(checkBox);

			_app.FastTap("MobileAutomationToggle");

			_app.WaitForDependencyPropertyValue(checkBox, "IsChecked", true);
		}

		[Test]
		[AutoRetry]
		public void When_Slider_IsLocatable_By_AutomationId()
		{
			RunPage();

			_app.WaitForElement("MobileAutomationRange");
		}

		[Test]
		[AutoRetry]
		public void When_TextBox_IsLocatable_And_Accepts_Input_By_AutomationId()
		{
			RunPage();
			_app.WaitForElement("MobileAutomationText");

			// Tap to focus, then replace the content.
			_app.Tap("MobileAutomationText");
			_app.ClearText("MobileAutomationText");
			_app.EnterText("MobileAutomationText", "TestUser");
			Query textBox = q => q.Marked("MobileAutomationText");
			_app.WaitForDependencyPropertyValue(textBox, "Text", "TestUser");
		}

		[Test]
		[AutoRetry]
		public void When_PasswordBox_IsLocatable_By_AutomationId()
		{
			RunPage();

			var elements = _app.Query("MobileAutomationPassword");
			Assert.IsTrue(elements.Length > 0,
				"PasswordBox must be locatable by its AutomationId 'MobileAutomationPassword'.");
		}

		[Test]
		[AutoRetry]
		public void When_FirstListItem_IsTapped_Then_Result_Shows_Item_Text()
		{
			RunPage();
			_app.WaitForElement("Item01");

			_app.FastTap("Item01");

			_app.WaitForDependencyPropertyValue(ResultQuery, "Text", "Item 01");
		}

		[Test]
		[AutoRetry]
		public void When_SecondListItem_IsTapped_Then_Result_Changes()
		{
			RunPage();
			_app.WaitForElement("Item02");

			_app.FastTap("Item02");

			_app.WaitForDependencyPropertyValue(ResultQuery, "Text", "Item 02");
		}

		[Test]
		[AutoRetry]
		public void When_RelatedField_IsLocatable_By_AutomationId()
		{
			RunPage();

			_app.WaitForElement("MobileAutomationRelatedField");
		}

		[Test]
		[AutoRetry]
		public void When_ResultTextBlock_IsLocatable_By_AutomationId()
		{
			RunPage();

			_app.WaitForElement("MobileAutomationResult");
		}
	}

	[ActivePlatforms(Platform.Android)]
	[NonParallelizable]
	public partial class MobileAccessibility_Android_UiAutomator_Tests : SampleControlUITestBase
	{
		private const string PageName =
			"UITests.Shared.Windows_UI.Xaml_Automation.AutomationProperties_AutomationId";

		private void RunPage()
		{
			// Close any soft keyboard left open by a previous test *before* navigating: a
			// BACK press can reach the app when the IME is already hiding, and navigating
			// afterwards guarantees the fixture page is the foreground content either way.
			AndroidUiAutomator.DismissKeyboard();

			Run(PageName, waitForSampleControl: false, skipInitialScreenshot: true);
		}

		[Test]
		[AutoRetry]
		public void When_PageLoads_Then_InvokeButton_IsLocatable_By_AutomationId()
		{
			RunPage();

			AndroidUiAutomator.WaitForNode("MobileAutomationInvoke");
		}

		[Test]
		[AutoRetry]
		public void When_InvokeButton_IsTapped_Then_ResultText_Changes()
		{
			RunPage();

			AndroidUiAutomator.Tap("MobileAutomationInvoke");
			AndroidUiAutomator.WaitForNode(
				"MobileAutomationResult",
				node => node.Attribute("content-desc")?.Value == "Fixture action invoked.");
		}

		[Test]
		[AutoRetry]
		public void When_CheckBox_IsTapped_Then_State_Changes()
		{
			RunPage();

			AndroidUiAutomator.Tap("MobileAutomationToggle");
			AndroidUiAutomator.WaitForNode(
				"MobileAutomationToggle",
				node => node.Attribute("checked")?.Value == "true");
		}

		[Test]
		[AutoRetry]
		public void When_Slider_IsLocatable_By_AutomationId()
		{
			RunPage();

			AndroidUiAutomator.WaitForNode("MobileAutomationRange");
		}

		[Test]
		[AutoRetry]
		public void When_TextBox_IsLocatable_And_Accepts_Input_By_AutomationId()
		{
			RunPage();

			AndroidUiAutomator.Tap("MobileAutomationText");
			AndroidUiAutomator.RunAdb("shell", "input", "keyevent", "KEYCODE_MOVE_END");
			AndroidUiAutomator.RunAdb("shell", "input", "text", "TestUser");
			AndroidUiAutomator.WaitForNode(
				"MobileAutomationText",
				node => node.Attribute("text")?.Value.Contains("TestUser", StringComparison.Ordinal) is true);
		}

		[Test]
		[AutoRetry]
		public void When_PasswordBox_IsLocatable_By_AutomationId()
		{
			RunPage();

			AndroidUiAutomator.WaitForNode("MobileAutomationPassword");
		}

		[Test]
		[AutoRetry]
		public void When_FirstListItem_IsTapped_Then_Result_Shows_Item_Text()
		{
			RunPage();

			AndroidUiAutomator.Tap("Item01");
			AndroidUiAutomator.WaitForNode(
				"MobileAutomationResult",
				node => node.Attribute("content-desc")?.Value == "Item 01");
		}

		[Test]
		[AutoRetry]
		public void When_SecondListItem_IsTapped_Then_Result_Changes()
		{
			RunPage();

			AndroidUiAutomator.Tap("Item02");
			AndroidUiAutomator.WaitForNode(
				"MobileAutomationResult",
				node => node.Attribute("content-desc")?.Value == "Item 02");
		}

		[Test]
		[AutoRetry]
		public void When_RelatedField_IsLocatable_By_AutomationId()
		{
			RunPage();

			AndroidUiAutomator.WaitForNode("MobileAutomationRelatedField");
		}

		[Test]
		[AutoRetry]
		public void When_ResultTextBlock_IsLocatable_By_AutomationId()
		{
			RunPage();

			AndroidUiAutomator.WaitForNode("MobileAutomationResult");
		}

		[Test]
		[AutoRetry]
		public void When_List_Items_Are_Exposed_Then_Each_AutomationId_Appears_Once()
		{
			RunPage();

			Assert.AreEqual(1, AndroidUiAutomator.CountNodes("Item01"));
			Assert.AreEqual(1, AndroidUiAutomator.CountNodes("Item02"));
			Assert.AreEqual(1, AndroidUiAutomator.CountNodes("Item03"));
		}

		[Test]
		[AutoRetry]
		public void When_ComboBox_Is_Expanded_Then_Choice_Can_Be_Selected()
		{
			// SC-006 ExpandCollapse + Selection through the native accessibility tree.
			RunPage();

			AndroidUiAutomator.Tap("MobileAutomationCombo");
			AndroidUiAutomator.WaitForNode("ComboChoice02");
			AndroidUiAutomator.Tap("ComboChoice02");

			AndroidUiAutomator.WaitForNode(
				"MobileAutomationResult",
				node => node.Attribute("content-desc")?.Value == "Choice 02");
		}

		[Test]
		[AutoRetry]
		public void When_Range_Is_Adjusted_Then_Exposed_Value_Changes()
		{
			// SC-006 RangeValue: the exposed range node must be operable and the new value
			// must become observable through the accessibility tree.
			RunPage();

			AndroidUiAutomator.WaitForNode(
				"MobileAutomationRangeValue",
				node => node.Attribute("content-desc")?.Value == "Volume 40");

			// Tapping near the right edge of the range control moves the thumb.
			AndroidUiAutomator.TapAtFraction("MobileAutomationRange", 0.9);

			AndroidUiAutomator.WaitForNode(
				"MobileAutomationRangeValue",
				node => node.Attribute("content-desc")?.Value != "Volume 40");
		}

		[Test]
		[AutoRetry]
		public void When_Scroll_Region_Is_Scrolled_Then_Offscreen_Item_Becomes_Reachable()
		{
			// SC-006 Scroll + ScrollItem: the collection must advertise itself as scrollable
			// and scrolling must bring a trailing item into the exposed tree.
			RunPage();

			var scroller = AndroidUiAutomator.WaitForNode(
				"MobileAutomationList",
				node => node.Attribute("scrollable")?.Value == "true");

			Assert.AreEqual(
				"true",
				scroller.Attribute("scrollable")?.Value,
				"The collection must be exposed as scrollable to automation clients.");

			Assert.AreEqual(
				0,
				AndroidUiAutomator.CountNodes("Item10"),
				"Precondition: the trailing item must start outside the exposed tree.");

			for (var attempt = 0; attempt < 5 && AndroidUiAutomator.CountNodes("Item10") == 0; attempt++)
			{
				AndroidUiAutomator.SwipeUpWithin("MobileAutomationList");
			}

			AndroidUiAutomator.WaitForNode("Item10");
		}

	}

	internal static class AndroidUiAutomator
	{
		private const string DeviceDumpPath = "/sdcard/uno-mobile-a11y.xml";
		private static readonly Regex BoundsRegex = new(
			@"^\[(\d+),(\d+)\]\[(\d+),(\d+)\]$",
			RegexOptions.Compiled | RegexOptions.CultureInvariant);

		internal static XElement WaitForNode(string automationId, Func<XElement, bool>? predicate = null)
		{
			var deadline = DateTime.UtcNow.AddSeconds(30);
			do
			{
				var node = FindNode(automationId);
				if (node is not null && (predicate is null || predicate(node)))
				{
					return node;
				}

				Thread.Sleep(250);
			}
			while (DateTime.UtcNow < deadline);

			Assert.Fail($"UIAutomator did not expose '{automationId}' with the expected state.");
			throw new InvalidOperationException();
		}

		internal static void Tap(string automationId)
		{
			var node = WaitForNode(automationId);
			var (left, top, right, bottom) = GetBounds(automationId, node);

			var centerX = (left + right) / 2;
			var centerY = (top + bottom) / 2;
			var x = centerX.ToString();
			var y = centerY.ToString();
			if (RunAdb("shell", "settings", "get", "secure", "touch_exploration_enabled").Trim() == "1")
			{
				// With touch exploration active a single tap only moves accessibility focus;
				// activation is a double tap, which the service turns into ACTION_CLICK.
				// Both taps go in one shell round trip so they land inside the double-tap window.
				RunAdb("shell", "input", "tap", x, y);
				Thread.Sleep(250);
				RunAdb("shell", $"input tap {x} {y}; input tap {x} {y}");
				Thread.Sleep(500);
			}
			else
			{
				RunAdb("shell", "input", "tap", x, y);
			}
		}

		// Taps at a horizontal fraction of the node so range controls can be moved to a
		// specific position rather than only toggled at their center.
		internal static void TapAtFraction(string automationId, double horizontalFraction)
		{
			var node = WaitForNode(automationId);
			var (left, top, right, bottom) = GetBounds(automationId, node);

			var x = (int)(left + ((right - left) * horizontalFraction));
			var y = (top + bottom) / 2;
			RunAdb("shell", "input", "tap", x.ToString(), y.ToString());
			Thread.Sleep(500);
		}

		// Swipes upward inside the node's bounds to scroll its content forward.
		internal static void SwipeUpWithin(string automationId)
		{
			var node = WaitForNode(automationId);
			var (left, top, right, bottom) = GetBounds(automationId, node);

			var x = ((left + right) / 2).ToString();
			var startY = (bottom - ((bottom - top) / 5)).ToString();
			var endY = (top + ((bottom - top) / 5)).ToString();
			RunAdb("shell", "input", "swipe", x, startY, x, endY, "300");
			Thread.Sleep(750);
		}

		private static (int Left, int Top, int Right, int Bottom) GetBounds(
			string automationId,
			XElement node)
		{
			var bounds = node.Attribute("bounds")?.Value;
			var match = bounds is null ? Match.Empty : BoundsRegex.Match(bounds);
			Assert.IsTrue(match.Success, $"UIAutomator node '{automationId}' has invalid bounds '{bounds}'.");

			return (
				int.Parse(match.Groups[1].Value),
				int.Parse(match.Groups[2].Value),
				int.Parse(match.Groups[3].Value),
				int.Parse(match.Groups[4].Value));
		}

		// Closes the soft keyboard when it is showing. BACK is only sent while the IME is
		// visible so the app is never navigated away from the fixture.
		internal static void DismissKeyboard()
		{
			for (var attempt = 0; attempt < 3; attempt++)
			{
				if (!RunAdb("shell", "dumpsys", "input_method").Contains("mInputShown=true", StringComparison.Ordinal))
				{
					return;
				}

				RunAdb("shell", "input", "keyevent", "KEYCODE_BACK");
				Thread.Sleep(500);
			}
		}

		internal static int CountNodes(string automationId)
		{
			var hierarchy = GetHierarchy();
			var resourceSuffix = $":id/{automationId}";
			return hierarchy
				.Descendants("node")
				.Count(node =>
					node.Attribute("resource-id")?.Value.EndsWith(resourceSuffix, StringComparison.Ordinal) is true);
		}

		internal static string RunAdb(params string[] arguments)
		{
			var androidHome = Environment.GetEnvironmentVariable("ANDROID_HOME")
				?? throw new InvalidOperationException("ANDROID_HOME is required for Android UI automation.");
			var adbPath = Path.Combine(
				androidHome,
				"platform-tools",
				OperatingSystem.IsWindows() ? "adb.exe" : "adb");

			var startInfo = new ProcessStartInfo(adbPath)
			{
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
			};

			var serial = Environment.GetEnvironmentVariable("ANDROID_SERIAL");
			if (!string.IsNullOrWhiteSpace(serial))
			{
				startInfo.ArgumentList.Add("-s");
				startInfo.ArgumentList.Add(serial);
			}

			foreach (var argument in arguments)
			{
				startInfo.ArgumentList.Add(argument);
			}

			using var process = Process.Start(startInfo)
				?? throw new InvalidOperationException($"Unable to start '{adbPath}'.");
			var output = process.StandardOutput.ReadToEnd();
			var error = process.StandardError.ReadToEnd();
			process.WaitForExit();

			Assert.AreEqual(
				0,
				process.ExitCode,
				$"adb {string.Join(" ", arguments)} failed: {error}");
			return output;
		}

		private static XElement? FindNode(string automationId)
		{
			var hierarchy = GetHierarchy();
			var resourceSuffix = $":id/{automationId}";
			return hierarchy
				.Descendants("node")
				.FirstOrDefault(node =>
					node.Attribute("resource-id")?.Value.EndsWith(resourceSuffix, StringComparison.Ordinal) is true);
		}

		private static XDocument GetHierarchy()
		{
			RunAdb("shell", "uiautomator", "dump", "--compressed", DeviceDumpPath);
			return XDocument.Parse(RunAdb("exec-out", "cat", DeviceDumpPath));
		}
	}
}
