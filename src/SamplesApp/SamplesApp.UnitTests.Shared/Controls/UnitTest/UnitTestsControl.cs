using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Uno.UI.Samples.Tests
{
	public sealed partial class UnitTestsControl : UserControl
	{
		private Task _runner;
		private CancellationTokenSource _cts = new CancellationTokenSource();
		private readonly TimeSpan DefaultUnitTestTimeout = TimeSpan.FromSeconds(60);

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
		}

		private void OnRunTests(object sender, RoutedEventArgs e)
		{
			_cts = new CancellationTokenSource();
			_runner = Task.Run(async () => await RunTests(_cts.Token));
		}

		private void ReportMessage(string message)
		{
			var t = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => runStatus.Text = message);
		}

		private void ReportRunTestCount(string message)
		{
			var t = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => runTestCount.Text = message);
		}

		private void ReportFailedTests(int failedCount)
		{
			void Update()
			{
				failedTests.Text = failedCount.ToString();
			}

			var t = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, Update);
		}

		private void ReportTestClass(TypeInfo testClass)
		{
			var t = Dispatcher.RunAsync(
				Windows.UI.Core.CoreDispatcherPriority.Normal,
				() =>
				{
					var testResult = new TextBlock()
					{
						Text = $"{testClass.Name} ({testClass.Assembly.GetName().Name})"
					};

					testResults.Children.Insert(0, testResult);
				}
			);
		}

		private void ReportTestResult(string testName, TestResult testResult, Exception error = null, string message = null)
		{
			void Update()
			{
				var testResultBlock = new TextBlock()
				{
					Text = testName,
					TextWrapping = TextWrapping.Wrap,
					FontFamily = new FontFamily("Courier New"),
					Foreground = new SolidColorBrush(GetTestResultColor(testResult))
				};

				if (message != null)
				{
					testResultBlock.Text += ", " + message;
				}

				if (error != null)
				{
					testResultBlock.Text += ", " + error.Message;

					if (testResult == TestResult.Failed || testResult == TestResult.Error)
					{
						failedTestDetails.Text += $"{testResult}: {testName} [{error.GetType()}] \n {error}\n\n";
					}
				}

				testResults.Children.Insert(0, testResultBlock);
			}

			var t = Dispatcher.RunAsync(
				Windows.UI.Core.CoreDispatcherPriority.Normal,
				Update);
		}

		private Color GetTestResultColor(TestResult testResult)
		{
			switch (testResult)
			{
				default:
				case TestResult.Error:
					return Colors.DarkRed;

				case TestResult.Failed:
					return Colors.Red;

				case TestResult.Ignored:
					return Colors.DarkOrange;

				case TestResult.Sucesss:
					return Colors.Green;
			}
		}

		private async Task RunTests(CancellationToken cts)
		{
			try
			{
				var failedTests = 0;
				var runTests = 0;

				ReportMessage("Enumerating tests");

				var testTypes = InitializeTests();

				ReportMessage("Running tests...");

				foreach (var type in testTypes.Where(t => t.type != null))
				{
					ReportTestClass(type.type.GetTypeInfo());
					ReportMessage($"Running {type.tests.Length}");

					var instance = Activator.CreateInstance(type: type.type);

					foreach (var testMethod in type.tests)
					{
						string testName = testMethod.Name;

						if (IsIgnored(testMethod, out var ignoreMessage))
						{
							ReportTestResult(testName, TestResult.Ignored, message: ignoreMessage);
							continue;
						}

						ReportMessage($"Running test {testName}");
						ReportRunTestCount($"Run tests: {++runTests}");

						var runsOnUIThread = testMethod.GetCustomAttribute(typeof(Uno.UI.RuntimeTests.RunsOnUIThreadAttribute)) != null;
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
							var fullTestName = $"{testName}({parameters.Select(p => p.ToString()).JoinBy(", ")})";
							try
							{
								type.init?.Invoke(instance, new object[0]);

								object returnValue = null;
								if (runsOnUIThread)
								{
									await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
									{
										returnValue = testMethod.Invoke(instance, parameters);
									});
								}
								else
								{
									returnValue = testMethod.Invoke(instance, parameters);
								}

								if (testMethod.ReturnType == typeof(Task))
								{
									var task = (Task)returnValue;
									var timeoutTask = Task.Delay(DefaultUnitTestTimeout);

									var resultingTask = await Task.WhenAny(task, timeoutTask);

									if (resultingTask == timeoutTask)
									{
										throw new TimeoutException($"Test execution timed out after {DefaultUnitTestTimeout}");
									}

									if (resultingTask.Exception != null)
									{
										throw resultingTask.Exception;
									}
								}
								ReportTestResult(fullTestName, TestResult.Sucesss);
							}
							catch (Exception e)
							{
								failedTests++;
								if (e is AggregateException agg)
								{
									e = agg.InnerExceptions.FirstOrDefault();
								}

								if (e is TargetInvocationException tie)
								{
									e = tie.InnerException;
								}

								ReportTestResult(fullTestName, TestResult.Failed, e);
							}

						}
						try
						{
							type.cleanup?.Invoke(instance, new object[0]);
						}
						catch (Exception e)
						{
							ReportTestResult(testName + " Cleanup", TestResult.Failed, e);
						}
					}
				}

				ReportMessage("Tests finished running.");
				ReportFailedTests(failedTests);
			}
			catch (Exception e)
			{
				ReportMessage($"Tests runner failed {e}");
				ReportFailedTests(-1);
			}
		}

		private bool IsIgnored(MethodInfo testMethod, out string ignoreMessage)
		{
			var ignoreAttribute = testMethod.GetCustomAttribute<Microsoft.VisualStudio.TestTools.UnitTesting.IgnoreAttribute>();

			if (ignoreAttribute != null)
			{
				ignoreMessage = string.IsNullOrEmpty(ignoreAttribute.IgnoreMessage) ? "Test is marked as ignored" : ignoreAttribute.IgnoreMessage;
				return true;
			}

			ignoreMessage = "";
			return false;
		}

		private IEnumerable<(Type type, MethodInfo[] tests, MethodInfo init, MethodInfo cleanup)> InitializeTests()
		{
			var testAssembliesTypes =
				from asm in AppDomain.CurrentDomain.GetAssemblies()
				where asm.GetName().Name.EndsWith("tests", StringComparison.OrdinalIgnoreCase)
				from type in asm.GetTypes()
				select type;

			var types = GetType().GetTypeInfo().Assembly.GetTypes().Concat(testAssembliesTypes);
			var ts = types.Select(t => t.FullName).ToArray();

			return from type in types
				   where type.GetTypeInfo().GetCustomAttribute(typeof(Microsoft.VisualStudio.TestTools.UnitTesting.TestClassAttribute)) != null
				   select BuildType(type);
		}

		private static (Type type, MethodInfo[] tests, MethodInfo initialize, MethodInfo cleanup) BuildType(Type type)
		{
			try
			{
				return (
					type: type,
					tests: GetMethodsWithAttribute(type, typeof(Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute)),
					initialize: GetMethodsWithAttribute(type, typeof(Microsoft.VisualStudio.TestTools.UnitTesting.TestInitializeAttribute)).FirstOrDefault(),
					cleanup: GetMethodsWithAttribute(type, typeof(Microsoft.VisualStudio.TestTools.UnitTesting.TestCleanupAttribute)).FirstOrDefault()
				);
			}
			catch (Exception)
			{
				return (null, null, null, null);
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
