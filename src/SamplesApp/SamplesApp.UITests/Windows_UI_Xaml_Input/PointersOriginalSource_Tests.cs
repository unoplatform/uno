using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Input;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.Testing;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;
#if __SKIA__
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif

namespace SamplesApp.UITests.Windows_UI_Xaml_Input
{
	[TestFixture]
	internal class PointersOriginalSource_Tests : SampleControlUITestBase
	{
		private const string _sample = "UITests.Windows_UI_Input.PointersTests.PointersOriginalSource";

		[Test]
		[AutoRetry]
		[InjectedPointer(PointerDeviceType.Touch)]
		public async Task When_Tapped_Then_OriginalSourceIsTopMostElement()
		{
			await RunAsync(_sample);

			var target = App.Query("GesturesTarget").Single().Rect;
			App.TapCoordinates(target.CenterX, target.CenterY);

			var result = App.Marked("_output").GetDependencyPropertyValue<string>("Text");
			result.Should().Be("GesturesTarget: TAPPED");
		}

		[Test]
		[AutoRetry]
		[Ignore("No effective way to test manipulations.")]
		[InjectedPointer(PointerDeviceType.Touch)]
		public async Task When_Manipulation_Then_OriginalSourceIsTopMostElement()
		{
			await RunAsync(_sample);

			var target = App.Query("ManipulationTarget").Single().Rect;
			App.DragCoordinates(target.X + 10, target.CenterY, target.Right - 10, target.CenterY);

			var result = App.Marked("_output").GetDependencyPropertyValue<string>("Text");
			result.Should().MatchRegex("ManipulationTarget: STARTING STARTED (DELTA)+ INERTIA STARTING (DELTA)+ COMPLETED");
		}
	}
}
