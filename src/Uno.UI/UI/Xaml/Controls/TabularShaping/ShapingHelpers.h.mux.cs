// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TabularShaping\ShapingHelpers.h, tag winui3/main, commit 79209e041

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.UI.Xaml.Interop;
using Windows.Foundation.Collections;
using _IBindableVector = System.Collections.IList;
using INotifyCollectionChanged = System.Collections.Specialized.INotifyCollectionChanged;

namespace Microsoft.UI.Xaml.Controls.Tabular;

// Free functions declared here and defined in ShapingHelpers.cpp are C# partial method
// declarations, implemented in ShapingHelpers.mux.cs. std::vector& parameters are mutated in place
// (their contents replaced), exactly as the C++ reference is.
internal static partial class ShapingHelpers
{
	// A group object that already knows its own stable identity.
	//
	// The key OBJECT is not a usable identity on its own. A key selector run twice can return
	// equal-but-not-identical objects, and a reference-typed key has no value form at all, so
	// re-deriving an identity from the key either flips it between shapes or yields nothing. A
	// group that minted an identity when it was built publishes it here, and consumers read it
	// instead of guessing.
	//
	// This lives in layer 1 because the layer that implements it and the layer that reads it are
	// siblings: neither may include the other's headers, so the shared contract has to sit below
	// both.
	// TODO Uno: COM tag interface __declspec(uuid("E74C4DC8-3FAC-4A24-AD34-01547B4672DF")); try_as becomes an `is` check.
	internal interface IGroupIdentity
	{
		string StableGroupIdentity();
	}

	// A group's count of IMMEDIATE children. Over a hierarchy a group's rows include visible
	// descendants, so only the projection that sliced the group knows the root count.
	// TODO Uno: COM tag interface __declspec(uuid("9D3B0F7A-6B2E-4E67-93A2-1C8A9F5E4B10")); try_as becomes an `is` check.
	internal interface IGroupChildCount
	{
		int GroupChildCount();
	}

	// Value keys compare numerically equal floats as equal: -0 folds into +0 and every NaN into one
	// canonical NaN, so the key is not the raw bit pattern.
	internal static uint CanonicalFloatBits(float value)
	{
		if (float.IsNaN(value)) { return 0x7FC00000u; }
		if (value == 0.0f) { return 0u; }
		return BitConverter.SingleToUInt32Bits(value);
	}

	internal static ulong CanonicalDoubleBits(double value)
	{
		if (double.IsNaN(value)) { return 0x7FF8000000000000ul; }
		if (value == 0.0) { return 0ul; }
		return BitConverter.DoubleToUInt64Bits(value);
	}

	internal static partial class ValueKey
	{
		// TODO Uno: IPropertyValue projection. The parameter is the boxed value; its WinRT property type is
		// resolved through ValueConversionHelpers.GetPropertyType.
		internal static partial bool TryFormatPropertyValue(
			object propertyValue,
			out string key,
			bool rejectEmptyString = false);

		internal static partial bool TryGetStablePropertyKey(
			object? value,
			out string key,
			bool rejectEmptyString = false);

		internal static partial string ToString(object? value);
		internal static partial string ToObjectLookupKey(object? value, bool rejectEmptyString = false);
	}

	internal static partial class ValueComparer
	{
		internal static partial int Compare(object? a, object? b);

		// Overload for callers that can hoist the reference-key tiebreak string out of the
		// comparison loop. `ValueKey::ToString` may call into app code (IStringable), so
		// recomputing it per comparison is both O(n log n) app calls and a strict-weak-ordering
		// hazard when the app's ToString is not deterministic. Pass nullptr to compute on demand.
		internal static partial int Compare(
			object? a,
			object? b,
			string? fallbackKeyA,
			string? fallbackKeyB);

		// True when Compare would fall through to the ToString-based tiebreak for this value,
		// i.e. it is a non-null, non-IPropertyValue reference.
		internal static partial bool UsesFallbackKey(object? value);
	}

	// Whose collection is read. A read of the caller's collection that throws is rethrown tagged as the caller's
	// (RethrowAsCallerError), except running out of memory while enumerating a collection with no indexed read, which
	// may be shaping's own allocation; a read of shaping's own collections, such as the group list it builds, never is.
	internal enum CollectionOwner { Shaping, Caller }

