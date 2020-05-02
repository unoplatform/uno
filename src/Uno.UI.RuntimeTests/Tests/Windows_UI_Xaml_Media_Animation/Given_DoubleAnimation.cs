using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media_Animation
{
	[TestClass]
	public class Given_DoubleAnimation
	{
		[TestMethod]
		[RunsOnUIThread]
		public void When_SeekAlignedToLastTick()
		{
			var target = new Border();
			var doubleAnimation = new DoubleAnimation()
			{
				From = 50d,
				To = 100d,
				EnableDependentAnimation = true
			};
			Storyboard.SetTarget(doubleAnimation, target);
			Storyboard.SetTargetProperty(doubleAnimation, "Height");
			var sb = new Storyboard()
			{
				Children =
				{
					doubleAnimation
				}
			};

			sb.Begin();
			sb.SeekAlignedToLastTick(TimeSpan.FromMilliseconds(50));
		}
	}
}
