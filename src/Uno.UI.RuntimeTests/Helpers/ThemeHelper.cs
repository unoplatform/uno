#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Microsoft.UI.Xaml;
#if HAS_UNO
using Uno.Helpers.Theming;
#else
using Uno.Disposables;
#endif

namespace Uno.UI.RuntimeTests.Helpers
{
	public static class ThemeHelper
	{
		/// <summary>
		/// Ensure dark theme is applied for the course of a single test.
		/// </summary>
		public static IDisposable UseDarkTheme()
		{
			var root = TestServices.WindowHelper.XamlRoot.Content as FrameworkElement;
			Assert.IsNotNull(root);
			var currentTheme = root.RequestedTheme;
			root.RequestedTheme = ElementTheme.Dark;

			return new DisposableAction(() =>
			{
				root.RequestedTheme = currentTheme;
			});
		}

		/// <summary>
		/// Ensure dark theme is applied at the application level for the course of a single test.
		/// Unlike <see cref="UseDarkTheme"/> which sets element-level theme on the content root,
		/// this changes the application-level theme affecting all windows and root elements.
		/// </summary>
		/// <remarks>
		/// On WinUI the application theme cannot be switched at runtime, so the test runs as-is when the app is
		/// already in this theme and is otherwise reported inconclusive (re-run after switching the OS theme).
		/// </remarks>
		public static IDisposable UseApplicationDarkTheme()
		{
#if HAS_UNO
			AssertFullWindowForApplicationTheme();
			var originalTheme = Application.Current.RequestedTheme;
			var wasExplicit = Application.Current.IsThemeSetExplicitly;
			Application.Current.SetExplicitRequestedTheme(ApplicationTheme.Dark);

			return new DisposableAction(() =>
			{
				if (wasExplicit)
				{
					Application.Current.SetExplicitRequestedTheme(originalTheme);
				}
				else
				{
					Application.Current.SetExplicitRequestedTheme(null);
				}
			});
#else
			return UseApplicationThemeOnWinUI(ApplicationTheme.Dark);
#endif
		}

		/// <summary>
		/// Ensure light theme is applied at the application level for the course of a single test.
		/// </summary>
		/// <remarks>
		/// On WinUI the application theme cannot be switched at runtime, so the test runs as-is when the app is
		/// already in this theme and is otherwise reported inconclusive (re-run after switching the OS theme).
		/// </remarks>
		public static IDisposable UseApplicationLightTheme()
		{
#if HAS_UNO
			AssertFullWindowForApplicationTheme();
			var originalTheme = Application.Current.RequestedTheme;
			var wasExplicit = Application.Current.IsThemeSetExplicitly;
			Application.Current.SetExplicitRequestedTheme(ApplicationTheme.Light);

			return new DisposableAction(() =>
			{
				if (wasExplicit)
				{
					Application.Current.SetExplicitRequestedTheme(originalTheme);
				}
				else
				{
					Application.Current.SetExplicitRequestedTheme(null);
				}
			});
#else
			return UseApplicationThemeOnWinUI(ApplicationTheme.Light);
#endif
		}

#if HAS_UNO
		// The SamplesApp test runner pins RequestedTheme=Light on its outer Frame
		// (App.Tests.cs HandleRuntimeTests). When tests run in the embedded host
		// (UseActualWindowRoot=false), that Frame sits between the application root
		// and the test content, so app-level theme changes cannot reach the test
		// subtree. [RequiresFullWindow] reparents to the actual window root and
		// bypasses the Frame.
		private static void AssertFullWindowForApplicationTheme()
		{
			if (!TestServices.WindowHelper.UseActualWindowRoot)
			{
				Assert.Fail(
					"ThemeHelper.UseApplicationDarkTheme/UseApplicationLightTheme require " +
					"[RequiresFullWindow] on the test method. Without it, the SamplesApp " +
					"runtime-test Frame (RequestedTheme=Light, set in App.Tests.cs) blocks " +
					"app-level theme propagation to the test content.");
			}
		}
#endif

#if !HAS_UNO
		// WinUI cannot switch the application theme at runtime (Application.RequestedTheme is fixed
		// after launch). A theming test that needs a specific application theme can still run on WinUI
		// when the app already happens to be in that theme; otherwise it is reported inconclusive so it
		// can be re-run after switching the OS theme (which drives the WinUI application theme).
		private static IDisposable UseApplicationThemeOnWinUI(ApplicationTheme requestedTheme)
		{
			var currentTheme = Application.Current.RequestedTheme;
			if (currentTheme != requestedTheme)
			{
				Assert.Inconclusive(
					$"This test requires the {requestedTheme} application theme, but WinUI is currently " +
					$"running in {currentTheme}. WinUI cannot switch the application theme at runtime — " +
					$"switch the OS theme to {requestedTheme} and run the test again.");
			}

			// Nothing was changed, so disposal is a no-op.
			return Disposable.Empty;
		}
#endif

		/// <summary>
		/// Overrides the ambient OS/system theme for the duration of a single test, independently of the
		/// real machine OS theme (analogous to <c>ScaleOverride</c>), so theming tests are deterministic on
		/// any developer machine — not only when the OS happens to be Light. On Uno the application follows
		/// the overridden theme exactly as it would a real OS change (unless the app theme was set
		/// explicitly), so the process-global ambient that ungoverned <c>{ThemeResource}</c> resolution
		/// falls back to (<c>ResourceDictionary.GetActiveTheme()</c>) reflects it. Restores the previous
		/// value on dispose, raising the change again so observers re-sync.
		/// </summary>
		/// <remarks>
		/// Overriding the OS theme has no WinUI equivalent. On WinUI this instead runs the test as-is when the
		/// application is already in <paramref name="systemTheme"/>, and otherwise reports it inconclusive so it
		/// can be re-run after switching the OS theme. Typical use:
		/// pin a Dark ambient while theming a subtree Light at the element level to reproduce the
		/// "OS dark, app light, materialized element leaks dark" defect deterministically; re-invoking with a
		/// different theme (or disposing) exercises a runtime OS-theme change. A test whose content must
		/// <em>follow</em> the overridden app theme (no element-level <c>RequestedTheme</c>) also needs
		/// <c>[RequiresFullWindow]</c> to escape the runtime-test host Frame.
		/// </remarks>
		public static IDisposable UseSystemThemeOverride(ApplicationTheme systemTheme)
		{
#if HAS_UNO
			var previous = SystemThemeHelper.SystemThemeOverride;
			SystemThemeHelper.SystemThemeOverride =
				systemTheme == ApplicationTheme.Dark ? SystemTheme.Dark : SystemTheme.Light;

			return new DisposableAction(() => SystemThemeHelper.SystemThemeOverride = previous);
#else
			return UseApplicationThemeOnWinUI(systemTheme);
#endif
		}

		public static ElementTheme CurrentTheme
		{
			get
			{
				var root = TestServices.WindowHelper.XamlRoot.Content as FrameworkElement;
				Assert.IsNotNull(root);
				return root.ActualTheme;
			}
		}
	}
}