	internal static partial List<object?> EnumerateInspectableItems(
		object? source,
		bool throwIfUnsupported = false,
		CollectionOwner owner = CollectionOwner.Shaping);

	// The kind a CallerError carries when the caller's collection or contract failed. An owner maps kinds onto its own
	// fault vocabulary; TableViewTip::AppFaultKind::Data is this value.
	internal const uint c_callerDataFault = 6;

	// An exception that left the caller's code: its collection, a broken contract, or a callback an owner ran. It
	// keeps the original HRESULT and error info, so the caller sees the same error, and records whose fault it was,
	// so an owner attributes a failure by the exception's type instead of by matching HRESULTs.
	// Uno specific: a managed wrapper would change the type the app catches, where the C++ ABI only sees the HRESULT, so
	// the kind is stored on the original exception instead of in a CallerError type.
	private const string c_callerErrorKindKey = "Uno.Tabular.CallerErrorKind";

	internal static Exception TagCallerError(Exception error, uint kind)
	{
		error.Data[c_callerErrorKindKey] = kind;
		return error;
	}

	// The caller's code ran out of memory (C++/WinRT throws std::bad_alloc for E_OUTOFMEMORY). Still an
	// OutOfMemoryException (the std::bad_alloc counterpart), so it leaves as E_OUTOFMEMORY without allocating,
	// and records whose fault it was like CallerError.
	internal sealed class CallerOutOfMemory : OutOfMemoryException
	{
		internal CallerOutOfMemory(uint kind) { m_kind = kind; }

		internal uint Kind() => m_kind;

		private readonly uint m_kind;
	}

	// In a catch: rethrows `error` tagged with `kind`, or a CallerOutOfMemory when the
	// caller's code ran out of memory. A tagged exception keeps its kind; any other exception leaves unchanged.
	// TODO Uno: C++ reads the current exception with a bare `throw;`; C# takes it as a parameter.
	[DoesNotReturn]
	internal static partial void RethrowAsCallerError(Exception error, uint kind);
	// In a catch: `error`'s kind when it is tagged, else 0.
	internal static partial uint CurrentCallerErrorKind(Exception error);

	// Caller delegates that threw and were contained by design (a predicate excludes its row, a key selector yields a
	// null key, a comparer calls the pair equal): how many, and the first one's HRESULT. An owner takes the tally
	// after a verb, so it can report the caller's fault without failing the verb.
	// TODO Uno: a class rather than a struct; C++ passes it by pointer/reference (SwallowedFaults* contained,
	// SwallowedFaults&), so the shared instance stands in for the pointer.
	internal sealed class SwallowedFaults
	{
		public uint count;
		public int firstError;

		// In a catch. Reads only the first fault's HRESULT.
		public void Note(Exception error)
		{
			if (count == 0)
			{
				firstError = error is OutOfMemoryException ? unchecked((int)0x8007000E) : error.HResult;
			}
			if (count != uint.MaxValue) { ++count; }
		}
	}

	// Resolves ONCE what a collection is and how to read it.
	//
	// A XAML items source can arrive as any of half a dozen interfaces, and the stack needs three
	// different things from it: read it by index, enumerate all of it, and observe it changing.
	// Asking those questions with a separate try_as ladder at each call site is how the ladders
	// drift: one of them omitted IVectorView, so for an IVectorView-only source the count answered
	// while the indexed read refused, and the incremental path silently degraded. Resolving the
	// interface once and reading every answer off the same resolution is what makes the answers
	// agree by construction.
	//
	// This lives in layer 1 because both layer 2 (the live projection) and layer 3 (grouping, which
	// classifies each group's items) need it, and siblings may not include each other's headers.
	// TODO Uno: a C++ value type (default-constructed members, assigned by copy), so it ports as a struct.
	// CollectionAccessor() = default is the implicit parameterless constructor; the explicit
	// CollectionAccessor(object? source, CollectionOwner owner) constructor is in ShapingHelpers.mux.cs.
	internal partial struct CollectionAccessor
	{
		// True when Count/GetAt are a direct call rather than an enumeration. An enumerable-only
		// source reports false; callers that need indexed access degrade instead of materializing
		// behind the caller's back.
		public partial bool IsIndexable();
		// True when the source publishes changes through any supported notification interface.
		public bool IsObservable() => m_notifyCollectionChanged is not null || m_observableVector is not null || m_bindableObservableVector is not null;

