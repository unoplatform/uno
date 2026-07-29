using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Loader;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions;

namespace Uno.UI.Tests.Windows_UI_Xaml
{
	[TestClass]
	public class Given_AdaptiveTrigger
	{
		private readonly List<AssemblyLoadContext> _scopedAlcs = new();

		[TestInitialize]
		public void Initialize()
		{
			UnitTestsApp.App.EnsureApplication();
		}

		[TestCleanup]
		public void Cleanup()
		{
			AdaptiveTrigger.SetWindowSizeOverride(null);

			foreach (var alc in _scopedAlcs)
			{
				AdaptiveTrigger.SetWindowSizeOverride(null, alc);
				try
				{
					alc.Unload();
				}
				catch (InvalidOperationException)
				{
					// Already unloaded by the test itself.
				}
			}

			_scopedAlcs.Clear();
		}

		[TestMethod]
		public void When_SingleActiveState()
		{
			Window.Current.SetWindowSize(new Size(100, 100));

			var border = new Border();
			border.ForceLoaded();

			var sut = new AdaptiveTrigger { MinWindowHeight = 10, MinWindowWidth = 10 };

			var state = new VisualState { Name = "activeState" };
			state.StateTriggers.Add(sut);

			var group = new VisualStateGroup();
			group.States.Add(state);

			VisualStateManager.SetVisualStateGroups(border, new List<VisualStateGroup>() { group });

			group.CurrentState.Should().Be(state);

			Window.Current.SetWindowSize(new Size(1, 1));
			group.CurrentState.Should().Be(null);
		}

		[TestMethod]
		public void When_SingleActiveState_ExactValue()
		{
			Window.Current.SetWindowSize(new Size(100d, 100d));
			var border = new Border();
			border.ForceLoaded();

			var sut = new AdaptiveTrigger { MinWindowHeight = 100d, MinWindowWidth = 100d };

			var state = new VisualState { Name = "activeState" };
			state.StateTriggers.Add(sut);

			var group = new VisualStateGroup();
			VisualStateManager.SetVisualStateGroups(border, new List<VisualStateGroup>() { group });

			group.States.Add(state);

			group.CurrentState.Should().Be(state);
		}

		[TestMethod]
		public void When_SingleInactiveState()
		{
			Window.Current.SetWindowSize(new Size(5, 5));

			var border = new Border();
			border.ForceLoaded();

			var sut = new AdaptiveTrigger { MinWindowHeight = 10, MinWindowWidth = 10 };

			var state = new VisualState { Name = "activeState" };
			state.StateTriggers.Add(sut);

			var group = new VisualStateGroup();
			group.States.Add(state);

			VisualStateManager.SetVisualStateGroups(border, new List<VisualStateGroup>() { group });

			group.CurrentState.Should().Be(null);

			Window.Current.SetWindowSize(new Size(15, 15));
			group.CurrentState.Should().Be(state);
		}

		[TestMethod]
		public void When_SingleActiveState_DefaultValue()
		{
			Window.Current.SetWindowSize(new Size(100, 100));

			var border = new Border();
			border.ForceLoaded();

			var sut = new AdaptiveTrigger { MinWindowWidth = 0 };

			var state = new VisualState { Name = "activeState" };
			state.StateTriggers.Add(sut);

			var group = new VisualStateGroup();
			group.States.Add(state);

			VisualStateManager.SetVisualStateGroups(border, new List<VisualStateGroup>() { group });

			group.CurrentState.Should().Be(state);
		}

		[TestMethod]
		public void When_SingleWithTwoConstraints_FailingWidth()
		{
			Window.Current.SetWindowSize(new Size(100, 100));

			var border = new Border();

			var sut = new AdaptiveTrigger { MinWindowWidth = 101, MinWindowHeight = 42 };

			var state = new VisualState { Name = "activeState" };
			state.StateTriggers.Add(sut);

			var group = new VisualStateGroup();
			group.States.Add(state);

			VisualStateManager.SetVisualStateGroups(border, new List<VisualStateGroup>() { group });
			border.ForceLoaded();

			group.CurrentState.Should().Be(null);
		}

