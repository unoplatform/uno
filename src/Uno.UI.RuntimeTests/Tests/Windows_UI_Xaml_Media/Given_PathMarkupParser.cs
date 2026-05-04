using AwesomeAssertions.Execution;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
#if !WINAPPSDK
using Uno.Media;
#endif
using Windows.Foundation;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_PathMarkupParser
	{
		[TestMethod]
		[DataRow("M 0,0 H 10", 10.0, 0.0)]
		[DataRow("M 0,0 h 10", 10.0, 0.0)]
		[DataRow("M 0,0 V 10", 0.0, 10.0)]
		[DataRow("M 0,0 v 10", 0.0, 10.0)]
		[DataRow("M 0,0 L 10,10", 10.0, 10.0)]
		[DataRow("M 0,0 l 10,10", 10.0, 10.0)]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public void When_Path_LinearCommand_Bounds(string data, double width, double height)
		{
			var geometry = (Geometry)XamlBindingHelper.ConvertValue(typeof(Geometry), data);
			var bounds = geometry.Bounds;

			Assert.AreEqual(width, bounds.Width, 0.01);
			Assert.AreEqual(height, bounds.Height, 0.01);
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public void When_Path_Implicit_Repeat_After_Move()
		{
			var data = (Geometry)XamlBindingHelper.ConvertValue(typeof(Geometry), "M 0,0 10,0 10,10 0,10 Z");
			var bounds = data.Bounds;

			Assert.AreEqual(10.0, bounds.Width, 0.01);
			Assert.AreEqual(10.0, bounds.Height, 0.01);
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public void When_Path_Z_Then_Command_Without_M_Reuses_Figure_Start()
		{
			var data = (Geometry)XamlBindingHelper.ConvertValue(typeof(Geometry), "M 0,0 L 10,0 10,10 0,10 Z L 5,5");

			var bounds = data.Bounds;
			Assert.AreEqual(10.0, bounds.Width, 0.01);
			Assert.AreEqual(10.0, bounds.Height, 0.01);
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public void When_Path_S_After_Non_Cubic_Uses_Current_Point()
		{
			var withS = (Geometry)XamlBindingHelper.ConvertValue(typeof(Geometry), "M 0,0 Q 5,5 10,0 S 20,10 30,0");
			var equivalentExplicit = (Geometry)XamlBindingHelper.ConvertValue(typeof(Geometry), "M 0,0 Q 5,5 10,0 C 10,0 20,10 30,0");

			withS.Bounds.Should().Be(equivalentExplicit.Bounds, 0.01);
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public void When_Path_T_After_Non_Quadratic_Uses_Current_Point()
		{
			var withT = (Geometry)XamlBindingHelper.ConvertValue(typeof(Geometry), "M 0,0 C 2,2 8,2 10,0 T 20,0");
			var equivalentExplicit = (Geometry)XamlBindingHelper.ConvertValue(typeof(Geometry), "M 0,0 C 2,2 8,2 10,0 Q 10,0 20,0");

			withT.Bounds.Should().Be(equivalentExplicit.Bounds, 0.01);
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public void When_Path_S_After_Cubic_Reflects_Control_Point()
		{
			var withS = (Geometry)XamlBindingHelper.ConvertValue(typeof(Geometry), "M 0,0 C 0,5 5,5 10,0 S 20,10 20,0");
			var equivalentExplicit = (Geometry)XamlBindingHelper.ConvertValue(typeof(Geometry), "M 0,0 C 0,5 5,5 10,0 C 15,-5 20,10 20,0");

			withS.Bounds.Should().Be(equivalentExplicit.Bounds, 0.01);
		}

#if !WINAPPSDK
		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI | RuntimeTestPlatforms.SkiaWasm)]
		public void When_Path_FillRule_F0_Is_EvenOdd()
		{
			var geometry = (StreamGeometry)XamlBindingHelper.ConvertValue(typeof(Geometry), "F0 M 0,0 L 10,0 10,10 0,10 Z");
			Assert.AreEqual(FillRule.EvenOdd, geometry.FillRule);
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI | RuntimeTestPlatforms.SkiaWasm)]
		public void When_Path_FillRule_F1_Is_Nonzero()
		{
			var geometry = (StreamGeometry)XamlBindingHelper.ConvertValue(typeof(Geometry), "F1 M 0,0 L 10,0 10,10 0,10 Z");
			Assert.AreEqual(FillRule.Nonzero, geometry.FillRule);
		}
#endif
	}
}