		// Read through to the live collection rather than a value captured at construction: this
		// classifies a source that goes on mutating, so a cached count would go stale.
		public partial uint Count();
		public partial bool TryGetAt(uint index, out object? item);
		public partial List<object?> Enumerate(bool throwIfUnsupported = false);

		// The resolved notification interfaces, so a subscriber binds to one without re-probing.
		// Precedence is the caller's: INotifyCollectionChanged carries per-change detail, the
		// vector interfaces carry an index and a verb.
		public INotifyCollectionChanged? AsNotifyCollectionChanged() => m_notifyCollectionChanged;
		public IObservableVector<object?>? AsObservableVector() => m_observableVector;
		public IBindableObservableVector? AsBindableObservableVector() => m_bindableObservableVector;

		// The indexed read itself, with no bounds check. Enumerate() has already established the
		// range and would otherwise pay a Size() call per item to re-derive it.
		private partial object? GetAtUnchecked(uint index);

		private object? m_source;
		private bool m_callerOwned;

		// Read: at most one of these is set, in IVector > IVectorView > IBindableVector order.
		private IList<object?>? m_vector;
		// TODO Uno: IVectorView<IInspectable> projects as IReadOnlyList<T>, IBindableVector as System.Collections.IList.
		private IReadOnlyList<object?>? m_vectorView;
		private _IBindableVector? m_bindableVector;

		// Observe: independent of the read resolution, since a source can be indexable through one
		// interface and observable through another.
		private INotifyCollectionChanged? m_notifyCollectionChanged;
		private IObservableVector<object?>? m_observableVector;
		private IBindableObservableVector? m_bindableObservableVector;
	}

	// Canonical predicate filter shared by Tabular shaping engines. Retains items for which
	// predicate(item) returns true; a predicate that throws is treated as "exclude", which is the
	// fail-safe every caller wants: a broken predicate hides rows rather than failing the shape. In-place; preserves relative order of kept items.
	// `contained`, when given, counts the predicates that threw.
	// std::function<bool(IInspectable const&)> is the same type as the Predicate alias, so the delegate is reused.
	internal static partial void ApplyPredicateFilter(
		List<object?> items,
		Predicate? predicate,
		SwallowedFaults? contained = null);

	// Canonical multi-axis stable sort shared by every Tabular shaping engine.
	// The caller supplies key extraction
	// (item, axisIndex) -> key and the per-axis sort direction; the routine performs a
	// decorate-sort-undecorate stable sort keyed by ValueComparer, which guarantees a
	// strict-weak-ordering. Extracting key selection into a functor lets a delegate-based and a
	// property-name/reflection-based engine share one sort implementation.
	internal static partial void StableSortByKeys(
		List<object?> items,
		int axisCount,
		Func<object?, int, object?> extractKey,
		Func<int, SortDirection> axisDirection);

	// One ordered group bucket carrying its representative key + identity, produced by
	// BucketizeToGroups. The Key is retained (not just the identity string) so grouping adapters
	// can drive per-group object caches and collision policy that need the original key instance.
	// TODO Uno: a class rather than a struct; every C++ access goes through a reference (auto& bucket),
	// and a C# struct would copy on each foreach.
	internal sealed class KeyedBucket
	{
		public object? Key;
		public string Identity = "";
		public List<object?> Items = new();
	}

	// TODO Uno: the C++ callback is an unnamed std::function<bool(IInspectable const&, hstring&, wchar_t const*&)>;
	// C# needs a named delegate to carry the reference parameters.
	internal delegate bool ResolveIdentityCallback(object? key, ref string identity, ref string? reason);

