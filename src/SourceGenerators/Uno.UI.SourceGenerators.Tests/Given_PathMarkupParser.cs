using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Media;
using Uno.MsBuildTasks.Utils.XamlPathParser;
using Windows.Foundation;
using Windows.UI.Xaml.Media;

namespace Uno.UI.SourceGenerators.Tests
{
	/// <summary>
	/// Test cases from Avalonia UI adjusted for our API usage:
	/// https://github.com/AvaloniaUI/Avalonia/blob/2dbc4be1a64a9bfc42cd647a9101e1d8ae79ad66/tests/Avalonia.Visuals.UnitTests/Media/PathMarkupParserTests.cs
	/// </summary>
	[TestClass]
	public class Given_PathMarkupParser
	{
		private string Parse(string data)
		{
			var result = Parsers.ParseGeometry(data, CultureInfo.InvariantCulture);
			// Asserts 'Should_AlwaysEndFigure' expectation to ALL tests.
			Assert.IsTrue(result.Contains(".SetClosedState(false);") || result.Contains(".SetClosedState(true);"));
			return result;
		}

		[TestMethod]
		public void Parses_Move()
		{
			var generatedCode = Parse("M10 10");

			Assert.AreEqual(@"global::Uno.Media.GeometryHelper.Build(c =>
{
c.BeginFigure(new global::Windows.Foundation.Point(10, 10), true);
c.SetClosedState(false);
}, global::Windows.UI.Xaml.Media.FillRule.EvenOdd)", generatedCode);
		}

		[TestMethod]
		public void Parses_Line()
		{
			var generatedCode = Parse("M0 0L10 10");
			Assert.AreEqual(@"global::Uno.Media.GeometryHelper.Build(c =>
{
c.BeginFigure(new global::Windows.Foundation.Point(0, 0), true);
c.LineTo(new global::Windows.Foundation.Point(10, 10), true, false);
c.SetClosedState(false);
}, global::Windows.UI.Xaml.Media.FillRule.EvenOdd)", generatedCode);
		}

		[TestMethod]
		public void Parses_Close()
		{
			var generatedCode = Parse("M0 0L10 10z");

			Assert.AreEqual(@"global::Uno.Media.GeometryHelper.Build(c =>
{
c.BeginFigure(new global::Windows.Foundation.Point(0, 0), true);
c.LineTo(new global::Windows.Foundation.Point(10, 10), true, false);
c.SetClosedState(true);
}, global::Windows.UI.Xaml.Media.FillRule.EvenOdd)", generatedCode);
		}

		[TestMethod]
		public void Parses_FillMode_Before_Move()
		{
			var generatedCode = Parse("F 1M0,0");
			Assert.AreEqual(@"global::Uno.Media.GeometryHelper.Build(c =>
{
c.BeginFigure(new global::Windows.Foundation.Point(0, 0), true);
c.SetClosedState(false);
}, global::Windows.UI.Xaml.Media.FillRule.Nonzero)", generatedCode);
		}

		[DataTestMethod]
		[DataRow("M0 0 10 10 20 20")]
		[DataRow("M0,0 10,10 20,20")]
		[DataRow("M0,0,10,10,20,20")]
		public void Parses_Implicit_Line_Command_After_Move(string pathData)
		{
			var generatedCode = Parse(pathData);
			Assert.AreEqual(@"global::Uno.Media.GeometryHelper.Build(c =>
{
c.BeginFigure(new global::Windows.Foundation.Point(0, 0), true);
c.LineTo(new global::Windows.Foundation.Point(10, 10), true, false);
c.LineTo(new global::Windows.Foundation.Point(20, 20), true, false);
c.SetClosedState(false);
}, global::Windows.UI.Xaml.Media.FillRule.EvenOdd)", generatedCode);
		}

