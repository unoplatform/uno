using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Samples.Helper;
using Uno.Extensions;
using Uno.UI.RuntimeTests;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

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

		private enum TestResult
		{
			Sucesss,
			Failed,
			Error,
			Ignored,
		}

		public UnitTestsControl()
		{
			this.InitializeComponent();

			Private.Infrastructure.TestServices.WindowHelper.RootControl = unitTestContentRoot;

			DataContext = null;

			Unloaded += (snd, evt) => StopRunningTests();
		}

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

		private void ReportTestsResults(UnitTestCounters counters)
		{
			void Update()
			{
				runTestCount.Text = counters.Run.ToString();
				ignoredTestCount.Text = counters.Ignored.ToString();
				succeededTestCount.Text = counters.Succeeded.ToString();
				failedTestCount.Text = counters.Failed.ToString();
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

		private void ReportTestResult(string testName, TestResult testResult, UnitTestCounters counters, Exception error = null, string message = null)
		{
			void Update()
			{
				runTestCount.Text = counters.Run.ToString();
				ignoredTestCount.Text = counters.Ignored.ToString();
				succeededTestCount.Text = counters.Succeeded.ToString();
				failedTestCount.Text = counters.Failed.ToString();

				var testResultBlock = new TextBlock()
				{
					TextWrapping = TextWrapping.Wrap,
					FontFamily = new FontFamily("Courier New"),
					Margin = ThicknessHelper2.FromLengths(8, 0, 0, 0),
					Foreground = new SolidColorBrush(Colors.LightGray)
				};

				testResultBlock.Inlines.Add(new Run
				{
					Text = GetTestResultIcon(testResult) + ' ' + testName,
					FontSize = 13.5d,
					Foreground = new SolidColorBrush(GetTestResultColor(testResult)),
					FontWeight = FontWeights.ExtraBold
				});

				if (message != null)
				{
					testResultBlock.Inlines.Add(new Run { Text = "\n  ..." + message, FontStyle = FontStyle.Italic });
				}

				if (error != null)
				{
					var isFailed = testResult == TestResult.Failed || testResult == TestResult.Error;

					var foreground = isFailed ? new SolidColorBrush(Colors.Red) : new SolidColorBrush(Colors.Yellow);
					testResultBlock.Inlines.Add(new Run { Text = "\nEXCEPTION>" + error.Message, Foreground = foreground });

					if (isFailed)
					{
						failedTestDetails.Text += $"{testResult}: {testName} [{error.GetType()}] \n {error}\n\n";
					}
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

		private string GetTestResultIcon(TestResult testResult)
		{
			switch (testResult)
			{
				default:
				case TestResult.Error:
				case TestResult.Failed:
					return "❌ (F)";

				case TestResult.Ignored:
					return "🚫 (I)";

				case TestResult.Sucesss:
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

				case TestResult.Ignored:
					return Colors.Orange;

				case TestResult.Sucesss:
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
				var counters = new UnitTestCounters();
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

					await ExecuteTestsForInstance(_cts.Token, testClassInstance, counters, testTypeInfo.Tests, testTypeInfo);
				}
				catch (Exception e)
				{
					counters.Failed = -1;
					_ = ReportMessage($"Tests runner failed {e}");
					ReportTestResult("Runtime exception", TestResult.Failed, counters, e);
					ReportTestsResults(counters);
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
			var counters = new UnitTestCounters();

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

					await ExecuteTestsForInstance(ct, instance, counters, tests, type);
				}

				_ = ReportMessage("Tests finished running.", isRunning: false);
				ReportTestsResults(counters);
			}
			catch (Exception e)
			{
				counters.Failed = -1;
				_ = ReportMessage($"Tests runner failed {e}");
				ReportTestResult("Runtime exception", TestResult.Failed, counters, e);
				ReportTestsResults(counters);
			}
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

		private async Task ExecuteTestsForInstance(
			CancellationToken ct,
			object instance,
			UnitTestCounters counters,
			MethodInfo[] tests,
			UnitTestClassInfo testClassInfo)
		{
			foreach (var testMethod in tests)
			{
				string testName = testMethod.Name;

				if (IsIgnored(testMethod, out var ignoreMessage))
				{
					counters.Ignored++;
					ReportTestResult(testName, TestResult.Ignored, counters, message: ignoreMessage);
					continue;
				}

				var runsOnUIThread = HasCustomAttribute<RunsOnUIThreadAttribute>(testMethod)
									 || HasCustomAttribute<RunsOnUIThreadAttribute>(testMethod.DeclaringType);
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

					counters.Run++;
					// We await this to make sure the UI is updated before running the test.
					// This will help developpers to identify faulty tests when the app is crashing.
					await ReportMessage($"Running test {fullTestName}");
					ReportTestsResults(counters);

					try
					{
						object returnValue = null;
						if (runsOnUIThread)
						{
							await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
							{
								testClassInfo.Initialize?.Invoke(instance, new object[0]);
								returnValue = testMethod.Invoke(instance, parameters);
							});
						}
						else
						{
							testClassInfo.Initialize?.Invoke(instance, new object[0]);
							returnValue = testMethod.Invoke(instance, parameters);
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

						if (expectedException == null)
						{
							counters.Succeeded++;
							ReportTestResult(fullTestName, TestResult.Sucesss, counters);
						}
						else
						{
							counters.Failed++;
							ReportTestResult(fullTestName, TestResult.Failed, counters,
								message:
								$"Test did not throw the excepted exception of type {expectedException.ExceptionType.Name}");
						}
					}
					catch (Exception e)
					{
						if (e is AggregateException agg)
						{
							e = agg.InnerExceptions.FirstOrDefault();
						}

						if (e is TargetInvocationException tie)
						{
							e = tie.InnerException;
						}

						if (e is AssertInconclusiveException inconclusiveException)
						{
							counters.Ignored++;
							ReportTestResult(fullTestName, TestResult.Ignored, counters, message: e.Message);
						}
						else if (expectedException == null || !expectedException.ExceptionType.IsInstanceOfType(e))
						{
							counters.Failed++;
							ReportTestResult(fullTestName, TestResult.Failed, counters, e);
						}
						else
						{
							counters.Succeeded++;
							ReportTestResult(fullTestName, TestResult.Sucesss, counters, e);
						}
					}
				}

				try
				{
					testClassInfo.Cleanup?.Invoke(instance, new object[0]);
				}
				catch (Exception e)
				{
					counters.Failed++;
					ReportTestResult(testName + " Cleanup", TestResult.Failed, counters, e);
				}

				if (ct.IsCancellationRequested)
				{
					_ = ReportMessage("Stopped by user.", false);
					return; // finish processing
				}
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