		[TestMethod]
		public void When_SingleWithTwoConstraints_FailingHeight()
		{
			Window.Current.SetWindowSize(new Size(100, 100));
			var border = new Border();

			var sut = new AdaptiveTrigger { MinWindowWidth = 42, MinWindowHeight = 101 };

			border.ForceLoaded();

			var state = new VisualState { Name = "activeState" };
			state.StateTriggers.Add(sut);

			var group = new VisualStateGroup();
			group.States.Add(state);

			VisualStateManager.SetVisualStateGroups(border, new List<VisualStateGroup>() { group });

			group.CurrentState.Should().Be(null);
		}

		[TestMethod]
		public void When_SingleNoConstraints()
		{
			Window.Current.SetWindowSize(new Size(100, 100));
			var border = new Border();
			border.ForceLoaded();

			var sut = new AdaptiveTrigger { MinWindowWidth = 0, MinWindowHeight = 0 };

			var state = new VisualState { Name = "activeState" };
			state.StateTriggers.Add(sut);

			var group = new VisualStateGroup();
			group.States.Add(state);

			VisualStateManager.SetVisualStateGroups(border, new List<VisualStateGroup>() { group });

			group.CurrentState.Should().Be(state);
		}

		[TestMethod]
		// when the widths differ, the widest win
		[DataRow(1, 100, 100, "{10,10}|{20,0}")]
		[DataRow(1, 100, 100, "{10,10}|{20,20}")]
		// when the widths are the same and the heights differ, the tallest win
		[DataRow(0, 100, 100, "{10,10}|{10,0}")]
		[DataRow(1, 100, 100, "{10,10}|{10,20}")]
		// when the widths and the heights are all same, the first in declaration order win
		[DataRow(0, 100, 100, "{10,}|{10,}")]
		[DataRow(0, 100, 100, "{,10}|{,10}")]
		[DataRow(0, 100, 100, "{10,10}|{10,10}")]
		public void When_Multiple_AdaptiveTriggers(int expectedIndex, int windowWidth, int windowHeight, string context)
		{
			Window.Current.SetWindowSize(new Size(windowWidth, windowHeight));
			var border = new Border();
			border.ForceLoaded();

			var sut = new VisualStateGroup();
			var states = context.Split('|')
				.Select(x => BuildAdaptiveTrigger(x.Trim('{', '}').Split(',')))
				.Select(x => new VisualState
				{
					StateTriggers = { x }
				})
				.ForEach(sut.States.Add);
			VisualStateManager.SetVisualStateGroups(border, new List<VisualStateGroup>() { sut });

			sut.CurrentState.Should().Be(sut.States[expectedIndex]);

			AdaptiveTrigger BuildAdaptiveTrigger(string[] args)
			{
				var result = new AdaptiveTrigger();
				if (args[0] is { Length: > 0 } arg0)
				{
					result.MinWindowWidth = double.Parse(arg0);
				}
				if (args[1] is { Length: > 0 } arg1)
				{
					result.MinWindowHeight = double.Parse(arg1);
				}

				return result;
			}
		}

		[TestMethod]
		public void When_SizeOverride_WinsOverWindow()
		{
			// Real window is large enough to satisfy the trigger.
			Window.Current.SetWindowSize(new Size(1000, 1000));

			var border = new Border();
			border.ForceLoaded();

			var sut = new AdaptiveTrigger { MinWindowHeight = 100, MinWindowWidth = 100 };

			var state = new VisualState { Name = "activeState" };
			state.StateTriggers.Add(sut);

			var group = new VisualStateGroup();
			group.States.Add(state);

			VisualStateManager.SetVisualStateGroups(border, new List<VisualStateGroup>() { group });

			group.CurrentState.Should().Be(state);

			// A simulated (smaller) size overrides the real window, so the trigger goes inactive even
			// though the window itself never changed — the "host resizes the content" scenario.
			AdaptiveTrigger.SetWindowSizeOverride(new Size(50, 50));
			group.CurrentState.Should().Be(null);
		}

