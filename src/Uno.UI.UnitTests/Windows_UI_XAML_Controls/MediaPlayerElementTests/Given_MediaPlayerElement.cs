#nullable enable

using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Media.Core;

namespace Uno.UI.Tests.Windows_UI_XAML_Controls.MediaPlayerElementTests;

[UITestClass]
public class Given_MediaPlayerElement
{
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25795")]
	public void When_Source_Set_Before_Template_Applied()
	{
		var source = MediaSource.CreateFromUri(new Uri("https://example.invalid/video.mp4"));
		var sut = new MediaPlayerElement { Source = source };

		var player = sut.MediaPlayer;
		Assert.IsNotNull(player);
		Assert.AreSame(source, player.Source);

		var otherSource = MediaSource.CreateFromUri(new Uri("https://example.invalid/other.mp4"));
		sut.Source = otherSource;

		Assert.AreSame(player, sut.MediaPlayer);
		Assert.AreSame(otherSource, player.Source);
	}
}
