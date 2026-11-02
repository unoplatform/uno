using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Runtime.X11;

namespace Uno.UI.Tests.Windows_UI_Input;

/// <summary>
/// Tests for <see cref="X11TitleBarDecorations"/>: extending content into the title bar and the overlapped
/// presenter's title bar both write the Motif decorations word, so neither may undo the other.
/// </summary>
[UITestClass]
public class Given_X11TitleBarDecorations
{
	private const int NoTitleBar = 0x74;

	[TestMethod]
	public void When_Default_Then_All_Decorations()
		=> Assert.AreEqual(0xFF, new X11TitleBarDecorations().Decorations);

	[TestMethod]
	public void When_Extended_Then_Presenter_Restores_Title_Bar_Then_Stays_Undecorated()
	{
		var decorations = new X11TitleBarDecorations();

		decorations.ExtendContentIntoTitleBar(true);

		Assert.AreEqual(0, decorations.SetPresenterHasTitleBar(true));
	}

	[TestMethod]
	public void When_Presenter_Hides_Title_Bar_Then_Extended_Then_Stays_Undecorated()
	{
		var decorations = new X11TitleBarDecorations();

		decorations.SetPresenterHasTitleBar(false);

		Assert.AreEqual(0, decorations.ExtendContentIntoTitleBar(true));
	}

	[TestMethod]
	public void When_Extension_Removed_Then_Presenter_Title_Bar_Applies()
	{
		var decorations = new X11TitleBarDecorations();

		decorations.SetPresenterHasTitleBar(false);
		decorations.ExtendContentIntoTitleBar(true);

		Assert.AreEqual(NoTitleBar, decorations.ExtendContentIntoTitleBar(false));
		Assert.AreEqual(0xFF, decorations.SetPresenterHasTitleBar(true));
	}

	[TestMethod]
	public void When_Title_Bar_Hidden_Then_Title_And_Border_Cleared_Together()
	{
		var decorations = new X11TitleBarDecorations().SetPresenterHasTitleBar(false);

		Assert.AreEqual(0, decorations & (X11TitleBarDecorations.Title | X11TitleBarDecorations.Border));
		Assert.AreEqual(0, decorations & X11TitleBarDecorations.All, "All would turn the listed decorations into exclusions.");
	}
}
