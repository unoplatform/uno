// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference controls/test/testinfra/MUXTestInfra/Infra/TestHelpers.cs, commit fee1e8acc

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Tests.MUXControls.InteractionTests.Common;
using Microsoft.Windows.Apps.Test.Foundation;
using Microsoft.Windows.Apps.Test.Foundation.Patterns;
using Private.Infrastructure;

namespace Microsoft.UI.Xaml.Tests.MUXControls.InteractionTests.Infra
{
	// Uno: upstream drives a separate MUXControlsTestApp process. Here the pages are hosted in a Frame in the
	// test window, the constructor becomes the awaitable Create() and Dispose becomes DisposeAsync.
	public class TestSetupHelper : IAsyncDisposable
	{
		private int OpenedTestPages = 0;

		// Tests that fail will automatically get a screenshot taken when run in the ADO pipeline. We have to leave the
		// test in the broken state and not clean up after it so that the screenshot is meaningful. This flag lets a
		// test control whether or not to clean up and only clean up if the test passes.
		private bool _doCleanup = true;

		public class TestSetupHelperOptions
		{
			public string LanguageOverride { get; set; }

			public string ClassNameOfNavigationItemToInvoke { get; set; } = "Button";

			public string AutomationIdOfSafeItemToClick { get; set; } = "__CurrentPage";

			public bool AutomaticCleanup { get; set; } = true;
		}

		public static Task<TestSetupHelper> Create(string testName, TestSetupHelperOptions options = null)
			=> Create(new[] { testName }, options);

		// The value of 'testName' should match the Name of a page's [TopLevelTestPage] (suffixed with " Tests"),
		// the Name of its [AxeScanTestPage], or the automation name of a navigation button on the current page.
		public static async Task<TestSetupHelper> Create(
			ICollection<string> testNames,
			TestSetupHelperOptions options = null,
			bool shouldRestrictInnerFrameSize = true,
			string outputDebugStringLevel = "None",
			ICollection<string> outputDebugStringComponentTypes = null)
		{
			var helper = new TestSetupHelper(options, outputDebugStringLevel, outputDebugStringComponentTypes);
			await helper.Initialize(testNames);
			return helper;
		}

		protected TestSetupHelper(
			TestSetupHelperOptions options,
			string outputDebugStringLevel = "None",
			ICollection<string> outputDebugStringComponentTypes = null)
		{
			Options = options ?? new TestSetupHelperOptions();
			OutputDebugStringLevel = outputDebugStringLevel;
			OutputDebugStringComponentTypes = outputDebugStringComponentTypes;
			_doCleanup = Options.AutomaticCleanup;
		}

		// Custom sub-classes lile ItemsViewTestSetupHelper can turn on/off debug tracing based on this level which can be "None", "Info" or "Verbose".
		protected string OutputDebugStringLevel { get; private set; }

		// Custom sub-classes lile ItemsViewTestSetupHelper can turn on debug tracing for the type names in this collection ("ItemsView", "ItemsRepeater", "ScrollView", etc...).
		protected ICollection<string> OutputDebugStringComponentTypes { get; private set; }

		private TestSetupHelperOptions Options { get; set; }

		protected async Task Initialize(ICollection<string> testNames)
		{
			ElementCache.Clear();
			InteractionInput.Reset();
			await Wait.ForIdle();

			foreach (string testName in testNames)
			{
				Log.Comment(testName + " initializing TestSetupHelper");

				var frame = TestPageHost.Frame;
				object previousContent = frame?.Content;

				if (TestPageRegistry.TryGetPageType(testName, out var pageType))
				{
					frame = TestPageHost.EnsureCreated();
					TestPageHost.Navigate(pageType);
				}
				else
				{
					UIObject uiObject = String.IsNullOrEmpty(Options.ClassNameOfNavigationItemToInvoke)
						? TryFindElement.ByName(testName)
						: TryFindElement.ByNameAndClassName(testName, Options.ClassNameOfNavigationItemToInvoke);

					if (uiObject == null)
					{
						DumpHelper.DumpFullContext();
						throw new InvalidOperationException(string.Format("Cannot find test page for: {0}.", testName));
					}

					// We're now entering a new test page, so everything has changed.  As such, we should clear our
					// element cache in order to ensure that we don't accidentally retrieve any stale UI objects.
					ElementCache.Clear();

					var testButton = new InvokeImplementation(uiObject);
					var testSelectionItem = new SelectionItemImplementation<UIObject>(uiObject, UIObject.Factory);

					if (testButton.IsAvailable)
					{
						testButton.Invoke();
					}
					else if (testSelectionItem.IsAvailable)
					{
						testSelectionItem.Select();
					}
					else
					{
						uiObject.Click();
					}
				}

				await TestPageHost.WaitForPageLoaded(previousContent);

				Log.Comment("Test page has loaded");

				OpenedTestPages++;

				TestPageLoaded(testName);
			}

			TestCleanupHelper.TestSetupHelperPendingDisposals++;
		}

