using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System.Linq;
using Windows.Foundation;
using FluentAssertions;
using FluentAssertions.Execution;
using View = Windows.UI.Xaml.FrameworkElement;

namespace Uno.UI.Tests.GridTests
{
	[TestClass]
	public class Given_Grid : Context
	{
		[TestMethod]
		public void When_Empty_And_MeasuredEmpty()
		{
			using var _ = new AssertionScope();

			var SUT = new Grid { Name = "test" };

			SUT.Measure(default);
			var size = SUT.DesiredSize;
			SUT.Arrange(default);

			size.Should().Be(default);
			SUT.GetChildren().Should().BeEmpty();
		}

		[TestMethod]
		public void When_Empty_And_Measured_Non_Empty()
		{
			using var _ = new AssertionScope();

			var SUT = new Grid { Name = "test" };

			SUT.Measure(new Size(10, 10));
			var size = SUT.DesiredSize;
			SUT.Arrange(default);

			size.Should().Be(default);
			SUT.GetChildren().Should().BeEmpty();
		}

		[TestMethod]
		public void When_Grid_Has_One_Element()
		{
			using var _ = new AssertionScope();

			var SUT = new Grid { Name = "test" };

			SUT.AddChild(new View { Name = "Child01", RequestedDesiredSize = new Size(10, 10) });

			SUT.Measure(new Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			SUT.Arrange(new Rect(0, 0, 20, 20));


			SUT.GetChildren().First().Arranged.Should().Be(new Rect(0, 0, 20, 20));

			measuredSize.Should().Be(new Size(10, 10));
			SUT.GetChildren().Should().HaveCount(1);
		}

		[TestMethod]
		public void When_Grid_Has_One_Element_And_MinWidth_MinHeight_VerticalAlignment_Top_HorizontalAlignment_Center()
		{
			using var _ = new AssertionScope();

			var SUT = new Grid { Name = "test" };

			SUT.MinWidth = 40;
			SUT.MinHeight = 40;
			SUT.VerticalAlignment = VerticalAlignment.Top;
			SUT.HorizontalAlignment = HorizontalAlignment.Center;

			SUT.AddChild(new View
			{
				Name = "Child01",
				Width = 20,
				Height = 20
			});

			SUT.Measure(new Size(60, 60));
			var measuredSize = SUT.DesiredSize;
			SUT.Arrange(new Rect(0, 0, 60, 60));

			SUT.GetChildren().First().Arranged.Should().Be(new Rect(20, 20, 20, 20));

			measuredSize.Should().Be(new Size(40, 40));
			SUT.GetChildren().Should().HaveCount(1);
		}

		[TestMethod]
		public void When_Grid_Has_Two_Elements_And_MinWidth_MinHeight_VerticalAlignment_Top_HorizontalAlignment_Center_And_Child_Stretched_And_Centered()
		{
			using var _ = new AssertionScope();

			var SUT = new Grid { Name = "test" };

			SUT.MinWidth = 40;
			SUT.MinHeight = 40;
			SUT.VerticalAlignment = VerticalAlignment.Top;
			SUT.HorizontalAlignment = HorizontalAlignment.Center;

			var c1 = SUT
				.AddChild(new View
				{
					Name = "Child01",
					RequestedDesiredSize = new Size(20, 20),
					HorizontalAlignment = HorizontalAlignment.Stretch,
					VerticalAlignment = VerticalAlignment.Stretch
				});
			var c2 = SUT
				.AddChild(new View
				{
					Name = "Child02",
					RequestedDesiredSize = new Size(20, 20),
					HorizontalAlignment = HorizontalAlignment.Center,
					VerticalAlignment = VerticalAlignment.Center
				});

			SUT.Measure(new Size(60, 60));
			var measuredSize = SUT.DesiredSize;
			SUT.Arrange(new Rect(0, 0, 60, 60));

			c1.Arranged.Should().Be(new Rect(0, 0, 60, 60));
			c2.Arranged.Should().Be(new Rect(20, 20, 20, 20));

			measuredSize.Should().Be(new Size(40, 40));
			SUT.GetChildren().Should().HaveCount(2);
		}

		[TestMethod]
		public void When_Grid_Has_One_Colums_And_One_Row_And_No_Size_Spec()
		{
			using var _ = new AssertionScope();

			var SUT = new Grid { Name = "test" };

			var c1 = SUT
				.AddChild(new View
				{
					Name = "Child01",
					RequestedDesiredSize = new Size(10, 10)
				});
			var c2 = SUT
				.AddChild(new View
				{
					Name = "Child02",
					RequestedDesiredSize = new Size(10, 10)
				});

			SUT.Measure(new Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			SUT.Arrange(new Rect(0, 0, 20, 20));

			c1.Arranged.Should().Be(new Rect(0, 0, 20, 20));
			c2.Arranged.Should().Be(new Rect(0, 0, 20, 20));

			measuredSize.Should().Be(new Size(10, 10));
			SUT.GetChildren().Should().HaveCount(2);
		}

		[TestMethod]
		public void When_Grid_Has_Two_Colums_And_One_Row_And_No_Size_Spec()
		{
			using var _ = new AssertionScope();

			var SUT = new Grid { Name = "test" };

			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });

			var c1 = SUT
				.AddChild(new View
				{
					Name = "Child01",
					RequestedDesiredSize = new Size(10, 10)
				});
			var c2 = SUT
				.AddChild(new View
				{
					Name = "Child02",
					RequestedDesiredSize = new Size(10, 10)
				});

			Grid.SetColumn(c1, 0);
			Grid.SetColumn(c2, 1);

			SUT.Measure(new Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			SUT.Arrange(new Rect(0, 0, 20, 20));

			c1.Arranged.Should().Be(new Rect(0, 0, 10, 20));
			c2.Arranged.Should().Be(new Rect(10, 0, 10, 20));

			measuredSize.Should().Be(new Size(20, 10));
			SUT.GetChildren().Should().HaveCount(2);
		}

		[TestMethod]
		public void When_Grid_Has_Two_Colums_And_One_Row_And_MinWidth_MinHeight_VerticalAlignment_Top_HorizontalAlignment_Center_And_Child_Stretched_And_Centered()
		{
			using var _ = new AssertionScope();

			var SUT = new Grid { Name = "test" };

			SUT.MinWidth = 80;
			SUT.MinHeight = 80;
			SUT.VerticalAlignment = VerticalAlignment.Top;
			SUT.HorizontalAlignment = HorizontalAlignment.Center;

			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });

