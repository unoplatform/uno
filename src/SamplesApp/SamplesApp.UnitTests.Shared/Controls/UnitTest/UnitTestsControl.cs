using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Disposables;
using Uno.Extensions;
using Uno.UI.RuntimeTests;
using Uno.UI.Samples.Helper;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media;

namespace Uno.UI.Samples.Tests
{
	public sealed partial class UnitTestsControl : UserControl
	{
		private const StringComparison StrComp = StringComparison.InvariantCultureIgnoreCase;
		private Task _runner;
		private CancellationTokenSource _cts = new CancellationTokenSource();
#if DEBUG
		private readonly TimeSpan DefaultUnitTestTimeout = TimeSpan.FromSeconds(300);
#else
		private readonly TimeSpan DefaultUnitTestTimeout = TimeSpan.FromSeconds(60);
#endif

		private List<TestCase> _testCases = new List<TestCase>();
		private TestRun _currentRun;

		private enum TestResult
		{
			Passed,
			Failed,
			Error,
			Skipped,
		}

		public UnitTestsControl()
		{
			this.InitializeComponent();

			Private.Infrastructure.TestServices.WindowHelper.EmbeddedTestRootControl = unitTestContentRoot;

			DataContext = null;

			Unloaded += (snd, evt) => StopRunningTests();
		}

		public string NUnitTestResultsDocument
		{
			get => (string)GetValue(NUnitTestResultsDocumentProperty);
			set => SetValue(NUnitTestResultsDocumentProperty, value);
		}

		public static readonly DependencyProperty NUnitTestResultsDocumentProperty =
			DependencyProperty.Register(nameof(NUnitTestResultsDocument), typeof(string), typeof(UnitTestsControl), new PropertyMetadata(string.Empty));


		private void OnRunTests(object sender, RoutedEventArgs e)
		{
			Interlocked.Exchange(ref _cts, new CancellationTokenSource())?.Cancel(); // cancel any previous CTS

			var filter = testFilter.Text.Trim();
			if (string.IsNullOrEmpty(filter))
			{
				filter = null;
			}

			testResults.Children.Clear();

			async Task DoRunTests()
			{
				try
				{
					await RunTests(_cts.Token, filter?.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>());
				}
				finally
				{
					await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
					{
						runButton.IsEnabled = true;
						stopButton.IsEnabled = false;
					});
				}
			}

			_runner = Task.Run(DoRunTests);
		}

		private void OnStopTests(object sender, RoutedEventArgs e)
		{
			StopRunningTests();
		}

		private void StopRunningTests()
		{
			var cts = Interlocked.Exchange(ref _cts, null);
			cts?.Cancel();
		}

