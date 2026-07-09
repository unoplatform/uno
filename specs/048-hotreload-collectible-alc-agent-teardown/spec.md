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
   disposes its `HotReloadAgent`), then releases the per-context statics via the shared
   `ReleasePerContextStatics()` helper (disposing the shared `ElementUpdateAgent` and clearing
   the static references). A live (default-context) processor is never touched.

4. **Host-driven `ClientHotReloadProcessor.Dispose()` releases the per-context statics
   (Uno.UI.RemoteControl/HotReload/ClientHotReloadProcessor.cs):** when the processor copy is
   owned by a collectible non-default context, `Dispose()` — in addition to disposing its
   `_agent` — clears the static `_instance` (when it references the disposing instance) and
   calls `ReleasePerContextStatics()`. This makes an explicit host dispose sufficient on
   runtimes where `Unloading` never fires (browser-wasm), instead of leaving the shared
   `ElementUpdateAgent` subscribed to `AppDomain.AssemblyLoad` and pinning the context. The
   flow is deliberately non-recursive (`Dispose()` never calls `TearDownForAlcUnload()`, and
   the unload hook detaches `_instance` before disposing it) and idempotent (double-dispose
   and dispose-then-unload are both safe). Default-context `Dispose()` behavior is unchanged —
   a live host processor never tears down the shared element agent.

## Caveat: `Unloading` is not raised on browser-wasm

`AssemblyLoadContext.Unloading` was observed **not** to be raised on the browser-wasm runtime
— collectible-context unload is unimplemented there (dotnet/runtime#34072). The `Unloading`
hook is therefore best-effort and only helps on runtimes that raise the event. The
**load-bearing** path for wasm hosts is the `Dispose()`-side release (fixes 1, 2 and 4): a
host that disposes the processor during its own teardown genuinely releases the context,
without relying on `Unloading`.

## Validation

Seven `[TestMethod]`s in
`Uno.UI.RemoteControl.DevServer.Tests/HotReload/HotReloadAgentDisposeTests.cs` cover the fix.
Five of them (marked "Red before fix N") fail on the pre-fix agent `Dispose` bodies, which only
detached the `AssemblyLoad` subscription and left the caches/statics populated; the other two
(`AssemblyLoadSubscriptionDetached` and `SharedElementAgentUntouched`) are non-regression guards
that stay green either way.

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
- `When_HotReloadAgentDisposed_Then_HandlerActionsCacheCleared` — after `HotReloadAgent.Dispose()`,
  the `_handlerActions` cache (which captures handler delegates and `Type`s discovered by
  scanning the owning context's assemblies) is null. Red before fix 2; green after.
- `When_ElementUpdateAgentDisposed_Then_AssemblyLoadSubscriptionDetached` — guards the
  process-wide `AppDomain.AssemblyLoad` unsubscription (observed through the runtime's
  `AssemblyLoadContext.AssemblyLoad` backing field, which the `AppDomain` event forwards to).
- `When_CollectibleContextProcessorDisposed_Then_PerContextStaticsReleased` — loads a real
  copy of `Uno.UI.RemoteControl` into a collectible `AssemblyLoadContext` (the copy's
  `_processorAlc` then resolves to that context, so the production gate is exercised, not
  simulated), seeds the copy's static `_elementAgent`, and proves that a host-driven
  `Dispose()` releases the static agent and detaches its `AssemblyLoad` subscription, and
  that double-dispose is safe. The collectible context is `Unload()`ed in a `finally` after the
  assertions. Red before fix 4; green after.
- `When_DefaultContextProcessorDisposed_Then_SharedElementAgentUntouched` — proves the
  default-context `Dispose()` behavior is unchanged: the shared element agent stays alive
  and subscribed.
- `When_TearDownForAlcUnload_WithLiveInstance_Then_TearsDownWithoutRecursion` — drives the
  `Unloading`-path teardown against a collectible copy with a live `_instance` whose
  `Dispose()` now re-enters the static release, proving the flow neither recurses nor throws
  on repeat. The collectible context is `Unload()`ed in a `finally` after the assertions.

The agents' internals are exercised via `InternalsVisibleTo("Uno.UI.RemoteControl.DevServer.Tests")`,
which already exists; the private `_appliedAssemblies`/`_deltas` counts are read reflectively
to keep the test a true unit test with no UI scaffolding.

Evidence: with the committed fix all seven tests pass (7/7). Reverting only the two agent
`Dispose` bodies to the pre-fix expression form (`=> AppDomain.CurrentDomain.AssemblyLoad -= _assemblyLoad;`)
turns five assertions red — `HandlerMapCleared` on the non-empty handler map,
`DeltaAndAssemblyMapsCleared` on `_deltas` still holding the stashed delta,
`HandlerActionsCacheCleared` on the non-null cache, and the two collectible-context /
teardown tests (`PerContextStaticsReleased`, `TearDownForAlcUnload_WithLiveInstance...`) on the
released agent's `AssemblyLoad` subscription still being attached — while the two guards
(`AssemblyLoadSubscriptionDetached`, `SharedElementAgentUntouched`) stay green.

## Notes / follow-ups

- End-to-end, in a downstream host that repeatedly loads/unloads previewed apps in collectible
  contexts, heap-dump root-walks previously showed the unloaded context pinned via
  `HotReloadAgent`/`ElementUpdateAgent` (`AppDomain.AssemblyLoad`) and the processor statics;
  with a host that disposes on teardown those referrers are gone and the managed context object
  is collected.
- Other collectible-ALC pins in the UI layer (parse-context, resource-dictionary caches,
  resource `DataContext`) are addressed separately in spec 044.
