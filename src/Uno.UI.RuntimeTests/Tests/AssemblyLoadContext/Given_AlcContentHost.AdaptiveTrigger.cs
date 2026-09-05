#if HAS_UNO
#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;
using Windows.Foundation;

namespace Uno.UI.RuntimeTests.Tests.AssemblyLoadContext;

// AdaptiveTrigger ALC-scoped window-size override coverage in the real hosted-app topology: the
// guest page's triggers live inside the HOST's visual tree (shared XamlRoot), so these tests
// validate that a scoped override resolves through ancestor type identity, not through the tree
// root. Isolated storage/leak semantics are unit-tested in Given_AdaptiveTrigger.
public partial class Given_AlcContentHost
{
	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23721")]
	public async Task When_ScopedWindowSizeOverride_Then_AppliesToGuestTriggersOnly()
	{
		var contentHost = await StartSecondaryAlcAppAsync();
		var probe = GetAdaptiveStateProbe(contentHost);

		Assert.AreEqual(Visibility.Collapsed, probe.Visibility,
			"Pre-condition: the guest adaptive state must be inactive under the real window size");

		// A host-side trigger in the same visual tree, owned by host-typed elements only.
		var hostState = new VisualState { Name = "hostSimulatedWide" };
		hostState.StateTriggers.Add(new AdaptiveTrigger { MinWindowWidth = 4000 });
		var hostGroup = new VisualStateGroup();
		hostGroup.States.Add(hostState);
		VisualStateManager.SetVisualStateGroups(contentHost, new List<VisualStateGroup> { hostGroup });
		Assert.IsNull(hostGroup.CurrentState, "Pre-condition: the host trigger must be inactive under the real window size");

		try
		{
			AdaptiveTrigger.SetWindowSizeOverride(new Size(5000, 5000), _testAlc!);
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual(Visibility.Visible, probe.Visibility,
				"The scoped override must activate the guest's adaptive state");
			Assert.IsNull(hostGroup.CurrentState,
				"The scoped override must not affect triggers owned by host-typed elements");

			AdaptiveTrigger.SetWindowSizeOverride(null, _testAlc!);
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual(Visibility.Collapsed, probe.Visibility,
				"Clearing the scoped override must revert the guest to the real window size");
		}
		finally
		{
			AdaptiveTrigger.SetWindowSizeOverride(null, _testAlc!);
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23721")]
	public async Task When_ScopedAndGlobalWindowSizeOverride_Then_ScopedWinsForGuest()
	{
		var contentHost = await StartSecondaryAlcAppAsync();
		var probe = GetAdaptiveStateProbe(contentHost);

		try
		{
			// Without a scoped override the guest follows the global one…
			AdaptiveTrigger.SetWindowSizeOverride(new Size(5000, 5000));
			await TestServices.WindowHelper.WaitForIdle();
			Assert.AreEqual(Visibility.Visible, probe.Visibility,
				"The global override must apply to guest triggers when no scoped override exists");

			// …a scoped override then wins over the global one…
			AdaptiveTrigger.SetWindowSizeOverride(new Size(100, 100), _testAlc!);
			await TestServices.WindowHelper.WaitForIdle();
			Assert.AreEqual(Visibility.Collapsed, probe.Visibility,
				"A scoped override must take precedence over the global override for guest triggers");

			// …and clearing the scoped override falls back to the global one.
			AdaptiveTrigger.SetWindowSizeOverride(null, _testAlc!);
			await TestServices.WindowHelper.WaitForIdle();
			Assert.AreEqual(Visibility.Visible, probe.Visibility,
				"Clearing the scoped override must fall back to the global override");
		}
		finally
		{
			AdaptiveTrigger.SetWindowSizeOverride(null, _testAlc!);
			AdaptiveTrigger.SetWindowSizeOverride(null);
		}
	}

	private static TextBlock GetAdaptiveStateProbe(Uno.UI.Xaml.Controls.AlcContentHost contentHost)
	{
		var root = contentHost.Content as FrameworkElement;
		Assert.IsNotNull(root, "Secondary content should be a FrameworkElement");

		var probe = root!.FindName("AdaptiveStateProbe") as TextBlock;
		Assert.IsNotNull(probe, "AdaptiveStateProbe should be discoverable via FindName");

		return probe!;
	}
}
#endif