		[TestMethod]
		public void When_SizeOverride_ReevaluatesOnChange()
		{
			Window.Current.SetWindowSize(new Size(1000, 1000));

			var border = new Border();
			border.ForceLoaded();

			var sut = new AdaptiveTrigger { MinWindowHeight = 100, MinWindowWidth = 100 };

			var state = new VisualState { Name = "activeState" };
			state.StateTriggers.Add(sut);

			var group = new VisualStateGroup();
			group.States.Add(state);

			VisualStateManager.SetVisualStateGroups(border, new List<VisualStateGroup>() { group });

			// Start below the threshold via the override -> inactive.
			AdaptiveTrigger.SetWindowSizeOverride(new Size(50, 50));
			group.CurrentState.Should().Be(null);

			// Growing the override above the threshold re-evaluates without any window/XamlRoot change.
			AdaptiveTrigger.SetWindowSizeOverride(new Size(200, 200));
			group.CurrentState.Should().Be(state);
		}

		[TestMethod]
		public void When_SizeOverride_SetBeforeAttach_AppliesOnAttach()
		{
			Window.Current.SetWindowSize(new Size(1000, 1000));

			// The override is set before the trigger ever attaches — the primary host scenario where a
			// simulated form factor is active while new pages load.
			AdaptiveTrigger.SetWindowSizeOverride(new Size(50, 50));

			var border = new Border();
			border.ForceLoaded();

			var sut = new AdaptiveTrigger { MinWindowHeight = 100, MinWindowWidth = 100 };

			var state = new VisualState { Name = "activeState" };
			state.StateTriggers.Add(sut);

			var group = new VisualStateGroup();
			group.States.Add(state);

			VisualStateManager.SetVisualStateGroups(border, new List<VisualStateGroup>() { group });

			// The freshly attached trigger must evaluate against the pre-existing override, not the window.
			group.CurrentState.Should().Be(null);

			AdaptiveTrigger.SetWindowSizeOverride(new Size(200, 200));
			group.CurrentState.Should().Be(state);
		}

		[TestMethod]
		public void When_SizeOverride_FailsMinWindowHeightOnly()
		{
			Window.Current.SetWindowSize(new Size(1000, 1000));

			var border = new Border();
			border.ForceLoaded();

			var sut = new AdaptiveTrigger { MinWindowHeight = 100, MinWindowWidth = 100 };

			var state = new VisualState { Name = "activeState" };
			state.StateTriggers.Add(sut);

			var group = new VisualStateGroup();
			group.States.Add(state);

			VisualStateManager.SetVisualStateGroups(border, new List<VisualStateGroup>() { group });

			// The override width satisfies the trigger but its height does not — both constraints must be
			// evaluated against the override, not just the width.
			AdaptiveTrigger.SetWindowSizeOverride(new Size(200, 50));
			group.CurrentState.Should().Be(null);
		}

		[TestMethod]
		public void When_SizeOverride_Cleared_RevertsToWindow()
		{
			Window.Current.SetWindowSize(new Size(1000, 1000));

			var border = new Border();
			border.ForceLoaded();

			var sut = new AdaptiveTrigger { MinWindowHeight = 100, MinWindowWidth = 100 };

			var state = new VisualState { Name = "activeState" };
			state.StateTriggers.Add(sut);

			var group = new VisualStateGroup();
			group.States.Add(state);

			VisualStateManager.SetVisualStateGroups(border, new List<VisualStateGroup>() { group });

			AdaptiveTrigger.SetWindowSizeOverride(new Size(50, 50));
			group.CurrentState.Should().Be(null);

			// Clearing the override reverts evaluation to the real window size (1000x1000) -> active.
			AdaptiveTrigger.SetWindowSizeOverride(null);
			group.CurrentState.Should().Be(state);
		}

		// Scoped (per-AssemblyLoadContext) override tests — https://github.com/unoplatform/uno/issues/23721

