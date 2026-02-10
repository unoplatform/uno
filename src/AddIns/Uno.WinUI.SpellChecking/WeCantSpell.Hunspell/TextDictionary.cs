// UNO PREAMBLE START

// Note that this license is inherited from the original Hunspell project
// 	as this is derived from that work. Original license:
//
// Version: MPL 1.1/GPL 2.0/LGPL 2.1
//
// The contents of this file are subject to the Mozilla Public License Version
// 1.1 (the "License"); you may not use this file except in compliance with
// the License. You may obtain a copy of the License at
// http://www.mozilla.org/MPL/
//
// Software distributed under the License is distributed on an "AS IS" basis,
// WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License
// for the specific language governing rights and limitations under the
// License.
//
// 	The Original Code is Hunspell, based on MySpell.
//
// 	The Initial Developers of the Original Code are
// Kevin Hendricks (MySpell) and NÃ©meth LÃ¡szlÃ³ (Hunspell).
// 	Portions created by the Initial Developers are Copyright (C) 2002-2005
// the Initial Developers. All Rights Reserved.
//
// 	Contributor(s):
// David Einstein 
// Davide Prina
// Giuseppe Modugno 
// Gianluca Turconi
// Simon Brouwer
// Noll JÃ¡nos
// BÃ­rÃ³ ÃrpÃ¡d
// Goldman EleonÃ³ra
// SarlÃ³s TamÃ¡s
// BencsÃ¡th BoldizsÃ¡r
// HalÃ¡csy PÃ©ter
// Dvornik LÃ¡szlÃ³
// Gefferth AndrÃ¡s
// Nagy Viktor
// Varga DÃ¡niel
// Chris Halls
// Rene Engelhard
// Bram Moolenaar
// Dafydd Jones
// Harri PitkÃ¤nen
//
// 	Alternatively, the contents of this file may be used under the terms of
// 	either the GNU General Public License Version 2 or later (the "GPL"), or
// 	the GNU Lesser General Public License Version 2.1 or later (the "LGPL"),
// 	in which case the provisions of the GPL or the LGPL are applicable instead
// of those above. If you wish to allow use of your version of this file only
// under the terms of either the GPL or the LGPL, and not to allow others to
// use your version of this file under the terms of the MPL, indicate your
// decision by deleting the provisions above and replace them with the notice
// and other provisions required by the GPL or the LGPL. If you do not delete
// the provisions above, a recipient may use your version of this file under
// the terms of any one of the MPL, the GPL or the LGPL.

// https://github.com/aarondandy/WeCantSpell.Hunspell

#pragma warning disable IDE0055, CA1805, CA1815, CA1310,

// UNO PREAMBLE END

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace WeCantSpell.Hunspell;

/// <remarks>
/// I was hopeful that upcoming changes to <see cref="Dictionary{TKey, TValue}"/>
/// would replace this, but that did not turn out as I expected. Instead, I had to
/// piece this thing together. It works well enough.
/// </remarks>
/// <seealso href="https://github.com/dotnet/runtime/issues/27229">dotnet/runtime/issues/27229</seealso>
[DebuggerDisplay("Count = {Count}")]
internal sealed class TextDictionary<TValue> : IEnumerable<KeyValuePair<string, TValue>>, IDictionary<string, TValue>
{
    private const int MaxCapacity = 0X7FFFFFC7;

    public static TextDictionary<TValue> Clone(TextDictionary<TValue> source)
    {
        var builder = new Builder(source.Count);

        var sourceEntries = source._entries;
        for (var i = 0; i < sourceEntries.Length; i++)
        {
            ref var entry = ref sourceEntries[i];
            if (entry.Key is not null)
            {
                builder.Add(entry.HashCode, entry.Key, entry.Value);
            }
        }

        builder.Flush();

        return new TextDictionary<TValue>(ref builder);
    }

