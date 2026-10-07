#nullable enable

#if !WINAPPSDK

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.UI.Private.Controls;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.DevTools.Input;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;
using Windows.Foundation;
using Windows.UI.Input.Preview.Injection;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_ResizeGripper
{
	[TestMethod]
#if !HAS_INPUT_INJECTOR
	[Ignore("InputInjector is not supported on this platform.")]
#endif
	public async Task When_Touch_Drag_Canceled_Then_DragCompleted_Is_Canceled()
	{
		var gripper = new ResizeGripper
		{
			Width = 40,
			Height = 40,
			DragOrientation = Orientation.Horizontal,
		};
		var host = new Grid
		{
			Width = 300,
			Height = 100,
			Children = { gripper },
		};

		var started = 0;
		List<bool> completed = new();
		gripper.DragStarted += (_, _) => started++;
		gripper.DragCompleted += (_, e) => completed.Add(e.Canceled);

		try
		{
			await UITestHelper.Load(host);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var finger = injector.GetFinger();
			var toHost = gripper.TransformToVisual(null);
			var to = toHost.TransformPoint(new Point(60, 20));

			finger.Press(toHost.TransformPoint(new Point(20, 20)));
			finger.MoveTo(to, steps: 2);
			Given_UIElement_Manipulation.CancelTouch(injector, to);

			Assert.AreEqual(1, started);
			CollectionAssert.AreEqual(new[] { true }, completed);
			Assert.IsFalse(gripper.IsDragging);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}
}

#endif