		[DataTestMethod]
		[DataRow("m0 0 10 10 20 20")]
		[DataRow("m0,0 10,10 20,20")]
		[DataRow("m0,0,10,10,20,20")]
		public void Parses_Implicit_Line_Command_After_Relative_Move(string pathData)
		{
			var generatedCode = Parse(pathData);
			Assert.AreEqual(@"global::Uno.Media.GeometryHelper.Build(c =>
{
c.BeginFigure(new global::Windows.Foundation.Point(0, 0), true);
c.LineTo(new global::Windows.Foundation.Point(10, 10), true, false);
c.LineTo(new global::Windows.Foundation.Point(30, 30), true, false);
c.SetClosedState(false);
}, global::Windows.UI.Xaml.Media.FillRule.EvenOdd)", generatedCode);
		}

		[TestMethod]
		public void Parses_Scientific_Notation_Double()
		{
			var generatedCode = Parse("M -1.01725E-005 -1.01725e-005");
			Assert.AreEqual(@"global::Uno.Media.GeometryHelper.Build(c =>
{
c.BeginFigure(new global::Windows.Foundation.Point(-1.0172499969485216E-05, -1.0172499969485216E-05), true);
c.SetClosedState(false);
}, global::Windows.UI.Xaml.Media.FillRule.EvenOdd)", generatedCode);
		}

		[DataTestMethod]
		[DataRow("M5.5.5 5.5.5 5.5.5")]
		[DataRow("F1M9.0771,11C9.1161,10.701,9.1801,10.352,9.3031,10L9.0001,10 9.0001,6.166 3.0001,9.767 3.0001,10 "
					+ "9.99999999997669E-05,10 9.99999999997669E-05,0 3.0001,0 3.0001,0.234 9.0001,3.834 9.0001,0 "
					+ "12.0001,0 12.0001,8.062C12.1861,8.043 12.3821,8.031 12.5941,8.031 15.3481,8.031 15.7961,9.826 "
					+ "15.9201,11L16.0001,16 9.0001,16 9.0001,12.562 9.0001,11z")] // issue https://github.com/AvaloniaUI/Avalonia/issues/1708
		[DataRow("         M0 0")]
		[DataRow("F1 M24,14 A2,2,0,1,1,20,14 A2,2,0,1,1,24,14 z")] // issue https://github.com/AvaloniaUI/Avalonia/issues/1107
		[DataRow("M0 0L10 10z")]
		[DataRow("M50 50 L100 100 L150 50")]
		[DataRow("M50 50L100 100L150 50")]
		[DataRow("M50,50 L100,100 L150,50")]
		[DataRow("M50 50 L-10 -10 L10 50")]
		[DataRow("M50 50L-10-10L10 50")]
		[DataRow("M50 50 L100 100 L150 50zM50 50 L70 70 L120 50z")]
		[DataRow("M 50 50 L 100 100 L 150 50")]
		[DataRow("M50 50 L100 100 L150 50 H200 V100Z")]
		[DataRow("M 80 200 A 100 50 45 1 0 100 50")]
		[DataRow(
			"F1 M 16.6309 18.6563C 17.1309 8.15625 29.8809 14.1563 29.8809 14.1563C 30.8809 11.1563 34.1308 11.4063" +
			" 34.1308 11.4063C 33.5 12 34.6309 13.1563 34.6309 13.1563C 32.1309 13.1562 31.1309 14.9062 31.1309 14.9" +
			"062C 41.1309 23.9062 32.6309 27.9063 32.6309 27.9062C 24.6309 24.9063 21.1309 22.1562 16.6309 18.6563 Z" +
			" M 16.6309 19.9063C 21.6309 24.1563 25.1309 26.1562 31.6309 28.6562C 31.6309 28.6562 26.3809 39.1562 18" +
			".3809 36.1563C 18.3809 36.1563 18 38 16.3809 36.9063C 15 36 16.3809 34.9063 16.3809 34.9063C 16.3809 34" +
			".9063 10.1309 30.9062 16.6309 19.9063 Z ")]
		[DataRow(
			"F1M16,12C16,14.209 14.209,16 12,16 9.791,16 8,14.209 8,12 8,11.817 8.03,11.644 8.054,11.467L6.585,10 4,10 " +
			"4,6.414 2.5,7.914 0,5.414 0,3.586 3.586,0 4.414,0 7.414,3 7.586,3 9,1.586 11.914,4.5 10.414,6 " +
			"12.461,8.046C14.45,8.278,16,9.949,16,12")]
		public void Should_Parse(string pathData)
		{
			_ = Parse(pathData);
		}

