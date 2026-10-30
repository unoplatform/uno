using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_Control_FocusEngagement
{
	private static Control CreateControl(string name) => name switch
	{
		"Button" => new Button { Content = "B" },
		"Slider" => new Slider { Width = 200 },
		"ProgressBar" => new ProgressBar { Width = 200 },
		"DatePicker" => new DatePicker(),
		"CalendarDatePicker" => new CalendarDatePicker(),
		"MediaTransportControls" => new MediaTransportControls(),
		_ => throw new ArgumentException(name, nameof(name)),
	};

	[TestMethod]
	[DataRow("Button")]
	[DataRow("Slider")]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24672")]
	public async Task When_IsFocusEngaged_Set_On_Focused_Control(string name)
	{
		var control = CreateControl(name);
		control.IsFocusEngagementEnabled = true;

		try
		{
			await UITestHelper.Load(control);

			control.Focus(FocusState.Keyboard);
			await WindowHelper.WaitForIdle();

			var engagedCount = 0;
			control.FocusEngaged += (s, e) => engagedCount++;

			control.IsFocusEngaged = true;

			Assert.AreEqual(1, engagedCount, $"{name}: FocusEngaged was not raised.");
			Assert.IsTrue(control.IsFocusEngaged);
		}
		finally
		{
			control.IsFocusEngaged = false;
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[DataRow("Button")]
	[DataRow("Slider")]
	[DataRow("ProgressBar")]
	[DataRow("DatePicker")]
	[DataRow("CalendarDatePicker")]
	[DataRow("MediaTransportControls")]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24672")]
	public async Task When_IsFocusEngaged_Set_Without_Focus(string name)
	{
		var control = CreateControl(name);
		control.IsFocusEngagementEnabled = true;

		try
		{
			await UITestHelper.Load(control);

			try
			{
				control.IsFocusEngaged = true;
			}
			catch (Exception)
			{
				// Engaging focus on an unfocused control is rejected.
			}

			Assert.IsFalse(control.IsFocusEngaged, $"{name}: IsFocusEngaged stayed true on an unfocused control.");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}
}
