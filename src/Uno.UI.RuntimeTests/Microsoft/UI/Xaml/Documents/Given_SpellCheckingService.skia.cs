#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Documents;

[TestClass]
[RunsOnUIThread]
public class Given_SpellCheckingService
{
	// The spell-checking add-in is referenced by the app, not by the tests. The annotation keeps the members the tests
	// read when the app is trimmed.
	[DynamicallyAccessedMembers(
		DynamicallyAccessedMemberTypes.PublicConstructors
		| DynamicallyAccessedMemberTypes.NonPublicProperties
		| DynamicallyAccessedMemberTypes.NonPublicFields)]
	private static readonly Type? _serviceType = Type.GetType("Uno.WinUI.SpellChecking.SpellCheckingService, Uno.WinUI.SpellChecking");

	private static ISpellCheckingService CreateService()
	{
		if (_serviceType is null)
		{
			Assert.Inconclusive("The spell-checking add-in isn't part of this app.");
		}

		return (ISpellCheckingService)Activator.CreateInstance(_serviceType!, new object?[] { null })!;
	}

	private static int DictionaryLookups(ISpellCheckingService service)
		=> (int)_serviceType!.GetProperty("DictionaryLookupsForTesting", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(service)!;

	private static int RememberedVerdicts(ISpellCheckingService service)
		=> (int)_serviceType!.GetProperty("RememberedVerdictsForTesting", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(service)!;

	private static int GenerationSize
		=> (int)_serviceType!.GetField("RememberedVerdictsPerGeneration", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;

	// Words made of letters only (numbers aren't checked), all different.
	private static string DistinctWords(int count, out List<int> boundaries, string suffix = "")
	{
		var sb = new StringBuilder();
		boundaries = new List<int>(count);
		for (var i = 0; i < count; i++)
		{
			var n = i;
			do
			{
				sb.Append((char)('a' + n % 26));
				n /= 26;
			}
			while (n > 0);
			sb.Append("qz").Append(suffix).Append(' ');
			boundaries.Add(sb.Length);
		}

		return sb.ToString();
	}

	[TestMethod]
	public async Task When_TextBox_Typed_Into_Then_Only_New_Words_Are_Looked_Up()
	{
		var service = CreateService();
		UnicodeText.SpellCheckingServiceOverrideForTesting = service;
		var SUT = new TextBox { Width = 300, Height = 100, AcceptsReturn = true, TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap, IsSpellCheckEnabled = true, Text = string.Join("\r", Enumerable.Repeat("lorem ipsum dolor sit amet consectetur", 50)) };
		try
		{
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();
			var afterFirstLayout = DictionaryLookups(service);
			Assert.IsGreaterThan(0, afterFirstLayout);
			Assert.IsLessThanOrEqualTo(6, afterFirstLayout, "Each distinct word is looked up once, however often it repeats.");

			// A new word: looked up once, by every layout of this keystroke together.
			SUT.Select(0, 0);
			SUT.SelectedText = "x";
			SUT.UpdateLayout();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(afterFirstLayout + 1, DictionaryLookups(service), "Only the edited word (\"xlorem\") is new.");

			// Undone: every word is known again.
			SUT.Select(0, 1);
			SUT.SelectedText = "";
			SUT.UpdateLayout();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(afterFirstLayout + 1, DictionaryLookups(service));
		}
		finally
		{
			UnicodeText.SpellCheckingServiceOverrideForTesting = null;
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public void When_Words_Are_Known_Then_A_Check_Gives_The_Same_Corrections()
	{
		var service = CreateService();
		var text = "the quick brwn fox jumpd over the lazy dog, the quick brwn fox";
		var boundaries = new List<int>();
		for (var i = 0; i < text.Length; i++)
		{
			if (text[i] == ' ' || i == text.Length - 1)
			{
				boundaries.Add(i + 1);
			}
		}

		var first = service.SpellCheck(boundaries, text);
		var lookups = DictionaryLookups(service);
		var second = service.SpellCheck(boundaries, text);

		CollectionAssert.AreEqual(first, second);
		Assert.AreEqual(lookups, DictionaryLookups(service), "The second check looks nothing up.");
		var flagged = first.Select((c, i) => (c, i)).Where(x => x.c is not null).Select(x => text.Substring(x.i == 0 ? 0 : boundaries[x.i - 1], boundaries[x.i] - (x.i == 0 ? 0 : boundaries[x.i - 1])).Trim()).ToArray();
		CollectionAssert.AreEqual(new[] { "brwn", "jumpd", "brwn" }, flagged);
	}

	[TestMethod]
	public void When_Words_Are_Known_Then_A_Check_Allocates_No_String_Per_Word()
	{
		var service = CreateService();
		var shortWords = DistinctWords(2_000, out var shortBoundaries);
		var longWords = DistinctWords(2_000, out var longBoundaries, suffix: new string('x', 40));
		service.SpellCheck(shortBoundaries, shortWords);
		service.SpellCheck(longBoundaries, longWords);

		long Allocated(List<int> boundaries, string text)
		{
			var before = GC.GetAllocatedBytesForCurrentThread();
			service.SpellCheck(boundaries, text);
			return GC.GetAllocatedBytesForCurrentThread() - before;
		}

		var forShort = Allocated(shortBoundaries, shortWords);
		var forLong = Allocated(longBoundaries, longWords);

		// Copying the words would cost ~80 more bytes per long word.
		Assert.IsLessThan(2_000 * 8, Math.Abs(forLong - forShort), $"{forShort} bytes for 3-letter words, {forLong} for 43-letter ones.");
	}

	[TestMethod]
	public void When_Text_Has_More_Distinct_Words_Than_The_Bound_Then_The_Memory_Stays_Bounded()
	{
		var service = CreateService();
		var size = GenerationSize;
		var text = DistinctWords(size * 3, out var boundaries);

		service.SpellCheck(boundaries, text);

		Assert.IsLessThanOrEqualTo(2 * size, RememberedVerdicts(service));
		Assert.AreEqual(size * 3, DictionaryLookups(service));

		// The words of the last generation are still known.
		var tail = boundaries.Count - 10;
		var start = boundaries[tail - 1];
		var lookups = DictionaryLookups(service);
		service.SpellCheck(boundaries.Skip(tail).Select(b => b - start).ToList(), text.Substring(start));
		Assert.AreEqual(lookups, DictionaryLookups(service));
	}
}