		[DataTestMethod]
		[DataRow("M0 0L10 10", "false")]
		[DataRow("M0 0L10 10z", "true")]
		[DataRow("M0 0L10 10 \n ", "false")]
		[DataRow("M0 0L10 10z \n ", "true")]
		[DataRow("M0 0L10 10 ", "false")]
		[DataRow("M0 0L10 10z ", "true")]
		public void Should_AlwaysEndFigure(string pathData, string expectedClosedState)
		{
			var generatedCode = Parse(pathData);
			Assert.AreEqual($@"global::Uno.Media.GeometryHelper.Build(c =>
{{
c.BeginFigure(new global::Windows.Foundation.Point(0, 0), true);
c.LineTo(new global::Windows.Foundation.Point(10, 10), true, false);
c.SetClosedState({expectedClosedState});
}}, global::Windows.UI.Xaml.Media.FillRule.EvenOdd)", generatedCode);
		}

		[DataTestMethod]
		[DataRow("M 5.5, 5 L 5.5, 5 L 5.5, 5")]
		[DataRow("F1 M 9.0771, 11 C 9.1161, 10.701 9.1801, 10.352 9.3031, 10 L 9.0001, 10 L 9.0001, 6.166 L 3.0001, 9.767 L 3.0001, 10 "
			+ "L 9.99999999997669E-05, 10 L 9.99999999997669E-05, 0 L 3.0001, 0 L 3.0001, 0.234 L 9.0001, 3.834 L 9.0001, 0 "
			+ "L 12.0001, 0 L 12.0001, 8.062 C 12.1861, 8.043 12.3821, 8.031 12.5941, 8.031 C 15.3481, 8.031 15.7961, 9.826 "
			+ "15.9201, 11 L 16.0001, 16 L 9.0001, 16 L 9.0001, 12.562 L 9.0001, 11Z")]
		[DataRow("F1 M 24, 14 A 2, 2 0 1 1 20, 14 A 2, 2 0 1 1 24, 14Z")]
		[DataRow("M 0, 0 L 10, 10Z")]
		[DataRow("M 50, 50 L 100, 100 L 150, 50")]
		[DataRow("M 50, 50 L -10, -10 L 10, 50")]
		[DataRow("M 50, 50 L 100, 100 L 150, 50Z M 50, 50 L 70, 70 L 120, 50Z")]
		[DataRow("M 80, 200 A 100, 50 45 1 0 100, 50")]
		[DataRow("F1 M 16, 12 C 16, 14.209 14.209, 16 12, 16 C 9.791, 16 8, 14.209 8, 12 C 8, 11.817 8.03, 11.644 8.054, 11.467 L 6.585, 10 "
			+ "L 4, 10 L 4, 6.414 L 2.5, 7.914 L 0, 5.414 L 0, 3.586 L 3.586, 0 L 4.414, 0 L 7.414, 3 L 7.586, 3 L 9, 1.586 L "
			+ "11.914, 4.5 L 10.414, 6 L 12.461, 8.046 C 14.45, 8.278 16, 9.949 16, 12")]
		public void Parsed_Geometry_ToString_Should_Produce_Valid_Value(string pathData)
		{
			_ = Parse(pathData);
		}

