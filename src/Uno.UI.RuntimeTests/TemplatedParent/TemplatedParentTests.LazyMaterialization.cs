using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.Tests.TemplatedParent.Setup;

namespace Uno.UI.RuntimeTests.Tests.TemplatedParent;

/// <summary>
/// Templated-parent propagation for members created *after* the template builder returned: x:Load
/// elements and the content of a <see cref="VisualState"/>. Both capture the materialization settings
/// and run later, so they reach the templated parent through a different path than the members the
/// builder creates inline.
/// </summary>
public partial class TemplatedParentTests // lazy materialization
{
	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task LazyElement_In_DataTemplate_Test()
	{
		var setup = new LazyElement_DataTemplate();
		await UITestHelper.Load(setup);

		// FindName materializes the deferred element.
		var lazy = setup.SUT.FindName("LazyBorder") as Border ?? throw new Exception("failed to find Border#LazyBorder");
		var descendant = setup.SUT.FindFirstDescendantOrThrow<TextBlock>("LazyDescendant");

		// A DataTemplate's templated parent is the presenter hosting it, not a Control.
		Assert.AreEqual(setup.SUT, GetTemplatedParentCompat(lazy), "The lazy element didnt receive the correct templated-parent.");
		Assert.AreEqual(setup.SUT, GetTemplatedParentCompat(descendant), "The lazy descendant didnt receive the correct templated-parent.");
		Assert.AreEqual(setup.SUT.Content, descendant.Text, "The lazy descendant didnt have its templated-parent binding applied correctly");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task LazyVisualState_TemplatedParentBinding_Test()
	{
		var setup = new LazyVisualState();
		var tag = new SolidColorBrush(Microsoft.UI.Colors.Red);
		setup.SUT.Tag = tag;

		await UITestHelper.Load(setup);

		var contentElement = setup.SUT.FindFirstDescendantOrThrow<Border>("ContentElement");
		Assert.IsNull(contentElement.Background, "The state was applied before it was requested.");

		Assert.IsTrue(VisualStateManager.GoToState(setup.SUT, "TemplatedParentBound", false), "Failed to enter the state.");
		await UITestHelper.WaitForIdle();

		// Entering the state runs the lazy builder: the key-frame is created here, and can only
		// resolve its value if it was given the templated parent.
		Assert.AreEqual(tag, contentElement.Background, "The lazily built key-frame didnt resolve its templated-parent binding.");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task LazyVisualState_With_DeferredTarget_Test()
	{
		var setup = new LazyVisualState();
		await UITestHelper.Load(setup);

		// Both halves are lazy here: entering the state builds the setter, and resolving that
		// setter's target materializes the deferred element it points at.
		Assert.IsTrue(VisualStateManager.GoToState(setup.SUT, "DeferredTarget", false), "Failed to enter the state.");
		await UITestHelper.WaitForIdle();

		var lazy = setup.SUT.FindFirstDescendantOrThrow<Border>("LazyBorder");
		var descendant = setup.SUT.FindFirstDescendantOrThrow<ContentPresenter>("LazyDescendant");

		Assert.AreEqual(0.5, lazy.Opacity, delta: 0.001, "The setter didnt reach the materialized element.");
		Assert.AreEqual(setup.SUT, GetTemplatedParentCompat(lazy), "The lazy element didnt receive the correct templated-parent.");
		Assert.AreEqual(setup.SUT, GetTemplatedParentCompat(descendant), "The lazy descendant didnt receive the correct templated-parent.");
		Assert.AreEqual(setup.SUT.Content, descendant.Content, "The lazy descendant didnt have its template-binding applied correctly");
	}

#if HAS_UNO
	// Dematerialization has no WinUI equivalent: only ElementStub can put the deferred element back.
	[TestMethod]
	public async Task LazyElement_Rematerialization_Test()
	{
		var setup = new LazyElement_Rematerialization();
		await UITestHelper.Load(setup);

		var host = setup.HostButton;

		// The stub stands in for the deferred element in Children, but is not walkable as a descendant.
		var panel = host.FindFirstDescendantOrThrow<StackPanel>();
		var stub = panel.Children.OfType<ElementStub>().SingleOrDefault() ?? throw new Exception("failed to find the ElementStub");

		stub.Load = true;
		await UITestHelper.WaitForIdle();
		var first = host.FindFirstDescendantOrThrow<ContentPresenter>("LazyPresenter");
		AssertLazyPresenter(first, "first materialization");

		stub.Load = false;
		await UITestHelper.WaitForIdle();
		Assert.HasCount(1, panel.Children.OfType<ElementStub>(), "The element was not dematerialized.");

		// The second materialization re-invokes the content builder against the settings captured
		// by the first one, so the fresh instance has to be wired up just like the original.
		stub.Load = true;
		await UITestHelper.WaitForIdle();
		var second = host.FindFirstDescendantOrThrow<ContentPresenter>("LazyPresenter");
		Assert.AreNotSame(first, second, "The element was not re-created.");
		AssertLazyPresenter(second, "re-materialization");

		void AssertLazyPresenter(ContentPresenter presenter, string step)
		{
			Assert.AreEqual(host, GetTemplatedParentCompat(presenter), $"The lazy element didnt receive the correct templated-parent on {step}.");
			Assert.AreEqual(host.Content, presenter.Content, $"The lazy element didnt have its template-binding applied correctly on {step}.");
		}
	}
#endif
}