	// Canonical keyed group-bucketization shared by grouping adapters. Walks `items` once,
	// resolving each item's group key and stable identity string, bucketizing by identity while
	// preserving first-seen group order (items keep input order within a group). Returns the
	// buckets WITH their keys so the adapter can keep its own
	// identity/collision policy and group-object cache Pure — no XAML/dispatcher — so it is headless-testable.
	//
	// Callbacks (all supplied by the adapter, which owns the WinRT-coupled policy):
	//   resolveKey(item)            -> the group key (adapter wraps its selector incl. throw->null).
	//   resolveIdentity(key,id,why) -> false signals the bucketization CANNOT be produced (unstable
	//                                  or unresolvable identity); *why is a static reason string.
	//   keysConsideredEqual(a,b)    -> false on a genuine identity COLLISION (same identity string,
	//                                  logically-different keys), which also fails the bucketization.
	// Returns true with outBuckets populated (first-seen order); returns false and sets
	// rejectReason otherwise.
	//
	// This function itself does not throw and has no opinion about recovery -- it is pure, so it
	// reports and the caller decides. What the caller decides is NOT open, though: the shipped
	// policy is FAIL FAST. ShapedItemsSource::RebuildGroupedRows turns a false return into
	// hresult_invalid_argument. It deliberately does NOT fall back to a flat projection: a
	// grouping request that silently renders ungrouped is a bug an app ships without noticing.
	// A new caller that "recovers" by flattening is reintroducing exactly that bug.
	internal static partial bool BucketizeToGroups(
		List<object?> items,
		KeySelector? resolveKey,
		ResolveIdentityCallback? resolveIdentity,
		Func<object?, object?, bool>? keysConsideredEqual,
		List<KeyedBucket> outBuckets,
		ref string? rejectReason);

	// Upper-bound insertion index shared by the incremental fast-paths. Given an already-sorted
	// range of `count` items, returns the position where a newly-arrived item should be inserted
	// so it lands AFTER any equal-key items — matching the append order a stable_sort produces for
	// a new row. compareNewToExisting(i) compares the NEW item against the existing item at index i
	// (< 0 when the new item sorts strictly before it, >= 0 otherwise). Pure binary search — no
	// XAML/collection type — so every incremental fast path can share it and it
	// is directly headless-unit-testable.
	internal static partial uint UpperBoundInsertIndex(
		uint count,
		Func<uint, int> compareNewToExisting);

	// A pairwise ordering supplied by app code: returns < 0, 0, or > 0. The engine never hands one
	// to a std:: sort - it is untrusted and may be non-transitive, throwing, or reentrant, any of
	// which is undefined behaviour inside optimized sort internals. CustomSortRankAdapter is the
	// one place that ever invokes it, and it does so defensively.
	internal delegate int PairwiseComparer(object? a, object? b);

	// Adapts an untrusted pairwise comparer into stable integer sort keys, so the key-based engine
	// can order a comparer column as an ordinary axis instead of ever touching the comparer. The
	// comparer is run ONCE over the rows and each item's ordinal becomes its key; every later
	// reshape reads the frozen integer keys. Memory-safe under ANY comparer: a non-transitive,
	// throwing, or reentrant comparer yields a wrong-but-valid order, never framework corruption.
	//
	// This is the rank-system adapter that used to live in the control. It stays out of the pure
	// key engine proper (StableSortByKeys never sees a comparer) but belongs in the shaping
	// substrate rather than in TableView, so any key-based consumer can reduce a pairwise comparer
	// to a sort key the same way.
	internal sealed partial class CustomSortRankAdapter
	{
		// Runs `comparer` once over `rows` through a memory-safe stable merge sort and freezes
		// dense integer ranks. Equal items share a rank, so the projection imposes no order on a
		// tie the comparer called equal. Replaces any prior ranking and adopts `comparer` as the
		// one used to place late-arriving rows.
		public partial void Rank(PairwiseComparer? comparer, List<object?> rows);

		// The sort key for an item, boxed as int32. O(1) for a row the rank pass saw; a row added
		// afterwards is located among the existing ranks by the comparer and inserted, shifting the
		// ranks above it. Returns nullptr when no comparer is set, or when the state was cleared
		// underneath a reentrant call.
		public partial object? KeyFor(object? item);

		// Drops the comparer and all ranks. The adapter object itself is retained so a key selector
		// that closed over it by shared_ptr stays valid.
		public partial void Reset();

		// Re-places an item whose comparer-read property changed; true when any row's key changed.
		// With deferPlacement the rank is only dropped and the next KeyFor re-places it (or
		// re-ranks everything once that is cheaper).
		public partial bool Reposition(object? item, bool deferPlacement);

		// Every ranked item may have changed untracked (live shaping was off); the next key read
		// re-ranks everything once.
		public partial void InvalidateRanks();

		public bool HasComparer() => m_comparer is not null;

		// Drop rows that left the source so the ranks stop keeping them alive. Rank gaps left
		// behind are harmless: keys only have to order. Both are no-ops inside a comparer callback.
		public partial void Forget(object? item);
		public partial void RetainOnly(List<object?> liveRows);

