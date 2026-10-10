using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Runtime.Win32;

namespace Uno.UI.Tests.Hosting;

/// <summary>
/// Tests for <see cref="BackgroundPrewarm{T}"/>, which starts the Win32 Vulkan device creation off the UI thread
/// and hands it to the first window (the Vulkan calls themselves need a real driver).
/// </summary>
[TestClass]
public class Given_BackgroundPrewarm
{
	private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24849")]
	public void When_Warmed_And_Usable_Then_Warmed_Instance_Is_Claimed()
	{
		var warmed = new FakeResource();
		var sut = new BackgroundPrewarm<FakeResource>(() => warmed);

		var claimed = sut.Claim(_ => true, () => throw new AssertFailedException("Fallback must not run"));

		Assert.AreSame(warmed, claimed);
		Assert.IsFalse(warmed.IsDisposed);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24849")]
	public void When_Claimed_Before_Creation_Completes_Then_Waits_For_It()
	{
		using var gate = new ManualResetEventSlim();
		var warmed = new FakeResource();
		var sut = new BackgroundPrewarm<FakeResource>(() =>
		{
			gate.Wait(Timeout);
			return warmed;
		});

		var claim = Task.Run(() => sut.Claim(_ => true, () => new FakeResource()));
		Assert.IsFalse(claim.Wait(100), "Claim returned before the warm-up finished");

		gate.Set();

		Assert.IsTrue(claim.Wait(Timeout));
		Assert.AreSame(warmed, claim.Result);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24849")]
	public void When_Creation_Throws_Then_Falls_Back()
	{
		var fallback = new FakeResource();
		var sut = new BackgroundPrewarm<FakeResource>(() => throw new InvalidOperationException("No device"));

		var claimed = sut.Claim(_ => throw new AssertFailedException("Nothing to complete"), () => fallback);

		Assert.AreSame(fallback, claimed);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24849")]
	public void When_Warmed_Is_Not_Usable_Then_Disposed_And_Falls_Back()
	{
		var warmed = new FakeResource();
		var fallback = new FakeResource();
		var sut = new BackgroundPrewarm<FakeResource>(() => warmed);

		var claimed = sut.Claim(_ => false, () => fallback);

		Assert.AreSame(fallback, claimed);
		Assert.IsTrue(warmed.IsDisposed);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24849")]
	public void When_Completion_Throws_Then_Disposed_And_Falls_Back()
	{
		var warmed = new FakeResource();
		var fallback = new FakeResource();
		var sut = new BackgroundPrewarm<FakeResource>(() => warmed);

		var claimed = sut.Claim(_ => throw new InvalidOperationException("No swapchain"), () => fallback);

		Assert.AreSame(fallback, claimed);
		Assert.IsTrue(warmed.IsDisposed);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24849")]
	public void When_Discarded_Before_Creation_Completes_Then_Disposed_Once_Created()
	{
		using var gate = new ManualResetEventSlim();
		var warmed = new FakeResource();
		var sut = new BackgroundPrewarm<FakeResource>(() =>
		{
			gate.Wait(Timeout);
			return warmed;
		});

		sut.Discard();
		Assert.IsFalse(warmed.IsDisposed);

		gate.Set();

		Assert.IsTrue(warmed.Disposed.Wait(Timeout));
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24849")]
	public void When_Discarded_After_Failure_Then_Does_Not_Throw()
	{
		var sut = new BackgroundPrewarm<FakeResource>(() => throw new InvalidOperationException("No device"));

		sut.Discard();
	}

	private sealed class FakeResource : IDisposable
	{
		public ManualResetEventSlim Disposed { get; } = new();

		public bool IsDisposed => Disposed.IsSet;

		public void Dispose() => Disposed.Set();
	}
}
