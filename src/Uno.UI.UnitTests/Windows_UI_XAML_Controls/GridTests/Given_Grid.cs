using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Linq;
using Windows.Foundation;
using AwesomeAssertions.Execution;
using Uno.UI.Controls.Legacy;
using Uno.UI.Tests.Windows_UI_XAML_Controls.GridTests.Controls;

namespace Uno.UI.Tests.GridTests
{
	[TestClass]
	public partial class Given_Grid : Context
	{
		[TestMethod]
#if RUNTIME_NATIVE_AOT
		[Ignore(".BeEquivalentTo() unsupported under NativeAOT; see: https://github.com/AwesomeAssertions/AwesomeAssertions/issues/290")]
#endif  // RUNTIME_NATIVE_AOT
		public void When_Grid_Uses_Common_Syntax()
		{
			using var _ = new AssertionScope();
			var SUT = new Grid_Uses_Common_Syntax();

			SUT.ForceLoaded();

			SUT.grid.Should().NotBeNull();
			// Compare the parsed Height/Width GridLengths (what these parsing tests verify).
			// Comparing whole RowDefinition/ColumnDefinition objects would also pull in the
			// computed ActualHeight/ActualWidth, which are now populated once the grid is laid
			// out in the live test window (they were only 0 under the old non-laying-out mock).
			SUT.grid.RowDefinitions.Select(rd => rd.Height).Should().BeEquivalentTo(new[]
			{
				new GridLength(1, GridUnitType.Star),
				new GridLength(1, GridUnitType.Auto),
				new GridLength(25, GridUnitType.Pixel),
				new GridLength(14, GridUnitType.Pixel),
				new GridLength(20, GridUnitType.Pixel),
			});
			SUT.grid.ColumnDefinitions.Select(cd => cd.Width).Should().BeEquivalentTo(new[]
			{
				new GridLength(1, GridUnitType.Star),
				new GridLength(2, GridUnitType.Star),
				new GridLength(1, GridUnitType.Auto),
				new GridLength(1, GridUnitType.Star),
				new GridLength(300, GridUnitType.Pixel),
			});
		}

		[TestMethod]
#if RUNTIME_NATIVE_AOT
		[Ignore(".BeEquivalentTo() unsupported under NativeAOT; see: https://github.com/AwesomeAssertions/AwesomeAssertions/issues/290")]
#endif  // RUNTIME_NATIVE_AOT
		public void When_Grid_Uses_New_Succinct_Syntax()
		{
			using var _ = new AssertionScope();
			var SUT = new Grid_Uses_New_Succinct_Syntax();

			SUT.ForceLoaded();

			SUT.grid.Should().NotBeNull();
			// Compare the parsed Height/Width GridLengths (what these parsing tests verify).
			// Comparing whole RowDefinition/ColumnDefinition objects would also pull in the
			// computed ActualHeight/ActualWidth, which are now populated once the grid is laid
			// out in the live test window (they were only 0 under the old non-laying-out mock).
			SUT.grid.RowDefinitions.Select(rd => rd.Height).Should().BeEquivalentTo(new[]
			{
				new GridLength(1, GridUnitType.Star),
				new GridLength(1, GridUnitType.Auto),
				new GridLength(25, GridUnitType.Pixel),
				new GridLength(14, GridUnitType.Pixel),
				new GridLength(20, GridUnitType.Pixel),
			});
			SUT.grid.ColumnDefinitions.Select(cd => cd.Width).Should().BeEquivalentTo(new[]
			{
				new GridLength(1, GridUnitType.Star),
				new GridLength(2, GridUnitType.Star),
				new GridLength(1, GridUnitType.Auto),
				new GridLength(1, GridUnitType.Star),
				new GridLength(300, GridUnitType.Pixel),
			});
		}

		[TestMethod]
#if RUNTIME_NATIVE_AOT
		[Ignore(".BeEquivalentTo() unsupported under NativeAOT; see: https://github.com/AwesomeAssertions/AwesomeAssertions/issues/290")]
#endif  // RUNTIME_NATIVE_AOT
		public void When_Grid_Uses_New_Assigned_ContentProperty_Syntax()
		{
			using var _ = new AssertionScope();
			var SUT = new Grid_Uses_New_Assigned_ContentProperty_Syntax();

			SUT.ForceLoaded();

			SUT.grid.Should().NotBeNull();
			// Compare the parsed Height/Width GridLengths (what these parsing tests verify).
			// Comparing whole RowDefinition/ColumnDefinition objects would also pull in the
			// computed ActualHeight/ActualWidth, which are now populated once the grid is laid
			// out in the live test window (they were only 0 under the old non-laying-out mock).
			SUT.grid.RowDefinitions.Select(rd => rd.Height).Should().BeEquivalentTo(new[]
			{
				new GridLength(1, GridUnitType.Star),
				new GridLength(1, GridUnitType.Auto),
				new GridLength(25, GridUnitType.Pixel),
				new GridLength(14, GridUnitType.Pixel),
				new GridLength(20, GridUnitType.Pixel),
			});
			SUT.grid.ColumnDefinitions.Select(cd => cd.Width).Should().BeEquivalentTo(new[]
			{
				new GridLength(1, GridUnitType.Star),
				new GridLength(2, GridUnitType.Star),
				new GridLength(1, GridUnitType.Auto),
				new GridLength(1, GridUnitType.Star),
				new GridLength(300, GridUnitType.Pixel),
			});
		}
	}
}