		// The comparer calls that threw since the last take (SafeCompare); resets the tally.
		// TODO Uno: std::exchange(m_swallowedFaults, {}) returns the old value and leaves a fresh one.
		public SwallowedFaults TakeSwallowedFaults()
		{
			var taken = m_swallowedFaults;
			m_swallowedFaults = new();
			return taken;
		}

		private struct RankEntry
		{
			public object? Item;
			public int Rank;
			// COM identity (IUnknown), cached so lookups compare pointers instead of calling QI.
			// TODO Uno: void* becomes the object reference itself, which is its reference identity.
			public object? Identity;
		}

		private static partial object? IdentityOf(object? item);
		// m_ranks stays ordered by rank; PlaceUnranked relies on it to binary-search.
		private partial void AddRanked(object? item, int rank);
		private partial void InsertRanked(int position, object? item, int rank);

		// App code, so it never escapes: a throwing comparer degrades to "equal", which keeps the
		// merge stable rather than random, and is counted (TakeSwallowedFaults). Also normalizes the
		// result to -1 / 0 / 1.
		private partial int SafeCompare(object? left, object? right);

		// Orders two row indices under the comparer, breaking ties by source index so the sort is
		// stable. Sets staleState and returns false when the ranks were cleared mid-merge (a
		// reentrant comparer callback).
		private partial bool IndexComesBefore(
			List<object?> rows,
			ulong generation,
			int leftIndex,
			int rightIndex,
			ref bool staleState);

		// Deliberately not std::stable_sort: that would hand an app comparer to optimized library
		// internals where a non-strict-weak-ordering is UB. Every access here is loop-bound, so a
		// bad comparer can only produce a strange order, never memory corruption, at the same
		// O(n log n) comparer calls. Returns false when the ranks were cleared mid-merge.
		private partial bool StableMergeSortOrder(
			List<int> order,
			List<object?> rows,
			ulong generation);

		// Bumped every time the ranks are cleared. A comparer callback can synchronously re-enter
		// and clear this state, so anything that resumes after app code checks the generation
		// before trusting ranks written for a pass that is no longer current.
		private partial void ClearRanks();

		// Orders `rows` with the comparer and writes their dense ranks. Callers hold the reentrancy
		// guard and have just cleared the ranks.
		private partial void PopulateRanks(List<object?> rows, ulong generation);

		// Re-ranks every ranked and evicted item; current order breaks ties.
		private partial void RerankAll();

		// Places an unranked item. False when a reentrant comparer cleared the ranks. `tied`: it
		// joined an existing rank.
		private partial bool PlaceUnranked(object? item, ulong generation, out int rank, out bool tied);

		// Drops an item's rank (closing any gap) and queues it for re-placement. False if unranked.
		private partial bool EvictEntry(object? item, out int oldRank, out bool wasShared);

		// Re-places all evicted items before handing out any key, so keys are consistent.
		private partial void PlaceEvicted();

		// True when re-placing the evicted items costs more than re-ranking everything.
		private partial bool ShouldRerankAll();

		private PairwiseComparer? m_comparer;
		private List<RankEntry> m_ranks = new();
		// Items whose rank was dropped by Evict and not yet re-placed.
		private List<object?> m_evicted = new();
		// Set once enough items are evicted that PlaceEvicted will re-rank everything; further
		// deferred evictions are then skipped (the re-rank covers them).
		private bool m_rerankAllPending;
		// O(1) identity -> rank lookup for the common (reference-type item) case; falls back to the
		// comparer scan on a miss, e.g. a boxed value type whose CCW churned.
		// TODO Uno: unordered_map<void*, int32_t> keyed by the IUnknown address becomes a reference-equality dictionary.
		private Dictionary<object, int> m_rankByIdentity = new(ReferenceEqualityComparer.Instance);
		private ulong m_generation;
		// Reentrancy guard for the app comparer. Both rank paths iterate m_ranks and invoke the
		// comparer inside that iteration; a comparer that re-enters lands in the same vector and
		// reallocates it under the outer loop's iterators - UB, in practice a crash. A reentrant
		// comparer is an app bug (a sort predicate must be a pure function of its inputs), so fail
		// fast in chk and no-op safely in fre. WinUI is single-threaded, so a plain bool suffices.
		private bool m_comparerActive;
		private SwallowedFaults m_swallowedFaults = new();
	}

}
