using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media.Animation;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Tests.Windows_UI_Xaml_Media_Animation
{
	[TestClass]
	public class Given_BounceEase
	{
		[TestMethod]
		public void When_FinalValueGreaterThanInitial()
		{
			var sut = new BounceEase();

			EaseCore(0.0).Should().BeApproximately(100, .1);
			EaseCore(1.0).Should().BeApproximately(200, .1);

			double EaseCore(double normalizedTime)
				=> sut.Ease(
					currentTime: normalizedTime, duration: 1.0,
					startValue: 100, finalValue: 200);
		}

		[TestMethod]
		public void When_FinalValueLowerThanInitial()
		{
			var sut = new BounceEase();

			EaseCore(0.0).Should().BeApproximately(200, .1);
			EaseCore(1.0).Should().BeApproximately(100, .1);

			double EaseCore(double normalizedTime)
				=> sut.Ease(
					currentTime: normalizedTime, duration: 1.0,
					startValue: 200, finalValue: 100);
		}
	}
}
