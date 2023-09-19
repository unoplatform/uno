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
c.BeginFigure(new global::Windows.Foundation.Point(-1.01725E-05, -1.01725E-05), true);
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
c.BeginFigure(new global::Windows.Foundation.Point(17.432, 11.619), true);
c.BezierTo(new global::Windows.Foundation.Point(17.456, 11.701), new global::Windows.Foundation.Point(17.471999999999998, 11.783999999999999), new global::Windows.Foundation.Point(17.471999999999998, 11.866), true, false);
c.LineTo(new global::Windows.Foundation.Point(17.471999999999998, 26.54), true, false);
c.BezierTo(new global::Windows.Foundation.Point(17.471999999999998, 26.91), new global::Windows.Foundation.Point(17.200999999999997, 27.092), new global::Windows.Foundation.Point(16.866999999999997, 26.948999999999998), true, false);
c.LineTo(new global::Windows.Foundation.Point(11.527999999999997, 24.666999999999998), true, false);
c.BezierTo(new global::Windows.Foundation.Point(11.191999999999997, 24.523), new global::Windows.Foundation.Point(10.923999999999998, 24.108999999999998), new global::Windows.Foundation.Point(10.923999999999998, 23.741), true, false);
c.LineTo(new global::Windows.Foundation.Point(10.923999999999998, 9.066), true, false);
c.BezierTo(new global::Windows.Foundation.Point(10.923999999999998, 8.698), new global::Windows.Foundation.Point(11.193999999999997, 8.515), new global::Windows.Foundation.Point(11.527999999999997, 8.657), true, false);
c.LineTo(new global::Windows.Foundation.Point(16.866999999999997, 10.94), true, false);
c.ArcTo(new global::Windows.Foundation.Point(17.136999999999997, 11.128), new global::Windows.Foundation.Size(0.898, 0.898), 0d, false, global::Windows.UI.Xaml.Media.SweepDirection.Clockwise, true, false);
c.BezierTo(new global::Windows.Foundation.Point(17.226999999999997, 11.297), new global::Windows.Foundation.Point(17.325999999999997, 11.461), new global::Windows.Foundation.Point(17.432, 11.618), true, false);
c.SetClosedState(false);
c.BeginFigure(new global::Windows.Foundation.Point(9.615, 9.07), true);
c.LineTo(new global::Windows.Foundation.Point(9.615, 23.745), true, false);
c.BezierTo(new global::Windows.Foundation.Point(9.615, 24.113), new global::Windows.Foundation.Point(9.345, 24.527), new global::Windows.Foundation.Point(9.01, 24.67), true, false);
c.LineTo(new global::Windows.Foundation.Point(3.6709999999999994, 26.952), true, false);
c.BezierTo(new global::Windows.Foundation.Point(3.3369999999999993, 27.095000000000002), new global::Windows.Foundation.Point(3.0669999999999993, 26.912000000000003), new global::Windows.Foundation.Point(3.0669999999999993, 26.544), true, false);
c.LineTo(new global::Windows.Foundation.Point(3.0669999999999993, 11.868), true, false);
c.BezierTo(new global::Windows.Foundation.Point(3.0669999999999993, 11.5), new global::Windows.Foundation.Point(3.3359999999999994, 11.086), new global::Windows.Foundation.Point(3.6709999999999994, 10.942), true, false);
c.LineTo(new global::Windows.Foundation.Point(9.011, 8.66), true, false);
c.BezierTo(new global::Windows.Foundation.Point(9.344, 8.517), new global::Windows.Foundation.Point(9.614999999999998, 8.7), new global::Windows.Foundation.Point(9.614999999999998, 9.07), true, false);
c.SetClosedState(false);
c.BeginFigure(new global::Windows.Foundation.Point(25.327999999999996, 13.243), true);
c.LineTo(new global::Windows.Foundation.Point(25.327999999999996, 23.74), true, false);
c.BezierTo(new global::Windows.Foundation.Point(25.327999999999996, 24.107999999999997), new global::Windows.Foundation.Point(25.057999999999996, 24.522), new global::Windows.Foundation.Point(24.722999999999995, 24.665999999999997), true, false);
c.LineTo(new global::Windows.Foundation.Point(19.384999999999994, 26.947999999999997), true, false);
c.BezierTo(new global::Windows.Foundation.Point(19.050999999999995, 27.090999999999998), new global::Windows.Foundation.Point(18.780999999999995, 26.907999999999998), new global::Windows.Foundation.Point(18.780999999999995, 26.537999999999997), true, false);
c.LineTo(new global::Windows.Foundation.Point(18.780999999999995, 13.216), true, false);
c.BezierTo(new global::Windows.Foundation.Point(19.795999999999996, 14.447), new global::Windows.Foundation.Point(21.482999999999997, 16.831), new global::Windows.Foundation.Point(21.916999999999994, 19.516), true, false);
c.LineTo(new global::Windows.Foundation.Point(22.228999999999996, 19.516), true, false);
c.BezierTo(new global::Windows.Foundation.Point(22.658999999999995, 16.851), new global::Windows.Foundation.Point(24.315999999999995, 14.482999999999997), new global::Windows.Foundation.Point(25.327999999999996, 13.243999999999998), true, false);
c.SetClosedState(false);
c.BeginFigure(new global::Windows.Foundation.Point(22.110999999999997, 10.853999999999997), true);
c.BezierTo(new global::Windows.Foundation.Point(20.045999999999996, 10.853999999999997), new global::Windows.Foundation.Point(18.372999999999998, 9.148999999999997), new global::Windows.Foundation.Point(18.372999999999998, 7.045999999999998), true, false);
c.BezierTo(new global::Windows.Foundation.Point(18.372999999999998, 4.943999999999997), new global::Windows.Foundation.Point(20.046, 3.2389999999999977), new global::Windows.Foundation.Point(22.110999999999997, 3.2389999999999977), true, false);
c.BezierTo(new global::Windows.Foundation.Point(24.174999999999997, 3.2389999999999977), new global::Windows.Foundation.Point(25.848999999999997, 4.943999999999997), new global::Windows.Foundation.Point(25.848999999999997, 7.045999999999998), true, false);
c.BezierTo(new global::Windows.Foundation.Point(25.848999999999997, 9.148999999999997), new global::Windows.Foundation.Point(24.174999999999997, 10.853999999999997), new global::Windows.Foundation.Point(22.110999999999997, 10.853999999999997), true, false);
c.SetClosedState(false);
c.BeginFigure(new global::Windows.Foundation.Point(22.054, 2), true);
c.BezierTo(new global::Windows.Foundation.Point(19.285999999999998, 2), new global::Windows.Foundation.Point(17.041999999999998, 4.286), new global::Windows.Foundation.Point(17.041999999999998, 7.105), true, false);
c.BezierTo(new global::Windows.Foundation.Point(17.041999999999998, 8.483), new global::Windows.Foundation.Point(17.572999999999997, 9.798), new global::Windows.Foundation.Point(18.442999999999998, 10.716000000000001), true, false);
c.BezierTo(new global::Windows.Foundation.Point(18.442999999999998, 10.716000000000001), new global::Windows.Foundation.Point(21.371, 13.628), new global::Windows.Foundation.Point(21.930999999999997, 17.105), true, false);
c.LineTo(new global::Windows.Foundation.Point(22.209999999999997, 17.105), true, false);
c.BezierTo(new global::Windows.Foundation.Point(22.769999999999996, 13.628), new global::Windows.Foundation.Point(25.680999999999997, 10.716000000000001), new global::Windows.Foundation.Point(25.680999999999997, 10.716000000000001), true, false);
c.BezierTo(new global::Windows.Foundation.Point(26.554, 9.798000000000002), new global::Windows.Foundation.Point(27.066999999999997, 8.484000000000002), new global::Windows.Foundation.Point(27.066999999999997, 7.106000000000002), true, false);
c.BezierTo(new global::Windows.Foundation.Point(27.066999999999997, 4.286000000000001), new global::Windows.Foundation.Point(24.821999999999996, 2.0000000000000018), new global::Windows.Foundation.Point(22.053999999999995, 2.0000000000000018), true, false);
c.SetClosedState(false);
}, global::Windows.UI.Xaml.Media.FillRule.EvenOdd)", generatedCode);
		}
	}
}
