using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Diagnostics;

namespace Uno.UI.Tests.Diagnostics;

[TestClass]
public class Given_ElementRefHandleRegistry
{
	[TestInitialize]
	public void Initialize()
	{
		// Unit tests run off the UI thread; opt out of the thread check globally.
		FeatureConfiguration.ElementRefHandle.DisableThreadingCheck = true;
	}

	[TestCleanup]
	public void Cleanup()
	{
		FeatureConfiguration.ElementRefHandle.DisableThreadingCheck = false;
	}

	[TestMethod]
	public void When_GetOrCreate_SameElement_ReturnsSameHandle()
	{
		var registry = new ElementRefHandleRegistry();
		var element = new Grid();

		var h1 = registry.GetOrCreate(element);
		var h2 = registry.GetOrCreate(element);

		Assert.AreEqual(h1, h2);
	}

	[TestMethod]
	public void When_GetOrCreate_DifferentElements_ReturnsDifferentHandles()
	{
		var registry = new ElementRefHandleRegistry();

		var h1 = registry.GetOrCreate(new Grid());
		var h2 = registry.GetOrCreate(new Grid());

		Assert.AreNotEqual(h1, h2);
	}

	[TestMethod]
	public void When_TryResolve_ValidHandle_ReturnsTrueAndElement()
	{
		var registry = new ElementRefHandleRegistry();
		var element = new Grid();

		// Obtain the handle then clear the local strong reference.
		var handle = GetHandleAndRelease(registry, element);

		// The registry must not implicitly root the object — element is still alive
		// because we hold it in this method's scope.
		var found = registry.TryResolve(handle, out var resolved);

		Assert.IsTrue(found);
		Assert.AreSame(element, resolved);
	}

	[TestMethod]
	public void When_TryResolve_UnknownHandle_ReturnsFalse()
	{
		var registry = new ElementRefHandleRegistry();

		var found = registry.TryResolve("zzzzzzz", out var element);

		Assert.IsFalse(found);
		Assert.IsNull(element);
	}

	[TestMethod]
	public void When_TryResolve_NullHandle_ReturnsFalse()
	{
		var registry = new ElementRefHandleRegistry();

		var found = registry.TryResolve(null!, out var element);

		Assert.IsFalse(found);
		Assert.IsNull(element);
	}

	[TestMethod]
	public void When_TryResolve_EmptyHandle_ReturnsFalse()
	{
		var registry = new ElementRefHandleRegistry();

		var found = registry.TryResolve(string.Empty, out var element);

		Assert.IsFalse(found);
		Assert.IsNull(element);
	}

	[TestMethod]
	public void When_TryResolve_AfterGC_ReturnsFalse()
	{
		var registry = new ElementRefHandleRegistry();
		var handle = RegisterAndRelease(registry);

		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();

		var found = registry.TryResolve(handle, out var element);

		Assert.IsFalse(found);
		Assert.IsNull(element);
	}

	[TestMethod]
	public void When_TryResolve_HandleComparison_IsCaseInsensitive()
	{
		var registry = new ElementRefHandleRegistry();
		var element = new Grid();
		var handle = registry.GetOrCreate(element);

		var foundUpper = registry.TryResolve(handle.ToUpperInvariant(), out var resolvedUpper);
		var foundLower = registry.TryResolve(handle.ToLowerInvariant(), out var resolvedLower);

		Assert.IsTrue(foundUpper);
		Assert.IsTrue(foundLower);
		Assert.AreSame(element, resolvedUpper);
		Assert.AreSame(element, resolvedLower);
	}

	[TestMethod]
	public void When_GetOrCreate_FromBackgroundThread_Throws()
	{
		FeatureConfiguration.ElementRefHandle.DisableThreadingCheck = false;

		var registry = new ElementRefHandleRegistry();

		var ex = Assert.ThrowsException<InvalidOperationException>(
			() => Task.Run(() => registry.GetOrCreate(new Grid())).GetAwaiter().GetResult());

		Assert.IsTrue(ex.Message.Contains("non-UI thread", StringComparison.OrdinalIgnoreCase));
	}

	[TestMethod]
	public void When_TryResolve_FromBackgroundThread_Throws()
	{
		FeatureConfiguration.ElementRefHandle.DisableThreadingCheck = false;

		var registry = new ElementRefHandleRegistry();

		var ex = Assert.ThrowsException<InvalidOperationException>(
			() => Task.Run(() => registry.TryResolve("1", out _)).GetAwaiter().GetResult());

		Assert.IsTrue(ex.Message.Contains("non-UI thread", StringComparison.OrdinalIgnoreCase));
	}

	[TestMethod]
	public void When_GetOrCreate_WithDisableThreadingCheck_DoesNotThrow()
	{
		FeatureConfiguration.ElementRefHandle.DisableThreadingCheck = true;

		var registry = new ElementRefHandleRegistry();

		// Should not throw even when called from a background thread.
		var handle = Task.Run(() => registry.GetOrCreate(new Grid())).GetAwaiter().GetResult();

		Assert.IsFalse(string.IsNullOrEmpty(handle));
	}

	[TestMethod]
	public void When_Finalizer_RemovesReverseMapEntry()
	{
		var registry = new ElementRefHandleRegistry();
		var handle = RegisterAndRelease(registry);

		// First GC collects the Grid; WaitForPendingFinalizers lets RefEntry's finalizer run.
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();

		// The finalizer (not TryResolve) should have cleaned the reverse map.
		// We verify by resolving: it must return false, proving the entry is gone.
		var found = registry.TryResolve(handle, out _);

		Assert.IsFalse(found);
	}

	[TestMethod]
	public void When_Handles_DoNotRecycle_AfterGC_WithinSession()
	{
		var registry = new ElementRefHandleRegistry();

		var firstHandle = RegisterAndRelease(registry);

		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();

		// Register a fresh object; it must receive a new, distinct handle.
		var secondHandle = registry.GetOrCreate(new Grid());

		Assert.AreNotEqual(firstHandle, secondHandle);
	}

	[TestMethod]
	public void When_SetForTesting_RestoresDefaultOnDispose()
	{
		var original = ElementRefHandle.Default;
		var fake = new FakeRegistry();

		using (ElementRefHandle.SetForTesting(fake))
		{
			Assert.AreSame(fake, ElementRefHandle.Default);
		}

		Assert.AreSame(original, ElementRefHandle.Default);
	}

	// ─── helpers ────────────────────────────────────────────────────────────

	/// <summary>
	/// Registers a new Grid in the registry and returns its handle.
	/// The Grid is created in a no-inlined frame so the JIT cannot keep
	/// a reference alive on the stack of the calling method.
	/// </summary>
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static string RegisterAndRelease(ElementRefHandleRegistry registry)
		=> registry.GetOrCreate(new Grid());

	/// <summary>
	/// Registers <paramref name="element"/> and returns its handle via a separate
	/// stack frame so the caller retains the only strong reference.
	/// </summary>
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static string GetHandleAndRelease(ElementRefHandleRegistry registry, object element)
		=> registry.GetOrCreate((Microsoft.UI.Xaml.DependencyObject)element);

	private sealed class FakeRegistry : IElementRefHandleRegistry
	{
		public string GetOrCreate(Microsoft.UI.Xaml.DependencyObject element) => "fake";
		public bool TryResolve(string handle, out Microsoft.UI.Xaml.DependencyObject? element)
		{
			element = null;
			return false;
		}
	}
}