			var c1 = SUT
				.AddChild(new View
				{
					Name = "Child01",
					Width = 20,
					Height = 20,
					HorizontalAlignment = HorizontalAlignment.Stretch,
					VerticalAlignment = VerticalAlignment.Stretch
				});
			var c2 = SUT
				.AddChild(new View
				{
					Name = "Child02",
					Width = 20,
					Height = 20,
					HorizontalAlignment = HorizontalAlignment.Center,
					VerticalAlignment = VerticalAlignment.Center
				});

			Grid.SetColumn(c1, 0);
			Grid.SetColumn(c2, 1);

			SUT.Measure(new Size(100, 100));
			var measuredSize = SUT.DesiredSize;
			SUT.Arrange(new Rect(0, 0, 100, 100));

			c1.Arranged.Should().Be(new Rect(15, 40, 20, 20));
			c2.Arranged.Should().Be(new Rect(65, 40, 20, 20));

			measuredSize.Should().Be(new Size(80, 80));
			SUT.GetChildren().Should().HaveCount(2);
		}

		[TestMethod]
		public void When_Grid_Has_Two_Colums_And_One_Row_And_MinWidth_MinHeight_VerticalAlignment_Top_HorizontalAlignment_Stretch_And_Padding()
		{
			using var _ = new AssertionScope();

			var SUT = new Grid { Name = "test" };

			SUT.MinWidth = 80;
			SUT.MinHeight = 80;
			SUT.Padding = new Thickness(10, 20);
			SUT.VerticalAlignment = VerticalAlignment.Top;
			SUT.HorizontalAlignment = HorizontalAlignment.Stretch;

			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "Auto" });

			var c1 = SUT
				.AddChild(new View
				{
					Name = "Child01",
					RequestedDesiredSize = new Size(20, 20),
					HorizontalAlignment = HorizontalAlignment.Stretch,
					VerticalAlignment = VerticalAlignment.Stretch
				});
			var c2 = SUT
				.AddChild(new View
				{
					Name = "Child02",
					RequestedDesiredSize = new Size(20, 20),
					HorizontalAlignment = HorizontalAlignment.Center,
					VerticalAlignment = VerticalAlignment.Center
				});

			Grid.SetColumn(c1, 0);
			Grid.SetColumn(c2, 1);

			SUT.Measure(new Size(160, 160));
			var measuredSize = SUT.DesiredSize;
			SUT.Arrange(new Rect(0, 0, 160, 160));

			c1.Arranged.Should().Be(new Rect(10, 20, 120, 120));
			c2.Arranged.Should().Be(new Rect(130, 70, 20, 20));

			measuredSize.Should().Be(new Size(80, 80));
			SUT.GetChildren().Should().HaveCount(2);
		}

		[TestMethod]
		public void When_Grid_Has_Two_Colums_And_One_Row_And_MinWidth_MinHeight_VerticalAlignment_Top_HorizontalAlignment_Center_And_Child_Stretched_With_ColumnSpan_And_Centered()
		{
			using var _ = new AssertionScope();

			var SUT = new Grid { Name = "test" };

			SUT.MinWidth = 80;
			SUT.MinHeight = 80;
			SUT.VerticalAlignment = VerticalAlignment.Top;
			SUT.HorizontalAlignment = HorizontalAlignment.Center;

			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });

			var c1 = SUT
				.AddChild(new View
				{
					Name = "Child01",
					Width = 20,
					Height = 20,
					HorizontalAlignment = HorizontalAlignment.Stretch,
					VerticalAlignment = VerticalAlignment.Stretch
				}.GridColumnSpan(2));
			var c2 = SUT
				.AddChild(new View
				{
					Name = "Child02",
					Width = 20,
					Height = 20,
					HorizontalAlignment = HorizontalAlignment.Center,
					VerticalAlignment = VerticalAlignment.Center
				});

			Grid.SetColumn(c1, 0);
			Grid.SetColumn(c2, 1);

			SUT.Measure(new Size(100, 100));
			var measuredSize = SUT.DesiredSize;
			SUT.Arrange(new Rect(0, 0, 100, 100));

			c1.Arranged.Should().Be(new Rect(40, 40, 20, 20));
			c2.Arranged.Should().Be(new Rect(65, 40, 20, 20));

			measuredSize.Should().Be(new Size(80, 80));
			SUT.GetChildren().Should().HaveCount(2);
		}

		[TestMethod]
		public void When_Grid_Has_Two_Rows_And_One_Column_And_MinWidth_MinHeight_VerticalAlignment_Top_HorizontalAlignment_Center_And_Child_Stretched_With_RowSpan_And_Centered()
		{
			using var _ = new AssertionScope();

			var SUT = new Grid { Name = "test" };

			SUT.MinWidth = 80;
			SUT.MinHeight = 80;
			SUT.VerticalAlignment = VerticalAlignment.Top;
			SUT.HorizontalAlignment = HorizontalAlignment.Center;

			SUT.RowDefinitions.Add(new RowDefinition { Height = "*" });
			SUT.RowDefinitions.Add(new RowDefinition { Height = "*" });

			var c1 = SUT
				.AddChild(new View
				{
					Name = "Child01",
					Width = 20,
					Height = 20,
					HorizontalAlignment = HorizontalAlignment.Stretch,
					VerticalAlignment = VerticalAlignment.Stretch
				}.GridRowSpan(2));
			var c2 = SUT
				.AddChild(new View
				{
					Name = "Child02",
					Width = 20,
					Height = 20,
					HorizontalAlignment = HorizontalAlignment.Center,
					VerticalAlignment = VerticalAlignment.Center
				});

			Grid.SetRow(c1, 0);
			Grid.SetRow(c2, 1);

			SUT.Measure(new Size(100, 100));
			var measuredSize = SUT.DesiredSize;
			SUT.Arrange(new Rect(0, 0, 100, 100));

			c1.Arranged.Should().Be(new Rect(40, 40, 20, 20));
			c2.Arranged.Should().Be(new Rect(40, 65, 20, 20));

			measuredSize.Should().Be(new Size(80, 80));
			SUT.GetChildren().Should().HaveCount(2);
		}

		[TestMethod]
		public void When_Grid_Has_Two_Rows_And_One_Column_And_MinWidth_MinHeight_VerticalAlignment_Top_HorizontalAlignment_Center_And_Child_Stretched_And_Centered()
		{
			using var _ = new AssertionScope();

			var SUT = new Grid { Name = "test" };

			SUT.MinWidth = 80;
			SUT.MinHeight = 80;
			SUT.VerticalAlignment = VerticalAlignment.Top;
			SUT.HorizontalAlignment = HorizontalAlignment.Center;

			SUT.RowDefinitions.Add(new RowDefinition { Height = "*" });
			SUT.RowDefinitions.Add(new RowDefinition { Height = "*" });

			var c1 = SUT
				.AddChild(new View
				{
					Name = "Child01",
					Width = 20,
					Height = 20,
					HorizontalAlignment = HorizontalAlignment.Stretch,
					VerticalAlignment = VerticalAlignment.Stretch
				});
			var c2 = SUT
				.AddChild(new View
				{
					Name = "Child02",
					Width = 20,
					Height = 20,
					HorizontalAlignment = HorizontalAlignment.Center,
					VerticalAlignment = VerticalAlignment.Center
				});

			Grid.SetRow(c1, 0);
			Grid.SetRow(c2, 1);

			SUT.Measure(new Size(100, 100));
			var measuredSize = SUT.DesiredSize;
			SUT.Arrange(new Rect(0, 0, 100, 100));

			c1.Arranged.Should().Be(new Rect(40, 15, 20, 20));
			c2.Arranged.Should().Be(new Rect(40, 65, 20, 20));

			measuredSize.Should().Be(new Size(80, 80));
			SUT.GetChildren().Should().HaveCount(2);
		}

		[TestMethod]
		public void When_Grid_Has_Two_Rows_And_One_Column_And_MinWidth_MinHeight_VerticalAlignment_Top_HorizontalAlignment_Stretch_And_Padding()
		{
			using var _ = new AssertionScope();

			var SUT = new Grid { Name = "test" };

			SUT.MinWidth = 80;
			SUT.MinHeight = 80;
			SUT.Padding = new Thickness(10, 20);
			SUT.VerticalAlignment = VerticalAlignment.Stretch;
			SUT.HorizontalAlignment = HorizontalAlignment.Left;

			SUT.RowDefinitions.Add(new RowDefinition { Height = "*" });
			SUT.RowDefinitions.Add(new RowDefinition { Height = "Auto" });

			var c1 = SUT
				.AddChild(new View
				{
					Name = "Child01",
					Width = 20,
					Height = 20,
					HorizontalAlignment = HorizontalAlignment.Stretch,
					VerticalAlignment = VerticalAlignment.Stretch
				});
			var c2 = SUT
				.AddChild(new View
				{
					Name = "Child02",
					Width = 20,
					Height = 20,
					HorizontalAlignment = HorizontalAlignment.Center,
					VerticalAlignment = VerticalAlignment.Center
				});

			Grid.SetRow(c1, 0);
			Grid.SetRow(c2, 1);

			SUT.Measure(new Size(160, 160));
			var measuredSize = SUT.DesiredSize;
			SUT.Arrange(new Rect(0, 0, 160, 160));

			c1.Arranged.Should().Be(new Rect(70, 60, 20, 20));
			c2.Arranged.Should().Be(new Rect(70, 120, 20, 20));

			measuredSize.Should().Be(new Size(80, 80));
			SUT.GetChildren().Should().HaveCount(2);
		}

		[TestMethod]
		public void When_Grid_Has_Two_Colums_And_Two_Rows_And_No_Size_Spec()
		{
			using var _ = new AssertionScope();

			var SUT = new Grid { Name = "test" };

			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });

			SUT.RowDefinitions.Add(new RowDefinition { Height = "*" });
			SUT.RowDefinitions.Add(new RowDefinition { Height = "*" });

			var c1 = SUT.AddChild(
				new View
					{
						Name = "Child01",
						RequestedDesiredSize = new Size(10, 10)
					}
				.GridPosition(0, 0)
			);

			var c2 = SUT.AddChild(
				new View
					{
						Name = "Child02",
						RequestedDesiredSize = new Size(10, 10)
					}
				.GridPosition(0, 1)
			);

			var c3 = SUT.AddChild(
				new View
					{
						Name = "Child03",
						RequestedDesiredSize = new Size(10, 10)
					}
				.GridPosition(1, 0)
			);

			var c4 = SUT.AddChild(
				new View
					{
						Name = "Child04",
						RequestedDesiredSize = new Size(10, 10)
					}
				.GridPosition(1, 1)
			);

			SUT.Measure(new Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			SUT.Arrange(new Rect(0, 0, 20, 20));

			c1.Arranged.Should().Be(new Rect(0, 0, 10, 10));
			c2.Arranged.Should().Be(new Rect(10, 0, 10, 10));
			c3.Arranged.Should().Be(new Rect(0, 10, 10, 10));
			c4.Arranged.Should().Be(new Rect(10, 10, 10, 10));

			measuredSize.Should().Be(new Size(20, 20));
			SUT.GetChildren().Should().HaveCount(4);
		}

		[TestMethod]
		public void When_Grid_Has_Two_Colums_And_Two_Rows_And_MinWidth_MinHeight_VerticalAlignment_Top_HorizontalAlignment_Center_And_Child_Stretched_And_Centered()
		{
			using var _ = new AssertionScope();

			var SUT = new Grid { Name = "test" };

			SUT.MinWidth = 80;
			SUT.MinHeight = 80;
			SUT.VerticalAlignment = VerticalAlignment.Top;
			SUT.HorizontalAlignment = HorizontalAlignment.Center;

			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });

			SUT.RowDefinitions.Add(new RowDefinition { Height = "*" });
			SUT.RowDefinitions.Add(new RowDefinition { Height = "*" });

			var c1 = SUT.AddChild(
				new View
					{
						Name = "Child01",
						MinWidth = 20,
						MinHeight = 20,
						HorizontalAlignment = HorizontalAlignment.Stretch,
						VerticalAlignment = VerticalAlignment.Stretch
					}
				.GridPosition(0, 0)
			);

			var c2 = SUT.AddChild(
				new View
					{
						Name = "Child02",
						MinWidth = 20,
						MinHeight = 20,
						HorizontalAlignment = HorizontalAlignment.Center,
						VerticalAlignment = VerticalAlignment.Center
					}
				.GridPosition(0, 1)
			);

			var c3 = SUT.AddChild(
				new View
					{
						Name = "Child03",
						MinWidth = 20,
						MinHeight = 20,
						HorizontalAlignment = HorizontalAlignment.Center,
						VerticalAlignment = VerticalAlignment.Center
					}
				.GridPosition(1, 0)
			);

			var c4 = SUT.AddChild(
				new View
					{
						Name = "Child04",
						MinWidth = 20,
						MinHeight = 20,
						HorizontalAlignment = HorizontalAlignment.Stretch,
						VerticalAlignment = VerticalAlignment.Stretch
					}
				.GridPosition(1, 1)
			);

			SUT.Measure(new Size(100, 100));
			var measuredSize = SUT.DesiredSize;
			SUT.Arrange(new Rect(0, 0, 100, 100));

			c1.Arranged.Should().Be(new Rect(0, 0, 50, 50));
			c2.Arranged.Should().Be(new Rect(65, 15, 20, 20));
			c3.Arranged.Should().Be(new Rect(15, 65, 20, 20));
			c4.Arranged.Should().Be(new Rect(50, 50, 50, 50));

			measuredSize.Should().Be(new Size(80, 80));
			SUT.GetChildren().Should().HaveCount(4);
		}

		[TestMethod]
		public void When_Grid_Has_Two_Star_Uneven_Colums_And_One_Row()
		{
			using var _ = new AssertionScope();

			var SUT = new Grid { Name = "test" };

			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "2*" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });

			var c1 = SUT.AddChild(
				new View
					{
						Name = "Child01",
						MinWidth = 10,
						MinHeight = 10
					}
				.GridPosition(0, 0)
			);

			var c2 = SUT.AddChild(
				new View
					{
						Name = "Child02",
						MinWidth = 10,
						MinHeight = 10
					}
				.GridPosition(0, 1)
			);

			SUT.Measure(new Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			SUT.Arrange(new Rect(0, 0, 20, 20));

			c1.Arranged.Should().Be(new Rect(0, 0, (20 / 3.0) * 2.0, 20));
			c2.Arranged.Should().Be(new Rect((20 / 3.0) * 2.0, 0, 10, 20));

			measuredSize.Should().Be(new Size((20 / 3.0) + 10, 10));
			SUT.GetChildren().Should().HaveCount(2);
		}

		[TestMethod]
		public void When_Grid_Has_One_Absolute_Column_And_One_Star_Column_And_One_Row()
		{
			using var _ = new AssertionScope();

			var SUT = new Grid { Name = "test" };

			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "5" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });

			var c1 = SUT.AddChild(
				new View
					{
						Name = "Child01",
						Width = 10,
						Height = 10
					}
				.GridPosition(0, 0)
			);

			var c2 = SUT.AddChild(
				new View
					{
						Name = "Child02",
						Width = 10,
						Height = 10
					}
				.GridPosition(0, 1)
			);

			SUT.Measure(new Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			SUT.Arrange(new Rect(0, 0, 20, 20));

			c1.DesiredSize.Should().Be(new Size(5, 10));
			c2.DesiredSize.Should().Be(new Size(10, 10));

			c1.Arranged.Should().Be(new Rect(0, 5, 5, 10));
			c2.Arranged.Should().Be(new Rect(7.5, 5, 10, 10));

			measuredSize.Should().Be(new Size(15, 10));
			SUT.GetChildren().Should().HaveCount(2);
		}

		[TestMethod]
		public void When_Grid_Has_One_Variable_Sized_Element_With_ColSpan_and_Three_Columns()
		{
			using var _ = new AssertionScope();

			var SUT = new Grid { Name = "test" };

			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });

			var c1 = new View
				{
					Name = "Child01",
					DesiredSizeSelector = s => s.Width > 10
						? new Size(20, 5)
						: new Size(10, 10)
				}
				.GridColumnSpan(2);

			SUT.AddChild(c1);

			SUT.Measure(new Size(30, 30));
			SUT.DesiredSize.Should().Be(new Size(20, 5));
			SUT.UnclippedDesiredSize.Should().Be(new Size(20, 5));
			c1.DesiredSize.Should().Be(new Size(20, 5));
			c1.UnclippedDesiredSize.Should().Be(new Size(0, 0));

			SUT.Arrange(new Rect(0, 0, 30, 30));
			SUT.Arranged.Should().Be((Rect)"0,0,30,30");
			c1.Arranged.Should().Be((Rect)"0,0,20,30");

			SUT.GetChildren().Should().HaveCount(1);
		}

		[TestMethod]
		public void When_Grid_Has_Two_Variable_Sized_Element_With_ColSpan_and_One_Auto_Columns()
		{
			using var _ = new AssertionScope();

			var SUT = new Grid { Name = "test" };

			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "auto" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });

			var c1
				= new View
				{
					Name = "Child01",
					DesiredSizeSelector = s => s.Width > 10
						? new Size(20, 5)
						: new Size(10, 10)
				}
				.GridColumnSpan(2);

			var c2 = new View
				{
					Name = "Child02",
					DesiredSizeSelector = s => new Size(5, 5)
				}
				.GridPosition(0, 1);

			SUT.AddChild(c1);
			SUT.AddChild(c2);

			SUT.Measure(new Size(30, 30));
			SUT.DesiredSize.Should().Be(new Size(20, 5));
			SUT.UnclippedDesiredSize.Should().Be(new Size(20, 5));
			c1.DesiredSize.Should().Be(new Size(20, 5));
			c1.UnclippedDesiredSize.Should().Be(new Size(0, 0));
			c2.DesiredSize.Should().Be(new Size(5, 5));
			c2.UnclippedDesiredSize.Should().Be(new Size(0, 0));

			SUT.Arrange(new Rect(0, 0, 30, 30));
			SUT.Arranged.Should().Be((Rect)"0,0,30,30");
			c1.Arranged.Should().Be((Rect)"0,0,20,30");
			c2.Arranged.Should().Be((Rect)"15,0,5,30");

			SUT.GetChildren().Should().HaveCount(2);
		}

		[TestMethod]
		public void When_Grid_Has_One_Element_With_ColSpan_and_Three_Columns()
		{
			using var _ = new AssertionScope();

			var SUT = new Grid { Name = "test" };

			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });

			SUT.AddChild(
				new View
					{
						Name = "Child01",
						RequestedDesiredSize = new Size(10, 10)
					}
				.GridColumnSpan(2)
			);

			SUT.Measure(new Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			SUT.Arrange(new Rect(0, 0, 20, 20));

			SUT.GetChildren().First().Arranged.Should().Be(new Rect(0, 0, (20.0 / 3.0) * 2.0, 20));

			measuredSize.Should().Be(new Size(10, 10));
			SUT.GetChildren().Should().HaveCount(1);
		}

		[TestMethod]
		public void When_Grid_Has_Three_Element_With_ColSpan_and_Four_Progressing_Columns()
		{
			using var _ = new AssertionScope();

			var SUT = new Grid { Name = "test" };

			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "2*" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "3*" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "4*" });

			SUT.RowDefinitions.Add(new RowDefinition { Height = "10" });
			SUT.RowDefinitions.Add(new RowDefinition { Height = "11" });
			SUT.RowDefinitions.Add(new RowDefinition { Height = "12" });

			var c1 = SUT.AddChild(
				new View
					{
						Name = "Child01",
						RequestedDesiredSize = new Size(10, 10)
					}
				.GridColumnSpan(2)
			);

			var c2 = SUT.AddChild(
				new View
					{
						Name = "Child02",
						RequestedDesiredSize = new Size(10, 10)
					}
				.GridPosition(1, 1)
				.GridColumnSpan(2)
			);

			var c3 = SUT.AddChild(
				new View
					{
						Name = "Child03",
						RequestedDesiredSize = new Size(10, 10)
					}
				.GridPosition(2, 2)
				.GridColumnSpan(2)
			);

			SUT.Measure(new Size(20, 20));
			SUT.DesiredSize.Should().Be(new Size(20, 20));
			SUT.UnclippedDesiredSize.Should().Be(new Size(20, 33));

			SUT.Arrange(new Rect(0, 0, 20, 20));

			c1.Arranged.Should().Be(new Rect(0, 0, 10, 10));
			c2.Arranged.Should().Be(new Rect(4, 10, 11, 11));
			c3.Arranged.Should().Be(new Rect(10, 21, 10, 12));

			SUT.GetChildren().Should().HaveCount(3);
		}

		[TestMethod]
		public void When_Grid_Has_One_Element_With_ColSpan_and_RowSpan_and_Three_Columns()
		{
			using var _ = new AssertionScope();

			var SUT = new Grid { Name = "test" };

			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });

			SUT.RowDefinitions.Add(new RowDefinition { Height = "*" });
			SUT.RowDefinitions.Add(new RowDefinition { Height = "*" });
			SUT.RowDefinitions.Add(new RowDefinition { Height = "*" });

			SUT.AddChild(
				new View
					{
						Name = "Child01",
						RequestedDesiredSize = new Size(10, 10)
					}
				.GridColumnSpan(2)
				.GridRowSpan(2)
			);

			SUT.Measure(new Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			SUT.Arrange(new Rect(0, 0, 20, 20));

			SUT.GetChildren().First().Arranged
				.Should().Be(new Rect(0, 0, (20.0 / 3.0) * 2.0, (20.0 / 3.0) * 2.0));

			measuredSize.Should().Be(new Size(10, 10));
			SUT.GetChildren().Should().HaveCount(1);
		}

		[TestMethod]
		public void When_Grid_Has_One_Element_With_ColSpan_and_RowSpan_and_Three_Columns_And_Middle()
		{
			using var _ = new AssertionScope();

			var SUT = new Grid { Name = "test" };

			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });

			SUT.RowDefinitions.Add(new RowDefinition { Height = "*" });
			SUT.RowDefinitions.Add(new RowDefinition { Height = "*" });
			SUT.RowDefinitions.Add(new RowDefinition { Height = "*" });

			SUT.AddChild(
				new View
					{
						Name = "Child01",
						RequestedDesiredSize = new Size(10, 10)
					}
				.GridColumnSpan(2)
				.GridRowSpan(2)
				.GridPosition(1, 1)
			);

			SUT.Measure(new Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			SUT.Arrange(new Rect(0, 0, 20, 20));

			SUT.GetChildren().First().Arranged
				.Should().Be(
					new Rect(
						20.0 / 3.0,
						20.0 / 3.0,
						(20.0 / 3.0) * 2.0,
						(20.0 / 3.0) * 2.0
					)
				);

			measuredSize.Should().Be(new Size(10, 10));
			SUT.GetChildren().Should().HaveCount(1);
		}

		[TestMethod]
		public void When_Grid_Has_One_Element_With_ColSpan_Overflow_and_Three_Columns()
		{
			using var _ = new AssertionScope();

			var SUT = new Grid { Name = "test" };

			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });

			SUT.AddChild(
				new View
					{
						Name = "Child01",
						RequestedDesiredSize = new Size(10, 10)
					}
				.GridColumnSpan(4)
			);

			SUT.Measure(new Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			SUT.Arrange(new Rect(0, 0, 20, 20));

			SUT.GetChildren().First().Arranged.Should().Be(new Rect(0, 0, 20, 20));

			measuredSize.Should().Be(new Size(10, 10));
			SUT.GetChildren().Should().HaveCount(1);
		}

		[TestMethod]
		public void When_Grid_RowCollection_Changes()
		{
			var SUT = new Grid();

			SUT.ForceLoaded();

			var child = SUT.AddChild(
				new View
					{
						Name = "Child01",
						RequestedDesiredSize = new Size(10, 10)
					}
				.GridRow(1)
			);

			var measureAvailableSize = new Size(20, 20);
			var arrangeFinalRect = new Rect(default, measureAvailableSize);

			SUT.Measure(measureAvailableSize);
			SUT.Arrange(arrangeFinalRect);

			child.Arranged.Should().Be(arrangeFinalRect);

			var row1 = new RowDefinition { Height = new GridLength(5) };
			SUT.RowDefinitions.Add(row1);
			SUT.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

			SUT.Measure(measureAvailableSize);
			SUT.Arrange(arrangeFinalRect);

			child.Arranged.Should().Be(new Rect(0, 5, 20, 10));

			SUT.InvalidateMeasureCallCount.Should().Be(1);
			row1.Height = new GridLength(10);
			SUT.InvalidateMeasureCallCount.Should().Be(2);
		}

		[TestMethod]
		public void When_Grid_ColumnCollection_Changes()
		{
			using var _ = new AssertionScope();

			var SUT = new Grid();

			SUT.ForceLoaded();

			var child = SUT.AddChild(
				new View
					{
						Name = "Child01",
						RequestedDesiredSize = new Size(10, 10)
					}
				.GridColumn(1)
			);

			SUT.Measure(new Size(20, 20));
			SUT.Arrange(new Rect(0, 0, 20, 20));

			child.Arranged.Should().Be(new Rect(0, 0, 20, 20));

			var col1 = new ColumnDefinition { Width = new GridLength(5) };
			SUT.ColumnDefinitions.Add(col1);
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });

			SUT.Measure(new Size(20, 20));
			SUT.Arrange(new Rect(0, 0, 20, 20));

			child.Arranged.Should().Be(new Rect(5, 0, 10, 20));

			SUT.InvalidateMeasureCallCount.Should().Be(1);
			col1.Width = new GridLength(10);
			SUT.InvalidateMeasureCallCount.Should().Be(2);
		}

		[TestMethod]
		public void When_Grid_Column_Min_MaxWidth_Changes()
		{
			using var _ = new AssertionScope();

			var SUT = new Grid();

			SUT.ForceLoaded();

			SUT.Measure(new Size(20, 20));
			SUT.Arrange(new Rect(0, 0, 20, 20));

			ColumnDefinition ColumnDefinition1;

			SUT.ColumnDefinitions.Add(ColumnDefinition1 = new ColumnDefinition { Width = new GridLength(5) });
			SUT.InvalidateMeasureCallCount.Should().Be(1);

			SUT.Measure(new Size(20, 20)); // need to remeasure for the invalidation to be called again

			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
			SUT.InvalidateMeasureCallCount.Should().Be(2);

			ColumnDefinition1.MaxWidth = 22;
			SUT.InvalidateMeasureCallCount.Should().Be(2); // Already invalidated, no new invalidations should be done

			SUT.Measure(new Size(20, 20)); // need to remeasure for the invalidation to be called again

			ColumnDefinition1.MaxWidth = 23;
			SUT.InvalidateMeasureCallCount.Should().Be(3);

			ColumnDefinition1.MinWidth = 5;
			SUT.InvalidateMeasureCallCount.Should().Be(3); // Already invalidated, no new invalidations should be done

			SUT.Measure(new Size(20, 20)); // need to remeasure for the invalidation to be called again

			ColumnDefinition1.MinWidth = 6;
			SUT.InvalidateMeasureCallCount.Should().Be(4);
		}

		[TestMethod]
		public void When_Grid_Has_Two_Columns_And_VerticalAlignment_Top()
		{
			using var _ = new AssertionScope();

			var SUT = new Grid
			{
				Name = "test",
				VerticalAlignment = VerticalAlignment.Top
			};


			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });

			var child = new View
			{
				Name = "Child01",
				RequestedDesiredSize = new Size(10, 10)
			};
			SUT.AddChild(child);

			SUT.Measure(new Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			SUT.Arrange(new Rect(0, 0, 20, 20));
			var childArrangedSize = child.Arranged;

			measuredSize.Should().Be(new Size(10, 10));
			childArrangedSize.Should().Be(new Rect(0, 0, 10, 20));
			SUT.GetChildren().Should().HaveCount(1);
		}

		[TestMethod]
		public void When_Grid_Has_Two_StarColums_One_Variable_Column_And_Two_StarRows_One_Variable_Row_And_MinWidth_MinHeight_VerticalAlignment_Top_HorizontalAlignment_Center_And_Child_Stretched_And_Centered()
		{
			using var _ = new AssertionScope();

			var SUT = new Grid
			{
				Name = "test",
				MinWidth = 80,
				MinHeight = 80,
				VerticalAlignment = VerticalAlignment.Top,
				HorizontalAlignment = HorizontalAlignment.Center
			};

			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "Auto" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });

			SUT.RowDefinitions.Add(new RowDefinition { Height = "*" });
			SUT.RowDefinitions.Add(new RowDefinition { Height = "Auto" });
			SUT.RowDefinitions.Add(new RowDefinition { Height = "*" });

			var c1 = SUT.AddChild(
				new View { Name = "Child01", RequestedDesiredSize = new Size(10, 10), HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch }
				.GridPosition(0, 0)
			);

			var c2 = SUT.AddChild(
				new View { Name = "Child02", RequestedDesiredSize = new Size(10, 10), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }
				.GridPosition(1, 1)
			);

			var c3 = SUT.AddChild(
				new View { Name = "Child03", RequestedDesiredSize = new Size(10, 10), HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch }
				.GridPosition(2, 1)
			);

			var c4 = SUT.AddChild(
				new View { Name = "Child04", RequestedDesiredSize = new Size(10, 10), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }
				.GridPosition(2, 2)
			);

			SUT.Measure(new Size(100, 100));
			var measuredSize = SUT.DesiredSize;
			SUT.Arrange(new Rect(0, 0, 100, 100));

			c1.Arranged.Should().Be(new Rect(0, 0, 45, 45));
			c2.Arranged.Should().Be(new Rect(45, 45, 10, 10));
			c3.Arranged.Should().Be(new Rect(45, 55, 10, 45));
			c4.Arranged.Should().Be(new Rect(72.5, 72.5, 10, 10));

			measuredSize.Should().Be(new Size(80, 80));
			SUT.GetChildren().Should().HaveCount(4);
		}

		[TestMethod]
		public void When_Grid_Has_Two_StarColums_One_Variable_Column_And_Two_StarRows_One_Variable_Row_And_MinWidth_MinHeight_VerticalAlignment_Top_HorizontalAlignment_Center_And_Child_Stretched_And_Centered_With_RowSpan_And_ColumnSpan()
		{
			using var _ = new AssertionScope();

			var SUT = new Grid
			{
				Name = "test",
				MinWidth = 80,
				MinHeight = 80,
				VerticalAlignment = VerticalAlignment.Top,
				HorizontalAlignment = HorizontalAlignment.Center
			};

			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "Auto" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });

			SUT.RowDefinitions.Add(new RowDefinition { Height = "*" });
			SUT.RowDefinitions.Add(new RowDefinition { Height = "Auto" });
			SUT.RowDefinitions.Add(new RowDefinition { Height = "*" });

			var c1 = SUT.AddChild(
				new View { Name = "Child01", RequestedDesiredSize = new Size(10, 10), HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch }
				.GridPosition(0, 0)
				.GridColumnSpan(2)
			);

			var c2 = SUT.AddChild(
				new View { Name = "Child02", RequestedDesiredSize = new Size(10, 10), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }
				.GridPosition(0, 2)
			);

			var c3 = SUT.AddChild(
				new View { Name = "Child03", RequestedDesiredSize = new Size(10, 10), HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch }
				.GridPosition(1, 0)
				.GridRowSpan(2)
			);

			var c4 = SUT.AddChild(
				new View { Name = "Child04", RequestedDesiredSize = new Size(10, 10), HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch }
				.GridPosition(1, 1)
				.GridColumnSpan(2)
			);

			var c5 = SUT.AddChild(
				new View { Name = "Child05", RequestedDesiredSize = new Size(10, 10), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }
				.GridPosition(2, 1)
				.GridColumnSpan(2)
			);

			var c6 = SUT.AddChild(
				new View { Name = "Child06", RequestedDesiredSize = new Size(10, 10), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }
				.GridPosition(1, 1)
			);

			SUT.Measure(new Size(100, 100));
			var measuredSize = SUT.DesiredSize;
			SUT.Arrange(new Rect(0, 0, 100, 100));

			c1.Arranged.Should().Be(new Rect(0, 0, 55, 45));
			c2.Arranged.Should().Be(new Rect(72.5, 17.5, 10, 10));
			c3.Arranged.Should().Be(new Rect(0, 45, 45, 55));
			c4.Arranged.Should().Be(new Rect(45, 45, 55, 10));
			c5.Arranged.Should().Be(new Rect(67.5, 72.5, 10, 10));
			c6.Arranged.Should().Be(new Rect(45, 45, 10, 10));

			measuredSize.Should().Be(new Size(80, 80));
			SUT.GetChildren().Should().HaveCount(6);
		}

		[TestMethod]
		public void When_Row_Out_Of_Range()
		{
			using var _ = new AssertionScope();

			var SUT = new Grid();

			SUT.RowDefinitions.Add(new RowDefinition { Height = "5" });
			SUT.RowDefinitions.Add(new RowDefinition { Height = "5" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "5" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "5" });

			SUT.AddChild(new View { RequestedDesiredSize = new Size(100, 100) });
			SUT.AddChild(new View { RequestedDesiredSize = new Size(100, 100) })
				.GridRow(3);

			SUT.Measure(new Size(100, 1000));
			SUT.DesiredSize.Should().Be(new Size(100, 200));

			SUT.Arrange(new Rect(0, 0, 100, 1000));
			SUT.Arranged.Should().Be((Rect)"0,0,100,1000");
		}

		[TestMethod]
		public void When_Column_Out_Of_Range()
		{
			using var _ = new AssertionScope();

			var SUT = new Grid();

			SUT.RowDefinitions.Add(new RowDefinition { Height = "5" });
			SUT.RowDefinitions.Add(new RowDefinition { Height = "5" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "5" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "5" });

			SUT.AddChild(new View { RequestedDesiredSize = new Size(100, 100) });
			SUT.AddChild(new View { RequestedDesiredSize = new Size(100, 100) })
				.GridColumn(3);

			SUT.Measure(new Size(10, 10));
			SUT.DesiredSize.Should().Be(new Size(10, 10));
			SUT.UnclippedDesiredSize.Should().Be(new Size(200, 105));

			SUT.Arrange((Rect)"0,0,10,10");
		}

		[TestMethod]
		public void When_RowSpan_Out_Of_Range()
		{
			using var _ = new AssertionScope();

			var SUT = new Grid();

			SUT.RowDefinitions.Add(new RowDefinition { Height = "*" });
			SUT.RowDefinitions.Add(new RowDefinition { Height = "*" });

			SUT.AddChild(new View { RequestedDesiredSize = new Size(100, 100) });
			SUT.AddChild(new View { RequestedDesiredSize = new Size(100, 100) })
				.GridPosition(1, 0)
				.GridRowSpan(3);

			SUT.Measure(new Size(100, 1000));
			SUT.DesiredSize.Should().Be(new Size(100, 200));
			SUT.UnclippedDesiredSize.Should().Be(new Size(100, 200));

			SUT.Arrange((Rect)"0,0,100,1000");
		}

		[TestMethod]
		public void When_ColumnSpan_Out_Of_Range()
		{
			using var _ = new AssertionScope();

			var SUT = new Grid();

			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });

			SUT.AddChild(new View { RequestedDesiredSize = new Size(100, 100) });
			SUT.AddChild(new View { RequestedDesiredSize = new Size(100, 100) })
				.GridPosition(0, 1)
				.GridColumnSpan(3);

			SUT.Measure(new Size(1000, 100));
			SUT.DesiredSize.Should().Be(new Size(200, 100));
			SUT.UnclippedDesiredSize.Should().Be(new Size(200, 100));

			SUT.Arrange((Rect)"0,0,1000,100");
		}

		[TestMethod]
		public void When_Clear_ColumnDefinitions()
		{
			var SUT = new Grid();

			SUT.ColumnDefinitions.Clear();
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			SUT.ColumnDefinitions.Clear();

			SUT.ColumnDefinitions.Should().HaveCount(0);
		}

		[TestMethod]
		public void When_Clear_RowDefinitions()
		{
			var SUT = new Grid();
			SUT.ForceLoaded();

			SUT.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			SUT.RowDefinitions.Clear();

			SUT.RowDefinitions.Should().HaveCount(0);
		}

		[TestMethod]
		public void When_Zero_Star_Size()
		{
			var SUT = new Grid();
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0, GridUnitType.Star) });
			SUT.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			SUT.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

			SetChild(0, 0);
			SetChild(0, 1);
			SetChild(1, 0);
			SetChild(1, 1);

			SUT.Measure(new Size(800, 800));
			SUT.DesiredSize.Should().Be(new Size(50, 100));

			void SetChild(int col, int row)
			{
				var border = new Border { Height = 50, Width = 50 };
				Grid.SetColumn(border, col);
				Grid.SetRow(border, row);
				SUT.Children.Add(border);
			}
		}

		[TestMethod]
		public void When_RowSpan_Reuse()
		{
			// This sample is taken from the ToggleSwitch template

			var SUT = new StackPanel();

			var topLevel = new Grid();
			SUT.Children.Add(topLevel);

			topLevel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			topLevel.RowDefinitions.Add(new RowDefinition { Height = GridLengthHelper.FromPixels(10) });
			topLevel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			topLevel.RowDefinitions.Add(new RowDefinition { Height = GridLengthHelper.FromPixels(10) });

			topLevel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			topLevel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLengthHelper.FromPixels(12), MaxWidth = 12 });
			topLevel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLengthHelper.FromPixels(12) });
			topLevel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			topLevel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLengthHelper.OneStar });

			var spacer = new Grid() { Margin = new Thickness(0, 5) };
			topLevel.Children.Add(spacer);
			Grid.SetRow(spacer, 1);
			Grid.SetRowSpan(spacer, 3);
			Grid.SetColumnSpan(spacer, 3);

			var knob = new Grid()
			{
				HorizontalAlignment = HorizontalAlignment.Left,
				Width = 20,
				Height = 20
			};

			Grid.SetRow(spacer, 2);
			topLevel.Children.Add(knob);

			SUT.Measure(new Size(800, 800));
			Assert.AreEqual(new Size(20, 20), knob.DesiredSize);
		}
	}
}