    public static TextDictionary<TValue> Clone<TSourceValue>(TextDictionary<TSourceValue> source, Func<TSourceValue, TValue> valueSelector)
    {
        var builder = new Builder(source.Count);

        var sourceEntries = source._entries;
        for (var i = 0; i < sourceEntries.Length; i++)
        {
            ref var entry = ref sourceEntries[i];
            if (entry.Key is not null)
            {
                builder.Add(entry.HashCode, entry.Key, valueSelector(entry.Value));
            }
        }

        builder.Flush();

        return new TextDictionary<TValue>(ref builder);
    }

    public static TextDictionary<TValue> MapFromDictionary(Dictionary<string, TValue> source)
    {
        var builder = new Builder(source.Count);
        foreach (var entry in source)
        {
            builder.Add(entry.Key, entry.Value);
        }

        builder.Flush();

        return new TextDictionary<TValue>(ref builder);
    }

    internal static TextDictionary<TValue> MapFromPairs(KeyValuePair<string, TValue>[] source)
    {
        var builder = new Builder(source.Length);
        foreach (var entry in source)
        {
            builder.Add(entry.Key, entry.Value);
        }

        builder.Flush();

        return new TextDictionary<TValue>(ref builder);
    }

    public TextDictionary()
    {
        _entries = [];
        _cellarStartIndex = 0;
        _fastmodMul = 0;
        _collisionIndex = 0;
        _count = 0;
    }

    public TextDictionary(int desiredCapacity)
    {
        _entries = new Entry[ClampCapacity(desiredCapacity)];
        _cellarStartIndex = CalculateBestCellarIndexForCapacity(_entries.Length);
        _fastmodMul = CalculateFastmodMultiplier(_cellarStartIndex);
        _collisionIndex = _entries.Length - 1;
        _count = 0;
    }

    private TextDictionary(ref Builder builder)
    {
        _entries = builder.Entries;
        _cellarStartIndex = builder.CellarStartIndex;
        _fastmodMul = builder.FastmodMultiplier;
        _collisionIndex = builder.CollisionIndex;
        _count = builder.Count;
    }

    private Entry[] _entries;
    private ulong _fastmodMul;
    private uint _cellarStartIndex;
    private int _collisionIndex;
    private int _count;

    public int Count => _count;

    public bool HasItems => _count > 0;

    public bool IsEmpty => _count <= 0;

    public int Capacity => _entries.Length;

    internal uint HashSpace => _cellarStartIndex;

    public TValue this[string key]
    {
        get
        {
            if (!TryGetValue(key, out var result))
            {
                throwNotFound();
            }

            return result;

#if !NO_EXPOSED_NULLANNOTATIONS
            [System.Diagnostics.CodeAnalysis.DoesNotReturn]
#endif
            [MethodImpl(MethodImplOptions.NoInlining)]
            static void throwNotFound() => throw new KeyNotFoundException("Given key was not found");
        }
        set
        {
            ref var entryValue = ref GetOrAddValueRef(key);
            entryValue = value;
        }
    }

    public KeysCollection Keys => new(this);

    public ValuesCollection Values => new(this);

    private IEnumerable<Entry> FilledEntries => _entries.Where(static e => e.Key is not null);

    ICollection<string> IDictionary<string, TValue>.Keys => Keys;

    ICollection<TValue> IDictionary<string, TValue>.Values => Values;

    public bool IsReadOnly => false;

    public Enumerator GetEnumerator() => new(this);

    IEnumerator<KeyValuePair<string, TValue>> IEnumerable<KeyValuePair<string, TValue>>.GetEnumerator() => FilledEntries
        .Select(static e => new KeyValuePair<string, TValue>(e.Key, e.Value))
        .GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => this.AsEnumerable().GetEnumerator();

    public bool ContainsKey(string key) => !Unsafe.IsNullRef(ref GetRefByKey(key));

    public bool ContainsKey(ReadOnlySpan<char> key) => !Unsafe.IsNullRef(ref GetRefByKey(key));

    public bool Contains(KeyValuePair<string, TValue> item) =>
        TryGetValue(item.Key, out var value) && EqualityComparer<TValue>.Default.Equals(value, item.Value);

