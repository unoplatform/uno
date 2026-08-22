#if HAS_UNO
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.Tests.TemplatedParent.Setup;
using WindowHelper = Private.Infrastructure.TestServices.WindowHelper;

namespace Uno.UI.RuntimeTests.Tests.TemplatedParent;

/// <summary>
/// Templated-parent propagation when a template root is reused by the <see cref="FrameworkTemplatePool"/>:
/// every member of the recycled content has to point at the host that dequeued it, and not at the one that
/// materialized it. Lazily materialized members (x:Load, <see cref="VisualState"/>) are the interesting
/// case -- they are created outside the pool's snapshot of the template members, and the settings they
/// capture to reach the templated parent are fixed at materialization time.
/// </summary>
/// <remarks>
/// Ignored pending #18317, each test on a distinct missing piece: the snapshot is armed by the public
/// (permanently false) <see cref="FrameworkTemplatePool.IsPoolingEnabled"/> so nothing is ever tracked;
/// lazy members still do not receive the new templated parent once it is armed; and a member materialized
/// after the recycling reads its parent from the settings captured by the host that pooled the root.
/// </remarks>
public partial class TemplatedParentTests // recycling
{
	private const string RecyclingIsNotImplemented = "#18317 With TemplatedParent rework, the recycling part was not re-introduced/updated.";

	[TestMethod]
	[Ignore(RecyclingIsNotImplemented)]
	public Task Recycled_Template_Updates_TemplatedParent_Test() => WithPooledTemplate(async template =>
	{
		var first = CreateHost(template, "first");
		await UITestHelper.Load(first);
		var root = GetTemplateRoot(first);
		var eager = root.FindFirstDescendantOrThrow<Border>("EagerBorder");

		var second = await RecycleInto(template, first, root, "second");

		AssertTemplatedParent(second, root, "The recycled template root kept its previous templated-parent.");
		AssertTemplatedParent(second, eager, "The eager template member kept its previous templated-parent.");
	});

	[TestMethod]
	[Ignore(RecyclingIsNotImplemented)]
	public Task Recycled_Template_Updates_LazyMembers_TemplatedParent_Test() => WithPooledTemplate(async template =>
	{
		var first = CreateHost(template, "first");
		await UITestHelper.Load(first);
		var root = GetTemplateRoot(first);

		var lazy = await MaterializeDeferredElement(root);
		var keyFrame = MaterializeLazyState(root);

		// Guards the assertions below against a lazy member that never received a templated parent
		// in the first place, which would make the recycled expectation pass for the wrong reason.
		AssertTemplatedParent(first, lazy, "The deferred element didnt receive the templated-parent that materialized it.");
		AssertTemplatedParent(first, keyFrame, "The lazily built key-frame didnt receive the templated-parent that materialized it.");

		var second = await RecycleInto(template, first, root, "second");

		AssertTemplatedParent(second, lazy, "The deferred element kept its previous templated-parent.");
		AssertTemplatedParent(second, keyFrame, "The lazily built key-frame kept its previous templated-parent.");
	});

	[TestMethod]
	[Ignore(RecyclingIsNotImplemented)]
	public Task Recycled_Template_Updates_LazyMembers_Materialized_After_Recycling_Test() => WithPooledTemplate(async template =>
	{
		var first = CreateHost(template, "first");
		await UITestHelper.Load(first);
		var root = GetTemplateRoot(first);

		var second = await RecycleInto(template, first, root, "second");

		// Both are built against the materialization settings captured by the first host, so the
		// templated parent they reach for is the one those settings were created with.
		var lazy = await MaterializeDeferredElement(root);
		var keyFrame = MaterializeLazyState(root);

		AssertTemplatedParent(second, lazy, "The deferred element received the templated-parent of the host that pooled it.");
		AssertTemplatedParent(second, keyFrame, "The lazily built key-frame received the templated-parent of the host that pooled it.");
	});
}
public partial class TemplatedParentTests // recycling helper methods
{
	private static async Task WithPooledTemplate(Func<DataTemplate, Task> test)
	{
		using var _ = FeatureConfigurationHelper.UseTemplatePooling();

		// The pooled instance count is global, so leftovers from another test would break the guard in RecycleInto.
		FrameworkTemplatePool.Instance.Scavenge(isManual: true);

		var setup = new TemplateRecycling_Templates();
		try
		{
			await test((DataTemplate)setup.Resources["RecyclingTemplate"]);
		}
		finally
		{
			WindowHelper.WindowContent = null;
			FrameworkTemplatePool.Instance.Scavenge(isManual: true);

			// The template only holds a weak reference on the owner providing its factory.
			GC.KeepAlive(setup);
		}
	}

	private static ContentPresenter CreateHost(DataTemplate template, string content) => new()
	{
		ContentTemplate = template,
		Content = content,
		// The lazily built key-frame binds to the templated parent's Tag.
		Tag = content,
		// An explicit size is required: WaitForLoaded treats a zero-sized element as not loaded.
		Width = 50,
		Height = 50,
	};

	private static Grid GetTemplateRoot(ContentPresenter host) =>
		host.ContentTemplateRoot as Grid ?? throw new Exception("The template did not materialize.");

	private static async Task<ContentPresenter> RecycleInto(DataTemplate template, ContentPresenter from, UIElement root, string content)
	{
		// Detaching the template returns its root to the pool, from where the next host dequeues it.
		from.ContentTemplate = null;
		await UITestHelper.WaitForIdle();

		Assert.AreEqual(1, FrameworkTemplatePool.Instance.GetPooledTemplatesCount(), "The template root was not returned to the pool.");

		var to = CreateHost(template, content);
		await UITestHelper.Load(to);

		Assert.AreSame(root, to.ContentTemplateRoot, "The pooled template root was not reused.");

		return to;
	}

	private static async Task<Border> MaterializeDeferredElement(Grid root)
	{
		// The stub stands in for the deferred element in Children, but is not walkable as a descendant.
		var stub = root.Children.OfType<ElementStub>().SingleOrDefault() ?? throw new Exception("failed to find the ElementStub");

		stub.Load = true;
		await UITestHelper.WaitForIdle();

		return root.FindFirstDescendantOrThrow<Border>("LazyBorder");
	}

	private static DependencyObject MaterializeLazyState(Grid root)
	{
		var state = VisualStateManager.GetVisualStateGroups(root)[0].States.Single(x => x.Name == "LazyState");
		Assert.IsNotNull(state.LazyBuilder, "The state was materialized before the test requested it.");

		// Reading the storyboard runs the lazy builder that creates the key-frame.
		var storyboard = state.Storyboard ?? throw new Exception("The state did not materialize its storyboard.");
		var animation = (ObjectAnimationUsingKeyFrames)storyboard.Children[0];

		return animation.KeyFrames[0];
	}

	/// <summary>
	/// Both hosts are ContentPresenters, which stringify to their type name, so the expectation has to name
	/// them by their content for the failure to be readable.
	/// </summary>
	private static void AssertTemplatedParent(ContentPresenter expected, DependencyObject member, string message)
	{
		var actual = member.GetTemplatedParent();

		Assert.AreSame(expected, actual, $"{message} (expected={Describe(expected)}, actual={Describe(actual)})");

		static string Describe(object host) => (host as ContentPresenter)?.Content?.ToString() ?? DescribeObject(host);
	}
}
#endif
