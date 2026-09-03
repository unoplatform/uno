#nullable enable

using System;
using System.Collections.Generic;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml.Performance.RenderStress
{
	/// <summary>
	/// Bid-comparison archetype: a wide, deliberately NON-VIRTUALIZED matrix — one column per bidder, one row
	/// per line item, every cell a bordered amount with a variance annotation. Modelled on the real structure
	/// (nested ItemsControls inside a ScrollViewer, no virtualization at any level), which is where a comparison
	/// grid gets expensive: cell count is bidders × line items and every cell carries several visuals.
	/// </summary>
	[Sample("Performance", Name = "RealWorld_BidLeveling", Description = "Real-UI perf: a non-virtualized bid-comparison matrix (bidders × line items) with per-frame recalc. Stresses cell count + text at scale. Compare fps/frame-time across Skia and the WebGPU/ProGPU builds.")]
	public sealed class RealWorld_BidLeveling : PerfBenchBase
	{
		private const int Bidders = 6;

		private static readonly string[] _sections = { "Sitework", "Concrete", "Masonry", "Metals", "Openings", "Finishes" };
		private static readonly string[] _vendors = { "Apex Constr.", "Beacon Bldrs", "Cardinal Co", "Delta Works", "Everest LLC", "Foundry Grp" };

		private readonly List<TextBlock> _amountCells = new();
		private readonly List<TextBlock> _varianceCells = new();
		private readonly List<int> _amounts = new();
		private ScrollViewer _sv = null!;
		private double _offset;
		private int _dir = 1;

		protected override string ScenarioName => "BidLeveling";

		protected override int DefaultCount => 220;

		protected override UIElement BuildStage(int count)
		{
			var rng = new Rng(71);
			_amountCells.Clear();
			_varianceCells.Clear();
			_amounts.Clear();

			// A StackPanel of rows inside a ScrollViewer: no row virtualization, matching the real leveling view.
			var rows = new StackPanel { Padding = new Thickness(8) };

			rows.Children.Add(BuildHeaderRow());

			for (var i = 0; i < count; i++)
			{
				var row = new StackPanel { Orientation = Orientation.Horizontal };

				// Line-item description column.
				row.Children.Add(new Border
				{
					Width = 260,
					Height = 28,
					BorderThickness = new Thickness(0, 0, 1, 1),
					BorderBrush = new SolidColorBrush(Color.FromArgb(0x30, 0xFF, 0xFF, 0xFF)),
					Padding = new Thickness(6, 4, 6, 4),
					Child = new TextBlock
					{
						Text = $"{_sections[i % _sections.Length]} {1000 + i} — {(i % 7 == 0 ? "Allowance" : "Unit price")}",
						FontSize = 12,
						Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0xD0, 0xD6, 0xE6)),
						TextTrimming = TextTrimming.CharacterEllipsis,
					},
				});

				var baseAmount = rng.Int(5_000, 90_000);
				for (var b = 0; b < Bidders; b++)
				{
					var amount = baseAmount + rng.Int(-6_000, 6_000);
					_amounts.Add(amount);

					var amountText = new TextBlock
					{
						Text = amount.ToString("N0"),
						FontSize = 12,
						Foreground = new SolidColorBrush(Windows.UI.Colors.White),
						HorizontalAlignment = HorizontalAlignment.Right,
					};
					var varianceText = new TextBlock
					{
						Text = "+0.0%",
						FontSize = 10,
						Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0x88, 0x92, 0xB0)),
						HorizontalAlignment = HorizontalAlignment.Right,
					};
					_amountCells.Add(amountText);
					_varianceCells.Add(varianceText);

					var cellContent = new StackPanel { Spacing = 0 };
					cellContent.Children.Add(amountText);
					cellContent.Children.Add(varianceText);

					// Low/high highlight, the per-cell conditional colouring the real view drives with behaviors.
					var highlight = amount < baseAmount - 3_000
						? Color.FromArgb(0x28, 0x30, 0xE0, 0x80)
						: amount > baseAmount + 3_000 ? Color.FromArgb(0x28, 0xE0, 0x50, 0x50) : Color.FromArgb(0x10, 0xFF, 0xFF, 0xFF);

					row.Children.Add(new Border
					{
						Width = 132,
						Height = 40,
						Background = new SolidColorBrush(highlight),
						BorderThickness = new Thickness(0, 0, 1, 1),
						BorderBrush = new SolidColorBrush(Color.FromArgb(0x30, 0xFF, 0xFF, 0xFF)),
						Padding = new Thickness(6, 2, 6, 2),
						Child = cellContent,
					});
				}

				rows.Children.Add(row);
			}

			_sv = new ScrollViewer
			{
				Content = rows,
				VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
				HorizontalScrollMode = ScrollMode.Disabled,
			};
			_offset = 0;
			_dir = 1;
			return _sv;
		}

		private static UIElement BuildHeaderRow()
		{
			var header = new StackPanel { Orientation = Orientation.Horizontal };
			header.Children.Add(new Border { Width = 260, Height = 32 });
			for (var b = 0; b < Bidders; b++)
			{
				header.Children.Add(new Border
				{
					Width = 132,
					Height = 32,
					Background = new SolidColorBrush(Color.FromArgb(0x30, 0x2A, 0x6F, 0xF0)),
					Padding = new Thickness(6, 6, 6, 6),
					Child = new TextBlock
					{
						Text = _vendors[b],
						FontSize = 12,
						FontWeight = Microsoft.UI.Text.FontWeights.Bold,
						Foreground = new SolidColorBrush(Windows.UI.Colors.White),
					},
				});
			}

			return header;
		}

		protected override void Tick(long frame)
		{
			AdvanceScroll(_sv, ref _offset, ref _dir, 5.0);

			// Recalc a slice of the matrix every frame, the way a leveling recalculation repaints amounts and
			// their variance annotations together (two text changes per touched cell).
			var start = (int)(frame % 6);
			for (var i = start; i < _amountCells.Count; i += 6)
			{
				_amounts[i] = (_amounts[i] + 137) % 120_000;
				_amountCells[i].Text = _amounts[i].ToString("N0");
				_varianceCells[i].Text = ((_amounts[i] % 400) / 10.0 - 20.0).ToString("+0.0;-0.0") + "%";
			}
		}
	}
}