    public bool TryGetValue(
        string key,
#if !NO_EXPOSED_NULLANNOTATIONS
        [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)]
#endif
        out TValue value)
    {
        ref var entry = ref GetRefByKey(key);
        if (Unsafe.IsNullRef(ref entry))
        {
            value = default!;
            return false;
        }

        value = entry.Value;
        return true;
    }

    public bool TryGetValue(
        ReadOnlySpan<char> key,
#if !NO_EXPOSED_NULLANNOTATIONS
        [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)]
#endif
        out TValue value)
    {
        ref var entry = ref GetRefByKey(key);
        if (Unsafe.IsNullRef(ref entry))
        {
            value = default!;
            return false;
        }

        value = entry.Value;
        return true;
    }

    public bool TryGetValue(
        ReadOnlySpan<char> key,
#if !NO_EXPOSED_NULLANNOTATIONS
        [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)]
#endif
        out string actualKey,
#if !NO_EXPOSED_NULLANNOTATIONS
        [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)]
#endif
        out TValue value)
    {
        ref var entry = ref GetRefByKey(key);
        if (Unsafe.IsNullRef(ref entry))
        {
            actualKey = null!;
            value = default!;
            return false;
        }

        actualKey = entry.Key;
        value = entry.Value;
        return true;
    }

    public void EnsureCapacity(int capacity)
    {
        capacity = ClampCapacity(capacity);

        if (_entries.Length >= capacity)
        {
            return;
        }

        var builder = new Builder(capacity);
        foreach (var oldEntry in _entries)
        {
            if (oldEntry.Key is not null)
            {
                builder.Add(oldEntry.HashCode, oldEntry.Key, oldEntry.Value);
            }
        }

        builder.Flush();

#if DEBUG
        if (Count != builder.Count) ExceptionEx.ThrowInvalidOperation();
#endif

        _entries = builder.Entries;
        _cellarStartIndex = builder.CellarStartIndex;
        _fastmodMul = builder.FastmodMultiplier;
        _collisionIndex = builder.CollisionIndex;
    }

    public void CopyTo(KeyValuePair<string, TValue>[] array, int arrayIndex)
    {
        foreach (var pair in this)
        {
            array[arrayIndex++] = pair;
        }
    }

    public void Clear()
    {
        Array.Clear(_entries, 0, _entries.Length);
        _count = 0;
        _collisionIndex = _entries.Length - 1;
    }

    public bool Remove(string key)
    {
        return Remove(key.AsSpan());
    }

    public bool Remove(ReadOnlySpan<char> key)
    {
        if (_entries.Length == 0)
        {
            goto findFail;
        }

        var hash = CalculateHash(key);

        ref var entry = ref GetRefByHash(hash);

        if (entry.Key is null)
        {
            goto findFail;
        }

        ref var otherEntry = ref Unsafe.NullRef<Entry>();

        while (true)
        {
            if (entry.HashCode == hash && key.SequenceEqual(entry.Key))
            {
                if (Unsafe.IsNullRef(ref otherEntry))
                {
                    if (entry.NextNumber != 0)
                    {
                        // This is the first entry in the chain, so move the next entry into this one
                        otherEntry = ref entry; // there is no prev, so point at the first entry we need to replace
                        entry = ref _entries[entry.NextNumber - 1]; // capture a reference to the next one
                        otherEntry = entry; // copy what is in the next entry into the "other" current entry
                        // very soon, entry (the old next) will be cleared out
                    }
                }
                else
                {
                    otherEntry.NextNumber = entry.NextNumber;
                }

                entry = default;

                _count--;
                _collisionIndex = _entries.Length - 1;
                return true;
            }

            if (entry.NextNumber == 0)
            {
                break;
            }

            otherEntry = ref entry;
            entry = ref _entries[entry.NextNumber - 1];
        }

    findFail:
        return false;
    }

    public bool Remove(KeyValuePair<string, TValue> item)
    {
        return Contains(item) && Remove(item.Key);
    }

    public void Add(KeyValuePair<string, TValue> item)
    {
        Add(item.Key, item.Value);
    }

    public void Add(string key, TValue value)
    {
        ref var entry = ref GetOrAddEntryRefForKey(key, throwOnMatch: true);
        if (Unsafe.IsNullRef(ref entry))
        {
            ThrowAddFailed();
        }

        entry.Value = value;
    }

    internal ref TValue GetOrAddValueRef(string key)
    {
        ref var entry = ref GetOrAddEntryRefForKey(key, throwOnMatch: false);
        if (Unsafe.IsNullRef(ref entry))
        {
            ThrowAddFailed();
        }

        return ref entry.Value;
    }

    private ref Entry GetOrAddEntryRefForKey(string key, bool throwOnMatch)
    {
        var isSearchIteration = true;
        var hash = CalculateHash(key);

        if (_entries.Length == 0)
        {
            goto findFail;
        }

    retry:

        ref var entry = ref GetRefByHash(hash);

        if (entry.Key is null)
        {
            entry = new(hash, key);
            _count++;
            goto entrySelected;
        }

        while (true)
        {
            if (entry.HashCode == hash && key.Equals(entry.Key))
            {
                if (throwOnMatch)
                {
                    ThrowDuplicateKey();
                }

                goto entrySelected;
            }

            if (entry.NextNumber == 0)
            {
                break;
            }

            entry = ref _entries[entry.NextNumber - 1];
        }

        if (_count < _entries.Length && (_collisionIndex = FindNextEmptyCollisionIndex()) >= 0)
        {
            entry.NextNumber = (uint)(_collisionIndex + 1);

            entry = ref _entries[_collisionIndex--];
            entry = new(hash, key);
            _count++;

            goto entrySelected;
        }

    findFail:

        if (isSearchIteration)
        {
            isSearchIteration = false;
            EnsureCapacity(GetNextCapacity());
            goto retry;
        }

        return ref Unsafe.NullRef<Entry>();

    entrySelected:
        return ref entry;
    }

    private int FindNextEmptyCollisionIndex()
    {
        var i = _collisionIndex;
        for (; i >= 0 && _entries[i].Key is not null; i--) ;
        return i;
    }

    private ref Entry GetRefByKey(string key)
    {
        if (_entries.Length == 0)
        {
            goto fail;
        }

        var hash = CalculateHash(key);
        ref var entry = ref GetRefByHash(hash);

        if (entry.Key is not null)
        {
            do
            {
                if (entry.HashCode == hash && key.Equals(entry.Key))
                {
                    return ref entry;
                }

                if (entry.NextNumber == 0)
                {
                    goto fail;
                }

                entry = ref _entries[entry.NextNumber - 1];
            }
            while (true);
        }

    fail:
        return ref Unsafe.NullRef<Entry>();
    }

    private ref Entry GetRefByKey(ReadOnlySpan<char> key)
    {
        if (_entries.Length == 0)
        {
            goto fail;
        }

        var hash = CalculateHash(key);
        ref var entry = ref GetRefByHash(hash);

        if (entry.Key is not null)
        {
            do
            {
                if (entry.HashCode == hash && key.SequenceEqual(entry.Key))
                {
                    return ref entry;
                }

                if (entry.NextNumber == 0)
                {
                    goto fail;
                }

                entry = ref _entries[entry.NextNumber - 1];
            }
            while (true);
        }

    fail:
        return ref Unsafe.NullRef<Entry>();
    }

    private int GetNextCapacity() => Math.Max(_entries.Length * 2, 4);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ref Entry GetRefByHash(uint hash) =>
            ref _entries[GetIndexByHash(hash, _cellarStartIndex, _fastmodMul)];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint GetIndexByHash(uint hash, uint divisor, ulong multiplier) =>
        // I barely understand this algorithm, but it's how the .NET dictionary works on 64-bit platforms. Sources:
        // - https://lemire.me/blog/2019/02/08/faster-remainders-when-the-divisor-is-a-constant-beating-compilers-and-libdivide/
        // - https://github.com/dotnet/runtime/pull/406
        unchecked((uint)(((((multiplier * hash) >> 32) + 1u) * divisor) >> 32));

    private static ulong CalculateFastmodMultiplier(uint divisor) =>
        divisor == 0 ? 0 : (ulong.MaxValue / divisor) + 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint CalculateHash(string key) => StringEx.GetStableOrdinalHashCode(key);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint CalculateHash(ReadOnlySpan<char> key) => StringEx.GetStableOrdinalHashCode(key);

    private static uint CalculateBestCellarIndexForCapacity(int capacity)
    {
        if (capacity < 0)
        {
            return 0;
        }

        if (capacity > 3)
        {
            // this seems to be a good ratio to start with, based on something from wikipedia
            var index = (int)(((long)capacity * 86) / 100);
            if ((index & 1) == 0)
            {
                index--;
            }

            // gaps between primes don't seem too large so this shouldn't have to go through too many iterations
            for (; index < capacity; index += 2)
            {
                if (isOddPrime(index))
                {
                    return (uint)index;
                }
            }
        }

        return (uint)capacity;

        static bool isOddPrime(int n)
        {
            var limit = (int)Math.Sqrt(n);
            for (var i = 3; i <= limit; i += 2)
            {
                if (n % i == 0)
                {
                    return false;
                }
            }

            return true;
        }
    }

    private static int ClampCapacity(int capacity)
    {
        if (capacity < 0)
        {
            capacity = 0;
        }
        else if (capacity > MaxCapacity)
        {
            capacity = MaxCapacity;
        }

        return capacity;
    }

