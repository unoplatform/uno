using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
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

		public UnitTestsControl()
		{
			this.InitializeComponent();

			DataContext = this;
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

		private void ReportFailedTests(int failedCount)
		{
			var t = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => failedTests.Text = failedCount.ToString());
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

		private void ReportTestResult(string testName, bool failed, string errorMessage = "")
		{
			var t = Dispatcher.RunAsync(
				Windows.UI.Core.CoreDispatcherPriority.Normal,
				() =>
				{
					var testResult = new TextBlock()
					{
						Text = testName,
						FontFamily = new FontFamily("Courier New"),
						Foreground = new SolidColorBrush(failed ? Colors.Red : Colors.Green)
					};

					if (errorMessage.HasValue())
					{
						testResult.Text += ", " + errorMessage;
					}

					testResults.Children.Insert(0, testResult);
				}
			);
		}

		private async Task RunTests(CancellationToken cts)
		{
			try
			{
				int failedTests = 0;

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

						ReportMessage($"Running test {testName}");

						try
						{
							type.init?.Invoke(instance, new object[0]);

							var returnValue = testMethod.Invoke(instance, new object[0]);

							if (testMethod.ReturnType == typeof(Task))
							{
								var task = (Task)returnValue;
								await task;
							}

							ReportTestResult(testName, false);
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

							ReportTestResult(testName, true, e.Message);
						}
						try
						{
							type.cleanup?.Invoke(instance, new object[0]);
						}
						catch (Exception e)
						{
							ReportTestResult(testName + " Cleanup", true, e.Message);
						}
					}
				}

				ReportMessage("Tests finished running.");
				ReportFailedTests(failedTests);
			}
			catch(Exception e)
			{
				ReportMessage($"Tests runner failed {e}");
				ReportFailedTests(-1);
			}
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
