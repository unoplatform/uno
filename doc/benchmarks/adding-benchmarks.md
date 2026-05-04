# Adding a benchmark

The harness uses MSTest auto-discovery via reflection. Drop a class with at least
one `[Benchmark]`-annotated method under
`src/Uno.UI.RuntimeTests/Tests/Benchmarks/Suites/<category>/` and it shows up in
CI on the next run with no fixture-file edits.

## Pick a category

| Category | When | Notes |
|----------|------|-------|
| `Micro` | Tight ns-scale loops, single API call per benchmark | Default. Fastest, most stable. |
| `Scenario` | ms-scale bounded operations involving real framework wiring | Layout, theme resources, page load. |
| `Render` | Frame timing, compositor work, animations | Async, drives `CompositionTarget.Rendering`. |

Mark the class with `[BenchmarkCategory(BenchmarkCategory.<X>)]`. Without it,
the harness defaults to `Micro` and applies the Micro per-category Job config.

## Micro template

```csharp
using BenchmarkDotNet.Attributes;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Benchmarks.Suites.Micro;

[BenchmarkCategory(BenchmarkCategory.Micro)]
[MemoryDiagnoser]
public class MyApiBenchmark
{
	private MyControl _sut = null!;

	[GlobalSetup]
	public void Setup() => _sut = new MyControl();

	[Benchmark]
	public void Toggle_Foo()
	{
		_sut.Foo = 1;
		_sut.Foo = 0;
	}
}
```

Rules:
- The class must be public, non-generic, and have a parameterless constructor.
- The `[Benchmark]` method body must perform the *only* work being measured.
  Setup belongs in `[GlobalSetup]`, not inside `[Benchmark]`.
- Allocate state once in `[GlobalSetup]`, reuse it across iterations.
- Do not perform IO, dispatcher work, or async waits in Micro benchmarks — those
  belong in Scenario or Render.

## Scenario template

Scenario benchmarks usually need the dispatcher (layout, ItemsControl
virtualization, theme resource resolution). Use
`TestServices.WindowHelper.RootElementDispatcher.RunAsyncWithBypass` to marshal:

```csharp
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;

namespace Uno.UI.RuntimeTests.Tests.Benchmarks.Suites.Scenario;

[BenchmarkCategory(BenchmarkCategory.Scenario)]
[MemoryDiagnoser]
public class MyScenarioBenchmark
{
	private Grid _grid = null!;

	[GlobalSetup]
	public Task Setup() => TestServices.WindowHelper.RootElementDispatcher.RunAsyncWithBypass(() =>
	{
		_grid = BuildGrid();
	});

	[Benchmark]
	public Task DoTheThing() => TestServices.WindowHelper.RootElementDispatcher.RunAsyncWithBypass(() =>
	{
		_grid.Measure(new Size(500, 500));
		_grid.Arrange(new Rect(0, 0, 500, 500));
	});
}
```

`RunAsyncWithBypass` skips the dispatcher hop when already on the UI thread,
which keeps the measure-only hot path uncontaminated by dispatcher overhead.

## Render template

Render benchmarks set up a visual scene via `RenderBenchmarkHost.SetupSceneAsync`,
drive frames via `RenderBenchmarkHost.RenderFrameAsync`, and reset between runs
via `RenderBenchmarkHost.ResetSceneAsync`. Default pattern is 60 frames per
iteration so total iteration time is comparable to a real one-second of real-time
rendering.

```csharp
[BenchmarkCategory(BenchmarkCategory.Render)]
[MemoryDiagnoser]
public class MyRenderBenchmark
{
	[GlobalSetup]
	public Task Setup() => RenderBenchmarkHost.SetupSceneAsync(BuildScene);

	[Benchmark]
	public async Task Render60Frames()
	{
		for (int i = 0; i < 60; i++)
		{
			await RenderBenchmarkHost.RenderFrameAsync();
		}
	}

	[GlobalCleanup]
	public Task Cleanup() => RenderBenchmarkHost.ResetSceneAsync();
}
```

Render benchmarks are skipped on platforms without a real presenter (Linux
Framebuffer in v1; Wasm/Android/iOS gated until per-platform noise-budget work
is done).

## Common pitfalls

| Mistake | Why it matters |
|---------|----------------|
| Doing setup work inside `[Benchmark]` | The setup time is averaged into the measurement; results become meaningless. |
| Allocating in `[Benchmark]` when not testing alloc | Inflates `MemoryDiagnoser` numbers and pollutes the Mean too via GC. |
| Forgetting `await` on async benchmarks | BDN measures the time-to-task-creation rather than actual work. |
| Holding state across iterations that grows unboundedly | Memory leaks compound; later iterations get artificially slower. |
| Returning `void` from a benchmark whose result the JIT could elide | The JIT may dead-code-eliminate the work. Return a value. |
| Using `Task.Run` inside a benchmark to escape the dispatcher | The marshal cost is the workload; bypass deliberately or use the dispatcher's `RunAsyncWithBypass`. |

## Verify locally

Build the Skia solution filter, then run only your new benchmark:

```bash
cd src
dotnet test Uno.UI.RuntimeTests/Uno.UI.RuntimeTests.Skia.csproj \
  --filter "FullyQualifiedName~MyApiBenchmark"
```

The MSTest fixture will pick up your class automatically. If you don't see it in
the output, check:

1. The class is in namespace `Uno.UI.RuntimeTests.Tests.Benchmarks.Suites.<category>.*`.
2. The class is public, non-abstract, non-generic, has parameterless ctor.
3. At least one method has `[Benchmark]`.

## CI behavior

Once committed, your benchmark runs on master + nightly across all live targets,
publishes a JSONL row to the data branch on every run, and shows up on the
dashboard within minutes of the publish job completing.

No fixture-file edits required. No yaml edits required.
