#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Composition.Drawing;

namespace Uno.UI.Tests.Hosting;

/// <summary>
/// <see cref="GraphicsRegistry.FirstCandidateKind"/> picks what the Win32 host prewarms, so it must name the kind
/// negotiation asks the host for first.
/// </summary>
[UITestClass]
public class Given_GraphicsRegistry
{
	private static readonly FieldInfo _backendsField =
		typeof(GraphicsRegistry).GetField("_backends", BindingFlags.NonPublic | BindingFlags.Static)!;

	private object? _savedBackends;
	private GraphicsContextFactory? _savedFactory;
	private IReadOnlyCollection<GraphicsContextKind> _savedDisabled = Array.Empty<GraphicsContextKind>();

	[TestInitialize]
	public void Setup()
	{
		_savedBackends = _backendsField.GetValue(null);
		_savedFactory = GraphicsRegistry.ContextFactory;
		_savedDisabled = GraphicsRegistry.DisabledContextKinds;
	}

	[TestCleanup]
	public void Cleanup()
	{
		_backendsField.SetValue(null, _savedBackends);
		GraphicsRegistry.ContextFactory = _savedFactory;
		GraphicsRegistry.DisabledContextKinds = _savedDisabled;
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24849")]
	public async Task When_First_Kind_Disabled_Then_FirstCandidateKind_Matches_First_Negotiated_Kind()
	{
		GraphicsRegistry.Register(new IGraphicsProvider[]
		{
			new FakeProvider(GraphicsContextKind.Vulkan, GraphicsContextKind.OpenGL),
			new FakeProvider(GraphicsContextKind.Software),
		});
		GraphicsRegistry.DisabledContextKinds = new[] { GraphicsContextKind.Vulkan };

		var requested = await NegotiateAndRecordRequestedKinds();

		Assert.AreEqual(GraphicsContextKind.OpenGL, GraphicsRegistry.FirstCandidateKind);
		CollectionAssert.AreEqual(new[] { GraphicsContextKind.OpenGL, GraphicsContextKind.Software }, requested);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24849")]
	public async Task When_Every_Kind_Disabled_Then_FirstCandidateKind_Is_Null()
	{
		GraphicsRegistry.Register(new IGraphicsProvider[] { new FakeProvider(GraphicsContextKind.Vulkan) });
		GraphicsRegistry.DisabledContextKinds = new[] { GraphicsContextKind.Vulkan };

		var requested = await NegotiateAndRecordRequestedKinds();

		Assert.IsNull(GraphicsRegistry.FirstCandidateKind);
		Assert.IsEmpty(requested);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26153")]
	public async Task When_Context_Factory_Throws_Then_Error_Includes_Innermost_Message()
	{
		GraphicsRegistry.Register(new IGraphicsProvider[] { new FakeProvider(GraphicsContextKind.WebGpu) });
		GraphicsRegistry.DisabledContextKinds = Array.Empty<GraphicsContextKind>();
		var thrown = new TypeInitializationException(
			"FakeWebGpuNative",
			new DllNotFoundException("Unable to load DLL 'webgpu'"));
		GraphicsRegistry.ContextFactory = _ => throw thrown;

		var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(GraphicsRegistry.InitializeAsync);

		StringAssert.Contains(exception.Message, "FakeProvider/WebGpu: context factory threw (TypeInitializationException: Unable to load DLL 'webgpu')");
		Assert.AreSame(thrown, exception.InnerException);
	}

	[TestMethod]
	public async Task When_Several_Context_Factories_Throw_Then_InnerException_Aggregates_Them()
	{
		GraphicsRegistry.Register(new IGraphicsProvider[] { new FakeProvider(GraphicsContextKind.Vulkan, GraphicsContextKind.OpenGL) });
		GraphicsRegistry.DisabledContextKinds = Array.Empty<GraphicsContextKind>();
		List<Exception> thrown = new();
		GraphicsRegistry.ContextFactory = kind =>
		{
			var e = new InvalidOperationException($"{kind} unavailable");
			thrown.Add(e);
			throw e;
		};

		var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(GraphicsRegistry.InitializeAsync);

		var aggregate = Assert.IsInstanceOfType<AggregateException>(exception.InnerException);
		CollectionAssert.AreEqual(thrown, aggregate.InnerExceptions.ToList());
	}

	// Declines every kind, so negotiation walks all candidates and then reports that none initialized.
	private static async Task<List<GraphicsContextKind>> NegotiateAndRecordRequestedKinds()
	{
		List<GraphicsContextKind> requested = new();
		GraphicsRegistry.ContextFactory = kind =>
		{
			requested.Add(kind);
			return Task.FromResult<ISwapChain?>(null);
		};

		await Assert.ThrowsExactlyAsync<InvalidOperationException>(GraphicsRegistry.InitializeAsync);

		return requested;
	}

	private sealed class FakeProvider(params GraphicsContextKind[] kinds) : IGraphicsProvider
	{
		public IReadOnlyList<GraphicsContextKind> PreferredContexts => kinds;
	}
}
