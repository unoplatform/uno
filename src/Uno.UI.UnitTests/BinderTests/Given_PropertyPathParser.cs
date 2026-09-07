using System;
using System.Runtime.InteropServices;
using DirectUI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.DataBinding;

namespace Uno.UI.Tests.BinderTests;

[TestClass]
public class Given_PropertyPathParser
{
	private IBindableMetadataProvider _previousProvider;

	[TestInitialize]
	public void Initialize()
	{
		// PropertyInfoPropertyAccess requires a metadata provider to be set; the unit test host does not
		// generate one, so use the empty provider and let the steps fall back to reflection.
		_previousProvider = BindableMetadata.Provider;
		BindableMetadata.Provider = new BindableMetadataProvider();
	}

	[TestCleanup]
	public void Cleanup() => BindableMetadata.Provider = _previousProvider;

	[TestMethod]
	[DataRow("")]
	[DataRow((string)null)]
	[DataRow("\0Ignored")]
	public void When_EmptyPath_Then_SourceAccess(string path)
	{
		var parser = Parse(path);

		Assert.AreEqual(1, parser.DescriptorCount);
		Assert.AreEqual(PropertyPathStepDescriptorKind.SourceAccess, parser.GetDescriptorAt(0).Kind);
		Assert.IsNull(parser.GetDescriptorAt(0).Name);
	}

	[TestMethod]
	public void When_SingleProperty()
	{
		var parser = Parse("Name");

		Assert.AreEqual(1, parser.DescriptorCount);

		var descriptor = parser.GetDescriptorAt(0);
		Assert.AreEqual(PropertyPathStepDescriptorKind.PropertyAccess, descriptor.Kind);
		Assert.AreEqual("Name", descriptor.Name);
	}

	[TestMethod]
	public void When_SingleProperty_Then_SourceStringIsReused()
	{
		var path = new string("Name".ToCharArray());

		var parser = Parse(path);

		Assert.AreSame(path, parser.GetDescriptorAt(0).Name);
	}

	[TestMethod]
	[DataRow("Description")]
	[DataRow("Label")]
	[DataRow("CommandBarTemplateSettings")]
	[DataRow("TemplateSettings")]
	[DataRow("AccessKey")]
	[DataRow("KeyboardAccelerators")]
	[DataRow("IconSource")]
	[DataRow("IsEnabled")]
	[DataRow("IsVisible")]
	public void When_CommonPropertyName_Then_SharedStringIsReused(string name)
	{
		var parser = Parse($"Owner.{name}");

		Assert.AreEqual(name, parser.GetDescriptorAt(1).Name);
		Assert.AreSame(PropertyPathCommonNames.TryGetCommonPropertyName(name), parser.GetDescriptorAt(1).Name);
		Assert.IsNull(PropertyPathCommonNames.TryGetCommonPropertyName(name.ToLowerInvariant()));
	}

	[TestMethod]
	public void When_PathContainsNullTerminator_Then_RemainingCharactersAreIgnored()
	{
		var parser = Parse("Name\0.Ignored");

		Assert.AreEqual(1, parser.DescriptorCount);
		AssertPropertyAccess(parser, 0, "Name");
	}

	[TestMethod]
	[DataRow("")]
	[DataRow("Name")]
	[DataRow("Description.Label")]
	[DataRow("[1][2]")]
	public void When_InlineDescriptorsDoNotNeedStrings_Then_ParsingDoesNotAllocate(string path)
	{
		_ = Parse(path);
		var parser = new PropertyPathParser();
		var before = GC.GetAllocatedBytesForCurrentThread();

		parser.SetSource(path, null);

		var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
		Assert.AreEqual(0L, allocated);
		GC.KeepAlive(parser);
	}

	[TestMethod]
	public void When_NestedProperties()
	{
		var parser = Parse("First.Second.Third");

		Assert.AreEqual(3, parser.DescriptorCount);
		AssertPropertyAccess(parser, 0, "First");
		AssertPropertyAccess(parser, 1, "Second");
		AssertPropertyAccess(parser, 2, "Third");
	}

	[TestMethod]
	public void When_MoreSteps_Than_InlineCapacity()
	{
		var parser = Parse("A.B.C.D.E.F.G");

		Assert.AreEqual(7, parser.DescriptorCount);
		AssertPropertyAccess(parser, 0, "A");
		AssertPropertyAccess(parser, 1, "B");
		AssertPropertyAccess(parser, 2, "C");
		AssertPropertyAccess(parser, 3, "D");
		AssertPropertyAccess(parser, 4, "E");
		AssertPropertyAccess(parser, 5, "F");
		AssertPropertyAccess(parser, 6, "G");
	}

