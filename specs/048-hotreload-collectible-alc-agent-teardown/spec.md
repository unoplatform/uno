# 048 — Hot-Reload Client Agents Release Collectible-ALC State On Teardown

Issue: #23704

## Problem

A downstream host loads previewed apps into their own collectible
`AssemblyLoadContext`s (see spec 000). Each such context brings a per-context copy of
`Uno.UI.RemoteControl`, so the hot-reload client agents defined in that assembly run once
per context. When the host unloads a previewed app it calls `AssemblyLoadContext.Unload()`,
but the context was never collected: the hot-reload client machinery left per-context state
behind that kept the unloaded context reachable, leaking one full context per load/unload
cycle.

Heap-dump root-walks against a leaked context traced the pins to the two client agents:

- **Process-wide `AppDomain.CurrentDomain.AssemblyLoad` subscription.** Both
  `ElementUpdateAgent` and `HotReloadAgent` subscribe to `AssemblyLoad` at construction and
  keep the delegate for their lifetime. The delegate target is the agent instance, which
  captures the context's `AssemblyLoadContext` and its assemblies; because the event lives on
  the process-immortal `AppDomain`, the whole per-context object graph stays reachable after
  unload.
- **Type/assembly-keyed caches.** `ElementUpdateAgent._elementHandlerActions` is keyed on the
  context's element `Type`s; `HotReloadAgent._deltas` and `HotReloadAgent._appliedAssemblies`
  hold the context's module ids and `Assembly` instances. A single retained collectible
  `Type` or `Assembly` pins its `LoaderAllocator` and, through it, the entire previous app
  graph.

## Root cause

The agents' `Dispose()` implementations only detached the `AssemblyLoad` subscription; they
left the type- and assembly-keyed caches populated. There was also no path that disposed the
agents when the owning collectible context was torn down, so nothing released the state a
host teardown should have released.

## Fix

1. **`ElementUpdateAgent.Dispose()`
   (Uno.UI.RemoteControl/HotReload/MetadataUpdater/ElementUpdaterAgent.cs):** in addition to
   detaching the `AssemblyLoad` handler, now clears `_elementHandlerActions` so the context's
   element `Type` keys are no longer retained.

2. **`HotReloadAgent.Dispose()`
   (Uno.UI.RemoteControl/HotReload/MetadataUpdater/HotReloadAgent.cs):** in addition to
   detaching the `AssemblyLoad` handler, now clears `_deltas` and `_appliedAssemblies` so the
   context's module ids and `Assembly` references are no longer retained.

3. **Best-effort self-teardown
   (Uno.UI.RemoteControl/HotReload/ClientHotReloadProcessor.Common.cs / .Agent.cs):** the
   processor arms an `AssemblyLoadContext.Unloading` hook on its owning context, but only when
   that context is collectible and not the default. On unload it disposes the processor (which
   disposes its `HotReloadAgent`), disposes the shared `ElementUpdateAgent`, and clears the
   per-context statics. A live (default-context) processor is never touched.

## Caveat: `Unloading` is not raised on browser-wasm

`AssemblyLoadContext.Unloading` was observed **not** to be raised on the browser-wasm runtime
— collectible-context unload is unimplemented there (dotnet/runtime#34072). The `Unloading`
hook is therefore best-effort and only helps on runtimes that raise the event. The
**load-bearing** path for wasm hosts is the `Dispose()`-side cache clearing: a host that
disposes the processor/agents during its own teardown genuinely releases the context, without
relying on `Unloading`.

## Validation

Red/fix/green (both tests fail on the pre-fix `Dispose`, which only detached the
`AssemblyLoad` subscription):

- `Uno.UI.RemoteControl.DevServer.Tests/HotReload/HotReloadAgentDisposeTests.cs`
  - `When_ElementUpdateAgentDisposed_Then_HandlerMapCleared` — after `ElementUpdateAgent.Dispose()`,
    the `Type`-keyed `ElementHandlerActions` map is empty (the assembly-level
    `[ElementMetadataUpdateHandlerAttribute]`s in the test assembly cause handlers to be
    discovered at construction, so the map is non-empty before dispose). Red before fix 1;
    green after.
  - `When_HotReloadAgentDisposed_Then_DeltaAndAssemblyMapsCleared` — after
    `HotReloadAgent.Dispose()`, `_deltas` (populated via the public `ApplyDeltas` path with a
    random module id that matches no loaded module, so nothing is applied to the runtime) and
    `_appliedAssemblies` (seeded via reflection, as it has no public writer) are both empty.
    Red before fix 2; green after.

The agents' internals are exercised via `InternalsVisibleTo("Uno.UI.RemoteControl.DevServer.Tests")`,
which already exists; the private `_appliedAssemblies`/`_deltas` counts are read reflectively
to keep the test a true unit test with no UI scaffolding.

Evidence: with the committed fix the two tests pass (2/2); reverting only the two `Dispose`
bodies to the pre-fix expression form (`=> AppDomain.CurrentDomain.AssemblyLoad -= _assemblyLoad;`)
makes both fail (2/2), one on the non-empty handler map and one on `_deltas` still holding a
stashed delta.

## Notes / follow-ups

- End-to-end, in a downstream host that repeatedly loads/unloads previewed apps in collectible
  contexts, heap-dump root-walks previously showed the unloaded context pinned via
  `HotReloadAgent`/`ElementUpdateAgent` (`AppDomain.AssemblyLoad`) and the processor statics;
  with a host that disposes on teardown those referrers are gone and the managed context object
  is collected.
- Other collectible-ALC pins in the UI layer (parse-context, resource-dictionary caches,
  resource `DataContext`) are addressed separately in spec 044.
