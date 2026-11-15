// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference controls/test/testinfra/MUXTestInfra/Infra/TestEnvironment.cs, commit fee1e8acc

using System;
using System.Threading.Tasks;
using Common;
using Microsoft.UI.Xaml.Tests.MUXControls.InteractionTests.Common;
using Microsoft.Windows.Apps.Test.Foundation;
using Microsoft.Windows.Apps.Test.Foundation.Waiters;

namespace Microsoft.UI.Xaml.Tests.MUXControls.InteractionTests.Infra
{
	// Uno: the test app is the current process, so there is nothing to install, launch or restart.
	public class TestEnvironment
	{
		// Uno: the runner doesn't call [ClassInitialize], so there is never a context to keep.
		public static TestContext TestContext => null;

		public static bool IsLogVerbose { get; set; }

		public static bool IsLogSuperVerbose { get; set; }

		public static Application Application { get; } = new Application();

		public static bool ShouldRestartApplication { get; set; }

		public static void LogVerbose(string format, params object[] args)
		{
			if (IsLogVerbose)
			{
				Log.Comment(format, args);
			}
		}

		public static void LogSuperVerbose(string format, params object[] args)
		{
			if (IsLogSuperVerbose)
			{
				Log.Comment(format, args);
			}
		}

		public static void Initialize(TestContext testContext)
		{
		}

		public static void LogDumpTree(UIObject root = null)
		{
			Log.Comment("============ Dump Tree ============");
			LogDumpTreeWorker("", root ?? Application.CoreWindow);
			Log.Comment("===================================");
		}

		// Uno: element lookups are synchronous, so a missing element fails right away instead of polling.
		public static void WaitUntilElementLoadedById(string automationId)
		{
			WaitUntilElementLoaded(TryFindElement.ById(automationId), automationId);
		}

		public static void WaitUntilElementLoadedByName(string name)
		{
			WaitUntilElementLoaded(TryFindElement.ByName(name), name);
		}

		public static void WaitUntilElementLoadedByNameOrId(string nameOrId)
		{
			WaitUntilElementLoaded(TryFindElement.ByNameOrId(nameOrId), nameOrId);
		}

		public static void WaitUntilElementLoadedByClassName(string className)
		{
			WaitUntilElementLoaded(TryFindElement.ByClassName(className), className);
		}

		private static void WaitUntilElementLoaded(UIObject element, string key)
		{
			if (element == null)
			{
				throw new WaiterTimedOutException(string.Format("Could not find '{0}'!", key));
			}
		}

		private static void LogDumpTreeWorker(string indent, UIObject current)
		{
			Log.Comment(indent + current.GetDescription());
			indent += "  ";
			foreach (var uiObject in current.Children)
			{
				LogDumpTreeWorker(indent, uiObject);
			}
		}

		public static async Task VerifyAreEqualWithRetry(int maxRetries, Func<object> expectedFunc, Func<object> actualFunc, Func<Task> retryAction = null)
		{
			if (retryAction == null)
			{
				retryAction = async () =>
				{
					await Task.Delay(TimeSpan.FromMilliseconds(50));
					ElementCache.Clear(); /* Test is flaky sometimes -- perhaps element cache is stale? Clear it and try again. */
				};
			}

			for (int retry = 0; retry <= maxRetries; retry++)
			{
				object expected = expectedFunc();
				object actual = actualFunc();
				if (Object.Equals(expected, actual) || retry == maxRetries)
				{
					Log.Comment("Actual retry times: " + retry);
					Verify.AreEqual(expected, actual);
					return;
				}
				else
				{
					await retryAction();
				}
			}
		}
	}

	public class Application
	{
		public UIObject ApplicationFrameWindow => null;

		public UIObject CoreWindow => UIObject.Root;

		public Task GoBack() => TestSetupHelper.GoBack();
	}
}
