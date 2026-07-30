using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;
using Uno.UI.RemoteControl;
using Uno.UI.RemoteControl.HotReload.Messages;

namespace Uno.UI.RemoteControl.DevServer.Tests;

[TestClass]
public class Given_DevServerDiagnostics_CollectibleSinks
{
	[TestCleanup]
	public void Cleanup() => DevServerDiagnostics.ResetCurrent();

	[TestMethod]
	public void When_ClearCollectibleSinks_Then_Sink_Is_Released_From_Captured_Contexts()
	{
		// A sink whose type lives in a collectible assembly, captured into an ExecutionContext
		// snapshot (as timers / CTS registrations / pooled connections do). The snapshot itself is
		// immutable and cannot be scrubbed — only the holder indirection makes the sink reachable
		// for teardown.
		var (weakSink, capturedContext) = SetCollectibleSinkAndCaptureContext();

		DevServerDiagnostics.ClearCollectibleSinks();

		// Even when running ON the captured snapshot, the sink must be gone.
		ExecutionContext.Run(
			capturedContext,
			static _ => DevServerDiagnostics.Current.GetType().Name.Should().Be("NullSink"),
			null);

		for (var i = 0; i < 10 && weakSink.IsAlive; i++)
		{
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}

		Assert.IsFalse(
			weakSink.IsAlive,
			"ClearCollectibleSinks must release a collectible-assembly sink from every captured " +
			"ExecutionContext snapshot; otherwise the captures pin the unloading AssemblyLoadContext.");
	}

	[TestMethod]
	public void When_ClearCollectibleSinks_Then_NonCollectible_Sink_Follows_Target_Contract()
	{
		var sink = new TestSink();
		DevServerDiagnostics.Current = sink;

		DevServerDiagnostics.ClearCollectibleSinks();

		// The net5+ build discriminates by Assembly.IsCollectible and preserves host sinks; the
		// netstandard2.0 build cannot, and documents clearing every tracked sink as the
		// teardown-time fallback.
		var frameworkName = typeof(DevServerDiagnostics).Assembly
			.GetCustomAttribute<System.Runtime.Versioning.TargetFrameworkAttribute>()?.FrameworkName;
		if (frameworkName?.StartsWith(".NETStandard", StringComparison.Ordinal) == true)
		{
			DevServerDiagnostics.Current.GetType().Name.Should().Be(
				"NullSink",
				"the netstandard2.0 fallback clears every tracked sink at teardown");
		}
		else
		{
			DevServerDiagnostics.Current.Should().BeSameAs(sink, "non-collectible sinks must survive the teardown sweep");
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static (WeakReference WeakSink, ExecutionContext CapturedContext) SetCollectibleSinkAndCaptureContext()
	{
		var sink = CreateCollectibleSink();
		DevServerDiagnostics.Current = sink;

		// Capture the flow exactly as a timer or CTS registration would.
		var captured = ExecutionContext.Capture()!;

		return (new WeakReference(sink), captured);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static DevServerDiagnostics.ISink CreateCollectibleSink()
	{
		var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
			new AssemblyName("DevServerDiagnostics.CollectibleSinkProbe"),
			AssemblyBuilderAccess.RunAndCollect);
		var moduleBuilder = assemblyBuilder.DefineDynamicModule("main");
		var typeBuilder = moduleBuilder.DefineType(
			"CollectibleSink",
			TypeAttributes.Public,
			typeof(object),
			new[] { typeof(DevServerDiagnostics.ISink) });

		var methodBuilder = typeBuilder.DefineMethod(
			nameof(DevServerDiagnostics.ISink.ReportInvalidFrame),
			MethodAttributes.Public | MethodAttributes.Virtual,
			typeof(void),
			new[] { typeof(Frame) });
		methodBuilder.DefineGenericParameters("TContent");
		methodBuilder.GetILGenerator().Emit(OpCodes.Ret);
		typeBuilder.DefineMethodOverride(
			methodBuilder,
			typeof(DevServerDiagnostics.ISink).GetMethod(nameof(DevServerDiagnostics.ISink.ReportInvalidFrame))!);

		var sinkType = typeBuilder.CreateType()!;
		sinkType.Assembly.IsCollectible.Should().BeTrue("the probe sink must live in a collectible assembly");

		return (DevServerDiagnostics.ISink)Activator.CreateInstance(sinkType)!;
	}

	private sealed class TestSink : DevServerDiagnostics.ISink
	{
		public void ReportInvalidFrame<TContent>(Frame frame)
		{
		}
	}
}