		[TestMethod]
		public void When_ScopedOverride_AppliesOnlyToOwningAlcTrigger()
		{
			Window.Current.SetWindowSize(new Size(1000, 1000));

			var (alc, guestElement) = CreateCollectibleGuestElement();

			var hostBorder = new Border();
			hostBorder.ForceLoaded();
			var (hostGroup, hostState) = BuildTriggeredGroup(hostBorder);

			guestElement.ForceLoaded();
			var (guestGroup, guestState) = BuildTriggeredGroup(guestElement);

			hostGroup.CurrentState.Should().Be(hostState);
			guestGroup.CurrentState.Should().Be(guestState);

			// A size scoped to the guest ALC deactivates the guest trigger only — the host trigger
			// keeps evaluating against the real window (the "host chrome must not collapse" scenario).
			AdaptiveTrigger.SetWindowSizeOverride(new Size(50, 50), alc);

			guestGroup.CurrentState.Should().Be(null);
			hostGroup.CurrentState.Should().Be(hostState);
		}

		[TestMethod]
		public void When_ScopedOverride_WinsOverGlobal()
		{
			Window.Current.SetWindowSize(new Size(1000, 1000));

			var (alc, guestElement) = CreateCollectibleGuestElement();

			var hostBorder = new Border();
			hostBorder.ForceLoaded();
			var (hostGroup, hostState) = BuildTriggeredGroup(hostBorder);

			guestElement.ForceLoaded();
			var (guestGroup, _) = BuildTriggeredGroup(guestElement);

			// Global override satisfies the triggers, scoped override does not: the guest trigger must
			// use the scoped value while the host trigger keeps using the global one.
			AdaptiveTrigger.SetWindowSizeOverride(new Size(200, 200));
			AdaptiveTrigger.SetWindowSizeOverride(new Size(50, 50), alc);

			guestGroup.CurrentState.Should().Be(null);
			hostGroup.CurrentState.Should().Be(hostState);
		}

		[TestMethod]
		public void When_ScopedOverride_Cleared_RevertsToGlobal_ThenWindow()
		{
			Window.Current.SetWindowSize(new Size(1000, 1000));

			var (alc, guestElement) = CreateCollectibleGuestElement();

			guestElement.ForceLoaded();
			var (guestGroup, guestState) = BuildTriggeredGroup(guestElement);

			guestGroup.CurrentState.Should().Be(guestState);

			// Without a scoped override the guest trigger follows the global override…
			AdaptiveTrigger.SetWindowSizeOverride(new Size(50, 50));
			guestGroup.CurrentState.Should().Be(null);

			// …a scoped override then takes precedence over the global one…
			AdaptiveTrigger.SetWindowSizeOverride(new Size(200, 200), alc);
			guestGroup.CurrentState.Should().Be(guestState);

			// …clearing the scoped override falls back to the global override…
			AdaptiveTrigger.SetWindowSizeOverride(null, alc);
			guestGroup.CurrentState.Should().Be(null);

			// …and clearing the global override reverts to the real window size.
			AdaptiveTrigger.SetWindowSizeOverride(null);
			guestGroup.CurrentState.Should().Be(guestState);
		}

		[TestMethod]
		public void When_ScopedOverride_SetBeforeAttach_AppliesOnAttach()
		{
			Window.Current.SetWindowSize(new Size(1000, 1000));

			var (alc, guestElement) = CreateCollectibleGuestElement();

			// The scoped override is set before the guest trigger ever attaches — the host scenario
			// where a simulated form factor is active while guest pages load.
			AdaptiveTrigger.SetWindowSizeOverride(new Size(50, 50), alc);

			guestElement.ForceLoaded();
			var (guestGroup, guestState) = BuildTriggeredGroup(guestElement);

			guestGroup.CurrentState.Should().Be(null);

			AdaptiveTrigger.SetWindowSizeOverride(new Size(200, 200), alc);
			guestGroup.CurrentState.Should().Be(guestState);
		}