		[DataTestMethod]
		[DataRow("M5.5.5 5.5.5 5.5.5", "M 5.5, 0.5 L 5.5, 0.5 L 5.5, 0.5")]
		[DataRow("F1 M24,14 A2,2,0,1,1,20,14 A2,2,0,1,1,24,14 z", "F1 M 24, 14 A 2, 2 0 1 1 20, 14 A 2, 2 0 1 1 24, 14Z")]
		[DataRow("F1M16,12C16,14.209 14.209,16 12,16 9.791,16 8,14.209 8,12 8,11.817 8.03,11.644 8.054,11.467L6.585,10 4,10 "
					+ "4,6.414 2.5,7.914 0,5.414 0,3.586 3.586,0 4.414,0 7.414,3 7.586,3 9,1.586 11.914,4.5 10.414,6 "
					+ "12.461,8.046C14.45,8.278,16,9.949,16,12",
					"F1 M 16, 12 C 16, 14.209 14.209, 16 12, 16 C 9.791, 16 8, 14.209 8, 12 C 8, 11.817 8.03, 11.644 8.054, 11.467 L 6.585, 10 "
					+ "L 4, 10 L 4, 6.414 L 2.5, 7.914 L 0, 5.414 L 0, 3.586 L 3.586, 0 L 4.414, 0 L 7.414, 3 L 7.586, 3 L 9, 1.586 L "
					+ "11.914, 4.5 L 10.414, 6 L 12.461, 8.046 C 14.45, 8.278 16, 9.949 16, 12")]
		public void Parsed_Geometry_ToString_Should_Format_Value(string pathData, string formattedPathData)
		{
			Assert.AreEqual(Parse(pathData), Parse(formattedPathData));
		}

		[DataTestMethod]
		[DataRow("0 0")]
		[DataRow("j")]
		public void Throws_InvalidDataException_On_None_Defined_Command(string pathData)
		{
			Assert.ThrowsException<InvalidDataException>(() => Parse(pathData));
		}

		[TestMethod]
		public void CloseFigure_Should_Move_CurrentPoint_To_CreateFigurePoint()
		{
			var generatedCode = Parse("M10,10L100,100Z m10,10");
			Assert.AreEqual(@"global::Uno.Media.GeometryHelper.Build(c =>
{
c.BeginFigure(new global::Windows.Foundation.Point(10, 10), true);
c.LineTo(new global::Windows.Foundation.Point(100, 100), true, false);
c.SetClosedState(true);
c.BeginFigure(new global::Windows.Foundation.Point(20, 20), true);
c.SetClosedState(false);
}, global::Windows.UI.Xaml.Media.FillRule.EvenOdd)", generatedCode);
		}

