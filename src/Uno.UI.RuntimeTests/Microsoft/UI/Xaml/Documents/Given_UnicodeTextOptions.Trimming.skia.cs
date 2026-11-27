#if __SKIA__
#nullable enable

using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Documents;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Documents;

// Trimming and object lines as Line Services lays them out, which RichTextBlock's trimming tests check against WinUI.
public partial class Given_UnicodeTextOptions
{
	private static InlineObjectInfo Object(float width, float height, float ascent)
		=> new(null, null, width, height, ascent, Microsoft.UI.Text.VerticalCharacterAlignment.Baseline);

	[TestMethod]
	public async Task When_Trimmed_Without_Room_Keeps_First_Cluster()
	{
		// Line Services never formats an empty line: with room for no more than one character and the ellipsis, the
		// collapsed line keeps its first character.
		await WaitForFonts(new Inline[] { R("WWWWWWWWWW") }, 1000);
		var full = Layout(new Inline[] { R("WWWWWWWWWW") }, 1000, wrapping: TextWrapping.NoWrap).GetLineForTests(0);
		var oneCharacter = full.Clusters[0].Width;

		var line = Layout(new Inline[] { R("WWWWWWWWWW") }, oneCharacter + 1, trimming: TextTrimming.CharacterEllipsis, wrapping: TextWrapping.NoWrap).GetLineForTests(0);
		Assert.IsTrue(line.HasEllipsis, "The line should be trimmed");
		Assert.AreEqual(2, line.Clusters.Length, $"The first character and the ellipsis ({string.Join(" ", line.Clusters.Select(Describe))})");
		Assert.AreEqual(0, line.Clusters[0].Start);

		// A line of one character isn't trimmed.
		var single = Layout(new Inline[] { R("W") }, 1, trimming: TextTrimming.CharacterEllipsis, wrapping: TextWrapping.NoWrap).GetLineForTests(0);
		Assert.IsFalse(single.HasEllipsis, "A single character is never collapsed");
	}

	[TestMethod]
	public async Task When_Trimmed_At_An_Object_Keeps_It()
	{
		// Line Services never breaks an object: a collapsed line keeps the object it overflows at, then the ellipsis.
		var container = new InlineUIContainer();
		var options = new UnicodeTextOptions { InlineContainers = _ => Object(90, 20, 16) };
		await WaitForFonts(new Inline[] { R("ab  cd") }, 1000);

		var line = Layout(new Inline[] { R("ab "), container, R(" cd") }, 50, trimming: TextTrimming.CharacterEllipsis, options: options, wrapping: TextWrapping.NoWrap).GetLineForTests(0);
		Assert.IsTrue(line.HasEllipsis, "The line should be trimmed");
		var placeholder = line.Clusters.SingleOrDefault(c => c.Start == 3);
		Assert.AreNotEqual(default, placeholder, $"The object should stay on the line ({string.Join(" ", line.Clusters.Select(Describe))})");
		Assert.AreEqual(90, placeholder.Width, 0.001);
		Assert.AreEqual(4, line.Clusters[^1].Start, "The ellipsis follows the object");
	}

	[TestMethod]
	public async Task When_Line_Holds_Objects_Only()
	{
		// A line of objects alone has no text, so no font metrics: it is as tall as its objects, even when they are shorter
		// than a line of the font (WinUI: two 90 by 20 px objects on their own lines and a line of text measure 66.6).
		var container = new InlineUIContainer();
		var options = new UnicodeTextOptions { InlineContainers = _ => Object(90, 20, 16) };
		await WaitForFonts(new Inline[] { R("ab cd") }, 1000);

		var layout = Layout(new Inline[] { R("ab"), container, R("cd") }, 100, options: options);
		Assert.AreEqual(3, layout.LineCount, "The object takes a line of its own");
		Assert.IsTrue(layout.GetLineForTests(0).Height > 20, "Precondition: a line of the font is taller than the object");
		var objectLine = layout.GetLineForTests(1);
		Assert.AreEqual(20, objectLine.Height, 0.001, "The object line's height");
		Assert.AreEqual(16, objectLine.Baseline, 0.001, "The object line's baseline");

		// With text beside it, a small object's line keeps the font's metrics.
		var textOnly = Layout(new Inline[] { R("ab  cd") }, 1000).GetLineForTests(0);
		var mixed = Layout(new Inline[] { R("ab "), container, R(" cd") }, 1000, options: new UnicodeTextOptions { InlineContainers = _ => Object(10, 10, 8) }).GetLineForTests(0);
		Assert.AreEqual(textOnly.Height, mixed.Height, 0.001, "A line with text keeps the font's line height");
	}
}
#endif