		[TestMethod]
		public void When_ScopedOverride_NullAlc_Throws()
			=> Assert.ThrowsExactly<ArgumentNullException>(() => AdaptiveTrigger.SetWindowSizeOverride(new Size(10, 10), null));

		[TestMethod]
		public void When_ScopedOverride_DefaultAlc_Throws()
			=> Assert.ThrowsExactly<ArgumentException>(() => AdaptiveTrigger.SetWindowSizeOverride(new Size(10, 10), AssemblyLoadContext.Default));

		[TestMethod]
		public void When_ScopedOverride_ForForeignAlc_DoesNotAffectHost()
		{
			Window.Current.SetWindowSize(new Size(1000, 1000));

			var hostBorder = new Border();
			hostBorder.ForceLoaded();
			var (hostGroup, hostState) = BuildTriggeredGroup(hostBorder);

			hostGroup.CurrentState.Should().Be(hostState);

			// An override scoped to an ALC no element belongs to must leave every trigger alone.
			var foreignAlc = new AssemblyLoadContext(nameof(When_ScopedOverride_ForForeignAlc_DoesNotAffectHost), isCollectible: true);
			_scopedAlcs.Add(foreignAlc);
			AdaptiveTrigger.SetWindowSizeOverride(new Size(50, 50), foreignAlc);

			hostGroup.CurrentState.Should().Be(hostState);

			Window.Current.SetWindowSize(new Size(20, 20));
			hostGroup.CurrentState.Should().Be(null);
		}

		[TestMethod]
		public void When_ScopedOverride_AlcUnloaded_Then_NotRootedByOverride()
		{
			var weakAlc = StageScopedOverrideOnCollectibleAlc();

			for (var i = 0; i < 10 && weakAlc.IsAlive; i++)
			{
				GC.Collect();
				GC.WaitForPendingFinalizers();
			}

			Assert.IsFalse(
				weakAlc.IsAlive,
				"A scoped window-size override must never keep the target AssemblyLoadContext alive; " +
				"otherwise a collectible guest app could no longer unload.");
		}

		[TestMethod]
		public void When_ScopedOverride_AlcUnloaded_Then_ScopeSelfClears()
		{
			var alc = new AssemblyLoadContext(nameof(When_ScopedOverride_AlcUnloaded_Then_ScopeSelfClears), isCollectible: true);
			_scopedAlcs.Add(alc);

			AdaptiveTrigger.SetWindowSizeOverride(new Size(50, 50), alc);
			Assert.IsTrue(AdaptiveTrigger.HasScopedWindowSizeOverrides, "Setting a scoped override must engage the scoped-resolution path");

			// Unloading fires synchronously on the calling thread and must drop the scope even when the
			// host never cleared it, restoring the no-scoped-overrides fast path.
			alc.Unload();
			Assert.IsFalse(AdaptiveTrigger.HasScopedWindowSizeOverrides, "Unloading the scoped ALC must restore the fast path");
		}

		[TestMethod]
		public void When_ScopedOverride_ForUnloadingAlc_IsIgnored()
		{
			var alc = new AssemblyLoadContext(nameof(When_ScopedOverride_ForUnloadingAlc_IsIgnored), isCollectible: true);
			_scopedAlcs.Add(alc);
			alc.Unload();

			AdaptiveTrigger.SetWindowSizeOverride(new Size(50, 50), alc);

			Assert.IsFalse(
				AdaptiveTrigger.HasScopedWindowSizeOverrides,
				"An override for an already-unloading ALC must be ignored: its Unloading event can never fire again to clean it up");
		}

