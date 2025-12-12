using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SampleControl.Presentation;
using Uno.Disposables;
using Uno.Testing;
using Uno.UI.Samples.Helper;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Text;
using Windows.UI.ViewManagement;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Newtonsoft.Json;
using System.Text;
using System.Security.Cryptography;
using System.Collections.Immutable;
using Private.Infrastructure;

#if HAS_UNO
using Uno.Foundation.Logging;
#else
using Microsoft.Extensions.Logging;
using Uno.Logging;
#endif

namespace Uno.UI.Samples.Tests
{
	public sealed partial class UnitTestsControl : UserControl
	{
#pragma warning disable CS0109
#if HAS_UNO
		private new readonly Logger _log = Uno.Foundation.Logging.LogExtensionPoint.Log(typeof(UnitTestsControl));
#else
		private static readonly ILogger _log = Uno.Extensions.LogExtensionPoint.Log(typeof(UserControl));
#endif
#pragma warning restore CS0109

		private const StringComparison StrComp = StringComparison.InvariantCultureIgnoreCase;
		private Task _runner;
		private CancellationTokenSource _cts = new CancellationTokenSource();
#if DEBUG
		private readonly TimeSpan DefaultUnitTestTimeout = TimeSpan.FromSeconds(300);
#else
		private readonly TimeSpan DefaultUnitTestTimeout = TimeSpan.FromSeconds(60);
#endif

#if !WINAPPSDK
		private ApplicationView _applicationView;
#endif

		private List<TestCaseResult> _testCases = new();
		private TestRun _currentRun;

		// On WinUI/UWP dependency properties cannot be accessed outside of
		// UI thread. This field caches the current value so it can be accessed
		// asynchronously during test enumeration.
		private int _ciTestsGroupCountCache = -1;
		private int _ciTestGroupCache = -1;

		public UnitTestsControl()
		{
			// Force keep the Microsoft.VisualStudio.TestPlatform.TestFramework.Extensions assembly from mstest 3.6.3
			typeof(Microsoft.VisualStudio.TestTools.UnitTesting.TestContext).ToString();

#if DEBUG
			if (_ciTestsGroupCountCache != -1 || _ciTestGroupCache != -1)
			{
				throw new Exception("_ciTestsGroupCountCache or _ciTestGroupCache values are incorrect");
			}
#endif

			this.InitializeComponent();
			this.Loaded += OnLoaded;

			Private.Infrastructure.TestServices.WindowHelper.EmbeddedTestRoot =
			(
				control: unitTestContentRoot,
				getContent: () => unitTestContentRoot.Content as UIElement,
				setContent: elt =>
				{
					unitTestContentRoot.Content = elt;
				}
			);

			Private.Infrastructure.TestServices.WindowHelper.CurrentTestWindow ??=
				Microsoft.UI.Xaml.Window.Current;

			DataContext = null;

			SampleChooserViewModel.Instance.SampleChanging += OnSampleChanging;
			EnableConfigPersistence();
			OverrideDebugProviderAsserts();

#if !WINAPPSDK
			_applicationView = ApplicationView.GetForCurrentView();
#endif
		}

		private void OnLoaded(object sender, RoutedEventArgs args)
		{
			Private.Infrastructure.TestServices.WindowHelper.XamlRoot = XamlRoot;

			if (Private.Infrastructure.TestServices.WindowHelper.XamlRoot is null)
			{
				throw new InvalidOperationException("XamlRoot should not be null after Loaded");
			}

			Private.Infrastructure.TestServices.WindowHelper.IsXamlIsland =
#if HAS_UNO
				XamlRoot.HostWindow is null;
#else
				false;
#endif
		}

		private static void OverrideDebugProviderAsserts()
		{
#if NETSTANDARD2_0 || NET5_0_OR_GREATER
			if (Type.GetType("System.Diagnostics.DebugProvider") is { } type)
			{
				if (type.GetField("s_FailCore", BindingFlags.NonPublic | BindingFlags.Static) is { } fieldInfo)
				{
					fieldInfo.SetValue(null, (Action<string, string, string, string>)FailCore);
				}
			}
#endif
		}

		static void FailCore(string stackTrace, string message, string detailMessage, string errorSource)
			=> throw new Exception($"{message} ({detailMessage}) {stackTrace}");

		private void OnSampleChanging(object sender, EventArgs e)
		{
			StopRunningTests();
			SampleChooserViewModel.Instance.SampleChanging -= OnSampleChanging;
		}

		public bool IsRunningOnCI
		{
			get { return (bool)GetValue(IsRunningOnCIProperty); }
			set { SetValue(IsRunningOnCIProperty, value); }
		}

		// Using a DependencyProperty as the backing store for IsRunningOnCI.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty IsRunningOnCIProperty =
			DependencyProperty.Register("IsRunningOnCI", typeof(bool), typeof(UnitTestsControl), new PropertyMetadata(false));

		/// <summary>
		/// Defines the test group for splitting runtime tests on CI
		/// </summary>
		public int CITestGroup
		{
			get => (int)GetValue(CITestGroupProperty);
			set => SetValue(CITestGroupProperty, value);
		}