	[TestMethod]
	public void When_IntIndexer()
	{
		var parser = Parse("Items[42]");

		Assert.AreEqual(2, parser.DescriptorCount);
		AssertPropertyAccess(parser, 0, "Items");

		var indexer = parser.GetDescriptorAt(1);
		Assert.AreEqual(PropertyPathStepDescriptorKind.IntIndexer, indexer.Kind);
		Assert.AreEqual(42, indexer.Index);
		Assert.IsNull(indexer.Name);
	}

	[TestMethod]
	public void When_ConsecutiveIntIndexers()
	{
		var parser = Parse("[0][1][2]");

		Assert.AreEqual(3, parser.DescriptorCount);
		for (var i = 0; i < 3; i++)
		{
			Assert.AreEqual(PropertyPathStepDescriptorKind.IntIndexer, parser.GetDescriptorAt(i).Kind);
			Assert.AreEqual(i, parser.GetDescriptorAt(i).Index);
		}
	}

	[TestMethod]
	public void When_StringIndexer()
	{
		var parser = Parse("Items[key]");

		Assert.AreEqual(2, parser.DescriptorCount);
		AssertPropertyAccess(parser, 0, "Items");

		var indexer = parser.GetDescriptorAt(1);
		Assert.AreEqual(PropertyPathStepDescriptorKind.StringIndexer, indexer.Kind);
		Assert.AreEqual("key", indexer.Name);
		Assert.AreEqual(0, indexer.Index);
	}

	[TestMethod]
	public void When_IndexerFollowedByProperty()
	{
		var parser = Parse("Items[0].Name");

		Assert.AreEqual(3, parser.DescriptorCount);
		AssertPropertyAccess(parser, 0, "Items");
		Assert.AreEqual(PropertyPathStepDescriptorKind.IntIndexer, parser.GetDescriptorAt(1).Kind);
		AssertPropertyAccess(parser, 2, "Name");
	}

	[TestMethod]
	public void When_EmptyIndexer_Then_StringIndexer()
	{
		var parser = Parse("Items[]");

		Assert.AreEqual(2, parser.DescriptorCount);
		AssertPropertyAccess(parser, 0, "Items");

		var indexer = parser.GetDescriptorAt(1);
		Assert.AreEqual(PropertyPathStepDescriptorKind.StringIndexer, indexer.Kind);
		Assert.AreEqual("", indexer.Name);
	}

	[TestMethod]
	[DataRow("\u0661\u0662\u0663")]
	[DataRow("\u0967\u0968\u0969")]
	[DataRow("\uff11\uff12\uff13")]
	[DataRow("1\u0662\uff13")]
	public void When_UnicodeDigitIndexer_Then_IntIndexer(string digits)
	{
		var parser = Parse($"Items[{digits}]");

		Assert.AreEqual(2, parser.DescriptorCount);
		AssertPropertyAccess(parser, 0, "Items");

		var indexer = parser.GetDescriptorAt(1);
		Assert.AreEqual(PropertyPathStepDescriptorKind.IntIndexer, indexer.Kind);
		Assert.AreEqual(123, indexer.Index);
	}

	[TestMethod]
	[DataRow("\u00B2", 0)]
	[DataRow("12\u00B2", 12)]
	[DataRow("\u07C1", 0)]
	[DataRow("12\u07C1", 12)]
	[DataRow("\u0BE7", 0)]
	public void When_CrtDigitHasNoConversion_Then_ConversionStops(string digits, int expected)
	{
		var indexer = Parse($"Items[{digits}]").GetDescriptorAt(1);

		Assert.AreEqual(PropertyPathStepDescriptorKind.IntIndexer, indexer.Kind);
		Assert.AreEqual(expected, indexer.Index);
	}

	[TestMethod]
	[DataRow("\u0DE6")]
	[DataRow("\u1A80")]
	[DataRow("\uA9D0")]
	[DataRow("\uABF0")]
	public void When_DigitIsOutsideCrtTable_Then_StringIndexer(string digits)
	{
		var indexer = Parse($"Items[{digits}]").GetDescriptorAt(1);

		Assert.AreEqual(PropertyPathStepDescriptorKind.StringIndexer, indexer.Kind);
		Assert.AreEqual(digits, indexer.Name);
	}

	[TestMethod]
	[DataRow("2147483648")]
	[DataRow("4294967296")]
	[DataRow("999999999999999999999999999")]
	public void When_IndexerLargerThanInt_Then_IntMaxValue(string digits)
	{
		var parser = Parse($"Items[{digits}]");

		Assert.AreEqual(2, parser.DescriptorCount);
		AssertPropertyAccess(parser, 0, "Items");

		var indexer = parser.GetDescriptorAt(1);
		Assert.AreEqual(PropertyPathStepDescriptorKind.IntIndexer, indexer.Kind);
		Assert.AreEqual(int.MaxValue, indexer.Index);
	}