#if !NO_EXPOSED_NULLANNOTATIONS
    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
#endif
    private static void ThrowAddFailed() => ExceptionEx.ThrowInvalidOperation("Failed to add");

#if !NO_EXPOSED_NULLANNOTATIONS
    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
#endif
    private static void ThrowDuplicateKey() => ExceptionEx.ThrowInvalidOperation("Duplicate key");

    public sealed class KeysCollection : ICollection<string>
    {
        internal KeysCollection(TextDictionary<TValue> dictionary)
        {
            _dictionary = dictionary;
        }

        private readonly TextDictionary<TValue> _dictionary;

        public int Count => _dictionary.Count;
        public bool IsReadOnly => true;

        public void Add(string item) => ExceptionEx.ThrowNotSupported();
        public void Clear() => ExceptionEx.ThrowNotSupported();
        public bool Contains(string item) => _dictionary.ContainsKey(item);
        public void CopyTo(string[] array, int arrayIndex) => ExceptionEx.ThrowNotImplementedYet();
        public bool Remove(string item) => ExceptionEx.ThrowNotSupported<bool>();
        public IEnumerator<string> GetEnumerator() => _dictionary.FilledEntries.Select(static e => e.Key).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public sealed class ValuesCollection : ICollection<TValue>
    {
        internal ValuesCollection(TextDictionary<TValue> dictionary)
        {
            _dictionary = dictionary;
        }

        private readonly TextDictionary<TValue> _dictionary;

        public int Count => _dictionary.Count;
        public bool IsReadOnly => true;

        public void Add(TValue item) => ExceptionEx.ThrowNotSupported();
        public void Clear() => ExceptionEx.ThrowNotSupported();
        public bool Contains(TValue item) => GetValues().Contains(item);
        public void CopyTo(TValue[] array, int arrayIndex) => ExceptionEx.ThrowNotImplementedYet();
        public bool Remove(TValue item) => ExceptionEx.ThrowNotSupported<bool>();
        public IEnumerator<TValue> GetEnumerator() => GetValues().GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        private IEnumerable<TValue> GetValues() => _dictionary.FilledEntries.Select(static e => e.Value);
    }

    public struct Enumerator
    {
        internal Enumerator(TextDictionary<TValue> dictionary)
        {
            _entries = dictionary._entries;
            _index = 0;
            _current = default;
        }

        private readonly Entry[] _entries;
        private int _index;
        private KeyValuePair<string, TValue> _current;

        public readonly KeyValuePair<string, TValue> Current => _current;

        public bool MoveNext()
        {
            while (_index < _entries.Length)
            {
                ref var entry = ref _entries[_index++];
                if (entry.Key is not null)
                {
                    _current = new(entry.Key, entry.Value);
                    return true;
                }
            }

            return false;
        }
    }

    internal struct KeyLengthEnumerator
    {
        internal KeyLengthEnumerator(TextDictionary<TValue> dictionary, int minKeyLength, int maxKeyLength)
        {
#if DEBUG
            ExceptionEx.ThrowIfArgumentLessThan(maxKeyLength, minKeyLength, nameof(maxKeyLength));
#endif

            _entries = dictionary._entries;
            _minKeyLength = minKeyLength;
            _maxKeyLength = maxKeyLength;
            _index = 0;
            _current = default;
        }

        private readonly Entry[] _entries;
        private readonly int _minKeyLength;
        private readonly int _maxKeyLength;
        private int _index;
        private KeyValuePair<string, TValue> _current;

        public readonly KeyValuePair<string, TValue> Current => _current;

        public bool MoveNext()
        {
            while (_index < _entries.Length)
            {
                ref var entry = ref _entries[_index++];

                if (entry.Key is not null && CheckMatchingKeyLength(entry.Key.Length))
                {
                    _current = new(entry.Key, entry.Value);
                    return true;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly bool CheckMatchingKeyLength(int length) => length <= _maxKeyLength && length >= _minKeyLength;
    }

    private struct Builder
    {
        public Builder(int capacity)
        {
            Entries = new Entry[ClampCapacity(capacity)];
            CellarStartIndex = CalculateBestCellarIndexForCapacity(Entries.Length);
            FastmodMultiplier = CalculateFastmodMultiplier(CellarStartIndex);
            CollisionIndex = Entries.Length - 1;
            Count = 0;

            _leftovers = new((int)(Entries.Length - CellarStartIndex));
        }

        private readonly List<(uint hash, string key, TValue value)> _leftovers;

        public Entry[] Entries;
        public ulong FastmodMultiplier;
        public uint CellarStartIndex;
        public int CollisionIndex;
        public int Count;

        public void Add(string key, TValue value)
        {
            Add(CalculateHash(key), key, value);
        }

        public void Add(uint hash, string key, TValue value)
        {
            ref var entry = ref GetRefByHash(hash);
            if (entry.Key is null)
            {
                entry = new(hash, key, value);
            }
            else if (CollisionIndex >= CellarStartIndex)
            {
                ForceAppendCollisionEntry(hash, key, value);
            }
            else
            {
                _leftovers.Add((hash, key, value));
            }

            Count++;
        }

        public void Flush()
        {
            foreach (var (hash, key, value) in _leftovers)
            {
                ForceAppendCollisionEntry(hash, key, value);
            }
        }

        private void ForceAppendCollisionEntry(uint hash, string key, TValue value)
        {
            for (; CollisionIndex >= 0 && Entries[CollisionIndex].Key is not null; CollisionIndex--) ;

            if (CollisionIndex < 0)
            {
                throwNoRoomForCollision();
            }

            ref var entry = ref GetRefByHash(hash);

            while (entry.NextNumber != 0)
            {
                entry = ref Entries[entry.NextNumber - 1];
            }

            entry.NextNumber = (uint)(CollisionIndex + 1);

            Entries[CollisionIndex--] = new(hash, key, value);

#if !NO_EXPOSED_NULLANNOTATIONS
            [System.Diagnostics.CodeAnalysis.DoesNotReturn]
#endif
            [MethodImpl(MethodImplOptions.NoInlining)]
            static void throwNoRoomForCollision() => throw new NotSupportedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly ref Entry GetRefByHash(uint hash) =>
            ref Entries[GetIndexByHash(hash, CellarStartIndex, FastmodMultiplier)];
    }

    private struct Entry
    {
        public uint HashCode;
        public uint NextNumber; // 0 is for explicit terminal, > 0 for the next 1-based index
        public string Key;
        public TValue Value;

        public Entry(uint hashCode, string key)
        {
            HashCode = hashCode;
            NextNumber = 0;
            Key = key;
            Value = default!;
        }

        public Entry(uint hashCode, string key, TValue value)
        {
            HashCode = hashCode;
            NextNumber = 0;
            Key = key;
            Value = value;
        }
    }
}