		private async Task ReportMessage(string message, bool isRunning = true)
		{
			void Setter()
			{
				runButton.IsEnabled = !isRunning || _cts == null;
				stopButton.IsEnabled = _cts != null && !_cts.IsCancellationRequested || !isRunning;
				runningState.Text = isRunning ? "Running" : "Finished";
				runStatus.Text = message;
			}

			await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, Setter);
		}

		private void ReportTestsResults()
		{
			void Update()
			{
				runTestCount.Text = _currentRun.Run.ToString();
				ignoredTestCount.Text = _currentRun.Ignored.ToString();
				succeededTestCount.Text = _currentRun.Succeeded.ToString();
				failedTestCount.Text = _currentRun.Failed.ToString();
			}

			var t = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, Update);
		}

		private void GenerateTestResults()
		{
			void Update()
			{
				var results = GenerateNUnitTestResults(_testCases, _currentRun);

				NUnitTestResultsDocument = results;
			}

			var t = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, Update);
		}

		private void ReportTestClass(TypeInfo testClass)
		{
			var t = Dispatcher.RunAsync(
				Windows.UI.Core.CoreDispatcherPriority.Normal,
				() =>
				{
					var testResultBlock = new TextBlock()
					{
						Text = $"{testClass.Name} ({testClass.Assembly.GetName().Name})",
						Foreground = new SolidColorBrush(Colors.White),
						FontSize = 16d,
						IsTextSelectionEnabled = true
					};

					testResults.Children.Add(testResultBlock);
					testResultBlock.StartBringIntoView();
				}
			);
		}

		private void ReportTestResult(string testName, TimeSpan duration, TestResult testResult, Exception error = null, string message = null, string console = null)
		{
			_testCases.Add(
				new TestCase {
					TestName = testName,
					Duration = duration,
					TestResult = testResult,
					Message = error?.ToString() ?? message
				});

			void Update()
			{
				runTestCount.Text = _currentRun.Run.ToString();
				ignoredTestCount.Text = _currentRun.Ignored.ToString();
				succeededTestCount.Text = _currentRun.Succeeded.ToString();
				failedTestCount.Text = _currentRun.Failed.ToString();

				var testResultBlock = new TextBlock()
				{
					TextWrapping = TextWrapping.Wrap,
					FontFamily = new FontFamily("Courier New"),
					Margin = ThicknessHelper2.FromLengths(8, 0, 0, 0),
					Foreground = new SolidColorBrush(Colors.LightGray),
					IsTextSelectionEnabled = true
				};

				testResultBlock.Inlines.Add(new Run
				{
					Text = GetTestResultIcon(testResult) + ' ' + testName,
					FontSize = 13.5d,
					Foreground = new SolidColorBrush(GetTestResultColor(testResult)),
					FontWeight = FontWeights.ExtraBold
				});

				if (message is { })
				{
					testResultBlock.Inlines.Add(new Run { Text = "\n  ..." + message, FontStyle = FontStyle.Italic });
				}

				if (error is { })
				{
					var isFailed = testResult == TestResult.Failed || testResult == TestResult.Error;

					var foreground = isFailed ? new SolidColorBrush(Colors.Red) : new SolidColorBrush(Colors.Yellow);
					testResultBlock.Inlines.Add(new Run { Text = "\nEXCEPTION>" + error.Message, Foreground = foreground });

					if (isFailed)
					{
						failedTestDetails.Text += $"{testResult}: {testName} [{error.GetType()}] \n {error}\n\n";
					}
				}

				if (console is { })
				{
					testResultBlock.Inlines.Add(new Run { Text = "\nOUT>" + console, Foreground = new SolidColorBrush(Colors.Gray) });
				}

				testResults.Children.Add(testResultBlock);
				testResultBlock.StartBringIntoView();

				if (testResult == TestResult.Error || testResult == TestResult.Failed)
				{
					failedTests.Text += "§" + testName;
				}
			}

			var t = Dispatcher.RunAsync(
				Windows.UI.Core.CoreDispatcherPriority.Normal,
				Update);
		}

		private static string GenerateNUnitTestResults(List<TestCase> testCases, TestRun testRun)
		{
			var resultsId = Guid.NewGuid().ToString();

			var doc = new XmlDocument();
			var rootNode = doc.CreateElement("test-run");
			doc.AppendChild(rootNode);
			rootNode.SetAttribute("id", resultsId);
			rootNode.SetAttribute("name", "Runtime Tests");
			rootNode.SetAttribute("testcasecount", testRun.Run.ToString());
			rootNode.SetAttribute("result", testRun.Failed == 0 ? "Passed" : "Failed");
			rootNode.SetAttribute("time", "0");
			rootNode.SetAttribute("total", testRun.Run.ToString());
			rootNode.SetAttribute("errors", "0");
			rootNode.SetAttribute("passed", testRun.Succeeded.ToString());
			rootNode.SetAttribute("failed", testRun.Failed.ToString());
			rootNode.SetAttribute("inconclusive", "0");
			rootNode.SetAttribute("skipped", testRun.Ignored.ToString());
			rootNode.SetAttribute("asserts", "0");

			var now = DateTimeOffset.Now;
			rootNode.SetAttribute("run-date", now.ToString("yyyy-MM-dd"));
			rootNode.SetAttribute("start-time", now.ToString("HH:mm:ss"));
			rootNode.SetAttribute("end-time", now.ToString("HH:mm:ss"));

			var testSuiteAssemblyNode = doc.CreateElement("test-suite");
			rootNode.AppendChild(testSuiteAssemblyNode);
			testSuiteAssemblyNode.SetAttribute("type", "Assembly");
			testSuiteAssemblyNode.SetAttribute("name", typeof(UnitTestsControl).Assembly.GetName().Name);

			var environmentNode = doc.CreateElement("environment");
			testSuiteAssemblyNode.AppendChild(environmentNode);
			environmentNode.SetAttribute("machine-name", Environment.MachineName);
			environmentNode.SetAttribute("platform", "n/a");

			var testSuiteFixtureNode = doc.CreateElement("test-suite");
			testSuiteAssemblyNode.AppendChild(testSuiteFixtureNode);

			testSuiteFixtureNode.SetAttribute("type", "TestFixture");
			testSuiteFixtureNode.SetAttribute("name", resultsId);
			testSuiteFixtureNode.SetAttribute("executed", "true");

			testSuiteFixtureNode.SetAttribute("testcasecount", testRun.Run.ToString());
			testSuiteFixtureNode.SetAttribute("result", testRun.Failed == 0 ? "Passed" : "Failed");
			testSuiteFixtureNode.SetAttribute("time", "0");
			testSuiteFixtureNode.SetAttribute("total", testRun.Run.ToString());
			testSuiteFixtureNode.SetAttribute("errors", "0");
			testSuiteFixtureNode.SetAttribute("passed", testRun.Succeeded.ToString());
			testSuiteFixtureNode.SetAttribute("failed", testRun.Failed.ToString());
			testSuiteFixtureNode.SetAttribute("inconclusive", "0");
			testSuiteFixtureNode.SetAttribute("skipped", testRun.Ignored.ToString());
			testSuiteFixtureNode.SetAttribute("asserts", "0");

			foreach (var run in testCases)
			{
				var testCaseNode = doc.CreateElement("test-case");
				testSuiteFixtureNode.AppendChild(testCaseNode);

				testCaseNode.SetAttribute("name", run.TestName);
				testCaseNode.SetAttribute("fullname", run.TestName);
				testCaseNode.SetAttribute("duration", run.Duration.TotalSeconds.ToString(CultureInfo.InvariantCulture));
				testCaseNode.SetAttribute("time", "0");

				testCaseNode.SetAttribute("result", run.TestResult.ToString());

				if (run.TestResult == TestResult.Failed || run.TestResult == TestResult.Error)
				{
					var failureNode = doc.CreateElement("failure");
					testCaseNode.AppendChild(failureNode);

					var messageNode = doc.CreateElement("message");
					failureNode.AppendChild(messageNode);

					messageNode.InnerText = run.Message;
				}
			}

			using var w = new StringWriter();
			doc.Save(w);

			return w.ToString();
		}


		private string GetTestResultIcon(TestResult testResult)
		{
			switch (testResult)
			{
				default:
				case TestResult.Error:
				case TestResult.Failed:
					return "❌ (F)";

				case TestResult.Skipped:
					return "🚫 (I)";

				case TestResult.Passed:
					return "✔️ (S)";
			}
		}

		private Color GetTestResultColor(TestResult testResult)
		{
			switch (testResult)
			{
				case TestResult.Error:
				case TestResult.Failed:
				default:
					return Colors.Red;

				case TestResult.Skipped:
					return Colors.Orange;

				case TestResult.Passed:
					return Colors.LightGreen;
			}
		}

		public async Task RunTestsForInstance(object testClassInstance)
		{
			Interlocked.Exchange(ref _cts, new CancellationTokenSource())?.Cancel(); // cancel any previous CTS

			var filter = testFilter.Text.Trim();
			if (string.IsNullOrEmpty(filter))
			{
				filter = null;
			}

			var filters = filter != null ?
				filter.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries) :
				Array.Empty<string>();

			testResults.Children.Clear();

			try
			{
				try
				{
					var testTypeInfo = BuildType(testClassInstance.GetType());

					var tests = FilterTests(testTypeInfo, filters);

					if (tests.Length == 0)
					{
						return;
					}

					ReportTestClass(testTypeInfo.Type.GetTypeInfo());
					_ = ReportMessage($"Running {tests.Length} test methods");

					await ExecuteTestsForInstance(_cts.Token, testClassInstance, testTypeInfo.Tests, testTypeInfo);
				}
				catch (Exception e)
				{
					_currentRun.Failed = -1;
					_ = ReportMessage($"Tests runner failed {e}");
					ReportTestResult("Runtime exception", TimeSpan.Zero, TestResult.Failed, e);
					ReportTestsResults();
				}
			}
			finally
			{
				await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
				{
					runButton.IsEnabled = true;
					stopButton.IsEnabled = false;
				});
			}
		}

		private async Task RunTests(CancellationToken ct, string[] filters)
		{
			_currentRun = new TestRun();

			try
			{
				_ = ReportMessage("Enumerating tests");

				var testTypes = InitializeTests();

				_ = ReportMessage("Running tests...");

				foreach (var type in testTypes.Where(t => t.Type != null))
				{
					var tests = FilterTests(type, filters);

					if (tests.Length == 0)
					{
						continue;
					}

					ReportTestClass(type.Type.GetTypeInfo());
					_ = ReportMessage($"Running {tests.Length} test methods");

					var instance = Activator.CreateInstance(type: type.Type);

					await ExecuteTestsForInstance(ct, instance, tests, type);
				}

				_ = ReportMessage("Tests finished running.", isRunning: false);
				ReportTestsResults();
			}
			catch (Exception e)
			{
				_currentRun.Failed = -1;
				_ = ReportMessage($"Tests runner failed {e}");
				ReportTestResult("Runtime exception", TimeSpan.Zero, TestResult.Failed, e);
				ReportTestsResults();
			}

			GenerateTestResults();
		}

		private MethodInfo[] FilterTests(UnitTestClassInfo testClassInfo, string[] filters)
		{
			var testClassNameContainsFilters = filters?.Any(f => testClassInfo.Type.FullName.Contains(f, StrComp)) ?? false;
			return testClassInfo.Tests
				.Where(t => (filters?.None() ?? true)
							|| testClassNameContainsFilters
							|| filters.Any(f => t.DeclaringType.FullName.Contains(f, StrComp))
							|| filters.Any(f => t.Name.Contains(f, StrComp)))
				.ToArray();
		}

		private class CustomConsoleOutput : TextWriter
		{
			private readonly TextWriter _previousOutput;
			private readonly List<char> _accumulator = new List<char>();

			public CustomConsoleOutput(TextWriter previousOutput)
			{
				_previousOutput = previousOutput;
			}

			internal string GetContentAndReset()
			{
				var result = new string(_accumulator.ToArray());
				Reset();
				return result;
			}

			internal void Reset() => _accumulator.Clear();

			public override Encoding Encoding { get; }

			public override void Write(char value)
			{
				_previousOutput.Write(value);
				_accumulator.Add(value);
			}
		}

		private async Task ExecuteTestsForInstance(
			CancellationToken ct,
			object instance,
			MethodInfo[] tests,
			UnitTestClassInfo testClassInfo)
		{
			IDisposable consoleRegistration = default;
			CustomConsoleOutput testConsoleOutput = default;

			if (consoleOutput.IsChecked ?? false)
			{
				var previousOutput = Console.Out;

				testConsoleOutput = new CustomConsoleOutput(previousOutput);

				consoleRegistration = Disposable.Create(() => Console.SetOut(previousOutput));

				Console.SetOut(testConsoleOutput);
			}

			try
			{
				foreach (var testMethod in tests)
				{
					string testName = testMethod.Name;

					if (IsIgnored(testMethod, out var ignoreMessage))
					{
						_currentRun.Ignored++;
						ReportTestResult(testName, TimeSpan.Zero, TestResult.Skipped, message: ignoreMessage);
						continue;
					}

				var runsOnUIThread =
					HasCustomAttribute<RunsOnUIThreadAttribute>(testMethod) ||
					HasCustomAttribute<RunsOnUIThreadAttribute>(testMethod.DeclaringType);
				var requiresFullWindow =
					HasCustomAttribute<RequiresFullWindowAttribute>(testMethod) ||
					HasCustomAttribute<RequiresFullWindowAttribute>(testMethod.DeclaringType);
				var expectedException = testMethod.GetCustomAttributes<ExpectedExceptionAttribute>()
					.SingleOrDefault();
				var dataRows = testMethod.GetCustomAttributes<DataRowAttribute>();
				if (dataRows.Any())
				{
					foreach (var row in dataRows)
					{
						var d = row.Data;
						await InvokeTestMethod(d);
					}
				}
				else
				{
					await InvokeTestMethod(new object[0]);
				}

					async Task InvokeTestMethod(object[] parameters)
					{
						var fullTestName =
							$"{testName}({parameters.Select(p => p?.ToString() ?? "<null>").JoinBy(", ")})";

						_currentRun.Run++;
						// We await this to make sure the UI is updated before running the test.
						// This will help developpers to identify faulty tests when the app is crashing.
						await ReportMessage($"Running test {fullTestName}");
						ReportTestsResults();

						var sw = new Stopwatch();

					try
					{
						if (requiresFullWindow)
						{
							await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
							{
								Private.Infrastructure.TestServices.WindowHelper.UseActualWindowRoot = true;
								Private.Infrastructure.TestServices.WindowHelper.SaveOriginalWindowContent();
							});
						}

						object returnValue = null;
						if (runsOnUIThread)
						{
							await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
							{
								sw.Start();
								testClassInfo.Initialize?.Invoke(instance, new object[0]);
								returnValue = testMethod.Invoke(instance, parameters);
								sw.Stop();
							});
						}
						else
						{
							sw.Start();
							testClassInfo.Initialize?.Invoke(instance, new object[0]);
							returnValue = testMethod.Invoke(instance, parameters);
							sw.Stop();
						}

							if (testMethod.ReturnType == typeof(Task))
							{
								var task = (Task)returnValue;
								var timeoutTask = Task.Delay(DefaultUnitTestTimeout);

								var resultingTask = await Task.WhenAny(task, timeoutTask);

								if (resultingTask == timeoutTask)
								{
									throw new TimeoutException(
										$"Test execution timed out after {DefaultUnitTestTimeout}");
								}

								if (resultingTask.Exception != null)
								{
									throw resultingTask.Exception;
								}
							}

							var console = testConsoleOutput?.GetContentAndReset();

							if (expectedException == null)
							{
								_currentRun.Succeeded++;
								ReportTestResult(fullTestName, sw.Elapsed, TestResult.Passed, console: console);
							}
							else
							{
								_currentRun.Failed++;
								ReportTestResult(fullTestName, sw.Elapsed, TestResult.Failed,
									message: $"Test did not throw the excepted exception of type {expectedException.ExceptionType.Name}",
									console: console);
							}
						}
						catch (Exception e)
						{
							sw.Stop();

							if (e is AggregateException agg)
							{
								e = agg.InnerExceptions.FirstOrDefault();
							}

							if (e is TargetInvocationException tie)
							{
								e = tie.InnerException;
							}

							var console = testConsoleOutput?.GetContentAndReset();

							if (e is AssertInconclusiveException inconclusiveException)
							{
								_currentRun.Ignored++;
								ReportTestResult(fullTestName, sw.Elapsed, TestResult.Skipped, message: e.Message, console: console);
							}
							else if (expectedException == null || !expectedException.ExceptionType.IsInstanceOfType(e))
							{
								_currentRun.Failed++;
								ReportTestResult(fullTestName, sw.Elapsed, TestResult.Failed, e, console: console);
							}
							else
							{
								_currentRun.Succeeded++;
								ReportTestResult(fullTestName, sw.Elapsed, TestResult.Passed, e, console: console);
							}
						}
					}

					try
					{
						testClassInfo.Cleanup?.Invoke(instance, new object[0]);
					}
					catch (Exception e)
					{
						_currentRun.Failed++;
						ReportTestResult(testName + " Cleanup", TimeSpan.Zero, TestResult.Failed, e, console: testConsoleOutput.GetContentAndReset());
					}

					if (ct.IsCancellationRequested)
					{
						_ = ReportMessage("Stopped by user.", false);
						return; // finish processing
					}
				}
			}
			finally
			{
				consoleRegistration?.Dispose();
			}
		}

		private bool HasCustomAttribute<T>(MemberInfo testMethod)
			=> testMethod.GetCustomAttribute(typeof(T)) != null;

		private bool IsIgnored(MethodInfo testMethod, out string ignoreMessage)
		{
			var ignoreAttribute = testMethod.GetCustomAttribute<IgnoreAttribute>();
			if (ignoreAttribute == null)
			{
				ignoreAttribute = testMethod.DeclaringType.GetCustomAttribute<IgnoreAttribute>();
			}

			if (ignoreAttribute != null)
			{
				ignoreMessage = string.IsNullOrEmpty(ignoreAttribute.IgnoreMessage) ? "Test is marked as ignored" : ignoreAttribute.IgnoreMessage;
				return true;
			}

			ignoreMessage = "";
			return false;
		}

		private IEnumerable<UnitTestClassInfo> InitializeTests()
		{
			var testAssembliesTypes =
				from asm in AppDomain.CurrentDomain.GetAssemblies()
				where asm.GetName().Name.EndsWith("tests", StringComparison.OrdinalIgnoreCase)
				from type in asm.GetTypes()
				select type;

			var types = GetType().GetTypeInfo().Assembly.GetTypes().Concat(testAssembliesTypes);
			var ts = types.Select(t => t.FullName).ToArray();

			return from type in types
				   where type.GetTypeInfo().GetCustomAttribute(typeof(TestClassAttribute)) != null
				   orderby type.Name
				   select BuildType(type);
		}

		private static UnitTestClassInfo BuildType(Type type)
		{
			try
			{
				return new UnitTestClassInfo(
					type: type,
					tests: GetMethodsWithAttribute(type, typeof(Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute)),
					initialize: GetMethodsWithAttribute(type, typeof(Microsoft.VisualStudio.TestTools.UnitTesting.TestInitializeAttribute)).FirstOrDefault(),
					cleanup: GetMethodsWithAttribute(type, typeof(Microsoft.VisualStudio.TestTools.UnitTesting.TestCleanupAttribute)).FirstOrDefault()
				);
			}
			catch (Exception)
			{
				return new UnitTestClassInfo(null, null, null, null);
			}
		}

		private static MethodInfo[] GetMethodsWithAttribute(Type type, Type attributeType)
			=> (
				from method in type.GetMethods()
				where method.GetCustomAttribute(attributeType) != null
				select method
			).ToArray();
	}
}
