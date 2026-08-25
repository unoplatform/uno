#nullable enable

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls;

#if HAS_UNO
using Microsoft.UI.Xaml;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

/// <summary>
/// A materialized template must never keep its templated parent alive: a template root outlives its
/// templated parent whenever it is returned to the <see cref="FrameworkTemplatePool"/>, and the pool
/// is a static root. Members created after the template builder returned (a never-entered VisualState,
/// an unloaded x:Load element) hold on to the materialization settings, so the settings are the place
/// where a strong reference to the templated parent would turn into a leak.
/// </summary>
[TestClass]
[RunsOnUIThread]
#if RUNTIME_NATIVE_AOT
[Ignore("NativeAOT GC behavior may differ for leak detection tests")]
#endif
public class Given_FrameworkTemplate_And_Leak
{
#if HAS_UNO
	[TestMethod]
	public Task When_Pooled_Template_Has_Unentered_VisualState_Then_TemplatedParent_Collected()
		=> AssertPooledTemplateReleasesTemplatedParent("LazyVisualStateTemplate");

	[TestMethod]
	public Task When_Pooled_Template_Has_Deferred_Element_Then_TemplatedParent_Collected()
		=> AssertPooledTemplateReleasesTemplatedParent("DeferredElementTemplate");

	private static async Task AssertPooledTemplateReleasesTemplatedParent(string templateKey)
	{
		using var _ = FeatureConfigurationHelper.UseTemplatePooling();

		FrameworkTemplatePool.Instance.Scavenge(isManual: true);

		var host = new TemplatePool_Leak_Templates();
		var template = (DataTemplate)host.Resources[templateKey];

		var (templatedParent, templateRoot) = await MaterializeThenPool(template);

		// Guards against the test passing for the wrong reason: without a pooled root, nothing
		// would be holding the template content and the collection assertion would be vacuous.
		Assert.AreEqual(1, FrameworkTemplatePool.Instance.GetPooledTemplatesCount(), "The template root was not returned to the pool.");

		await AssertCollectedAsync(templatedParent, "The templated parent should have been collected while its template root sits in the pool.");

		Assert.IsTrue(templateRoot.IsAlive, "The template root should still be held by the pool.");

		GC.KeepAlive(host);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static async Task<(WeakReference TemplatedParent, WeakReference TemplateRoot)> MaterializeThenPool(DataTemplate template)
	{
		// ContentPresenter is the templated parent here, and the only host going through the pooled
		// materialization path -- Control.Template is materialized uncached.
		// An explicit size is required: WaitForLoaded treats a zero-sized element as not loaded.
		var presenter = new ContentPresenter
		{
			ContentTemplate = template,
			Content = "content",
			Width = 50,
			Height = 50,
		};

		TestServices.WindowHelper.WindowContent = presenter;
		await TestServices.WindowHelper.WaitForLoaded(presenter);

		var root = presenter.ContentTemplateRoot;
		Assert.IsNotNull(root, "The template did not materialize.");

		// Detaching the template returns its root to the pool while the templated parent is still alive.
		presenter.ContentTemplate = null;
		await TestServices.WindowHelper.WaitForIdle();

		TestServices.WindowHelper.WindowContent = null;
		await TestServices.WindowHelper.WaitForIdle();

		return (new WeakReference(presenter), new WeakReference(root));
	}

	private static async Task AssertCollectedAsync(WeakReference reference, string message)
	{
		var sw = Stopwatch.StartNew();
		var timeout = TimeSpan.FromSeconds(10);

		while (sw.Elapsed < timeout && reference.IsAlive)
		{
			GC.Collect(2);
			GC.WaitForPendingFinalizers();
			GC.Collect(2);

			await Task.Yield();
			await TestServices.WindowHelper.WaitForIdle();
		}

		Assert.IsFalse(reference.IsAlive, message);
	}
#endif
}