		[TestMethod]
		public void Boolean_Should_Be_Read_From_The_Next_Non_Whitespace_Character_Only()
		{
			var generatedCode = Parse("M17.432 11.619c.024.082.04.165.04.247V26.54c0 .37-.271.552-.605.409l-5.339-2.282c-.336-.144-.604-.558-.604-.926V9.066c0-.368.27-.551.604-.409l5.339 2.283a.898.898 0 01.27.188c.09.169.189.333.295.49M9.615 9.07v14.675c0 .368-.27.782-.605.925l-5.339 2.282c-.334.143-.604-.04-.604-.408V11.868c0-.368.269-.782.604-.926l5.34-2.282c.333-.143.604.04.604.41m15.713 4.173V23.74c0 .368-.27.782-.605.926l-5.338 2.282c-.334.143-.604-.04-.604-.41V13.216c1.015 1.231 2.702 3.615 3.136 6.3h.312c.43-2.665 2.087-5.033 3.099-6.272m-3.217-2.39c-2.065 0-3.738-1.705-3.738-3.808 0-2.102 1.673-3.807 3.738-3.807 2.064 0 3.738 1.705 3.738 3.807 0 2.103-1.674 3.808-3.738 3.808M22.054 2c-2.768 0-5.012 2.286-5.012 5.105 0 1.378.531 2.693 1.401 3.611 0 0 2.928 2.912 3.488 6.389h.279c.56-3.477 3.471-6.389 3.471-6.389.873-.918 1.386-2.232 1.386-3.61 0-2.82-2.245-5.106-5.013-5.106");

			Assert.AreEqual(@"global::Uno.Media.GeometryHelper.Build(c =>
{
c.BeginFigure(new global::Windows.Foundation.Point(17.43199920654297, 11.619000434875488), true);
c.BezierTo(new global::Windows.Foundation.Point(17.45599937438965, 11.701000213623047), new global::Windows.Foundation.Point(17.472000122070312, 11.784000396728516), new global::Windows.Foundation.Point(17.472000122070312, 11.866000175476074), true, false);
c.LineTo(new global::Windows.Foundation.Point(17.472000122070312, 26.540000915527344), true, false);
c.BezierTo(new global::Windows.Foundation.Point(17.472000122070312, 26.910001754760742), new global::Windows.Foundation.Point(17.201000213623047, 27.09200096130371), new global::Windows.Foundation.Point(16.867000579833984, 26.94900131225586), true, false);
c.LineTo(new global::Windows.Foundation.Point(11.528000831604004, 24.667001724243164), true, false);
c.BezierTo(new global::Windows.Foundation.Point(11.192000389099121, 24.52300262451172), new global::Windows.Foundation.Point(10.92400074005127, 24.10900115966797), new global::Windows.Foundation.Point(10.92400074005127, 23.74100112915039), true, false);
c.LineTo(new global::Windows.Foundation.Point(10.92400074005127, 9.065999984741211), true, false);
c.BezierTo(new global::Windows.Foundation.Point(10.92400074005127, 8.697999954223633), new global::Windows.Foundation.Point(11.194001197814941, 8.515000343322754), new global::Windows.Foundation.Point(11.528000831604004, 8.656999588012695), true, false);
c.LineTo(new global::Windows.Foundation.Point(16.867000579833984, 10.9399995803833), true, false);
c.ArcTo(new global::Windows.Foundation.Point(17.137001037597656, 11.127999305725098), new global::Windows.Foundation.Size(0.8980000019073486, 0.8980000019073486), 0d, false, global::Windows.UI.Xaml.Media.SweepDirection.Clockwise, true, false);
c.BezierTo(new global::Windows.Foundation.Point(17.227001190185547, 11.296998977661133), new global::Windows.Foundation.Point(17.326000213623047, 11.460999488830566), new global::Windows.Foundation.Point(17.4320011138916, 11.617999076843262), true, false);
c.SetClosedState(false);
c.BeginFigure(new global::Windows.Foundation.Point(9.614999771118164, 9.069999694824219), true);
c.LineTo(new global::Windows.Foundation.Point(9.614999771118164, 23.744998931884766), true, false);
c.BezierTo(new global::Windows.Foundation.Point(9.614999771118164, 24.112998962402344), new global::Windows.Foundation.Point(9.344999313354492, 24.52699851989746), new global::Windows.Foundation.Point(9.010000228881836, 24.669998168945312), true, false);
c.LineTo(new global::Windows.Foundation.Point(3.6710002422332764, 26.951997756958008), true, false);
c.BezierTo(new global::Windows.Foundation.Point(3.3370001316070557, 27.09499740600586), new global::Windows.Foundation.Point(3.067000150680542, 26.911996841430664), new global::Windows.Foundation.Point(3.067000150680542, 26.543996810913086), true, false);
c.LineTo(new global::Windows.Foundation.Point(3.067000150680542, 11.868000030517578), true, false);
c.BezierTo(new global::Windows.Foundation.Point(3.067000150680542, 11.5), new global::Windows.Foundation.Point(3.3360002040863037, 11.086000442504883), new global::Windows.Foundation.Point(3.6710002422332764, 10.942000389099121), true, false);
c.LineTo(new global::Windows.Foundation.Point(9.011000633239746, 8.660000801086426), true, false);
c.BezierTo(new global::Windows.Foundation.Point(9.344000816345215, 8.517001152038574), new global::Windows.Foundation.Point(9.61500072479248, 8.700000762939453), new global::Windows.Foundation.Point(9.61500072479248, 9.070000648498535), true, false);
c.SetClosedState(false);
c.BeginFigure(new global::Windows.Foundation.Point(25.328001022338867, 13.243000984191895), true);
c.LineTo(new global::Windows.Foundation.Point(25.328001022338867, 23.739999771118164), true, false);
c.BezierTo(new global::Windows.Foundation.Point(25.328001022338867, 24.107999801635742), new global::Windows.Foundation.Point(25.058000564575195, 24.52199935913086), new global::Windows.Foundation.Point(24.72300148010254, 24.666000366210938), true, false);
c.LineTo(new global::Windows.Foundation.Point(19.38500213623047, 26.947999954223633), true, false);
c.BezierTo(new global::Windows.Foundation.Point(19.051002502441406, 27.090999603271484), new global::Windows.Foundation.Point(18.781002044677734, 26.90799903869629), new global::Windows.Foundation.Point(18.781002044677734, 26.538000106811523), true, false);
c.LineTo(new global::Windows.Foundation.Point(18.781002044677734, 13.215999603271484), true, false);
c.BezierTo(new global::Windows.Foundation.Point(19.796001434326172, 14.446999549865723), new global::Windows.Foundation.Point(21.483001708984375, 16.83099937438965), new global::Windows.Foundation.Point(21.917001724243164, 19.51599884033203), true, false);
c.LineTo(new global::Windows.Foundation.Point(22.229001998901367, 19.51599884033203), true, false);
c.BezierTo(new global::Windows.Foundation.Point(22.65900230407715, 16.850997924804688), new global::Windows.Foundation.Point(24.316001892089844, 14.482998847961426), new global::Windows.Foundation.Point(25.3280029296875, 13.243998527526855), true, false);
c.SetClosedState(false);
c.BeginFigure(new global::Windows.Foundation.Point(22.111003875732422, 10.853998184204102), true);
c.BezierTo(new global::Windows.Foundation.Point(20.046003341674805, 10.853998184204102), new global::Windows.Foundation.Point(18.373003005981445, 9.148998260498047), new global::Windows.Foundation.Point(18.373003005981445, 7.0459980964660645), true, false);
c.BezierTo(new global::Windows.Foundation.Point(18.373003005981445, 4.943997859954834), new global::Windows.Foundation.Point(20.046003341674805, 3.2389981746673584), new global::Windows.Foundation.Point(22.111003875732422, 3.2389981746673584), true, false);
c.BezierTo(new global::Windows.Foundation.Point(24.175003051757812, 3.2389981746673584), new global::Windows.Foundation.Point(25.8490047454834, 4.943998336791992), new global::Windows.Foundation.Point(25.8490047454834, 7.0459980964660645), true, false);
c.BezierTo(new global::Windows.Foundation.Point(25.8490047454834, 9.148998260498047), new global::Windows.Foundation.Point(24.175004959106445, 10.853998184204102), new global::Windows.Foundation.Point(22.111003875732422, 10.853998184204102), true, false);
c.SetClosedState(false);
c.BeginFigure(new global::Windows.Foundation.Point(22.054000854492188, 2), true);
c.BezierTo(new global::Windows.Foundation.Point(19.286001205444336, 2), new global::Windows.Foundation.Point(17.042001724243164, 4.285999774932861), new global::Windows.Foundation.Point(17.042001724243164, 7.105000019073486), true, false);
c.BezierTo(new global::Windows.Foundation.Point(17.042001724243164, 8.482999801635742), new global::Windows.Foundation.Point(17.573001861572266, 9.79800033569336), new global::Windows.Foundation.Point(18.44300079345703, 10.715999603271484), true, false);
c.BezierTo(new global::Windows.Foundation.Point(18.44300079345703, 10.715999603271484), new global::Windows.Foundation.Point(21.371000289916992, 13.627999305725098), new global::Windows.Foundation.Point(21.931001663208008, 17.104999542236328), true, false);
c.LineTo(new global::Windows.Foundation.Point(22.21000099182129, 17.104999542236328), true, false);
c.BezierTo(new global::Windows.Foundation.Point(22.770000457763672, 13.627999305725098), new global::Windows.Foundation.Point(25.681001663208008, 10.715999603271484), new global::Windows.Foundation.Point(25.681001663208008, 10.715999603271484), true, false);
c.BezierTo(new global::Windows.Foundation.Point(26.554000854492188, 9.797999382019043), new global::Windows.Foundation.Point(27.067001342773438, 8.483999252319336), new global::Windows.Foundation.Point(27.067001342773438, 7.10599946975708), true, false);
c.BezierTo(new global::Windows.Foundation.Point(27.067001342773438, 4.285999298095703), new global::Windows.Foundation.Point(24.82200050354004, 1.9999995231628418), new global::Windows.Foundation.Point(22.054000854492188, 1.9999995231628418), true, false);
c.SetClosedState(false);
}, global::Windows.UI.Xaml.Media.FillRule.EvenOdd)", generatedCode);
		}
	}
}