		public static readonly DependencyProperty CITestGroupProperty =
			DependencyProperty.Register("CITestGroup", typeof(int), typeof(UnitTestsControl), new PropertyMetadata(-1, OnCITestGroupChanged));

		private static void OnCITestGroupChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var unitTestsControl = (UnitTestsControl)d;
			unitTestsControl._ciTestGroupCache = (int)e.NewValue;
		}

		/// <summary>
		/// Defines the test group for splitting runtime tests on CI
		/// </summary>
		public int CITestGroupCount
		{
			get => (int)GetValue(CITestGroupCountProperty);
			set => SetValue(CITestGroupCountProperty, value);
		}

		public static readonly DependencyProperty CITestGroupCountProperty =
			DependencyProperty.Register("CITestGroupCount", typeof(int), typeof(UnitTestsControl), new PropertyMetadata(-1, OnCITestsGroupCountChanged));

		private static void OnCITestsGroupCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var unitTestsControl = (UnitTestsControl)d;
			unitTestsControl._ciTestsGroupCountCache = (int)e.NewValue;
		}

		public string CITestFilter
		{
			get { return (string)GetValue(CITestFilterProperty); }
			set { SetValue(CITestFilterProperty, value); }
		}

		// Using a DependencyProperty as the backing store for CITestFilter.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty CITestFilterProperty =
			DependencyProperty.Register("CITestFilter", typeof(string), typeof(UnitTestsControl), new PropertyMetadata(""));

		public string NUnitTestResultsDocument
		{
			get => (string)GetValue(NUnitTestResultsDocumentProperty);
			set => SetValue(NUnitTestResultsDocumentProperty, value);
		}

		public static readonly DependencyProperty NUnitTestResultsDocumentProperty =
			DependencyProperty.Register(nameof(NUnitTestResultsDocument), typeof(string), typeof(UnitTestsControl), new PropertyMetadata(string.Empty));

		/// <summary>
		/// Gets the unit tests runner status (Used by the Uno.UITests test side)
		/// </summary>
		public string RunningStateForUITest
		{
			get => (string)GetValue(RunningStateForUITestProperty);
			set => SetValue(RunningStateForUITestProperty, value);
		}

		public static readonly DependencyProperty RunningStateForUITestProperty =
			DependencyProperty.Register(nameof(RunningStateForUITest), typeof(string), typeof(UnitTestsControl), new PropertyMetadata("n/a"));

		/// <summary>
		/// Gets the unit tests that have run (Used by the Uno.UITests test side)
		/// </summary>
		public string RunTestCountForUITest
		{
			get => (string)GetValue(RunTestCountForUITestProperty);
			set => SetValue(RunTestCountForUITestProperty, value);
		}

		public static readonly DependencyProperty RunTestCountForUITestProperty =
			DependencyProperty.Register(nameof(RunTestCountForUITest), typeof(string), typeof(UnitTestsControl), new PropertyMetadata("-1"));

		/// <summary>
		/// Gets the unit tests that have failed (Used by the Uno.UITests test side)
		/// </summary>
		public string FailedTestCountForUITest
		{
			get => (string)GetValue(FailedTestCountForUITestProperty);
			set => SetValue(FailedTestCountForUITestProperty, value);
		}

		public static readonly DependencyProperty FailedTestCountForUITestProperty =
			DependencyProperty.Register(nameof(FailedTestCountForUITest), typeof(string), typeof(UnitTestsControl), new PropertyMetadata("-1"));

		private void OnRunTests(object sender, RoutedEventArgs e)
		{
			Interlocked.Exchange(ref _cts, new CancellationTokenSource())?.Cancel(); // cancel any previous CTS

			var config = BuildConfig();
			testResults.Children.Clear();

			_runner = Task.Run(() => RunTests(_cts.Token, config));
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
#if HAS_UNO
			_log?.Info(message);
#endif

			void Setter()
			{
				testFilter.IsEnabled = runButton.IsEnabled = !isRunning || _cts == null; // Disable the testFilter to avoid SIP to re-open

				if (IsRunningOnCI)
				{
					// Improves perf on CI by not re-rendering the whole test result live during tests
					testResults.Visibility = Visibility.Collapsed;
				}

				stopButton.IsEnabled = _cts != null && !_cts.IsCancellationRequested || !isRunning;
				RunningStateForUITest = runningState.Text = isRunning ? "Running" : "Finished";
				runStatus.Text = message;
				var windowTitle = $"{message} | {SampleChooserViewModel.DefaultAppTitle}";
#if HAS_UNO_WINUI || WINAPPSDK
				if (Private.Infrastructure.TestServices.WindowHelper.CurrentTestWindow is Microsoft.UI.Xaml.Window window)
				{
					window.Title = windowTitle;
				}
#else
				_applicationView.Title = windowTitle;
#endif
			}

			await TestServices.WindowHelper.RootElementDispatcher.RunAsync(Setter);
		}

		private void ReportTestsResults()
		{
			void Update()
			{
				RunTestCountForUITest = runTestCount.Text = _currentRun.Run.ToString();
				ignoredTestCount.Text = _currentRun.Ignored.ToString();
				inconclusiveTestCount.Text = _currentRun.Inconclusive.ToString();
				succeededTestCount.Text = _currentRun.Succeeded.ToString();
				FailedTestCountForUITest = failedTestCount.Text = _currentRun.Failed.ToString();
			}

			var t = TestServices.WindowHelper.RootElementDispatcher.RunAsync(Update);
		}

		private async Task GenerateTestResults()
		{
			void Update()
			{
				var results = GenerateNUnitTestResults(_testCases, _currentRun);

				NUnitTestResultsDocument = results;
			}

			await TestServices.WindowHelper.RootElementDispatcher.RunAsync(Update);
		}

		private void ReportTestClass(TypeInfo testClass)
		{
			TestServices.WindowHelper.RootElementDispatcher.RunAsync(
				() =>
				{
					if (!IsRunningOnCI)
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
				}
			);
		}

		private void ReportTestResult(string className, string methodName, string testName, TimeSpan duration, TestResult testResult, Exception error = null, string message = null, string console = null)
		{
			_testCases.Add(
				new TestCaseResult
				{
					ClassName = className,
					MethodName = methodName,
					TestName = testName,
					Duration = duration,
					TestResult = testResult,
					Message = error?.ToString() ?? message,
					ConsoleOutput = console,
				});

			void Update()
			{
				runTestCount.Text = _currentRun.Run.ToString();
				ignoredTestCount.Text = _currentRun.Ignored.ToString();
				inconclusiveTestCount.Text = _currentRun.Inconclusive.ToString();
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

				var retriesText = _currentRun.CurrentRepeatCount != 0 ? $" (Retried {_currentRun.CurrentRepeatCount} time(s))" : "";

				testResultBlock.Inlines.Add(new Run
				{
					Text = GetTestResultIcon(testResult) + ' ' + testName + retriesText,
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
						if (failedTestDetailsRow.Height.Value == 0)
						{
							failedTestDetailsRow.Height = new GridLength(100);
						}
					}
				}

				if (console is { })
				{
					testResultBlock.Inlines.Add(new Run { Text = "\nOUT>" + console, Foreground = new SolidColorBrush(Colors.Gray) });
				}

				if (!IsRunningOnCI)
				{
					testResults.Children.Add(testResultBlock);
					testResultBlock.StartBringIntoView();
				}

				if (testResult == TestResult.Error || testResult == TestResult.Failed)
				{
					failedTests.Text += "§" + testName;
				}
			}

			var t = TestServices.WindowHelper.RootElementDispatcher.RunAsync(
				Update);
		}

		private static string GenerateNUnitTestResults(List<TestCaseResult> testCases, TestRun testRun)
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
			rootNode.SetAttribute("inconclusive", testRun.Inconclusive.ToString());
			rootNode.SetAttribute("skipped", testRun.Ignored.ToString());
			rootNode.SetAttribute("asserts", "0");

			var now = DateTimeOffset.Now;
			rootNode.SetAttribute("run-date", now.ToString("yyyy-MM-dd"));
			rootNode.SetAttribute("start-time", testRun.StartTime.ToString("HH:mm:ss"));
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
			testSuiteFixtureNode.SetAttribute("inconclusive", testRun.Inconclusive.ToString());
			testSuiteFixtureNode.SetAttribute("skipped", testRun.Ignored.ToString());
			testSuiteFixtureNode.SetAttribute("asserts", "0");

			foreach (var run in testCases)
			{
				var testCaseNode = doc.CreateElement("test-case");
				testSuiteFixtureNode.AppendChild(testCaseNode);

				testCaseNode.SetAttribute("name", run.TestName);
				testCaseNode.SetAttribute("fullname", run.FullName);
				if (run.ClassName is not null)
				{
					testCaseNode.SetAttribute("classname", run.ClassName);
				}
				if (run.MethodName is not null)
				{
					testCaseNode.SetAttribute("methodname", run.MethodName);
				}
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

				if (run.ConsoleOutput is not null)
				{
					var outputNode = doc.CreateElement("output");
					testCaseNode.AppendChild(outputNode);
					outputNode.InnerText = run.ConsoleOutput;
				}
			}

			using var w = new StringWriter();
			doc.Save(w);

			return w.ToString();
		}

		private void EnableConfigPersistence()
		{
			if (ApplicationData.Current.LocalSettings.Values.TryGetValue("unitestcontrols_config", out var configRaw)
				&& configRaw is string configStr)
			{
				try
				{
					var config = JsonConvert.DeserializeObject<UnitTestEngineConfig>(configStr);

					consoleOutput.IsChecked = config.IsConsoleOutputEnabled;
					runIgnored.IsChecked = config.IsRunningIgnored;
					retry.IsChecked = config.Attempts > 1;
					testFilter.Text = string.Join(";", config.Filters);
				}
				catch (Exception e)
				{
					_log.Error("Failed to restore runtime tests config", e);
				}
			}

			ListenConfigChanged();
		}

		private void ListenConfigChanged()
		{
			consoleOutput.Checked += (snd, e) => StoreConfig();
			consoleOutput.Unchecked += (snd, e) => StoreConfig();
			runIgnored.Checked += (snd, e) => StoreConfig();
			runIgnored.Unchecked += (snd, e) => StoreConfig();
			retry.Checked += (snd, e) => StoreConfig();
			retry.Unchecked += (snd, e) => StoreConfig();
			testFilter.TextChanged += (snd, e) => StoreConfig();

			void StoreConfig()
			{
				var config = BuildConfig();
				ApplicationData.Current.LocalSettings.Values["unitestcontrols_config"] = JsonConvert.SerializeObject(config);
			}
		}

		private UnitTestEngineConfig BuildConfig()
		{
			var isConsoleOutput = consoleOutput.IsChecked ?? false;
			var isRunningIgnored = runIgnored.IsChecked ?? false;
			var attempts = (retry.IsChecked ?? true) ? UnitTestEngineConfig.DefaultRepeatCount : 1;
			var filter = testFilter.Text.Trim();
			if (string.IsNullOrEmpty(filter))
			{
				filter = null;
			}

			return new UnitTestEngineConfig
			{
				Filters = filter?.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>(),
				IsConsoleOutputEnabled = isConsoleOutput,
				IsRunningIgnored = isRunningIgnored,
				Attempts = attempts,
			};
		}

		private string GetTestResultIcon(TestResult testResult)
		{
			switch (testResult)
			{
				default:
				case TestResult.Error:
				case TestResult.Failed:
					return "\uE711";

				case TestResult.Skipped:
					return "\uEE35";

				case TestResult.Passed:
					return "\uE73E";
				case TestResult.Inconclusive:
					return "?";
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
					return Colors.DarkGray;

				case TestResult.Passed:
					return Colors.LightGreen;

				case TestResult.Inconclusive:
					return Colors.DarkViolet;
			}
		}

		public async Task RunTestsForInstance(object testClassInstance)
		{
			Interlocked.Exchange(ref _cts, new CancellationTokenSource())?.Cancel(); // cancel any previous CTS

			testResults.Children.Clear();

			try
			{
				try
				{
					var testTypeInfo = BuildType(testClassInstance.GetType());
					var engineConfig = BuildConfig();

					await ExecuteTestsForInstance(_cts.Token, testClassInstance, testTypeInfo, engineConfig);
				}
				catch (Exception e)
				{
					_currentRun.Failed = -1;
					_ = ReportMessage($"Tests runner failed {e}");
					ReportTestResult(null, null, "Runtime exception", TimeSpan.Zero, TestResult.Failed, e);
					ReportTestsResults();
				}
			}
			finally
			{
				await TestServices.WindowHelper.RootElementDispatcher.RunAsync(() =>
				{
					testFilter.IsEnabled = runButton.IsEnabled = true; // Disable the testFilter to avoid SIP to re-open
					testResults.Visibility = Visibility.Visible;
					stopButton.IsEnabled = false;
				});
			}
		}

		public async Task RunTests(CancellationToken ct, UnitTestEngineConfig config)
		{
			_currentRun = new TestRun();

			try
			{
				_ = ReportMessage("Enumerating tests");

				var testTypes = InitializeTests();

				_ = ReportMessage($"Running tests ({testTypes.Count()} fixtures)...");

				foreach (var type in testTypes)
				{
					if (ct.IsCancellationRequested)
					{
						_ = ReportMessage("Stopped by user.", false);
						break;
					}

					var instance = Activator.CreateInstance(type: type.Type);

					await ExecuteTestsForInstance(ct, instance, type, config);
				}

				_ = ReportMessage("Tests finished running.", isRunning: false);
				ReportTestsResults();
			}
			catch (Exception e)
			{
				_currentRun.Failed = -1;
				_ = ReportMessage($"Tests runner failed {e}");
				ReportTestResult(null, null, "Runtime exception", TimeSpan.Zero, TestResult.Failed, e);
				ReportTestsResults();
			}
			finally
			{
				await TestServices.WindowHelper.RootElementDispatcher.RunAsync(() =>
				{
					testFilter.IsEnabled = runButton.IsEnabled = true; // Disable the testFilter to avoid SIP to re-open
					if (!IsRunningOnCI)
					{
						testResults.Visibility = Visibility.Visible;
					}
					stopButton.IsEnabled = false;
				});
			}

			await GenerateTestResults();
		}

		private IEnumerable<MethodInfo> FilterTests(UnitTestClassInfo testClassInfo, string[] filters)
		{
			var testClassNameContainsFilters = filters?.Any(f => testClassInfo.Type.FullName.Contains(f, StrComp)) ?? false;
			return testClassInfo.Tests
				.Where(t => !(filters?.Any() ?? false)
					|| testClassNameContainsFilters
					|| filters.Any(f => t.DeclaringType.FullName.Contains(f, StrComp))
					|| filters.Any(f => t.Name.Contains(f, StrComp)));
		}

		private async Task ExecuteTestsForInstance(
			CancellationToken ct,
			object instance,
			UnitTestClassInfo testClassInfo,
			UnitTestEngineConfig config)
		{
			using var consoleRecorder = config.IsConsoleOutputEnabled
				? ConsoleOutputRecorder.Start()
				: default;

			var tests = FilterTests(testClassInfo, config.Filters)
				.Select(method => new UnitTestMethodInfo(instance, method))
				.ToArray();
			if (!tests.Any())
			{
				return;
			}

			ReportTestClass(testClassInfo.Type.GetTypeInfo());
			_ = ReportMessage($"Running {tests.Length} test methods");

			foreach (var test in tests)
			{
				var testName = test.Name;

				if (ct.IsCancellationRequested)
				{
					_ = ReportMessage("Stopped by user.", false);
					return;
				}

				if (test.IsIgnored(out var ignoreMessage))
				{
					if (config.IsRunningIgnored)
					{
						ignoreMessage = $"\n--> [Ignored] IS BYPASSED...";
					}

					_currentRun.Ignored++;
					ReportTestResult(testClassInfo.TestClassName, test.Method.Name, testName, TimeSpan.Zero, TestResult.Skipped, message: ignoreMessage);

					if (!config.IsRunningIgnored)
					{
						continue;
					}
				}

				foreach (var testCase in test.GetCases())
				{
					if (ct.IsCancellationRequested)
					{
						_ = ReportMessage("Stopped by user.", false);
						return;
					}

					await InvokeTestMethod(testCase);
				}

				async Task InvokeTestMethod(TestCase testCase)
				{
					var fullTestName = testName + testCase.ToString();

					_currentRun.Run++;
					_currentRun.CurrentRepeatCount = 0;

					// We await this to make sure the UI is updated before running the test.
					// This will help developpers to identify faulty tests when the app is crashing.
					await ReportMessage($"Running test {fullTestName}");
					ReportTestsResults();

					var sw = new Stopwatch();
					var canRetry = true;

					while (canRetry)
					{
						canRetry = false;
						var cleanupActions = new List<Func<Task>>
						{
							GeneralCleanupAsync
						};

						try
						{
							if (test.RequiresFullWindow)
							{
								await TestServices.WindowHelper.RootElementDispatcher.RunAsync(() =>
								{
#if __ANDROID__
									// Hide the systray!
									ApplicationView.GetForCurrentView().TryEnterFullScreenMode();
#endif

									Private.Infrastructure.TestServices.WindowHelper.UseActualWindowRoot = true;
									Private.Infrastructure.TestServices.WindowHelper.SaveOriginalWindowContent();
								});
								cleanupActions.Add(async () =>
								{
									await TestServices.WindowHelper.RootElementDispatcher.RunAsync(() =>
									{
#if __ANDROID__
										// Restore the systray!
										ApplicationView.GetForCurrentView().ExitFullScreenMode();
#endif
										Private.Infrastructure.TestServices.WindowHelper.RestoreOriginalWindowContent();
										Private.Infrastructure.TestServices.WindowHelper.UseActualWindowRoot = false;
									});
								});
							}

#if HAS_UNO // Test scaling override is currently supported only on Uno Platform targets
							if (test.RequiresScaling is not null)
							{
								await TestServices.WindowHelper.RootElementDispatcher.RunAsync(() =>
								{
									Private.Infrastructure.TestServices.WindowHelper.SetTestScaling(test.RequiresScaling.Value);
								});
								cleanupActions.Add(async () =>
								{
									await TestServices.WindowHelper.RootElementDispatcher.RunAsync(() =>
									{
										Private.Infrastructure.TestServices.WindowHelper.UnsetTestScaling();
									});
								});
							}
#endif

							await GeneralInitAsync();

							object returnValue = null;
							var methodArguments = testCase.Parameters;
							if (test.PassFiltersAsFirstParameter)
							{
								var configFilters = config.Filters ??= Array.Empty<string>();
								methodArguments = methodArguments.ToImmutableArray().Insert(0, string.Join(";", configFilters)).ToArray();
							}
							if (test.RunsOnUIThread)
							{
								var cts = new TaskCompletionSource<bool>();

								_ = TestServices.WindowHelper.RootElementDispatcher.RunAsync(async () =>
								{
									try
									{
										if (instance is IInjectPointers pointersInjector)
										{
											pointersInjector.CleanupPointers();
										}

										if (testCase.Pointer is { } pt)
										{
											var ptSubscription = (instance as IInjectPointers ?? throw new InvalidOperationException("test class does not supports pointer selection.")).SetPointer(pt);

											cleanupActions.Add(() =>
											{
												ptSubscription.Dispose();
												return Task.CompletedTask;
											});
										}

										sw.Start();
										var initializeReturn = testClassInfo.Initialize?.Invoke(instance, Array.Empty<object>());
										if (initializeReturn is Task initializeReturnTask)
										{
											await initializeReturnTask;
										}

										returnValue = InvokeMethod(test.Method, instance, methodArguments);

										sw.Stop();

										cts.TrySetResult(true);
									}
									catch (Exception e)
									{
										cts.TrySetException(e);
									}
								});

								await cts.Task;
							}
							else
							{
								if (testCase.Pointer is { } pt)
								{
									var ptSubscription = (instance as IInjectPointers ?? throw new InvalidOperationException("test class does not supports pointer selection.")).SetPointer(pt);
									cleanupActions.Add(() =>
									{
										ptSubscription.Dispose();
										return Task.CompletedTask;
									});
								}

								sw.Start();

								var initializeReturn = testClassInfo.Initialize?.Invoke(instance, Array.Empty<object>());
								if (initializeReturn is Task initializeReturnTask)
								{
									await initializeReturnTask;
								}

								returnValue = InvokeMethod(test.Method, instance, methodArguments);
								sw.Stop();
							}

							if (test.Method.ReturnType == typeof(Task))
							{
								var task = (Task)returnValue;
								var timeout = GetTestTimeout(test);
								var timeoutTask = Task.Delay(timeout);

								var resultingTask = await Task.WhenAny(task, timeoutTask);

								if (resultingTask == timeoutTask)
								{
									throw new TimeoutException(
										$"Test execution timed out after {timeout}");
								}

								// Rethrow exception if failed OR task cancelled if task **internally** raised
								// a TaskCancelledException (we don't provide any cancellation token).
								await resultingTask;
							}

							var console = consoleRecorder?.GetContentAndReset();

							if (test.ExpectedException is null)
							{
								_currentRun.Succeeded++;
								ReportTestResult(testClassInfo.TestClassName, test.Method.Name, fullTestName, sw.Elapsed, TestResult.Passed, console: console);
							}
							else
							{
								_currentRun.Failed++;
								ReportTestResult(testClassInfo.TestClassName, test.Method.Name, fullTestName, sw.Elapsed, TestResult.Failed,
									message: $"Test did not throw the excepted exception of type {test.ExpectedException.Name}",
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

							var console = consoleRecorder?.GetContentAndReset();

							if (e is AssertInconclusiveException inconclusiveException)
							{
								_currentRun.Inconclusive++;
								ReportTestResult(testClassInfo.TestClassName, test.Method.Name, fullTestName, sw.Elapsed, TestResult.Inconclusive, message: e.Message, console: console);
							}
							else if (test.ExpectedException is null || !test.ExpectedException.IsInstanceOfType(e))
							{
								if (_currentRun.CurrentRepeatCount < config.Attempts - 1)
								{
									_currentRun.CurrentRepeatCount++;
									canRetry = true;

									await RunCleanup(instance, testClassInfo, test, testName, test.RunsOnUIThread);
								}
								else
								{
									_currentRun.Failed++;
									ReportTestResult(testClassInfo.TestClassName, test.Method.Name, fullTestName, sw.Elapsed, TestResult.Failed, e, console: console);
								}
							}
							else
							{
								_currentRun.Succeeded++;
								ReportTestResult(testClassInfo.TestClassName, test.Method.Name, fullTestName, sw.Elapsed, TestResult.Passed, e, console: console);
							}
						}
						finally
						{
							foreach (var cleanup in cleanupActions)
							{
								await cleanup();
							}
						}
					}
				}

				await RunCleanup(instance, testClassInfo, test, testName, test.RunsOnUIThread);

				if (ct.IsCancellationRequested)
				{
					_ = ReportMessage("Stopped by user.", false);
					return; // finish processing
				}
			}

			async Task GeneralInitAsync()
			{
#if HAS_UNO
				await TestServices.WindowHelper.RootElementDispatcher.RunAsync(() =>
				{
					ResetLastInputDeviceType();
				});
#else
				await Task.CompletedTask;
#endif
			}

			async Task GeneralCleanupAsync()
			{
				await TestServices.WindowHelper.RootElementDispatcher.RunAsync(() =>
				{
					CloseRemainingPopups();

				});
			}

			async Task RunCleanup(object instance, UnitTestClassInfo testClassInfo, UnitTestMethodInfo testMethod, string testName, bool runsOnUIThread)
			{
				async Task Run()
				{
					try
					{
						await WaitResult(testClassInfo.Cleanup?.Invoke(instance, Array.Empty<object>()), "cleanup");
					}
					catch (Exception e)
					{
						_currentRun.Failed++;
						ReportTestResult(testClassInfo.TestClassName, testMethod.Method.Name, testName + " Cleanup", TimeSpan.Zero, TestResult.Failed, e, console: consoleRecorder?.GetContentAndReset());
					}
				}

				if (runsOnUIThread)
				{
					await ExecuteOnDispatcher(Run, CancellationToken.None); // No CT for cleanup!
				}
				else
				{
					await Run();
				}
			}

			async ValueTask WaitResult(object returnValue, string step)
			{
				if (returnValue is Task asyncResult)
				{
					var timeoutTask = Task.Delay(DefaultUnitTestTimeout, ct);
					var resultingTask = await Task.WhenAny(asyncResult, timeoutTask);

					if (resultingTask == timeoutTask)
					{
						throw new TimeoutException($"Test {step} timed out after {DefaultUnitTestTimeout}");
					}

					// Rethrow exception if failed OR task cancelled if task **internally** raised
					// a TaskCancelledException (we don't provide any cancellation token).
					await resultingTask;
				}
			}
		}

		private static void CloseRemainingPopups()
		{
			var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(TestServices.WindowHelper.XamlRoot);
			if (popups.Count > 0)
			{
				foreach (var popup in popups)
				{
					popup.IsOpen = false;
				}
			}
		}

#if HAS_UNO
		private static void ResetLastInputDeviceType()
		{
			// Some tests inject keyboard input, which can then mean that subsequent tests will display
			// system focus visuals which are unexpected. This resets the last input device type to mouse.
			// Mouse is to stay in line with WinUI integration tests, as some rely on the fact that on
			// initial focus of a TextBox the last input device type is not touch (this device type does not
			// select all text on initial focus, only on second tap of the input).
			// If this is changed ComboBoxIntegrationTests.ValidateTextSubmittedHandledProperty will probably
			// start to fail for example.
			if (TestServices.WindowHelper.XamlRoot?.VisualTree?.ContentRoot?.InputManager is { } inputManager)
			{
				inputManager.LastInputDeviceType = Xaml.Input.InputDeviceType.Mouse;
			}
		}
#endif

		private async ValueTask ExecuteOnDispatcher(Func<Task> asyncAction, CancellationToken ct = default)
		{
			var tcs = new TaskCompletionSource<object>();
			await TestServices.WindowHelper.RootElementDispatcher.RunAsync(async () =>
			{
				try
				{
					if (ct.IsCancellationRequested)
					{
						tcs.TrySetCanceled();
					}

					using var ctReg = ct.Register(() => tcs.TrySetCanceled());
					await asyncAction();

					tcs.TrySetResult(default);
				}
				catch (Exception e)
				{
					tcs.TrySetException(e);
				}
			});

			await tcs.Task;
		}

		static string GetTestMethodPrototype(MethodInfo method, ParameterInfo[] parameters)
			=> $"{method.DeclaringType?.FullName ?? "[unknown]"}.{method.Name}(" +
			string.Join(", ", parameters.Select(p => p.ParameterType.FullName)) +
			")";

		static object InvokeMethod(MethodInfo method, object instance, object[] methodArguments)
		{
			var methodParameters = method.GetParameters();
			if (methodParameters.Length > methodArguments.Length)
			{
				methodArguments = ExpandArgumentsWithDefaultValues(method, methodArguments, methodParameters);
			}

			try
			{
				return method.Invoke(instance, methodArguments);
			}
			catch (Exception e)
			{
				if (e is TargetInvocationException tie &&
						tie.InnerException is UnitTestAssertException)
				{
					throw;
				}

				string message =
					$"Exception thrown while invoking {GetTestMethodPrototype(method, methodParameters)} " +
					$"with arguments {{ {string.Join(", ", methodArguments.Select(a => PrintValue(a)))} }}.";

#if RUNTIME_NATIVE_AOT
				if (e is TargetParameterCountException tpce &&
						(methodParameters ?? method.GetParameters()).Length == methodArguments.Length)
				{
					Console.WriteLine($"# jonp: {message}");
					Console.WriteLine($"# jonp: {e.ToString()}");
					// This makes NO sense, and is seen under NativeAOT; "ignore" the test for now.
					throw new AssertInconclusiveException(message, e);
				}
#endif  // RUNTIME_NATIVE_AOT

				throw new InvalidOperationException(message, e);
			}
		}

		static string PrintValue(object value)
		{
			if (value is object[] array)
			{
				return $"{value.GetType().FullName}{{ {string.Join(", ", array.Select(a => PrintValue(a)))} }}";
			}
			return $"{value?.ToString() ?? "null"} as {value?.GetType().FullName ?? "null"}";
		}

		private static object[] ExpandArgumentsWithDefaultValues(MethodInfo testMethod, object[] methodArguments, ParameterInfo[] methodParameters)
		{
			var expandedArguments = new List<object>(methodParameters.Length);
			for (int i = 0; i < methodArguments.Length; i++)
			{
				// HACK to try to work around "extra array wrapping" under NativeAOT
				var arg = methodArguments[i];
				if (arg is object[] nestedArray &&
						!typeof(object[]).IsAssignableFrom(methodParameters[i].ParameterType))
				{
					expandedArguments.AddRange(nestedArray);
				}
				else
				{
					expandedArguments.Add(arg);
				}
			}
			// Try to get default values for the rest
			for (int i = expandedArguments.Count; i < methodParameters.Length; i++)
			{
				var parameter = methodParameters[i];
				if (!parameter.HasDefaultValue)
				{
					var message =
						$"Missing default parameter value: parameter {methodArguments.Length + i} in test method " +
						GetTestMethodPrototype(testMethod, methodParameters) +
						" does not have a default value. Current arguments: " +
						$"`{PrintValue(expandedArguments.ToArray())}`.";
					Console.WriteLine($"# jonp: {message}");
					throw new InvalidOperationException(message);
				}
				else
				{
					expandedArguments.Add(parameter.DefaultValue);
				}
			}

			return expandedArguments.ToArray();
		}

		private TimeSpan GetTestTimeout(UnitTestMethodInfo test)
		{
			if (test.Method.GetCustomAttribute(typeof(TimeoutAttribute)) is TimeoutAttribute methodAttribute)
			{
				return TimeSpan.FromMilliseconds(methodAttribute.Timeout);
			}

			if (test.Method.DeclaringType.GetCustomAttribute(typeof(TimeoutAttribute)) is TimeoutAttribute typeAttribute)
			{
				return TimeSpan.FromMilliseconds(typeAttribute.Timeout);
			}

			return DefaultUnitTestTimeout;
		}

		private IEnumerable<UnitTestClassInfo> InitializeTests()
		{
			var testAssembliesTypes =
				from asm in AppDomain.CurrentDomain.GetAssemblies()
				where asm.GetName().Name.EndsWith("tests", StringComparison.OrdinalIgnoreCase)
				from type in asm.GetTypes()
				select type;

			var types = GetType().GetTypeInfo().Assembly.GetTypes().Concat(testAssembliesTypes);

			if (_ciTestsGroupCountCache != -1)
			{
#if !DEBUG && HAS_UNO
				this.Log().Info($"Filtered groups summary for {_ciTestsGroupCountCache} groups:");

				var totalCount = 0;
				Dictionary<int, MethodInfo[]> filteredGroups = new();

				for (int i = 0; i < _ciTestsGroupCountCache; i++)
				{
					var testGroup = GetFilteredTests(types, _ciTestsGroupCountCache, i);
					var tests = testGroup.SelectMany(t => t.Tests);
					var testCount = tests.Count();
					totalCount += testCount;

					filteredGroups.Add(i, [.. tests]);

					this.Log().Info($"Filtered group {i}: {testCount} tests");
				}

				// Ensure that tests are not present in multiple groups
				var allTests = filteredGroups.SelectMany(t => t.Value).ToArray();
				var allTestsCount = allTests.Length;
				var distinctTestsCount = allTests.Distinct().Count();

				if (allTestsCount != distinctTestsCount)
				{
					throw new Exception($"Test filter inconsistent (Got {allTestsCount}, expected {distinctTestsCount})");
				}

				var unfilteredTestCount = GetFilteredTests(types, -1, -1).SelectMany(t => t.Tests).Count();

				if (totalCount != unfilteredTestCount)
				{
					throw new Exception($"Test filter inconsistent (Got {totalCount}, expected {unfilteredTestCount})");
				}

				this.Log().Info($"Filtering with group #{_ciTestGroupCache}");
#endif

				Console.WriteLine($"Filtering with group #{_ciTestGroupCache}");
			}

			var groupedList = GetFilteredTests(types, _ciTestsGroupCountCache, _ciTestGroupCache);

			return groupedList.ToArray();
		}

		private IEnumerable<UnitTestClassInfo> GetFilteredTests(IEnumerable<Type> types, int groupCount, int activeGroup)
		{
			var testClasses =
				from type in types
				where type.GetTypeInfo().GetCustomAttribute(typeof(TestClassAttribute)) != null
				orderby type.Name
				select type;

			var groupedList =
				from type in testClasses
				from test in GetMethodsWithAttribute(type, typeof(Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute)).OrderBy(m => m.Name)
				where groupCount == -1 || (groupCount != -1 && (GetTypeTestGroup(test) % (ulong)groupCount) == (ulong)activeGroup)
				group test by type into g
				where g.Count() != 0
				select BuildTestClassInfo(g.Key, g.ToArray());
			return groupedList;
		}

		private static SHA1 _sha1 = SHA1.Create();

		private ulong GetTypeTestGroup(MethodInfo method)
		{
			// Compute a stable hash of the full metadata name
			var buffer = Encoding.UTF8.GetBytes(method.DeclaringType.FullName + "." + method.Name);
			var hash = _sha1.ComputeHash(buffer);

			return BitConverter.ToUInt64(hash, 0);
		}

		private static UnitTestClassInfo BuildTestClassInfo(Type type, MethodInfo[] tests)
		{
			try
			{
				return new UnitTestClassInfo(
					type: type,
					tests: tests,
					initialize: GetMethodsWithAttribute(type, typeof(Microsoft.VisualStudio.TestTools.UnitTesting.TestInitializeAttribute)).FirstOrDefault(),
					cleanup: GetMethodsWithAttribute(type, typeof(Microsoft.VisualStudio.TestTools.UnitTesting.TestCleanupAttribute)).FirstOrDefault()
				);
			}
			catch (Exception)
			{
				return new UnitTestClassInfo(null, null, null, null);
			}
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

		private void UpdateFailedTestDetailsSize(object sender, ManipulationDeltaRoutedEventArgs e)
			=> failedTestDetailsRow.Height = new GridLength(Math.Max(0, failedTestDetailsRow.ActualHeight + e.Delta.Translation.Y));

		private void UpdateOuputSize(object sender, ManipulationDeltaRoutedEventArgs e)
			=> outputColumn.Width = new GridLength(Math.Max(0, outputColumn.ActualWidth + e.Delta.Translation.X));

		private void CopyFailedTestDetails(object sender, RoutedEventArgs e)
		{
			var data = new DataPackage();
			data.SetText(failedTestDetails.Text);

			Clipboard.SetContent(data);
		}

		private void CopyTestResults(object sender, RoutedEventArgs e)
		{
			var data = new DataPackage();
			data.SetText(NUnitTestResultsDocument);

			Clipboard.SetContent(data);
		}
	}
}