		[TestMethod]
		public void When_TwoScopedOverrides_EachAppliesToItsOwnAlc()
		{
			Window.Current.SetWindowSize(new Size(1000, 1000));

			var (alc1, guest1) = CreateCollectibleGuestElement();
			var (alc2, guest2) = CreateCollectibleGuestElement();

			guest1.ForceLoaded();
			var (group1, state1) = BuildTriggeredGroup(guest1);

			guest2.ForceLoaded();
			var (group2, state2) = BuildTriggeredGroup(guest2);

			// Two guests simulated at different sizes at the same time: each trigger follows the
			// override of its own load context.
			AdaptiveTrigger.SetWindowSizeOverride(new Size(50, 50), alc1);
			AdaptiveTrigger.SetWindowSizeOverride(new Size(200, 200), alc2);

			group1.CurrentState.Should().Be(null);
			group2.CurrentState.Should().Be(state2);

			AdaptiveTrigger.SetWindowSizeOverride(null, alc1);

			group1.CurrentState.Should().Be(state1);
			group2.CurrentState.Should().Be(state2);
		}

		[TestMethod]
		public void When_ScopedOverride_OwnerResolvedBeforeParenting_ReresolvesOnAttach()
		{
			Window.Current.SetWindowSize(new Size(1000, 1000));

			var (alc, guestElement) = CreateCollectibleGuestElement(typeof(ViewLibrary.MyExtBorder));

			AdaptiveTrigger.SetWindowSizeOverride(new Size(50, 50), alc);

			// Build the triggered subtree BEFORE parenting it under the guest element: the property
			// change forces an evaluation whose ancestor walk finds no guest-typed ancestor yet.
			var grid = new Grid();
			var sut = new AdaptiveTrigger { MinWindowHeight = 100 };
			var state = new VisualState { Name = "activeState" };
			state.StateTriggers.Add(sut);
			var group = new VisualStateGroup();
			group.States.Add(state);
			VisualStateManager.SetVisualStateGroups(grid, new List<VisualStateGroup>() { group });

			sut.MinWindowWidth = 100;

			// Parenting the subtree under a guest-typed ancestor fires no owner hook on the trigger —
			// the attach that follows must re-resolve the ancestry instead of trusting a cached miss.
			((Border)guestElement).Child = grid;
			guestElement.ForceLoaded();

			group.CurrentState.Should().Be(null);

			AdaptiveTrigger.SetWindowSizeOverride(new Size(200, 200), alc);
			group.CurrentState.Should().Be(state);
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
		private static WeakReference StageScopedOverrideOnCollectibleAlc()
		{
			var alc = new AssemblyLoadContext(nameof(StageScopedOverrideOnCollectibleAlc), isCollectible: true);
			AdaptiveTrigger.SetWindowSizeOverride(new Size(50, 50), alc);
			alc.Unload();
			return new WeakReference(alc);
		}

		/// <summary>
		/// Loads a second copy of the view library into a collectible <c>AssemblyLoadContext</c> (Uno
		/// assemblies unify from the default context) and instantiates its <c>MyExtControl</c>, giving a
		/// live element whose type belongs to that context — the shape of a hosted guest app's page.
		/// </summary>
		private (AssemblyLoadContext Alc, FrameworkElement GuestElement) CreateCollectibleGuestElement()
			=> CreateCollectibleGuestElement(typeof(ViewLibrary.MyExtControl));

		private (AssemblyLoadContext Alc, FrameworkElement GuestElement) CreateCollectibleGuestElement(Type viewLibraryType)
		{
			var alc = new AssemblyLoadContext($"{nameof(Given_AdaptiveTrigger)}-guest", isCollectible: true);
			_scopedAlcs.Add(alc);

			var guestAssembly = alc.LoadFromAssemblyPath(viewLibraryType.Assembly.Location);
			var guestType = guestAssembly.GetType(viewLibraryType.FullName, throwOnError: true);
			var guestElement = (FrameworkElement)Activator.CreateInstance(guestType);

			return (alc, guestElement);
		}

		private static (VisualStateGroup Group, VisualState State) BuildTriggeredGroup(FrameworkElement element)
		{
			var state = new VisualState { Name = "activeState" };
			state.StateTriggers.Add(new AdaptiveTrigger { MinWindowHeight = 100, MinWindowWidth = 100 });

			var group = new VisualStateGroup();
			group.States.Add(state);

			VisualStateManager.SetVisualStateGroups(element, new List<VisualStateGroup>() { group });

			return (group, state);
		}
	}
}
