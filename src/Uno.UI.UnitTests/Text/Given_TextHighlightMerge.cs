// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TextHighlightMergeUnitTests.cpp, tag winui3/release/1.8.2, commit 4a1c6184c

using System.Collections.Generic;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Tests.Text
{
	[TestClass]
	public class Given_TextHighlightMerge
	{
		// Defines for easier readability of the code
		// Even #'s are foreground
		// Odd #'s are background
		private const int FIRST_FG = 0x1;
		private const int FIRST_BG = 0x2;

		private const int SECOND_FG = 0x3;
		private const int SECOND_BG = 0x4;

		private const int THIRD_FG = 0x5;
		private const int THIRD_BG = 0x6;

		private const int FOURTH_FG = 0x7;
		private const int FOURTH_BG = 0x8;

		private const int FIFTH_FG = 0x9;
		private const int FIFTH_BG = 0xA;

		// NOTE: All SolidColorBrushes are objects but this unit test exploits to use
		//       the references as identity values for unit testing purposes.
		private readonly Dictionary<int, SolidColorBrush> _brushes = new();

		private SolidColorBrush Brush(int token)
			=> _brushes.TryGetValue(token, out var brush) ? brush : _brushes[token] = new SolidColorBrush();

		private struct TextRegion
		{
			public int StartIndex;
			public int EndIndex;
			public int ForegroundBrushValue;
			public int BackgroundBrushValue;

			public TextRegion(int startIndex, int endIndex, int foregroundBrushValue, int backgroundBrushValue)
			{
				StartIndex = startIndex;
				EndIndex = endIndex;
				ForegroundBrushValue = foregroundBrushValue;
				BackgroundBrushValue = backgroundBrushValue;
			}
		}

		private void RegionValidator(
			IReadOnlyList<TextRegion> inputRegions,
			IReadOnlyList<TextRegion> outputRegions)
		{
			var merge = new TextHighlightMerge();

			foreach (var region in inputRegions)
			{
				merge.AddRegion(
					new HighlightRegion(
						region.StartIndex,
						region.EndIndex,
						Brush(region.ForegroundBrushValue),
						Brush(region.BackgroundBrushValue)));
			}

			var result = merge.Regions;
			Assert.AreEqual(outputRegions.Count, result.Count);

			for (var i = 0; i < outputRegions.Count; i++)
			{
				Assert.AreEqual(outputRegions[i].StartIndex, result[i].StartIndex);
				Assert.AreEqual(outputRegions[i].EndIndex, result[i].EndIndex);
				Assert.AreSame(Brush(outputRegions[i].ForegroundBrushValue), result[i].ForegroundBrush);
				Assert.AreSame(Brush(outputRegions[i].BackgroundBrushValue), result[i].BackgroundBrush);
			}
		}

		[TestMethod]
		public void When_NonOverlapped()
		{
			var inputRegions = new List<TextRegion>
			{
				new(0, 1, FIRST_FG, FIRST_BG),
				new(5, 6, SECOND_FG, SECOND_BG),
			};

			RegionValidator(inputRegions, inputRegions);
		}

		[TestMethod]
		public void When_OverlapNextPartial()
		{
			var inputRegions = new List<TextRegion>
			{
				new(5, 10, FIRST_FG, FIRST_BG),
				new(3, 8, SECOND_FG, SECOND_BG),
			};

			var outputRegions = new List<TextRegion>
			{
				new(3, 8, SECOND_FG, SECOND_BG),
				new(9, 10, FIRST_FG, FIRST_BG),
			};

			RegionValidator(inputRegions, outputRegions);
		}

		[TestMethod]
		public void When_OverlapPrevPartial()
		{
			var inputRegions = new List<TextRegion>
			{
				new(5, 10, FIRST_FG, FIRST_BG),
				new(8, 12, SECOND_FG, SECOND_BG),
			};

			var outputRegions = new List<TextRegion>
			{
				new(5, 7, FIRST_FG, FIRST_BG),
				new(8, 12, SECOND_FG, SECOND_BG),
			};

			RegionValidator(inputRegions, outputRegions);
		}

		[TestMethod]
		public void When_OverlapNextFull()
		{
			var inputRegions = new List<TextRegion>
			{
				new(5, 10, FIRST_FG, FIRST_BG),
				new(3, 12, SECOND_FG, SECOND_BG),
			};

			var outputRegions = new List<TextRegion>
			{
				new(3, 12, SECOND_FG, SECOND_BG),
			};

			RegionValidator(inputRegions, outputRegions);
		}

		[TestMethod]
		public void When_OverlapEqualFull()
		{
			var inputRegions = new List<TextRegion>
			{
				new(5, 10, FIRST_FG, FIRST_BG),
				new(5, 12, SECOND_FG, SECOND_BG),
			};

			var outputRegions = new List<TextRegion>
			{
				new(5, 12, SECOND_FG, SECOND_BG),
			};

			RegionValidator(inputRegions, outputRegions);
		}

		[TestMethod]
		public void When_OverlapEqualPartial()
		{
			var inputRegions = new List<TextRegion>
			{
				new(5, 10, FIRST_FG, FIRST_BG),
				new(5, 8, SECOND_FG, SECOND_BG),
			};

			var outputRegions = new List<TextRegion>
			{
				new(5, 8, SECOND_FG, SECOND_BG),
				new(9, 10, FIRST_FG, FIRST_BG),
			};

			RegionValidator(inputRegions, outputRegions);
		}

		[TestMethod]
		public void When_OverlapEqualMultiple()
		{
			var inputRegions = new List<TextRegion>
			{
				new(5, 10, FIRST_FG, FIRST_BG),
				new(11, 20, SECOND_FG, SECOND_BG),
				new(21, 30, THIRD_FG, THIRD_BG),
				new(5, 25, FOURTH_FG, FOURTH_BG),
			};

			var outputRegions = new List<TextRegion>
			{
				new(5, 25, FOURTH_FG, FOURTH_BG),
				new(26, 30, THIRD_FG, THIRD_BG),
			};

			RegionValidator(inputRegions, outputRegions);
		}

		[TestMethod]
		public void When_OverlapInner()
		{
			var inputRegions = new List<TextRegion>
			{
				new(0, 10, FIRST_FG, FIRST_BG),
				new(4, 8, SECOND_FG, SECOND_BG),
			};

			var outputRegions = new List<TextRegion>
			{
				new(0, 3, FIRST_FG, FIRST_BG),
				new(4, 8, SECOND_FG, SECOND_BG),
				new(9, 10, FIRST_FG, FIRST_BG),
			};

			RegionValidator(inputRegions, outputRegions);
		}

		[TestMethod]
		public void When_Overlap1()
		{
			var inputRegions = new List<TextRegion>
			{
				new(0, 2, FIRST_FG, FIRST_BG),
				new(4, 6, SECOND_FG, SECOND_BG),
				new(2, 4, THIRD_FG, THIRD_BG),
			};

			var outputRegions = new List<TextRegion>
			{
				new(0, 1, FIRST_FG, FIRST_BG),
				new(2, 4, THIRD_FG, THIRD_BG),
				new(5, 6, SECOND_FG, SECOND_BG),
			};

			RegionValidator(inputRegions, outputRegions);
		}

		[TestMethod]
		public void When_OverlapMultiple()
		{
			var inputRegions = new List<TextRegion>
			{
				new(0, 5, FIRST_FG, FIRST_BG),
				new(8, 12, SECOND_FG, SECOND_BG),
				new(14, 20, THIRD_FG, THIRD_BG),
				new(20, 30, FOURTH_FG, FOURTH_BG),
				new(2, 25, FIFTH_FG, FIFTH_BG),
			};

			var outputRegions = new List<TextRegion>
			{
				new(0, 1, FIRST_FG, FIRST_BG),
				new(2, 25, FIFTH_FG, FIFTH_BG),
				new(26, 30, FOURTH_FG, FOURTH_BG),
			};

			RegionValidator(inputRegions, outputRegions);
		}
	}
}
