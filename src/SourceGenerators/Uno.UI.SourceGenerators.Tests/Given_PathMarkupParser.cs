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
c.BeginFigure(new global::Windows.Foundation.Point(10, 10), true, false);
c.SetClosedState(false);
}, global::Windows.UI.Xaml.Media.FillRule.EvenOdd)", generatedCode);
		}

		[TestMethod]
		public void Parses_Line()
		{
			var generatedCode = Parse("M0 0L10 10");
			Assert.AreEqual(@"global::Uno.Media.GeometryHelper.Build(c =>
{
c.BeginFigure(new global::Windows.Foundation.Point(0, 0), true, false);
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
c.BeginFigure(new global::Windows.Foundation.Point(0, 0), true, false);
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
c.BeginFigure(new global::Windows.Foundation.Point(0, 0), true, false);
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
c.BeginFigure(new global::Windows.Foundation.Point(0, 0), true, false);
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
c.BeginFigure(new global::Windows.Foundation.Point(0, 0), true, false);
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
c.BeginFigure(new global::Windows.Foundation.Point(-1.01725E-05, -1.01725E-05), true, false);
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
c.BeginFigure(new global::Windows.Foundation.Point(0, 0), true, false);
c.LineTo(new global::Windows.Foundation.Point(10, 10), true, false);
c.SetClosedState({expectedClosedState});
}}, global::Windows.UI.Xaml.Media.FillRule.EvenOdd)", generatedCode);
		}

		//[DataTestMethod]
		//[DataRow("M 5.5, 5 L 5.5, 5 L 5.5, 5")]
		//[DataRow("F1 M 9.0771, 11 C 9.1161, 10.701 9.1801, 10.352 9.3031, 10 L 9.0001, 10 L 9.0001, 6.166 L 3.0001, 9.767 L 3.0001, 10 "
		//	+ "L 9.99999999997669E-05, 10 L 9.99999999997669E-05, 0 L 3.0001, 0 L 3.0001, 0.234 L 9.0001, 3.834 L 9.0001, 0 "
		//	+ "L 12.0001, 0 L 12.0001, 8.062 C 12.1861, 8.043 12.3821, 8.031 12.5941, 8.031 C 15.3481, 8.031 15.7961, 9.826 "
		//	+ "15.9201, 11 L 16.0001, 16 L 9.0001, 16 L 9.0001, 12.562 L 9.0001, 11Z")]
		//[DataRow("F1 M 24, 14 A 2, 2 0 1 1 20, 14 A 2, 2 0 1 1 24, 14Z")]
		//[DataRow("M 0, 0 L 10, 10Z")]
		//[DataRow("M 50, 50 L 100, 100 L 150, 50")]
		//[DataRow("M 50, 50 L -10, -10 L 10, 50")]
		//[DataRow("M 50, 50 L 100, 100 L 150, 50Z M 50, 50 L 70, 70 L 120, 50Z")]
		//[DataRow("M 80, 200 A 100, 50 45 1 0 100, 50")]
		//[DataRow("F1 M 16, 12 C 16, 14.209 14.209, 16 12, 16 C 9.791, 16 8, 14.209 8, 12 C 8, 11.817 8.03, 11.644 8.054, 11.467 L 6.585, 10 "
		//	+ "L 4, 10 L 4, 6.414 L 2.5, 7.914 L 0, 5.414 L 0, 3.586 L 3.586, 0 L 4.414, 0 L 7.414, 3 L 7.586, 3 L 9, 1.586 L "
		//	+ "11.914, 4.5 L 10.414, 6 L 12.461, 8.046 C 14.45, 8.278 16, 9.949 16, 12")]
		//public void Parsed_Geometry_ToString_Should_Produce_Valid_Value(string pathData)
		//{
		//	var target = PathGeometry.Parse(pathData);

		//	string output = target.ToString();

		//	Assert.AreEqual(pathData, output);
		//}

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
c.BeginFigure(new global::Windows.Foundation.Point(10, 10), true, false);
c.LineTo(new global::Windows.Foundation.Point(100, 100), true, false);
c.SetClosedState(true);
c.BeginFigure(new global::Windows.Foundation.Point(20, 20), true, false);
c.SetClosedState(false);
}, global::Windows.UI.Xaml.Media.FillRule.EvenOdd)", generatedCode);
		}
	}
}
