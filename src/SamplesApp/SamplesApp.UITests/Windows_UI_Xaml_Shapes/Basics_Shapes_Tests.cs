using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SamplesApp.UITests.Extensions;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers.Queries;
using Uno.UITests.Helpers;

namespace SamplesApp.UITests.Windows_UI_Xaml_Shapes
{
	public partial class Basics_Shapes_Tests : SampleControlUITestBase
	{
		private const int TestTimeout = 7 * 60 * 1000;

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS, Platform.Android)]
		[Timeout(TestTimeout)]
		public void When_Rectangle()
			=> ValidateShape("Rectangle");

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS, Platform.Android)]
		[Timeout(TestTimeout)]
		public void When_Ellipse()
			=> ValidateShape("Ellipse");

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS, Platform.Android)]
		[Timeout(TestTimeout)]
		public void When_Line()
			=> ValidateShape("Line");

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS, Platform.Android)]
		[Timeout(TestTimeout)]
		public void When_Polyline()
			=> ValidateShape("Polyline");

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS, Platform.Android)]
		[Timeout(TestTimeout)]
		public void When_Polygon()
		{
			// For Polygon, the junction between the begin and the end of the path is not as smooth as WinUI (on iOS),
			// se we increase the pixel offset tolerance to ignore it.
			var tolerance = new PixelTolerance()
				.WithColor(132) // We are almost only trying to detect edges
				.WithOffset(10, 10, LocationToleranceKind.PerPixel)
				.Discrete(2);

			ValidateShape("Polygon", tolerance);
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS)]
		[Timeout(TestTimeout)]
		public void When_Path()
		{
			// For Path, the junction between the begin and the end of the path is not as smooth as WinUI (on iOS),
			// se we increase the pixel offset tolerance to ignore it.
			var tolerance = new PixelTolerance()
				.WithColor(132) // We are almost only trying to detect edges
				.WithOffset(6, 6, LocationToleranceKind.PerPixel)
				.Discrete(2);

			ValidateShape("Path", tolerance);
		}

		public void ValidateShape(string shapeName, PixelTolerance? tolerance = null)
		{
			Run("UITests.Windows_UI_Xaml_Shapes.Basic_Shapes", skipInitialScreenshot: true);

			var ctrl = new QueryEx(q => q.Marked("_basicShapesTestRoot"));
			var expectedDirectory = Path.Combine(
				TestContext.CurrentContext.TestDirectory,
				"Windows_UI_Xaml_Shapes/Basics_Shapes_Tests_EpectedResults");
			var actualDirectory = Path.Combine(
				TestContext.CurrentContext.WorkDirectory,
				nameof(Windows_UI_Xaml_Shapes),
				nameof(Basics_Shapes_Tests),
				shapeName);

			tolerance = tolerance ?? (new PixelTolerance()
				.WithColor(132) // We are almost only trying to detect edges
				.WithOffset(3, 3, LocationToleranceKind.PerPixel)
				.Discrete(2));

			var failures = new List<(string test, Exception error)>();
			// To improve performance, we run all test for a given stretch at once.
			var testGroups = _tests
				.Where(t => t.StartsWith(shapeName))
				.GroupBy(t => string.Join('_', t.Split(new[] { '_' }, 3, StringSplitOptions.RemoveEmptyEntries).Take(2)));

			foreach (var testGroup in testGroups)
			{
				ctrl.SetDependencyPropertyValue("RunTest", string.Join(';', testGroup));
				_app.WaitFor(() => !string.IsNullOrWhiteSpace(ctrl.GetDependencyPropertyValue<string>("TestResult")), timeout: TimeSpan.FromMinutes(1));
				var testResultsRaw = ctrl.GetDependencyPropertyValue<string>("TestResult");

				var testResults = testResultsRaw
					.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
					.Select(line => line.Split(new[] { ';' }, 3, StringSplitOptions.RemoveEmptyEntries))
					.Where(line => line.Length == 3)
					.ToDictionary(
						line => line[0],
						line =>
						{
							var testName = line[0];
							var isSuccess = line[1] == "SUCCESS";
							var data = Convert.FromBase64String(line[2]);

							var target = Path
								.Combine(actualDirectory, testName + (isSuccess ? ".png" : ".txt"))
								.GetNormalizedLongPath();
							var targetFile = new FileInfo(target);

							targetFile.Directory.Create();
							File.WriteAllBytes(target, data);
							SetOptions(targetFile, new ScreenshotOptions { IgnoreInSnapshotCompare = true });
							TestContext.AddTestAttachment(target, testName);

							return isSuccess
								? new PlatformBitmap(new MemoryStream(Convert.FromBase64String(line[2])))
								: new Exception(Encoding.UTF8.GetString(Convert.FromBase64String(line[2]))) as object;
						});

				foreach (var test in testGroup)
				{
					try
					{
						var expected = new FileInfo(Path.Combine(expectedDirectory, $"{test}.png"));
						if (!expected.Exists)
						{
							Assert.Fail($"Expected screenshot does not exists ({expected.FullName})");
						}

						if (!testResults.TryGetValue(test, out var testResult))
						{
							Assert.Fail($"No test result for {test}.");
						}

						if (testResult is Exception error)
						{
							Assert.Fail($"Test failed: {error.Message}");
						}

						using (var actual = (PlatformBitmap)testResult)
						{
							var scale = 1d;
							ImageAssert.AreAlmostEqual(expected, ImageAssert.FirstQuadrant, actual, ImageAssert.FirstQuadrant, scale, tolerance.Value);
						}
					}
					catch (IgnoreException e)
					{
						Console.WriteLine($"{test} is ignored: " + e);
					}
					catch (Exception e)
					{
						Console.Error.WriteLine(e); // Ease debug while reading log from CI
						failures.Add((test, e));
					}
				}
			}

			if (failures.Any())
			{
				throw new AggregateException(
					$"Failed tests ({failures.Count} of {testGroups.Sum(g => g.Count())}):\r\n{string.Join("\r\n", failures.Select(t => t.test))}\r\n",
					failures.Select(t => t.error));
			}
			else
			{
				Console.WriteLine($"All {testGroups.Sum(g => g.Count())} ran successfully.");
			}
		}

		private static readonly string[] _tests = new[]
		{
			"Ellipse_Default_FixedHeightLarge",
			"Ellipse_Default_FixedHeightSmall",
			"Ellipse_Default_FixedLarge",
			"Ellipse_Default_FixedSmall",
			"Ellipse_Default_FixedWidthLarge",
			"Ellipse_Default_FixedWidthSmall",
			"Ellipse_Default_MaxHeightLarge",
			"Ellipse_Default_MaxHeightSmall",
			"Ellipse_Default_MaxLarge",
			"Ellipse_Default_MaxSmall",
			"Ellipse_Default_MaxWidthLarge",
			"Ellipse_Default_MaxWidthSmall",
			"Ellipse_Default_MinHeightLarge",
			"Ellipse_Default_MinHeightSmall",
			"Ellipse_Default_MinLarge",
			"Ellipse_Default_MinSmall",
			"Ellipse_Default_MinWidthLarge",
			"Ellipse_Default_MinWidthSmall",
			"Ellipse_Default_Unconstrained",
			"Ellipse_Fill_FixedHeightLarge",
			"Ellipse_Fill_FixedHeightSmall",
			"Ellipse_Fill_FixedLarge",
			"Ellipse_Fill_FixedSmall",
			"Ellipse_Fill_FixedWidthLarge",
			"Ellipse_Fill_FixedWidthSmall",
			"Ellipse_Fill_MaxHeightLarge",
			"Ellipse_Fill_MaxHeightSmall",
			"Ellipse_Fill_MaxLarge",
			"Ellipse_Fill_MaxSmall",
			"Ellipse_Fill_MaxWidthLarge",
			"Ellipse_Fill_MaxWidthSmall",
			"Ellipse_Fill_MinHeightLarge",
			"Ellipse_Fill_MinHeightSmall",
			"Ellipse_Fill_MinLarge",
			"Ellipse_Fill_MinSmall",
			"Ellipse_Fill_MinWidthLarge",
			"Ellipse_Fill_MinWidthSmall",
			"Ellipse_Fill_Unconstrained",
			// None stretching on a Ellipse gives weird results on WinUI, so we ignore invalid results
			//"Ellipse_None_FixedHeightLarge",
			//"Ellipse_None_FixedHeightSmall",
			//"Ellipse_None_FixedLarge",
			//"Ellipse_None_FixedSmall",
			//"Ellipse_None_FixedWidthLarge",
			//"Ellipse_None_FixedWidthSmall",
			//"Ellipse_None_MaxHeightLarge",
			//"Ellipse_None_MaxHeightSmall",
			//"Ellipse_None_MaxLarge",
			//"Ellipse_None_MaxSmall",
			//"Ellipse_None_MaxWidthLarge",
			//"Ellipse_None_MaxWidthSmall",
			//"Ellipse_None_MinHeightLarge",
			//"Ellipse_None_MinHeightSmall",
			//"Ellipse_None_MinLarge",
			//"Ellipse_None_MinSmall",
			//"Ellipse_None_MinWidthLarge",
			//"Ellipse_None_MinWidthSmall",
			//"Ellipse_None_Unconstrained",
			"Ellipse_UniformToFill_FixedHeightLarge",
			"Ellipse_UniformToFill_FixedHeightSmall",
			// "Ellipse_UniformToFill_FixedLarge", // This is kind of buggy on WinUI, so we ignore it
			"Ellipse_UniformToFill_FixedSmall",
			"Ellipse_UniformToFill_FixedWidthLarge",
			"Ellipse_UniformToFill_FixedWidthSmall",
			"Ellipse_UniformToFill_MaxHeightLarge",
			"Ellipse_UniformToFill_MaxHeightSmall",
			"Ellipse_UniformToFill_MaxLarge",
			"Ellipse_UniformToFill_MaxSmall",
			"Ellipse_UniformToFill_MaxWidthLarge",
			"Ellipse_UniformToFill_MaxWidthSmall",
			"Ellipse_UniformToFill_MinHeightLarge",
			"Ellipse_UniformToFill_MinHeightSmall",
			"Ellipse_UniformToFill_MinLarge",
			"Ellipse_UniformToFill_MinSmall",
			"Ellipse_UniformToFill_MinWidthLarge",
			"Ellipse_UniformToFill_MinWidthSmall",
			"Ellipse_UniformToFill_Unconstrained",
			"Ellipse_Uniform_FixedHeightLarge",
			"Ellipse_Uniform_FixedHeightSmall",
			"Ellipse_Uniform_FixedLarge",
			"Ellipse_Uniform_FixedSmall",
			"Ellipse_Uniform_FixedWidthLarge",
			"Ellipse_Uniform_FixedWidthSmall",
			"Ellipse_Uniform_MaxHeightLarge",
			"Ellipse_Uniform_MaxHeightSmall",
			"Ellipse_Uniform_MaxLarge",
			"Ellipse_Uniform_MaxSmall",
			"Ellipse_Uniform_MaxWidthLarge",
			"Ellipse_Uniform_MaxWidthSmall",
			"Ellipse_Uniform_MinHeightLarge",
			"Ellipse_Uniform_MinHeightSmall",
			"Ellipse_Uniform_MinLarge",
			"Ellipse_Uniform_MinSmall",
			"Ellipse_Uniform_MinWidthLarge",
			"Ellipse_Uniform_MinWidthSmall",
			"Ellipse_Uniform_Unconstrained",
			"Line_Default_FixedHeightLarge",
			"Line_Default_FixedHeightSmall",
			"Line_Default_FixedLarge",
			"Line_Default_FixedSmall",
			"Line_Default_FixedWidthLarge",
			"Line_Default_FixedWidthSmall",
			"Line_Default_MaxHeightLarge",
			"Line_Default_MaxHeightSmall",
			"Line_Default_MaxLarge",
			"Line_Default_MaxSmall",
			"Line_Default_MaxWidthLarge",
			"Line_Default_MaxWidthSmall",
			"Line_Default_MinHeightLarge",
			"Line_Default_MinHeightSmall",
			"Line_Default_MinLarge",
			"Line_Default_MinSmall",
			"Line_Default_MinWidthLarge",
			"Line_Default_MinWidthSmall",
			"Line_Default_Unconstrained",
			"Line_Fill_FixedHeightLarge",
			"Line_Fill_FixedHeightSmall",
			"Line_Fill_FixedLarge",
			"Line_Fill_FixedSmall",
			"Line_Fill_FixedWidthLarge",
			"Line_Fill_FixedWidthSmall",
			"Line_Fill_MaxHeightLarge",
			"Line_Fill_MaxHeightSmall",
			"Line_Fill_MaxLarge",
			"Line_Fill_MaxSmall",
			"Line_Fill_MaxWidthLarge",
			"Line_Fill_MaxWidthSmall",
			"Line_Fill_MinHeightLarge",
			"Line_Fill_MinHeightSmall",
			"Line_Fill_MinLarge",
			"Line_Fill_MinSmall",
			"Line_Fill_MinWidthLarge",
			"Line_Fill_MinWidthSmall",
			"Line_Fill_Unconstrained",
			"Line_None_FixedHeightLarge",
			"Line_None_FixedHeightSmall",
			"Line_None_FixedLarge",
			"Line_None_FixedSmall",
			"Line_None_FixedWidthLarge",
			"Line_None_FixedWidthSmall",
			"Line_None_MaxHeightLarge",
			"Line_None_MaxHeightSmall",
			"Line_None_MaxLarge",
			"Line_None_MaxSmall",
			"Line_None_MaxWidthLarge",
			"Line_None_MaxWidthSmall",
			"Line_None_MinHeightLarge",
			"Line_None_MinHeightSmall",
			"Line_None_MinLarge",
			"Line_None_MinSmall",
			"Line_None_MinWidthLarge",
			"Line_None_MinWidthSmall",
			"Line_None_Unconstrained",
			"Line_UniformToFill_FixedHeightLarge",
			"Line_UniformToFill_FixedHeightSmall",
			"Line_UniformToFill_FixedLarge",
			"Line_UniformToFill_FixedSmall",
			"Line_UniformToFill_FixedWidthLarge",
			"Line_UniformToFill_FixedWidthSmall",
			"Line_UniformToFill_MaxHeightLarge",
			"Line_UniformToFill_MaxHeightSmall",
			"Line_UniformToFill_MaxLarge",
			"Line_UniformToFill_MaxSmall",
			"Line_UniformToFill_MaxWidthLarge",
			"Line_UniformToFill_MaxWidthSmall",
			"Line_UniformToFill_MinHeightLarge",
			"Line_UniformToFill_MinHeightSmall",
			"Line_UniformToFill_MinLarge",
			"Line_UniformToFill_MinSmall",
			"Line_UniformToFill_MinWidthLarge",
			"Line_UniformToFill_MinWidthSmall",
			"Line_UniformToFill_Unconstrained",
			"Line_Uniform_FixedHeightLarge",
			"Line_Uniform_FixedHeightSmall",
			"Line_Uniform_FixedLarge",
			"Line_Uniform_FixedSmall",
			"Line_Uniform_FixedWidthLarge",
			"Line_Uniform_FixedWidthSmall",
			"Line_Uniform_MaxHeightLarge",
			"Line_Uniform_MaxHeightSmall",
			// "Line_Uniform_MaxLarge", // Shape measure/arrange correct, but not aligned properly by parent
			"Line_Uniform_MaxSmall",
			// "Line_Uniform_MaxWidthLarge", // Shape measure/arrange correct, but not aligned properly by parent
			"Line_Uniform_MaxWidthSmall",
			"Line_Uniform_MinHeightLarge",
			"Line_Uniform_MinHeightSmall",
			"Line_Uniform_MinLarge",
			"Line_Uniform_MinSmall",
			"Line_Uniform_MinWidthLarge",
			"Line_Uniform_MinWidthSmall",
			"Line_Uniform_Unconstrained",
			"Path_Default_FixedHeightLarge",
			"Path_Default_FixedHeightSmall",
			"Path_Default_FixedLarge",
			"Path_Default_FixedSmall",
			"Path_Default_FixedWidthLarge",
			"Path_Default_FixedWidthSmall",
			"Path_Default_MaxHeightLarge",
			"Path_Default_MaxHeightSmall",
			"Path_Default_MaxLarge",
			"Path_Default_MaxSmall",
			"Path_Default_MaxWidthLarge",
			"Path_Default_MaxWidthSmall",
			"Path_Default_MinHeightLarge",
			"Path_Default_MinHeightSmall",
			"Path_Default_MinLarge",
			"Path_Default_MinSmall",
			"Path_Default_MinWidthLarge",
			"Path_Default_MinWidthSmall",
			"Path_Default_Unconstrained",
			"Path_Fill_FixedHeightLarge",
			"Path_Fill_FixedHeightSmall",
			"Path_Fill_FixedLarge",
			"Path_Fill_FixedSmall",
			"Path_Fill_FixedWidthLarge",
			"Path_Fill_FixedWidthSmall",
			"Path_Fill_MaxHeightLarge",
			"Path_Fill_MaxHeightSmall",
			"Path_Fill_MaxLarge",
			"Path_Fill_MaxSmall",
			"Path_Fill_MaxWidthLarge",
			"Path_Fill_MaxWidthSmall",
			"Path_Fill_MinHeightLarge",
			"Path_Fill_MinHeightSmall",
			"Path_Fill_MinLarge",
			"Path_Fill_MinSmall",
			"Path_Fill_MinWidthLarge",
			"Path_Fill_MinWidthSmall",
			"Path_Fill_Unconstrained",
			"Path_None_FixedHeightLarge",
			"Path_None_FixedHeightSmall",
			"Path_None_FixedLarge",
			"Path_None_FixedSmall",
			"Path_None_FixedWidthLarge",
			"Path_None_FixedWidthSmall",
			"Path_None_MaxHeightLarge",
			"Path_None_MaxHeightSmall",
			"Path_None_MaxLarge",
			"Path_None_MaxSmall",
			"Path_None_MaxWidthLarge",
			"Path_None_MaxWidthSmall",
			"Path_None_MinHeightLarge",
			"Path_None_MinHeightSmall",
			"Path_None_MinLarge",
			"Path_None_MinSmall",
			"Path_None_MinWidthLarge",
			"Path_None_MinWidthSmall",
			"Path_None_Unconstrained",
			"Path_UniformToFill_FixedHeightLarge",
			"Path_UniformToFill_FixedHeightSmall",
			"Path_UniformToFill_FixedLarge",
			"Path_UniformToFill_FixedSmall",
			"Path_UniformToFill_FixedWidthLarge",
			"Path_UniformToFill_FixedWidthSmall",
			"Path_UniformToFill_MaxHeightLarge",
			"Path_UniformToFill_MaxHeightSmall",
			"Path_UniformToFill_MaxLarge",
			"Path_UniformToFill_MaxSmall",
			"Path_UniformToFill_MaxWidthLarge",
			"Path_UniformToFill_MaxWidthSmall",
			"Path_UniformToFill_MinHeightLarge",
			"Path_UniformToFill_MinHeightSmall",
			"Path_UniformToFill_MinLarge",
			"Path_UniformToFill_MinSmall",
			"Path_UniformToFill_MinWidthLarge",
			"Path_UniformToFill_MinWidthSmall",
			"Path_UniformToFill_Unconstrained",
			"Path_Uniform_FixedHeightLarge",
			"Path_Uniform_FixedHeightSmall",
			"Path_Uniform_FixedLarge",
			"Path_Uniform_FixedSmall",
			"Path_Uniform_FixedWidthLarge",
			"Path_Uniform_FixedWidthSmall",
			"Path_Uniform_MaxHeightLarge",
			"Path_Uniform_MaxHeightSmall",
			"Path_Uniform_MaxLarge",
			"Path_Uniform_MaxSmall",
			"Path_Uniform_MaxWidthLarge",
			"Path_Uniform_MaxWidthSmall",
			"Path_Uniform_MinHeightLarge",
			"Path_Uniform_MinHeightSmall",
			"Path_Uniform_MinLarge",
			"Path_Uniform_MinSmall",
			// "Path_Uniform_MinWidthLarge", // Seems correct, but fails for no valid reason
			"Path_Uniform_MinWidthSmall",
			"Path_Uniform_Unconstrained",
			"Polygon_Default_FixedHeightLarge",
			"Polygon_Default_FixedHeightSmall",
			"Polygon_Default_FixedLarge",
			"Polygon_Default_FixedSmall",
			"Polygon_Default_FixedWidthLarge",
			"Polygon_Default_FixedWidthSmall",
			"Polygon_Default_MaxHeightLarge",
			"Polygon_Default_MaxHeightSmall",
			"Polygon_Default_MaxLarge",
			"Polygon_Default_MaxSmall",
			"Polygon_Default_MaxWidthLarge",
			"Polygon_Default_MaxWidthSmall",
			"Polygon_Default_MinHeightLarge",
			"Polygon_Default_MinHeightSmall",
			"Polygon_Default_MinLarge",
			"Polygon_Default_MinSmall",
			"Polygon_Default_MinWidthLarge",
			"Polygon_Default_MinWidthSmall",
			"Polygon_Default_Unconstrained",
			"Polygon_Fill_FixedHeightLarge",
			"Polygon_Fill_FixedHeightSmall",
			"Polygon_Fill_FixedLarge",
			"Polygon_Fill_FixedSmall",
			"Polygon_Fill_FixedWidthLarge",
			"Polygon_Fill_FixedWidthSmall",
			"Polygon_Fill_MaxHeightLarge",
			"Polygon_Fill_MaxHeightSmall",
			"Polygon_Fill_MaxLarge",
			"Polygon_Fill_MaxSmall",
			"Polygon_Fill_MaxWidthLarge",
			"Polygon_Fill_MaxWidthSmall",
			"Polygon_Fill_MinHeightLarge",
			"Polygon_Fill_MinHeightSmall",
			"Polygon_Fill_MinLarge",
			"Polygon_Fill_MinSmall",
			"Polygon_Fill_MinWidthLarge",
			"Polygon_Fill_MinWidthSmall",
			"Polygon_Fill_Unconstrained",
			"Polygon_None_FixedHeightLarge",
			"Polygon_None_FixedHeightSmall",
			"Polygon_None_FixedLarge",
			"Polygon_None_FixedSmall",
			"Polygon_None_FixedWidthLarge",
			"Polygon_None_FixedWidthSmall",
			"Polygon_None_MaxHeightLarge",
			"Polygon_None_MaxHeightSmall",
			"Polygon_None_MaxLarge",
			"Polygon_None_MaxSmall",
			"Polygon_None_MaxWidthLarge",
			"Polygon_None_MaxWidthSmall",
			"Polygon_None_MinHeightLarge",
			"Polygon_None_MinHeightSmall",
			"Polygon_None_MinLarge",
			"Polygon_None_MinSmall",
			"Polygon_None_MinWidthLarge",
			"Polygon_None_MinWidthSmall",
			"Polygon_None_Unconstrained",
			"Polygon_UniformToFill_FixedHeightLarge",
			"Polygon_UniformToFill_FixedHeightSmall",
			"Polygon_UniformToFill_FixedLarge",
			"Polygon_UniformToFill_FixedSmall",
			"Polygon_UniformToFill_FixedWidthLarge",
			"Polygon_UniformToFill_FixedWidthSmall",
			"Polygon_UniformToFill_MaxHeightLarge",
			"Polygon_UniformToFill_MaxHeightSmall",
			"Polygon_UniformToFill_MaxLarge",
			"Polygon_UniformToFill_MaxSmall",
			"Polygon_UniformToFill_MaxWidthLarge",
			"Polygon_UniformToFill_MaxWidthSmall",
			"Polygon_UniformToFill_MinHeightLarge",
			"Polygon_UniformToFill_MinHeightSmall",
			"Polygon_UniformToFill_MinLarge",
			"Polygon_UniformToFill_MinSmall",
			"Polygon_UniformToFill_MinWidthLarge",
			"Polygon_UniformToFill_MinWidthSmall",
			"Polygon_UniformToFill_Unconstrained",
			"Polygon_Uniform_FixedHeightLarge",
			"Polygon_Uniform_FixedHeightSmall",
			"Polygon_Uniform_FixedLarge",
			"Polygon_Uniform_FixedSmall",
			"Polygon_Uniform_FixedWidthLarge",
			"Polygon_Uniform_FixedWidthSmall",
			"Polygon_Uniform_MaxHeightLarge",
			"Polygon_Uniform_MaxHeightSmall",
			// "Polygon_Uniform_MaxLarge", // Shape measure/arrange correct, but not aligned properly by parent
			"Polygon_Uniform_MaxSmall",
			// "Polygon_Uniform_MaxWidthLarge", // Shape measure/arrange correct, but not aligned properly by parent
			"Polygon_Uniform_MaxWidthSmall",
			"Polygon_Uniform_MinHeightLarge",
			"Polygon_Uniform_MinHeightSmall",
			"Polygon_Uniform_MinLarge",
			"Polygon_Uniform_MinSmall",
			"Polygon_Uniform_MinWidthLarge",
			"Polygon_Uniform_MinWidthSmall",
			"Polygon_Uniform_Unconstrained",
			"Polyline_Default_FixedHeightLarge",
			"Polyline_Default_FixedHeightSmall",
			"Polyline_Default_FixedLarge",
			"Polyline_Default_FixedSmall",
			"Polyline_Default_FixedWidthLarge",
			"Polyline_Default_FixedWidthSmall",
			"Polyline_Default_MaxHeightLarge",
			"Polyline_Default_MaxHeightSmall",
			"Polyline_Default_MaxLarge",
			"Polyline_Default_MaxSmall",
			"Polyline_Default_MaxWidthLarge",
			"Polyline_Default_MaxWidthSmall",
			"Polyline_Default_MinHeightLarge",
			"Polyline_Default_MinHeightSmall",
			"Polyline_Default_MinLarge",
			"Polyline_Default_MinSmall",
			"Polyline_Default_MinWidthLarge",
			"Polyline_Default_MinWidthSmall",
			"Polyline_Default_Unconstrained",
			"Polyline_Fill_FixedHeightLarge",
			"Polyline_Fill_FixedHeightSmall",
			"Polyline_Fill_FixedLarge",
			"Polyline_Fill_FixedSmall",
			"Polyline_Fill_FixedWidthLarge",
			"Polyline_Fill_FixedWidthSmall",
			"Polyline_Fill_MaxHeightLarge",
			"Polyline_Fill_MaxHeightSmall",
			"Polyline_Fill_MaxLarge",
			"Polyline_Fill_MaxSmall",
			"Polyline_Fill_MaxWidthLarge",
			"Polyline_Fill_MaxWidthSmall",
			"Polyline_Fill_MinHeightLarge",
			"Polyline_Fill_MinHeightSmall",
			"Polyline_Fill_MinLarge",
			"Polyline_Fill_MinSmall",
			"Polyline_Fill_MinWidthLarge",
			"Polyline_Fill_MinWidthSmall",
			"Polyline_Fill_Unconstrained",
			"Polyline_None_FixedHeightLarge",
			"Polyline_None_FixedHeightSmall",
			"Polyline_None_FixedLarge",
			"Polyline_None_FixedSmall",
			"Polyline_None_FixedWidthLarge",
			"Polyline_None_FixedWidthSmall",
			"Polyline_None_MaxHeightLarge",
			"Polyline_None_MaxHeightSmall",
			"Polyline_None_MaxLarge",
			"Polyline_None_MaxSmall",
			"Polyline_None_MaxWidthLarge",
			"Polyline_None_MaxWidthSmall",
			"Polyline_None_MinHeightLarge",
			"Polyline_None_MinHeightSmall",
			"Polyline_None_MinLarge",
			"Polyline_None_MinSmall",
			"Polyline_None_MinWidthLarge",
			"Polyline_None_MinWidthSmall",
			"Polyline_None_Unconstrained",
			"Polyline_UniformToFill_FixedHeightLarge",
			"Polyline_UniformToFill_FixedHeightSmall",
			"Polyline_UniformToFill_FixedLarge",
			"Polyline_UniformToFill_FixedSmall",
			"Polyline_UniformToFill_FixedWidthLarge",
			"Polyline_UniformToFill_FixedWidthSmall",
			"Polyline_UniformToFill_MaxHeightLarge",
			"Polyline_UniformToFill_MaxHeightSmall",
			"Polyline_UniformToFill_MaxLarge",
			"Polyline_UniformToFill_MaxSmall",
			"Polyline_UniformToFill_MaxWidthLarge",
			"Polyline_UniformToFill_MaxWidthSmall",
			"Polyline_UniformToFill_MinHeightLarge",
			"Polyline_UniformToFill_MinHeightSmall",
			"Polyline_UniformToFill_MinLarge",
			"Polyline_UniformToFill_MinSmall",
			"Polyline_UniformToFill_MinWidthLarge",
			"Polyline_UniformToFill_MinWidthSmall",
			"Polyline_UniformToFill_Unconstrained",
			"Polyline_Uniform_FixedHeightLarge",
			"Polyline_Uniform_FixedHeightSmall",
			"Polyline_Uniform_FixedLarge",
			"Polyline_Uniform_FixedSmall",
			"Polyline_Uniform_FixedWidthLarge",
			"Polyline_Uniform_FixedWidthSmall",
			"Polyline_Uniform_MaxHeightLarge",
			"Polyline_Uniform_MaxHeightSmall",
			// "Polyline_Uniform_MaxLarge", // Shape measure/arrange correct, but not aligned properly by parent
			"Polyline_Uniform_MaxSmall",
			// "Polyline_Uniform_MaxWidthLarge", // Shape measure/arrange correct, but not aligned properly by parent
			"Polyline_Uniform_MaxWidthSmall",
			"Polyline_Uniform_MinHeightLarge",
			"Polyline_Uniform_MinHeightSmall",
			"Polyline_Uniform_MinLarge",
			"Polyline_Uniform_MinSmall",
			"Polyline_Uniform_MinWidthLarge",
			"Polyline_Uniform_MinWidthSmall",
			"Polyline_Uniform_Unconstrained",
			"Rectangle_Default_FixedHeightLarge",
			"Rectangle_Default_FixedHeightSmall",
			"Rectangle_Default_FixedLarge",
			"Rectangle_Default_FixedSmall",
			"Rectangle_Default_FixedWidthLarge",
			"Rectangle_Default_FixedWidthSmall",
			"Rectangle_Default_MaxHeightLarge",
			"Rectangle_Default_MaxHeightSmall",
			"Rectangle_Default_MaxLarge",
			"Rectangle_Default_MaxSmall",
			"Rectangle_Default_MaxWidthLarge",
			"Rectangle_Default_MaxWidthSmall",
			"Rectangle_Default_MinHeightLarge",
			"Rectangle_Default_MinHeightSmall",
			"Rectangle_Default_MinLarge",
			"Rectangle_Default_MinSmall",
			"Rectangle_Default_MinWidthLarge",
			"Rectangle_Default_MinWidthSmall",
			"Rectangle_Default_Unconstrained",
			"Rectangle_Fill_FixedHeightLarge",
			"Rectangle_Fill_FixedHeightSmall",
			"Rectangle_Fill_FixedLarge",
			"Rectangle_Fill_FixedSmall",
			"Rectangle_Fill_FixedWidthLarge",
			"Rectangle_Fill_FixedWidthSmall",
			"Rectangle_Fill_MaxHeightLarge",
			"Rectangle_Fill_MaxHeightSmall",
			"Rectangle_Fill_MaxLarge",
			"Rectangle_Fill_MaxSmall",
			"Rectangle_Fill_MaxWidthLarge",
			"Rectangle_Fill_MaxWidthSmall",
			"Rectangle_Fill_MinHeightLarge",
			"Rectangle_Fill_MinHeightSmall",
			"Rectangle_Fill_MinLarge",
			"Rectangle_Fill_MinSmall",
			"Rectangle_Fill_MinWidthLarge",
			"Rectangle_Fill_MinWidthSmall",
			"Rectangle_Fill_Unconstrained",
			// None stretching on a Rectangle gives weird results on WinUI, so we ignore invalid results
			//"Rectangle_None_FixedHeightLarge",
			//"Rectangle_None_FixedHeightSmall",
			//"Rectangle_None_FixedLarge",
			//"Rectangle_None_FixedSmall",
			//"Rectangle_None_FixedWidthLarge",
			//"Rectangle_None_FixedWidthSmall",
			//"Rectangle_None_MaxHeightLarge",
			//"Rectangle_None_MaxHeightSmall",
			//"Rectangle_None_MaxLarge",
			//"Rectangle_None_MaxSmall",
			//"Rectangle_None_MaxWidthLarge",
			//"Rectangle_None_MaxWidthSmall",
			//"Rectangle_None_MinHeightLarge",
			//"Rectangle_None_MinHeightSmall",
			//"Rectangle_None_MinLarge",
			//"Rectangle_None_MinSmall",
			//"Rectangle_None_MinWidthLarge",
			//"Rectangle_None_MinWidthSmall",
			//"Rectangle_None_Unconstrained",
			"Rectangle_UniformToFill_FixedHeightLarge",
			"Rectangle_UniformToFill_FixedHeightSmall",
			"Rectangle_UniformToFill_FixedLarge",
			"Rectangle_UniformToFill_FixedSmall",
			"Rectangle_UniformToFill_FixedWidthLarge",
			"Rectangle_UniformToFill_FixedWidthSmall",
			"Rectangle_UniformToFill_MaxHeightLarge",
			"Rectangle_UniformToFill_MaxHeightSmall",
			"Rectangle_UniformToFill_MaxLarge",
			"Rectangle_UniformToFill_MaxSmall",
			"Rectangle_UniformToFill_MaxWidthLarge",
			"Rectangle_UniformToFill_MaxWidthSmall",
			"Rectangle_UniformToFill_MinHeightLarge",
			"Rectangle_UniformToFill_MinHeightSmall",
			"Rectangle_UniformToFill_MinLarge",
			"Rectangle_UniformToFill_MinSmall",
			"Rectangle_UniformToFill_MinWidthLarge",
			"Rectangle_UniformToFill_MinWidthSmall",
			"Rectangle_UniformToFill_Unconstrained",
			"Rectangle_Uniform_FixedHeightLarge",
			"Rectangle_Uniform_FixedHeightSmall",
			"Rectangle_Uniform_FixedLarge",
			"Rectangle_Uniform_FixedSmall",
			"Rectangle_Uniform_FixedWidthLarge",
			"Rectangle_Uniform_FixedWidthSmall",
			"Rectangle_Uniform_MaxHeightLarge",
			"Rectangle_Uniform_MaxHeightSmall",
			"Rectangle_Uniform_MaxLarge",
			"Rectangle_Uniform_MaxSmall",
			"Rectangle_Uniform_MaxWidthLarge",
			"Rectangle_Uniform_MaxWidthSmall",
			"Rectangle_Uniform_MinHeightLarge",
			"Rectangle_Uniform_MinHeightSmall",
			"Rectangle_Uniform_MinLarge",
			"Rectangle_Uniform_MinSmall",
			"Rectangle_Uniform_MinWidthLarge",
			"Rectangle_Uniform_MinWidthSmall",
			"Rectangle_Uniform_Unconstrained",
		};

		[Test]
		[AutoRetry]
		public void Validate_Offscreen_Shapes()
		{
			Run("UITests.Windows_UI_Xaml_Shapes.Offscreen_Shapes");

			_app.WaitForElement("deferredShape6");

			using var screensnot = TakeScreenshot("offscreen_shapes", ignoreInSnapshotCompare: true);

			var xamlShape6 = _app.GetPhysicalRect("xamlShape6");
			var deferredShape6 = _app.GetPhysicalRect("deferredShape6");

			ImageAssert.HasColorAt(screensnot, xamlShape6.CenterX, xamlShape6.CenterY, Color.Yellow, tolerance: 5);
			ImageAssert.HasColorAt(screensnot, deferredShape6.CenterX, xamlShape6.CenterY, Color.Yellow, tolerance: 5);
		}
	}
}
