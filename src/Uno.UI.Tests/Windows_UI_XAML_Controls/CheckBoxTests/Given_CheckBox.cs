using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Tests.Windows_UI_XAML_Controls.CheckBoxTests
{
	[TestClass]
	public class Given_CheckBox
	{
		[TestMethod]
		public void When_IsChecked()
		{
			var checkedCount = 0;
			var uncheckedCount = 0;
			var indeterminateCount = 0;

			var SUT = new CheckBox();
			SUT.Checked += (s, e) => checkedCount++;
			SUT.Unchecked += (s, e) => uncheckedCount++;
			SUT.Indeterminate += (s, e) => indeterminateCount++;

			SUT.IsChecked = true;
			checkedCount.Should().Be(1);
			uncheckedCount.Should().Be(0);
			SUT.IsChecked.Should().BeTrue();

			SUT.IsChecked = true;
			checkedCount.Should().Be(1);
			uncheckedCount.Should().Be(0);
			SUT.IsChecked.Should().BeTrue();

			SUT.IsChecked = false;
			checkedCount.Should().Be(1);
			uncheckedCount.Should().Be(1);
			SUT.IsChecked.Should().BeFalse();

			SUT.IsChecked = false;
			checkedCount.Should().Be(1);
			uncheckedCount.Should().Be(1);
			SUT.IsChecked.Should().BeFalse();

			indeterminateCount.Should().Be(0);
		}

		[TestMethod]
		public void When_IsChecked_True_And_Null()
		{
			var checkedCount = 0;
			var uncheckedCount = 0;
			var indeterminateCount = 0;

			var SUT = new CheckBox();
			SUT.Checked += (s, e) => checkedCount++;
			SUT.Unchecked += (s, e) => uncheckedCount++;
			SUT.Indeterminate += (s, e) => indeterminateCount++;

			SUT.IsChecked = true;
			checkedCount.Should().Be(1);
			uncheckedCount.Should().Be(0);
			SUT.IsChecked.Should().BeTrue();

			SUT.IsChecked = true;
			checkedCount.Should().Be(1);
			uncheckedCount.Should().Be(0);
			SUT.IsChecked.Should().BeTrue();

			SUT.IsChecked = null;
			checkedCount.Should().Be(1);
			indeterminateCount.Should().Be(1);
			uncheckedCount.Should().Be(0);
			SUT.IsChecked.Should().BeNull();

			SUT.IsChecked = null;
			checkedCount.Should().Be(1);
			uncheckedCount.Should().Be(0);
			SUT.IsChecked.Should().BeNull();

			indeterminateCount.Should().Be(1);
		}

		[TestMethod]
		public void When_ThreeState()
		{
			var checkedCount = 0;
			var uncheckedCount = 0;
			var indeterminateCount = 0;

			var SUT = new CheckBox() { IsThreeState = true };
			SUT.Checked += (s, e) => checkedCount++;
			SUT.Unchecked += (s, e) => uncheckedCount++;
			SUT.Indeterminate += (s, e) => indeterminateCount++;

			SUT.RaiseClick();

			SUT.IsChecked.Should().BeTrue();
			checkedCount.Should().Be(1);
			uncheckedCount.Should().Be(0);
			indeterminateCount.Should().Be(0);

			SUT.RaiseClick();

			SUT.IsChecked.Should().BeNull();
			checkedCount.Should().Be(1);
			uncheckedCount.Should().Be(0);
			indeterminateCount.Should().Be(1);

			SUT.RaiseClick();

			SUT.IsChecked.Should().BeFalse();
			checkedCount.Should().Be(1);
			uncheckedCount.Should().Be(1);
			indeterminateCount.Should().Be(1);
		}
	}
}
