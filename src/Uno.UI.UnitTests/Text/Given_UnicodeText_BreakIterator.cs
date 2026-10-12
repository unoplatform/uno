#nullable enable

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Foundation.Logging;
using Windows.Foundation;

namespace Uno.UI.Tests.Text;

[TestClass]
public class Given_UnicodeText_BreakIterator
{
	private const string LongText = "The quick brown fox jumps over the lazy dog while the band plays on and on into the night.";

	[TestMethod]
	public void When_Measuring_Many_Wrapped_Paragraphs_Then_Break_Iterator_Is_Not_Reopened()
	{
		// Warm-up opens this thread's cached iterator.
		MeasureWrapped("warm-up paragraph", 100);

		var before = UnicodeText.BreakIteratorOpenCount;
		for (var i = 0; i < 50; i++)
		{
			MeasureWrapped($"Paragraph {i} wraps across several lines because it is long enough", 100);
			MeasureWrapped($"Příliš žluťoučký kůň úpěl ďábelské ódy č. {i}", 100);
		}

		var opened = UnicodeText.BreakIteratorOpenCount - before;
		Assert.AreEqual(0, opened, "Each measure re-opened an ICU break iterator instead of reusing the cached one.");
	}

	[TestMethod]
	public void When_Reused_Iterator_Then_Line_Breaks_Match_A_Fresh_Measure()
	{
		var first = MeasureWrapped(LongText, 120);

		// Re-point the cached iterator at unrelated texts of other lengths, including an empty one.
		MeasureWrapped("short", 120);
		MeasureWrapped("", 120);
		MeasureWrapped("مرحبا بالعالم، هذا نص عربي طويل بما يكفي ليلتف", 120);

		var again = MeasureWrapped(LongText, 120);

		Assert.AreEqual(first, again);
		Assert.IsTrue(first.Height > MeasureWrapped("short", 120).Height, "The long text is expected to wrap.");
	}

	[TestMethod]
	public void When_Measuring_Thread_Exits_Then_Its_Break_Iterators_Are_Closed()
	{
		var before = UnicodeText.BreakIteratorCloseCount;

		var thread = new Thread(() => MeasureWrapped(LongText, 120));
		thread.Start();
		thread.Join();

		GC.Collect();
		GC.WaitForPendingFinalizers();

		var closed = UnicodeText.BreakIteratorCloseCount - before;
		Assert.IsTrue(closed > 0, "The exited thread's cached ICU break iterators were never closed.");
	}

	[TestMethod]
	public void When_Icu_Reports_A_Warning_And_Trace_Is_Off_Then_Nothing_Is_Allocated()
	{
		var icu = typeof(UnicodeText).GetNestedType("ICU", BindingFlags.NonPublic)!;
		var checkErrorCode = icu
			.GetMethod("CheckErrorCode", BindingFlags.Public | BindingFlags.Static)!
			.MakeGenericMethod(icu.GetNestedType("ubrk_open")!)
			.CreateDelegate<Action<int>>();

		// Make sure ICU is loaded before measuring.
		MeasureWrapped("load ICU", 100);

		// The test host reports Trace as enabled for a None-level logger (#24757), so silence ICU's logger explicitly.
		var loggers = (ConditionalWeakTable<Type, Logger>)typeof(LogExtensionPoint)
			.GetField("_loggers", BindingFlags.NonPublic | BindingFlags.Static)!
			.GetValue(null)!;
		loggers.TryGetValue(icu, out var previousLogger);
		loggers.AddOrUpdate(icu, new Logger(null));

		long allocated;
		try
		{
			// U_USING_DEFAULT_WARNING, which ubrk_open reports on every call.
			const int warning = -127;
			checkErrorCode(warning);

			var before = GC.GetAllocatedBytesForCurrentThread();
			for (var i = 0; i < 1000; i++)
			{
				checkErrorCode(warning);
			}
			allocated = GC.GetAllocatedBytesForCurrentThread() - before;
		}
		finally
		{
			if (previousLogger is not null)
			{
				loggers.AddOrUpdate(icu, previousLogger);
			}
			else
			{
				loggers.Remove(icu);
			}
		}

		Assert.IsTrue(allocated < 1000, $"CheckErrorCode allocated {allocated} bytes for 1000 discarded warnings.");
	}

	private static Size MeasureWrapped(string text, double width)
	{
		var textBlock = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap };
		textBlock.Measure(new Size(width, double.PositiveInfinity));
		return textBlock.DesiredSize;
	}
}
