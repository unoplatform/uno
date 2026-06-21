# Spec 042 — Implementation Progress

> Spec: [spec.md](./spec.md) — Branch: dev/cdb/element-ref-id

## Production code

- [x] `IElementRefHandleRegistry.cs`
- [x] `ElementRefHandle.cs` (facade + SetForTesting)
- [x] `ElementRefHandleRegistry.cs` (internal sealed)
- [x] `FeatureConfiguration.ElementRefHandle.DisableThreadingCheck`

## Tests (Uno.UI.Tests)

> Note: local build of `Uno.UI.Tests.csproj` is blocked by a pre-existing
> source-generator regression on `master` (344 errors in `DependencyPropertyMixins.g.cs`,
> unrelated to this feature). Tests will be validated in CI once the regression is fixed.

- [x] GetOrCreate_SameElement_ReturnsSameHandle
- [x] GetOrCreate_DifferentElements_ReturnsDifferentHandles
- [x] TryResolve_ValidHandle_ReturnsTrueAndElement (no implicit rooting)
- [x] TryResolve_UnknownHandle_ReturnsFalse
- [x] TryResolve_NullOrEmpty_ReturnsFalse
- [x] TryResolve_AfterGC_ReturnsFalse
- [x] TryResolve_HandleComparison_IsCaseInsensitive
- [x] GetOrCreate_FromBackgroundThread_Throws
- [x] TryResolve_FromBackgroundThread_Throws
- [x] GetOrCreate_WithDisableThreadingCheck_DoesNotThrow
- [x] Finalizer_RemovesReverseMapEntry
- [x] Handles_DoNotRecycle_AfterGC_WithinSession
- [x] SetForTesting_RestoresDefaultOnDispose

## Validation

- [ ] `dotnet build src/Uno.UI-UnitTests-only.slnf --no-restore` — blocked (pre-existing)
- [ ] `dotnet test src/Uno.UI/Uno.UI.Tests.csproj --filter "FullyQualifiedName~Given_ElementRefHandleRegistry"` — pending CI
- [ ] Full unit-test suite green — pending CI
- [x] Public API diff reviewed (no PublicAPI analyzer in Uno.UI)

## Downstream alignment (tracked separately)

- [ ] Downstream consumers updated to use `ElementRefHandle` (out of scope for this PR)
