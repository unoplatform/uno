using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System.Linq;
using Windows.Foundation;
using FluentAssertions;
using FluentAssertions.Execution;
using Uno.UI.Controls.Legacy;
using Uno.UI.Tests.Windows_UI_XAML_Controls.GridTests.Controls;

namespace Uno.UI.Tests.GridTests
{
	[TestClass]
	public partial class Given_Grid : Context
	{
		[TestMethod]
		public void When_Grid_Uses_Common_Syntax()
		{
			using var _ = new AssertionScope();
			var SUT = new Grid_Uses_Common_Syntax();

			SUT.ForceLoaded();

			SUT.grid.Should().NotBeNull();
			SUT.grid.RowDefinitions.Should().BeEquivalentTo(new[]
			{
				new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
				new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) },
				new RowDefinition { Height = new GridLength(25, GridUnitType.Pixel) },
				new RowDefinition { Height = new GridLength(14, GridUnitType.Pixel) },
				new RowDefinition { Height = new GridLength(20, GridUnitType.Pixel) },
			});
			SUT.grid.ColumnDefinitions.Should().BeEquivalentTo(new[]
			{
				new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
				new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) },
				new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) },
				new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
				new ColumnDefinition { Width = new GridLength(300, GridUnitType.Pixel) },
			});
		}

		[TestMethod]
		public void When_Grid_Uses_New_Succinct_Syntax()
		{
			using var _ = new AssertionScope();
			var SUT = new Grid_Uses_New_Succinct_Syntax();

			SUT.ForceLoaded();

			SUT.grid.Should().NotBeNull();
			SUT.grid.RowDefinitions.Should().BeEquivalentTo(new[]
			{
				new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
				new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) },
				new RowDefinition { Height = new GridLength(25, GridUnitType.Pixel) },
				new RowDefinition { Height = new GridLength(14, GridUnitType.Pixel) },
				new RowDefinition { Height = new GridLength(20, GridUnitType.Pixel) },
			});
			SUT.grid.ColumnDefinitions.Should().BeEquivalentTo(new[]
			{
				new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
				new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) },
				new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) },
				new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
				new ColumnDefinition { Width = new GridLength(300, GridUnitType.Pixel) },
			});
		}

		[TestMethod]
		public void When_Grid_Uses_New_Assigned_ContentProperty_Syntax()
		{
			using var _ = new AssertionScope();
			var SUT = new Grid_Uses_New_Assigned_ContentProperty_Syntax();

			SUT.ForceLoaded();

			SUT.grid.Should().NotBeNull();
			SUT.grid.RowDefinitions.Should().BeEquivalentTo(new[]
			{
				new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
				new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) },
				new RowDefinition { Height = new GridLength(25, GridUnitType.Pixel) },
				new RowDefinition { Height = new GridLength(14, GridUnitType.Pixel) },
				new RowDefinition { Height = new GridLength(20, GridUnitType.Pixel) },
			});
			SUT.grid.ColumnDefinitions.Should().BeEquivalentTo(new[]
			{
				new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
				new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) },
				new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) },
				new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
				new ColumnDefinition { Width = new GridLength(300, GridUnitType.Pixel) },
			});
		}
	}
}
