// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference controls/test/testinfra/MUXTestInfra/Common/WaitHelper.cs, commit fee1e8acc

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Common;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Tests.MUXControls.InteractionTests.Infra;
using Microsoft.Windows.Apps.Test.Foundation.Waiters;
using Private.Infrastructure;

namespace Microsoft.UI.Xaml.Tests.MUXControls.InteractionTests.Common
{
	// Uno: the test shares the UI thread with the app, so waiting yields to it instead of polling an idle checkbox through UIA.
	public class Wait
	{
		// keep call retryEvalFunc until retryEvalFunc returns true or timeout
		public static async Task RetryUntilEvalFuncSuccessOrTimeout(Func<bool> retryEvalFunc, int retryTimoutByMilliseconds = 500)
		{
			uint timeElapsedByMilliseconds = 0;
			uint waitTimeByMilliseconds = 100;

			while (!retryEvalFunc())
			{
				if (timeElapsedByMilliseconds >= retryTimoutByMilliseconds)
				{
					Log.Warning("RetryUntilSuccessOrTimeout timeout after milliseconds " + timeElapsedByMilliseconds);
					return;
				}
				await ForMilliseconds(waitTimeByMilliseconds);
				timeElapsedByMilliseconds += waitTimeByMilliseconds;
			}
		}

		public static Task ForSeconds(uint time)
		{
			return ForMilliseconds(time * 1000);
		}

		public static Task ForMilliseconds(uint time)
		{
			return new TimeWaiter(TimeSpan.FromMilliseconds(time)).Wait();
		}

		public static async Task ForIdle(bool findElementsIfNull = true)
		{
			TestEnvironment.LogVerbose("Wait.ForIdle: Begin");
			await TestServices.WindowHelper.WaitForIdle();

			// Like the test app's IdleSynchronizer, idle also means no finite animation is still running.
			var stopwatch = Stopwatch.StartNew();
			while (HasActiveFiniteAnimations())
			{
				if (stopwatch.Elapsed > AnimationsCompleteTimeout)
				{
					Log.Warning("Wait.ForIdle: animations did not complete within {0}.", AnimationsCompleteTimeout);
					break;
				}

				await Task.Delay(16);
				await TestServices.WindowHelper.WaitForIdle();
			}

			TestEnvironment.LogVerbose("Wait.ForIdle: End");
		}

		private static readonly TimeSpan AnimationsCompleteTimeout = TimeSpan.FromSeconds(5);

		private static bool HasActiveFiniteAnimations()
		{
#if __SKIA__
			return Storyboard.ActiveFiniteCount > 0
				|| (TestServices.WindowHelper.WindowContent?.Visual?.Compositor.IsAnimating ?? false);
#else
			return false;
#endif
		}

		internal static void ForLayout()
		{
			TestServices.WindowHelper.WindowContent?.UpdateLayout();
		}
	}
}