		public void AllowCleanup()
		{
			_doCleanup = true;
		}

		public void ClearOpenedPages()
		{
			OpenedTestPages = 0;
		}

		public static async Task SetInnerFrameFlowDirection(FlowDirection flowDirection)
		{
			if (TestPageHost.Frame is { } frame && frame.FlowDirection != flowDirection)
			{
				Log.Comment("Setting the inner frame flow direction to {0}.", flowDirection);
				frame.FlowDirection = flowDirection;
				await Wait.ForIdle();
			}
		}

		public static async Task GoBack()
		{
			// Once a test has completed, we'll go back to the main page to get ready for the next test,
			// if there is one.
			TestPageHost.GoBack();
			ElementCache.Clear();
			await Wait.ForIdle();
		}

		// In order to allow multiple consecutive runs of the same test every TestSetupHelper *must* be disposed
		// to go back, so that the next test starts from a clean state.
		public async ValueTask DisposeAsync()
		{
			await DisposeAsyncCore();
			GC.SuppressFinalize(this);
		}

		// Let custom sub-classes like ItemsViewTestSetupHelper perform custom actions after each page was loaded.
		protected virtual void TestPageLoaded(string testName)
		{
		}

		protected virtual async ValueTask DisposeAsyncCore()
		{
			Log.Comment("TestSetupHelper.Dispose()");
			TestCleanupHelper.TestSetupHelperPendingDisposals--;
			InteractionInput.Reset();

			if (_doCleanup)
			{
				while (OpenedTestPages > 0)
				{
					await GoBack();
					OpenedTestPages--;
				}

				if (TestCleanupHelper.TestSetupHelperPendingDisposals == 0)
				{
					TestPageHost.Close();
				}
			}
			else
			{
				Log.Comment("> Skipping going back due to test failure.");
			}
		}
	}

	public class TestCleanupHelper
	{
		public static int TestSetupHelperPendingDisposals { get; set; }

		public static void Cleanup()
		{
			Log.Comment("Cleaning up test...");

			if (TestSetupHelperPendingDisposals > 0)
			{
				Log.Error("A TestSetupHelper was not disposed!  Make sure that all uses of TestSetupHelper are wrapped in an await using () statement.");
				TestSetupHelperPendingDisposals = 0;
			}

			InteractionInput.Reset();
			TestPageHost.Close();

			Log.Comment("Test cleanup complete.");
		}
	}

	public static class DumpHelper
	{
		public static void DumpFullContext()
		{
			Log.Comment("Dumping UIA tree...");
			TestEnvironment.LogDumpTree();
			Log.Comment("Dumping element cache...");
			ElementCache.Dump();
		}
	}

	// Uno: stands in for MUXControlsTestApp's MainPage and inner frame.
	internal static class TestPageHost
	{
		private static Grid _root;

		public static Frame Frame { get; private set; }

		public static Frame EnsureCreated()
		{
			if (Frame is null || !ReferenceEquals(TestServices.WindowHelper.WindowContent, _root))
			{
				Frame = new Frame();
				_root = new Grid { Children = { Frame } };
				TestServices.WindowHelper.WindowContent = _root;
			}

			return Frame;
		}

		public static void Navigate(Type pageType)
		{
			if (typeof(Page).IsAssignableFrom(pageType))
			{
				Frame.Navigate(pageType);
			}
			else
			{
				Frame.Navigate(typeof(TestPageContainer));
				((Page)Frame.Content).Content = (UIElement)Activator.CreateInstance(pageType);
			}
		}

		public static async Task WaitForPageLoaded(object previousContent)
		{
			await TestServices.WindowHelper.WaitFor(
				() => Frame?.Content is FrameworkElement page && !ReferenceEquals(page, previousContent) && page.IsLoaded,
				timeoutMS: 10000,
				message: "Timed out waiting for the test page to load.");
			await TestServices.WindowHelper.WaitForIdle();
		}

		public static void GoBack()
		{
			if (Frame is null)
			{
				return;
			}

			if (Frame.CanGoBack)
			{
				Frame.GoBack();
			}
			else
			{
				Frame.Content = null;
			}
		}

		public static void Close()
		{
			if (_root is not null && ReferenceEquals(TestServices.WindowHelper.WindowContent, _root))
			{
				TestServices.WindowHelper.WindowContent = null;
			}

			_root = null;
			Frame = null;
		}
	}

	// Hosts test pages that are not a Page (several Uno samples are UserControls) so the Frame can navigate to them.
	internal sealed partial class TestPageContainer : Page
	{
	}
}
