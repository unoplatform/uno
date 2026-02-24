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

#if HAS_FROZENDICTIONARY
using System.Collections.Frozen;
#endif

namespace WeCantSpell.Hunspell;

/// <summary>
/// An affix is either a prefix or a suffix attached to root words to make other words.
/// </summary>
/// <remarks>
/// 
/// Appendix:  Understanding Affix Code
/// 
/// <para>
/// An affix is either a  prefix or a suffix attached to root words to make 
/// other words.
/// </para>
/// 
/// <para>
/// An <see cref="AffixGroup{TAffixEntry}"/> is set of affix objects
/// which store information about the prefix or suffix along
/// with supporting routines to check if a word has a particular
/// prefix or suffix or a combination.
/// </para>
/// 
/// <para>
/// Zero stripping or affix are indicated by zero. Zero condition is indicated by dot.
/// Condition is a simplified, regular expression-like pattern, which must be met
/// before the affix can be applied. (Dot signs an arbitrary character.Characters in braces
/// sign an arbitrary character from the character subset.Dash hasn't got special
/// meaning, but circumflex(^) next the first brace sets the complementer character set.)
/// </para>
/// 
/// </remarks>
/// 
/// <example>
/// 
/// <para>
/// Here is a suffix borrowed from the en_US.aff file.  This file 
/// is whitespace delimited.
/// </para>
/// 
/// <code>
/// SFX D Y 4 
/// SFX D   0     e          d
/// SFX D   y     ied        [^aeiou]y
/// SFX D   0     ed         [^ey]
/// SFX D   0     ed         [aeiou]y
/// </code>
/// 
/// This information can be interpreted as follows
/// 
/// <para>
/// In the first line has 4 fields which define the <see cref="AffixGroup{TAffixEntry}"/> for this affix that will contain four <see cref="SuffixEntry"/> objects.
/// </para>
/// 
/// <code>
/// Field
/// -----
/// 1     SFX - indicates this is a suffix
/// 2     D   - is the name of the character flag which represents this suffix
/// 3     Y   - indicates it can be combined with prefixes (cross product)
/// 4     4   - indicates that sequence of 4 affentry structures are needed to
///                properly store the affix information
/// </code>
/// 
/// <para>
/// The remaining lines describe the unique information for the 4 <see cref="SuffixEntry"/> 
/// objects that make up this affix.  Each line can be interpreted
/// as follows: (note fields 1 and 2 are as a check against line 1 info)
/// </para>
/// 
/// <code>
/// Field
/// -----
/// 1     SFX         - indicates this is a suffix
/// 2     D           - is the name of the character flag for this affix
/// 3     y           - the string of chars to strip off before adding affix
///                          (a 0 here indicates the NULL string)
/// 4     ied         - the string of affix characters to add
/// 5     [^aeiou]y   - the conditions which must be met before the affix
///                     can be applied
/// </code>
/// 
/// <para>
/// Field 5 is interesting.  Since this is a suffix, field 5 tells us that
/// there are 2 conditions that must be met.  The first condition is that 
/// the next to the last character in the word must *NOT* be any of the 
/// following "a", "e", "i", "o" or "u".  The second condition is that
/// the last character of the word must end in "y".
/// </para>
/// 
/// </example>
/// 
/// <seealso cref="PrefixCollection"/>
/// <seealso cref="SuffixCollection"/>
[DebuggerDisplay("Count = {Count}")]
public abstract class AffixCollection<TAffixEntry> : IEnumerable<AffixGroup<TAffixEntry>>
    where TAffixEntry : AffixEntry
{
    protected AffixCollection()
    {
    }

#if HAS_FROZENDICTIONARY

    private FrozenDictionary<FlagValue, AffixGroup<TAffixEntry>> _affixesByFlag = null!; // implementing types are expected to initialize
    private FrozenDictionary<char, EntryTreeNode> _affixTreeRootsByFirstKeyChar = FrozenDictionary<char, EntryTreeNode>.Empty;

#else

    private Dictionary<FlagValue, AffixGroup<TAffixEntry>> _affixesByFlag = null!; // implementing types are expected to initialize
    private Dictionary<char, EntryTreeNode> _affixTreeRootsByFirstKeyChar = [];

#endif

    private TAffixEntry[] _affixesWithEmptyKeys = [];

    public FlagSet ContClasses { get; protected set; } = FlagSet.Empty;

    public bool HasAffixes => _affixesByFlag.Count > 0;

    public int Count => _affixesByFlag.Count;

    public IEnumerable<FlagValue> FlagValues => _affixesByFlag.Keys;

    public IEnumerator<AffixGroup<TAffixEntry>> GetEnumerator() => ((IEnumerable<AffixGroup<TAffixEntry>>)_affixesByFlag.Values).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public AffixGroup<TAffixEntry>? GetByFlag(FlagValue flag) => _affixesByFlag.GetValueOrDefault(flag);

    internal TAffixEntry[] GetAffixesWithEmptyKeys() => _affixesWithEmptyKeys;

    internal AffixesByFlagsEnumerator GetByFlags(FlagSet flags) => new(flags, _affixesByFlag);

    internal GroupsByFlagsEnumerator GetGroupsByFlags(FlagSet flags) => new(flags, _affixesByFlag);

    internal WordEnumerator GetMatchingAffixes(ReadOnlySpan<char> word)
    {
        if (word.IsEmpty || !_affixTreeRootsByFirstKeyChar.TryGetValue(GetKeyIndexValueForWord(word), out var node))
        {
            node = null;
        }

        return new(word, node);
    }

    protected abstract char GetKeyIndexValueForWord(ReadOnlySpan<char> word);

    [DebuggerDisplay("Count = {Count}")]
    public abstract class BuilderBase
    {
        protected BuilderBase()
        {
        }

        protected Dictionary<FlagValue, GroupBuilder> _byFlag = [];
        private protected ArrayBuilder<TAffixEntry> _emptyKeys = [];
        protected HashSet<FlagValue> _contClassAccumulator = [];
        protected Dictionary<char, List<TAffixEntry>> _byFirstKeyChar = [];

        public int Count => _byFlag.Count;

        public GroupBuilder ForGroup(FlagValue aFlag)
        {
            if (!_byFlag.TryGetValue(aFlag, out var groupBuilder))
            {
                groupBuilder = new(this, aFlag);
                _byFlag.Add(aFlag, groupBuilder);
            }

            return groupBuilder;
        }

        protected void ApplyToCollection(AffixCollection<TAffixEntry> target, bool allowDestructiveApplication)
        {
            Dictionary<char, EntryTreeNode> affixTreeRootsByFirstKeyCharBuilder = [];

            // loop through each prefix list starting point
            foreach (var (firstChar, affixes) in _byFirstKeyChar)
            {
                if (BuildTree(affixes, allowDestructive: allowDestructiveApplication) is { } root)
                {
                    affixTreeRootsByFirstKeyCharBuilder.Add(firstChar, root);
                }
            }

            target.ContClasses = FlagSet.Create(_contClassAccumulator);
            target._affixesWithEmptyKeys = _emptyKeys.MakeOrExtractArray(extract: allowDestructiveApplication);

            Func<KeyValuePair<FlagValue, GroupBuilder>, AffixGroup<TAffixEntry>> buildByFlagValue = allowDestructiveApplication
                ? static (x) => x.Value.Extract()
                : static (x) => x.Value.Build();

#if HAS_FROZENDICTIONARY
            target._affixesByFlag = _byFlag.ToFrozenDictionary(static x => x.Key, buildByFlagValue);
            target._affixTreeRootsByFirstKeyChar = affixTreeRootsByFirstKeyCharBuilder.ToFrozenDictionary();
#else
            target._affixesByFlag = _byFlag.ToDictionary(static x => x.Key, buildByFlagValue);
            target._affixTreeRootsByFirstKeyChar = affixTreeRootsByFirstKeyCharBuilder;
#endif

            if (allowDestructiveApplication)
            {
                _byFirstKeyChar.Clear();
                _byFlag.Clear();
            }
        }

        private static EntryTreeNode? BuildTree(List<TAffixEntry> affixes, bool allowDestructive)
        {
            if (affixes.Count == 0)
            {
                return null;
            }

            // convert from binary tree to sorted list
            // NOTE: the above comment is a lie, but that is what this sort and initial tree build is for

            var allNodes = new EntryTreeNode[affixes.Count];
            int i;

            if (allowDestructive)
            {
                affixes.Sort(static (a, b) => StringComparer.Ordinal.Compare(a.Key, b.Key));

                i = allNodes.Length - 1;
                allNodes[i] = new(affixes[affixes.Count - 1]);

                while (--i >= 0)
                {
                    allNodes[i] = new(affixes[i])
                    {
                        Next = allNodes[i + 1]
                    };
                }
            }
            else
            {
                for (i = 0; i < allNodes.Length; i++)
                {
                    allNodes[i] = new(affixes[i]);
                }

                Array.Sort(allNodes, static (a, b) => StringComparer.Ordinal.Compare(a.Affix.Key, b.Affix.Key));

                for (i = 1; i < allNodes.Length; i++)
                {
                    allNodes[i - 1].Next = allNodes[i];
                }
            }

            prepareTree(allNodes);
            cleanTree(allNodes);

            return allNodes[0];

            static void prepareTree(EntryTreeNode[] allNodes)
            {
                // look through the remainder of the list
                // and find next entry with affix that
                // the current one is not a subset of
                // mark that as destination for NextNE
                // use next in list that you are a subset
                // of as NextEQ

                foreach (var ptr in allNodes)
                {
                    var nptr = ptr.Next;
                    while (nptr is not null && ptr.IsKeySubset(nptr))
                    {
                        nptr = nptr.Next;
                    }

                    ptr.NextNotEqual = nptr;
                    // ptr.NextEqual = null;

                    if (ptr.Next is not null && ptr.IsKeySubset(ptr.Next))
                    {
                        ptr.NextEqual = ptr.Next;
                    }
                }
            }

            static void cleanTree(EntryTreeNode[] allNodes)
            {
                // now clean up by adding smart search termination strings:
                // if you are already a superset of the previous affix
                // but not a subset of the next, search can end here
                // so set NextNE properly

                foreach (var ptr in allNodes)
                {
                    EntryTreeNode? mptr = null;
                    var nptr = ptr.Next;
                    while (nptr is not null && ptr.IsKeySubset(nptr))
                    {
                        mptr = nptr;
                        nptr = nptr.Next;
                    }

                    mptr?.NextNotEqual = null;
                }
            }
        }

        [DebuggerDisplay("AFlag = {AFlag}, Options = {Options}, Count = {Count}")]
        public sealed class GroupBuilder
        {
            internal GroupBuilder(BuilderBase parent, FlagValue aFlag)
            {
                _parent = parent;
                AFlag = aFlag;
                _entries = [];
            }

            private readonly BuilderBase _parent;
            private readonly ArrayBuilder<TAffixEntry> _entries;

            public IList<TAffixEntry> Entries => _entries;

            public FlagValue AFlag { get; set; }

            public AffixEntryOptions Options { get; set; }

            public bool IsInitialized { get; private set; }

            public int Count => _entries.Count;

            public void Initialize(AffixEntryOptions options, int expectedCapacity)
            {
                Options = options;

                if (expectedCapacity is > 0 and <= CollectionsEx.CollectionPreallocationLimit)
                {
                    _entries.EnsureCapacity(expectedCapacity);
                }

                IsInitialized = true;
            }

            public void AddEntry(
                string strip,
                string affixText,
                CharacterConditionGroup conditions,
                MorphSet morph,
                FlagSet contClass)
            {
                var entry = CreateEntry(strip, affixText, conditions, morph, contClass);

                _entries.Add(entry);

                if (entry.ContClass.HasItems)
                {
                    _parent._contClassAccumulator.UnionWith(entry.ContClass);
                }

                if (string.IsNullOrEmpty(entry.Key))
                {
                    _parent._emptyKeys.Add(entry);
                }
                else
                {
                    var firstChar = entry.Key[0];
                    if (!_parent._byFirstKeyChar.TryGetValue(firstChar, out var byKeyGroup))
                    {
                        byKeyGroup = [];
                        _parent._byFirstKeyChar.Add(firstChar, byKeyGroup);
                    }

                    byKeyGroup.Add(entry);
                }
            }

            public AffixGroup<TAffixEntry> Build() =>
                AffixGroup<TAffixEntry>.CreateUsingArray(
                    AFlag,
                    Options,
                    _entries.ToArray());

            public AffixGroup<TAffixEntry> Extract() =>
                AffixGroup<TAffixEntry>.CreateUsingArray(
                    AFlag,
                    Options,
                    _entries.Extract());

            public AffixGroup<TAffixEntry> ExtractOrBuild(bool extract) =>
                extract ? Extract() : Build();

            private TAffixEntry CreateEntry(string strip,
                string affixText,
                CharacterConditionGroup conditions,
                MorphSet morph,
                FlagSet contClass)
            {
                if (typeof(TAffixEntry) == typeof(PrefixEntry))
                {
                    return (TAffixEntry)(AffixEntry)new PrefixEntry(
                        strip,
                        affixText,
                        conditions,
                        morph,
                        contClass,
                        AFlag,
                        Options);
                }
                else
                {
                    return (TAffixEntry)(AffixEntry)new SuffixEntry(
                        strip,
                        affixText,
                        conditions,
                        morph,
                        contClass,
                        AFlag,
                        Options);
                }
            }
        }
    }

    internal struct AffixesByFlagsEnumerator
    {
#if HAS_FROZENDICTIONARY
        public AffixesByFlagsEnumerator(FlagSet flags, FrozenDictionary<FlagValue, AffixGroup<TAffixEntry>> affixesByFlag)
#else
        public AffixesByFlagsEnumerator(FlagSet flags, Dictionary<FlagValue, AffixGroup<TAffixEntry>> affixesByFlag)
#endif
        {
            _flags = flags.GetInternalText();
            _byFlag = affixesByFlag;
            _group = null;
            _flagsIndex = 0;
            _groupIndex = 0;
            _current = default!;
        }

#if HAS_FROZENDICTIONARY
        private readonly FrozenDictionary<FlagValue, AffixGroup<TAffixEntry>> _byFlag;
#else
        private readonly Dictionary<FlagValue, AffixGroup<TAffixEntry>> _byFlag;
#endif

        private readonly string _flags;
        private AffixGroup<TAffixEntry>? _group;
        private int _flagsIndex;
        private int _groupIndex;
        private TAffixEntry _current;

        public readonly TAffixEntry Current => _current;

        public readonly AffixesByFlagsEnumerator GetEnumerator() => this;

        public bool MoveNext()
        {
            if (_group is null || _group.Count <= _groupIndex)
            {
                if (!MoveNextGroup())
                {
                    return false;
                }
            }

            _current = _group![_groupIndex++];
            return true;
        }

        private bool MoveNextGroup()
        {
            while (_flagsIndex < _flags.Length)
            {
                if (_byFlag.TryGetValue((FlagValue)_flags[_flagsIndex++], out _group) && _group.Count != 0)
                {
                    _groupIndex = 0;
                    return true;
                }
            }

            return false;
        }
    }

    internal struct GroupsByFlagsEnumerator
    {
#if HAS_FROZENDICTIONARY
        public GroupsByFlagsEnumerator(FlagSet flags, FrozenDictionary<FlagValue, AffixGroup<TAffixEntry>> byFlag)
#else
        public GroupsByFlagsEnumerator(FlagSet flags, Dictionary<FlagValue, AffixGroup<TAffixEntry>> byFlag)
#endif
        {
            _flags = flags.GetInternalText();
            _byFlag = byFlag;
            _current = default!;
            _flagsIndex = 0;
        }

#if HAS_FROZENDICTIONARY
        private readonly FrozenDictionary<FlagValue, AffixGroup<TAffixEntry>> _byFlag;
#else
        private readonly Dictionary<FlagValue, AffixGroup<TAffixEntry>> _byFlag;
#endif
        private readonly string _flags;
        private AffixGroup<TAffixEntry> _current;
        private int _flagsIndex;

        public readonly AffixGroup<TAffixEntry> Current => _current;

        public readonly GroupsByFlagsEnumerator GetEnumerator() => this;

        public bool MoveNext()
        {
            while (_flagsIndex < _flags.Length)
            {
                if (_byFlag.TryGetValue((FlagValue)_flags[_flagsIndex++], out _current!))
                {
                    return true;
                }
            }

            return false;
        }
    }

    internal ref struct WordEnumerator
    {
        internal WordEnumerator(ReadOnlySpan<char> word, EntryTreeNode? node)
        {
            _word = word;
            _node = node;
            _current = default!;
        }

        private readonly ReadOnlySpan<char> _word;
        private EntryTreeNode? _node;
        private TAffixEntry _current;

        public readonly TAffixEntry Current => _current;

        public readonly WordEnumerator GetEnumerator() => this;

        public bool MoveNext()
        {
            while (_node is not null)
            {
                if (_node.Affix.IsWordSubset(_word))
                {
                    _current = _node.Affix;
                    _node = _node.NextEqual;
                    return true;
                }
                else
                {
                    _node = _node.NextNotEqual;
                }
            }

            return false;
        }
    }

    internal sealed class EntryTreeNode
    {
        public EntryTreeNode(TAffixEntry affix)
        {
            Affix = affix;
        }

        public TAffixEntry Affix { get; }
        public EntryTreeNode? NextEqual { get; set; }
        public EntryTreeNode? NextNotEqual { get; set; }
        public EntryTreeNode? Next { get; set; }
        public bool IsKeySubset(EntryTreeNode other) => Affix.IsKeySubset(other.Affix.Key);
    }
}

[DebuggerDisplay("Count = {Count}")]
public sealed class SuffixCollection : AffixCollection<SuffixEntry>
{
    public static SuffixCollection Empty { get; } = new Builder().BuildOrExtract(extract: true);

    private SuffixCollection()
    {
    }

    protected override char GetKeyIndexValueForWord(ReadOnlySpan<char> word) => word[word.Length - 1];

    public sealed class Builder : BuilderBase
    {
        public SuffixCollection BuildOrExtract(bool extract)
        {
            var result = new SuffixCollection();
            ApplyToCollection(result, allowDestructiveApplication: extract);
            return result;
        }
    }
}

[DebuggerDisplay("Count = {Count}")]
public sealed class PrefixCollection : AffixCollection<PrefixEntry>
{
    public static PrefixCollection Empty { get; } = new Builder().BuildOrExtract(extract: true);

    private PrefixCollection()
    {
    }

    protected override char GetKeyIndexValueForWord(ReadOnlySpan<char> word) => word[0];

    public sealed class Builder : BuilderBase
    {
        public PrefixCollection BuildOrExtract(bool extract)
        {
            var result = new PrefixCollection();
            ApplyToCollection(result, allowDestructiveApplication: extract);
            return result;
        }
    }
}