	[TestMethod]
	public void When_AttachedProperty()
	{
		var expected = Grid.RowProperty;

		var parser = Parse("(Grid.Row)");

		Assert.AreEqual(1, parser.DescriptorCount);

		var descriptor = parser.GetDescriptorAt(0);
		Assert.AreEqual(PropertyPathStepDescriptorKind.DependencyProperty, descriptor.Kind);
		Assert.AreSame(expected, descriptor.Property);
		Assert.IsNull(descriptor.Name);
	}

	[TestMethod]
	public void When_AttachedProperty_Then_FollowedByProperty()
	{
		var expected = Canvas.LeftProperty;

		var parser = Parse("(Canvas.Left).Something");

		Assert.AreEqual(2, parser.DescriptorCount);
		Assert.AreEqual(PropertyPathStepDescriptorKind.DependencyProperty, parser.GetDescriptorAt(0).Kind);
		Assert.AreSame(expected, parser.GetDescriptorAt(0).Property);
		AssertPropertyAccess(parser, 1, "Something");
	}

	[TestMethod]
	public void When_AttachedProperty_Then_FollowedByIndexer()
	{
		var expected = Grid.RowProperty;

		var parser = Parse("(Grid.Row)[3]");

		Assert.AreEqual(2, parser.DescriptorCount);
		Assert.AreEqual(PropertyPathStepDescriptorKind.DependencyProperty, parser.GetDescriptorAt(0).Kind);
		Assert.AreSame(expected, parser.GetDescriptorAt(0).Property);
		Assert.AreEqual(PropertyPathStepDescriptorKind.IntIndexer, parser.GetDescriptorAt(1).Kind);
		Assert.AreEqual(3, parser.GetDescriptorAt(1).Index);
	}

	[TestMethod]
	public void When_PropertyFollowedByAttachedProperty()
	{
		var expected = Grid.RowProperty;

		var parser = Parse("Child.(Grid.Row)");

		Assert.AreEqual(2, parser.DescriptorCount);
		AssertPropertyAccess(parser, 0, "Child");
		Assert.AreEqual(PropertyPathStepDescriptorKind.DependencyProperty, parser.GetDescriptorAt(1).Kind);
		Assert.AreSame(expected, parser.GetDescriptorAt(1).Property);
	}

	[TestMethod]
	[DataRow("(Grid.Row", DisplayName = "Unterminated attached property")]
	[DataRow("(Grid.Row)Name", DisplayName = "Missing separator after attached property")]
	[DataRow("(Unknown.Unknown)", DisplayName = "Unresolvable attached property")]
	[DataRow("Items[0", DisplayName = "Unterminated indexer")]
	[DataRow("Items[\0]", DisplayName = "Null-terminated indexer")]
	[DataRow("Items[0]Name", DisplayName = "Missing separator after indexer")]
	[DataRow("Name.", DisplayName = "Trailing separator")]
	[DataRow("First..Second", DisplayName = "Empty step")]
	public void When_InvalidPath_Then_Throws(string path)
	{
		Assert.ThrowsExactly<ArgumentException>(() => Parse(path));
	}

	[TestMethod]
	[DataRow("Name.")]
	[DataRow("A.B.C.D.")]
	[DataRow("Items[0]Name")]
	public void When_ParseFails_Then_NoPartialDescriptorsArePublished(string path)
	{
		var parser = new PropertyPathParser();
		Assert.ThrowsExactly<ArgumentException>(() => parser.SetSource(path, null));
		Assert.AreEqual(0, parser.DescriptorCount);

		parser.SetSource("Replacement", null);
		Assert.AreEqual(1, parser.DescriptorCount);
		AssertPropertyAccess(parser, 0, "Replacement");
	}

	[TestMethod]
	public void When_SetSource_CalledTwice_Then_UnexpectedHResult()
	{
		var parser = new PropertyPathParser();
		parser.SetSource("First", null);
		var exception = Assert.ThrowsExactly<COMException>(() => parser.SetSource("Second.Third", null));
		Assert.AreEqual(unchecked((int)0x8000FFFF), exception.HResult);

		Assert.AreEqual(1, parser.DescriptorCount);
		AssertPropertyAccess(parser, 0, "First");
	}

