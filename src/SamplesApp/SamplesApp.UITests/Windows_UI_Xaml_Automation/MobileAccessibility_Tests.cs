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
			=> Run(PageName, waitForSampleControl: false, skipInitialScreenshot: true);

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
			var bounds = node.Attribute("bounds")?.Value;
			var match = bounds is null ? Match.Empty : BoundsRegex.Match(bounds);
			Assert.IsTrue(match.Success, $"UIAutomator node '{automationId}' has invalid bounds '{bounds}'.");

			var centerX = (int.Parse(match.Groups[1].Value) + int.Parse(match.Groups[3].Value)) / 2;
			var centerY = (int.Parse(match.Groups[2].Value) + int.Parse(match.Groups[4].Value)) / 2;
			var x = centerX.ToString();
			var y = centerY.ToString();
			if (RunAdb("shell", "settings", "get", "secure", "touch_exploration_enabled").Trim() == "1")
			{
				RunAdb("shell", "input", "tap", x, y);
				Thread.Sleep(250);
				RunAdb("shell", "input", "keyevent", "KEYCODE_ENTER");
			}
			else
			{
				RunAdb("shell", "input", "tap", x, y);
			}
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
			RunAdb("shell", "uiautomator", "dump", "--compressed", DeviceDumpPath);
			var hierarchy = XDocument.Parse(RunAdb("exec-out", "cat", DeviceDumpPath));
			var resourceSuffix = $":id/{automationId}";
			return hierarchy
				.Descendants("node")
				.FirstOrDefault(node =>
					node.Attribute("resource-id")?.Value.EndsWith(resourceSuffix, StringComparison.Ordinal) is true);
		}
	}
}