	[TestMethod]
	public void When_GetDescriptorAt_OutOfRange_Then_Throws()
	{
		var parser = Parse("Name");

		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => parser.GetDescriptorAt(1));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => parser.GetDescriptorAt(-1));
	}

	[TestMethod]
	public void When_Listener_Then_StepsAreCreatedInOrder()
	{
		var parser = Parse("First.Second.Third");

		using var listener = new PropertyPathListener();
		listener.Initialize(null, parser, fListenToChanges: false, fUseWeakReferenceForSource: false);

		var step = listener.DebugGetFirstStep();
		Assert.AreEqual("First", step.DebugGetPropertyName());

		step = step.GetNextStep();
		Assert.AreEqual("Second", step.DebugGetPropertyName());

		step = step.GetNextStep();
		Assert.AreEqual("Third", step.DebugGetPropertyName());
		Assert.IsNull(step.GetNextStep());
	}

	[TestMethod]
	[DataRow("Child.Values[1]", "b")]
	[DataRow("Child.Values[\u0661]", "b")]
	[DataRow("Child.Values[\u00B2]", "a")]
	public void When_Listener_Then_ValueIsResolved(string path, string expected)
	{
		var parser = Parse(path);

		using var listener = new PropertyPathListener();
		listener.Initialize(null, parser, fListenToChanges: false, fUseWeakReferenceForSource: false);

		listener.SetSource(new Source { Child = new Child { Values = new[] { "a", "b", "c" } } });

		Assert.IsTrue(listener.FullPathExists());
		Assert.AreEqual(expected, listener.GetValue());
	}

	[TestMethod]
	public void When_Listener_EmptyPath_Then_SourceIsReturned()
	{
		var parser = Parse("");
		var source = new Source();

		using var listener = new PropertyPathListener();
		listener.Initialize(null, parser, fListenToChanges: false, fUseWeakReferenceForSource: false);

		listener.SetSource(source);

		Assert.AreSame(source, listener.GetValue());
	}

	[TestMethod]
	public void When_Listener_Disposed_Then_ChainIsBroken()
	{
		var parser = Parse("Child.Name");

		var listener = new PropertyPathListener();
		listener.Initialize(null, parser, fListenToChanges: false, fUseWeakReferenceForSource: false);
		listener.SetSource(new Source { Child = new Child { Name = "value" } });

		var first = listener.DebugGetFirstStep();
		Assert.IsTrue(listener.FullPathExists());

		listener.Dispose();

		Assert.IsNull(first.GetNextStep());
		Assert.IsFalse(listener.FullPathExists());
	}

	// The expected values below were measured on net10.0/x64 with GC.GetAllocatedBytesForCurrentThread().
	// The "before" figures come from the previous descriptor design (a List<PropertyPathStepDescriptor>
	// backing array plus one heap object per step).

	[TestMethod]
	public void When_SimplePath_Then_ParsingDoesNotAllocate()
	{
		// Before: 80 bytes (backing array + descriptor object).
		// After: the single segment reuses the source string and the descriptor fits the inline storage.
		AssertAllocationsPerParse("Name", maximumBytesPerIteration: 0);
	}

	[TestMethod]
	public void When_TwoStepsPath_Then_OnlySegmentStringsAreAllocated()
	{
		// Before: 176 bytes. After: only the 2 segment strings ("First" = 32 + "Second" = 40).
		AssertAllocationsPerParse("First.Second", maximumBytesPerIteration: 72);
	}

	[TestMethod]
	public void When_IndexedPath_Then_IndexIsParsedWithoutAllocating()
	{
		// Before: 168 bytes. After: only the "Items" segment; the index is parsed straight from a span.
		AssertAllocationsPerParse("Items[1024]", maximumBytesPerIteration: 32);
	}

	private static void AssertAllocationsPerParse(string path, long maximumBytesPerIteration)
	{
		const int Iterations = 10_000;

		var parsers = new PropertyPathParser[Iterations];
		for (var i = 0; i < Iterations; i++)
		{
			parsers[i] = new PropertyPathParser();
		}

		// Warm up so that JIT-time allocations are not accounted for below.
		for (var i = 0; i < 200; i++)
		{
			new PropertyPathParser().SetSource(path, null);
		}

		var before = GC.GetAllocatedBytesForCurrentThread();
		for (var i = 0; i < Iterations; i++)
		{
			parsers[i].SetSource(path, null);
		}
		var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

		Assert.IsTrue(
			allocated <= maximumBytesPerIteration * Iterations,
			$"Parsing '{path}' allocated {allocated / (double)Iterations:0.##} bytes per iteration, " +
			$"expected at most {maximumBytesPerIteration}.");

		// Keep the parsers alive so the measured work cannot be elided.
		Assert.AreEqual(parsers[0].DescriptorCount, parsers[Iterations - 1].DescriptorCount);
	}

	private static PropertyPathParser Parse(string path)
	{
		var parser = new PropertyPathParser();
		parser.SetSource(path, null);

		return parser;
	}

	private static void AssertPropertyAccess(PropertyPathParser parser, int index, string expectedName)
	{
		var descriptor = parser.GetDescriptorAt(index);

		Assert.AreEqual(PropertyPathStepDescriptorKind.PropertyAccess, descriptor.Kind);
		Assert.AreEqual(expectedName, descriptor.Name);
	}

	private class Source
	{
		public Child Child { get; set; }

		public string Name { get; set; }
	}

	private class Child
	{
		public string Name { get; set; }

		public string[] Values { get; set; }
	}
}
