using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Xaml.Media;
using Windows.UI.Text;

namespace Uno.UI.Tests.Windows_UI_Xaml_Media;

[TestClass]
public class Given_FontManifest
{
	[TestMethod]
	public void When_Stretch_Matches_Exactly()
	{
		FontInfo[] infos =
		[
			new FontInfo()
			{
				FamilyName = "ms-appx:///test1.ttf",
				FontStretch = FontStretch.ExtraExpanded,
				FontStyle = FontStyle.Italic,
				FontWeight = FontWeights.Bold.Weight,
			},
			new FontInfo()
			{
				FamilyName = "ms-appx:///test2.ttf",
				FontStretch = FontStretch.Normal,
				FontStyle = FontStyle.Normal,
				FontWeight = FontWeights.Normal.Weight,
			},
			new FontInfo()
			{
				FamilyName = "ms-appx:///test3.ttf",
				FontStretch = FontStretch.SemiExpanded,
				FontStyle = FontStyle.Normal,
				FontWeight = FontWeights.Normal.Weight,
			},
			new FontInfo()
			{
				FamilyName = "ms-appx:///test4.ttf",
				FontStretch = FontStretch.SemiCondensed,
				FontStyle = FontStyle.Normal,
				FontWeight = FontWeights.Normal.Weight,
			},
		];

		PermuteAndAssert(infos, manifest =>
		{
			var actual = FontManifestHelpers.GetFamilyNameFromManifest(manifest, FontWeights.Normal, FontStyle.Normal, FontStretch.ExtraExpanded);
			Assert.AreEqual("ms-appx:///test1.ttf", actual);
		});
	}

	[TestMethod]
	public void When_Stretch_Is_Expanded_Prefer_Expanded()
	{
		FontInfo[] infos =
		[
			new FontInfo()
			{
				FamilyName = "ms-appx:///test1.ttf",
				FontStretch = FontStretch.ExtraExpanded,
				FontStyle = FontStyle.Italic,
				FontWeight = FontWeights.Bold.Weight,
			},
			new FontInfo()
			{
				FamilyName = "ms-appx:///test2.ttf",
				FontStretch = FontStretch.UltraExpanded,
				FontStyle = FontStyle.Normal,
				FontWeight = FontWeights.Normal.Weight,
			},
			new FontInfo()
			{
				FamilyName = "ms-appx:///test2.ttf",
				FontStretch = FontStretch.Normal,
				FontStyle = FontStyle.Normal,
				FontWeight = FontWeights.Normal.Weight,
			},
			new FontInfo()
			{
				FamilyName = "ms-appx:///test3.ttf",
				FontStretch = FontStretch.SemiCondensed,
				FontStyle = FontStyle.Normal,
				FontWeight = FontWeights.Normal.Weight,
			},
		];

		PermuteAndAssert(infos, manifest =>
		{
			var actual = FontManifestHelpers.GetFamilyNameFromManifest(manifest, FontWeights.Normal, FontStyle.Normal, FontStretch.SemiExpanded);
			Assert.AreEqual("ms-appx:///test1.ttf", actual);
		});
	}

	[TestMethod]
	public void When_Style_Matches_Exactly()
	{
		FontInfo[] infos =
		[
			new FontInfo()
			{
				FamilyName = "ms-appx:///test1.ttf",
				FontStretch = FontStretch.SemiExpanded,
				FontStyle = FontStyle.Italic,
				FontWeight = FontWeights.Bold.Weight,
			},
			new FontInfo()
			{
				FamilyName = "ms-appx:///test2.ttf",
				FontStretch = FontStretch.SemiExpanded,
				FontStyle = FontStyle.Normal,
				FontWeight = FontWeights.Normal.Weight,
			},
			new FontInfo()
			{
				FamilyName = "ms-appx:///test3.ttf",
				FontStretch = FontStretch.SemiExpanded,
				FontStyle = FontStyle.Oblique,
				FontWeight = FontWeights.Bold.Weight,
			},
		];

		PermuteAndAssert(infos, manifest =>
		{
			var actual = FontManifestHelpers.GetFamilyNameFromManifest(manifest, FontWeights.Normal, FontStyle.Italic, FontStretch.ExtraExpanded);
			Assert.AreEqual("ms-appx:///test1.ttf", actual);

			actual = FontManifestHelpers.GetFamilyNameFromManifest(manifest, FontWeights.Normal, FontStyle.Oblique, FontStretch.ExtraExpanded);
			Assert.AreEqual("ms-appx:///test3.ttf", actual);

			actual = FontManifestHelpers.GetFamilyNameFromManifest(manifest, FontWeights.Normal, FontStyle.Normal, FontStretch.ExtraExpanded);
			Assert.AreEqual("ms-appx:///test2.ttf", actual);
		});
	}

	[TestMethod]
	public void When_Style_Not_Matched_Requested_Oblique()
	{
		FontInfo[] infos =
		[
			new FontInfo()
			{
				FamilyName = "ms-appx:///test1.ttf",
				FontStretch = FontStretch.Normal,
				FontStyle = FontStyle.Italic,
				FontWeight = FontWeights.Normal.Weight,
			},
			new FontInfo()
			{
				FamilyName = "ms-appx:///test2.ttf",
				FontStretch = FontStretch.Normal,
				FontStyle = FontStyle.Normal,
				FontWeight = FontWeights.Normal.Weight,
			},
		];

		PermuteAndAssert(infos, manifest =>
		{
			var actual = FontManifestHelpers.GetFamilyNameFromManifest(manifest, FontWeights.Normal, FontStyle.Oblique, FontStretch.SemiExpanded);
			Assert.AreEqual("ms-appx:///test1.ttf", actual);
		});
	}

	[TestMethod]
	public void When_Style_Not_Matched_Requested_Italic()
	{
		FontInfo[] infos =
		[
			new FontInfo()
			{
				FamilyName = "ms-appx:///test1.ttf",
				FontStretch = FontStretch.Normal,
				FontStyle = FontStyle.Oblique,
				FontWeight = FontWeights.Normal.Weight,
			},
			new FontInfo()
			{
				FamilyName = "ms-appx:///test2.ttf",
				FontStretch = FontStretch.Normal,
				FontStyle = FontStyle.Normal,
				FontWeight = FontWeights.Normal.Weight,
			},
		];

		PermuteAndAssert(infos, manifest =>
		{
			var actual = FontManifestHelpers.GetFamilyNameFromManifest(manifest, FontWeights.Normal, FontStyle.Italic, FontStretch.SemiExpanded);
			Assert.AreEqual("ms-appx:///test1.ttf", actual);
		});
	}

	[TestMethod]
	public void When_Style_Not_Matched_Requested_Normal()
	{
		FontInfo[] infos =
		[
			new FontInfo()
			{
				FamilyName = "ms-appx:///test1.ttf",
				FontStretch = FontStretch.Normal,
				FontStyle = FontStyle.Oblique,
				FontWeight = FontWeights.Normal.Weight,
			},
			new FontInfo()
			{
				FamilyName = "ms-appx:///test2.ttf",
				FontStretch = FontStretch.Normal,
				FontStyle = FontStyle.Italic,
				FontWeight = FontWeights.Normal.Weight,
			},
		];

		PermuteAndAssert(infos, manifest =>
		{
			var actual = FontManifestHelpers.GetFamilyNameFromManifest(manifest, FontWeights.Normal, FontStyle.Normal, FontStretch.SemiExpanded);
			Assert.AreEqual("ms-appx:///test1.ttf", actual);
		});
	}

	[TestMethod]
	public void When_Weight_Requested_Less_Than_400_Has_Match()
	{
		FontInfo[] infos =
		[
			new FontInfo()
			{
				FamilyName = "ms-appx:///test1.ttf",
				FontStretch = FontStretch.Normal,
				FontStyle = FontStyle.Normal,
				FontWeight = 350,
			},
			new FontInfo()
			{
				FamilyName = "ms-appx:///test2.ttf",
				FontStretch = FontStretch.Normal,
				FontStyle = FontStyle.Normal,
				FontWeight = 300,
			},
		];

		PermuteAndAssert(infos, manifest =>
		{
			var actual = FontManifestHelpers.GetFamilyNameFromManifest(manifest, new FontWeight(350), FontStyle.Normal, FontStretch.Normal);
			Assert.AreEqual("ms-appx:///test1.ttf", actual);
		});
	}

	[TestMethod]
	public void When_Weight_Requested_Less_Than_400_No_Match_Prefer_Lighter()
	{
		FontInfo[] infos =
		[
			new FontInfo()
			{
				FamilyName = "ms-appx:///test1.ttf",
				FontStretch = FontStretch.Normal,
				FontStyle = FontStyle.Normal,
				FontWeight = 300,
			},
			new FontInfo()
			{
				FamilyName = "ms-appx:///test2.ttf",
				FontStretch = FontStretch.Normal,
				FontStyle = FontStyle.Normal,
				FontWeight = 400,
			},
			new FontInfo()
			{
				FamilyName = "ms-appx:///test3.ttf",
				FontStretch = FontStretch.Normal,
				FontStyle = FontStyle.Normal,
				FontWeight = 500,
			},
		];

		PermuteAndAssert(infos, manifest =>
		{
			var actual = FontManifestHelpers.GetFamilyNameFromManifest(manifest, new FontWeight(350), FontStyle.Normal, FontStretch.Normal);
			Assert.AreEqual("ms-appx:///test1.ttf", actual);
		});
	}

	[TestMethod]
	public void When_Weight_Requested_Less_Than_400_No_Lighter()
	{
		FontInfo[] infos =
		[
			new FontInfo()
			{
				FamilyName = "ms-appx:///test1.ttf",
				FontStretch = FontStretch.Normal,
				FontStyle = FontStyle.Normal,
				FontWeight = 370,
			},
			new FontInfo()
			{
				FamilyName = "ms-appx:///test2.ttf",
				FontStretch = FontStretch.Normal,
				FontStyle = FontStyle.Normal,
				FontWeight = 400,
			},
			new FontInfo()
			{
				FamilyName = "ms-appx:///test3.ttf",
				FontStretch = FontStretch.Normal,
				FontStyle = FontStyle.Normal,
				FontWeight = 500,
			},
		];

		PermuteAndAssert(infos, manifest =>
		{
			var actual = FontManifestHelpers.GetFamilyNameFromManifest(manifest, new FontWeight(350), FontStyle.Normal, FontStretch.Normal);
			Assert.AreEqual("ms-appx:///test1.ttf", actual);
		});
	}

	[TestMethod]
	public void When_Weight_Requested_Greater_Than_500_Prefer_Bolder()
	{
		FontInfo[] infos =
		[
			new FontInfo()
			{
				FamilyName = "ms-appx:///test1.ttf",
				FontStretch = FontStretch.Normal,
				FontStyle = FontStyle.Normal,
				FontWeight = 600,
			},
			new FontInfo()
			{
				FamilyName = "ms-appx:///test2.ttf",
				FontStretch = FontStretch.Normal,
				FontStyle = FontStyle.Normal,
				FontWeight = 540,
			},
			new FontInfo()
			{
				FamilyName = "ms-appx:///test3.ttf",
				FontStretch = FontStretch.Normal,
				FontStyle = FontStyle.Normal,
				FontWeight = 500,
			},
		];

		PermuteAndAssert(infos, manifest =>
		{
			var actual = FontManifestHelpers.GetFamilyNameFromManifest(manifest, new FontWeight(550), FontStyle.Normal, FontStretch.Normal);
			Assert.AreEqual("ms-appx:///test1.ttf", actual);
		});
	}

	[TestMethod]
	public void When_Weight_Requested_Greater_Than_500_No_Bolder()
	{
		FontInfo[] infos =
		[
			new FontInfo()
			{
				FamilyName = "ms-appx:///test1.ttf",
				FontStretch = FontStretch.Normal,
				FontStyle = FontStyle.Normal,
				FontWeight = 540,
			},
			new FontInfo()
			{
				FamilyName = "ms-appx:///test2.ttf",
				FontStretch = FontStretch.Normal,
				FontStyle = FontStyle.Normal,
				FontWeight = 500,
			},
		];

		PermuteAndAssert(infos, manifest =>
		{
			var actual = FontManifestHelpers.GetFamilyNameFromManifest(manifest, new FontWeight(550), FontStyle.Normal, FontStretch.Normal);
			Assert.AreEqual("ms-appx:///test1.ttf", actual);
		});
	}

	[TestMethod]
	public void When_Weight_Requested_Is_400_Prefer_500()
	{
		FontInfo[] infos =
		[
			new FontInfo()
			{
				FamilyName = "ms-appx:///test1.ttf",
				FontStretch = FontStretch.Normal,
				FontStyle = FontStyle.Normal,
				FontWeight = 500,
			},
			new FontInfo()
			{
				FamilyName = "ms-appx:///test2.ttf",
				FontStretch = FontStretch.Normal,
				FontStyle = FontStyle.Normal,
				FontWeight = 450,
			},
			new FontInfo()
			{
				FamilyName = "ms-appx:///test2.ttf",
				FontStretch = FontStretch.Normal,
				FontStyle = FontStyle.Normal,
				FontWeight = 350,
			},
		];

		PermuteAndAssert(infos, manifest =>
		{
			var actual = FontManifestHelpers.GetFamilyNameFromManifest(manifest, new FontWeight(400), FontStyle.Normal, FontStretch.Normal);
			Assert.AreEqual("ms-appx:///test1.ttf", actual);
		});
	}

	[TestMethod]
	public void When_Weight_Requested_Is_500_Prefer_400()
	{
		FontInfo[] infos =
		[
			new FontInfo()
			{
				FamilyName = "ms-appx:///test1.ttf",
				FontStretch = FontStretch.Normal,
				FontStyle = FontStyle.Normal,
				FontWeight = 400,
			},
			new FontInfo()
			{
				FamilyName = "ms-appx:///test2.ttf",
				FontStretch = FontStretch.Normal,
				FontStyle = FontStyle.Normal,
				FontWeight = 450,
			},
			new FontInfo()
			{
				FamilyName = "ms-appx:///test2.ttf",
				FontStretch = FontStretch.Normal,
				FontStyle = FontStyle.Normal,
				FontWeight = 550,
			},
		];

		PermuteAndAssert(infos, manifest =>
		{
			var actual = FontManifestHelpers.GetFamilyNameFromManifest(manifest, new FontWeight(500), FontStyle.Normal, FontStretch.Normal);
			Assert.AreEqual("ms-appx:///test1.ttf", actual);
		});
	}

	[TestMethod]
	// Equivalent cases
	[DataRow(FontStretch.Normal, FontStretch.Normal, (int)FontManifestHelpers.FontMatchingResult.Equivalent)]
	[DataRow(FontStretch.UltraCondensed, FontStretch.UltraCondensed, (int)FontManifestHelpers.FontMatchingResult.Equivalent)]
	[DataRow(FontStretch.UltraExpanded, FontStretch.UltraExpanded, (int)FontManifestHelpers.FontMatchingResult.Equivalent)]

	// Prefer condensed over expanded cases
	[DataRow(FontStretch.Condensed, FontStretch.SemiExpanded, (int)FontManifestHelpers.FontMatchingResult.CandidateIsBetter)]

	// Prefer condensed that's closest to Normal
	[DataRow(FontStretch.SemiCondensed, FontStretch.Condensed, (int)FontManifestHelpers.FontMatchingResult.CandidateIsBetter)]

	// Prefer expanded that's closest to Normal
	[DataRow(FontStretch.SemiExpanded, FontStretch.Expanded, (int)FontManifestHelpers.FontMatchingResult.CandidateIsBetter)]
	public void When_FontStretch_Requested_Is_Normal(FontStretch candidate, FontStretch bestSoFar, int expectedResult)
	{
		Assert.AreEqual((FontManifestHelpers.FontMatchingResult)expectedResult, FontManifestHelpers.IsBetterStretch(candidate, bestSoFar, FontStretch.Normal));

		var reversedExpected = expectedResult switch
		{
			(int)FontManifestHelpers.FontMatchingResult.CandidateIsBetter => FontManifestHelpers.FontMatchingResult.CandidateIsWorse,
			(int)FontManifestHelpers.FontMatchingResult.CandidateIsWorse => FontManifestHelpers.FontMatchingResult.CandidateIsBetter,
			_ => FontManifestHelpers.FontMatchingResult.Equivalent
		};
		Assert.AreEqual(reversedExpected, FontManifestHelpers.IsBetterStretch(bestSoFar, candidate, FontStretch.Normal));
	}

	[TestMethod]
	// Equivalent cases
	[DataRow(FontStretch.Normal, FontStretch.Normal, (int)FontManifestHelpers.FontMatchingResult.Equivalent)]
	[DataRow(FontStretch.UltraCondensed, FontStretch.UltraCondensed, (int)FontManifestHelpers.FontMatchingResult.Equivalent)]
	[DataRow(FontStretch.UltraExpanded, FontStretch.UltraExpanded, (int)FontManifestHelpers.FontMatchingResult.Equivalent)]

	// Prefer narrower
	[DataRow(FontStretch.ExtraCondensed, FontStretch.UltraCondensed, (int)FontManifestHelpers.FontMatchingResult.CandidateIsBetter)]
	[DataRow(FontStretch.ExtraCondensed, FontStretch.SemiCondensed, (int)FontManifestHelpers.FontMatchingResult.CandidateIsBetter)]

	// No narrower, take nearest wider
	[DataRow(FontStretch.SemiCondensed, FontStretch.Normal, (int)FontManifestHelpers.FontMatchingResult.CandidateIsBetter)]
	[DataRow(FontStretch.SemiCondensed, FontStretch.SemiExpanded, (int)FontManifestHelpers.FontMatchingResult.CandidateIsBetter)]
	[DataRow(FontStretch.Normal, FontStretch.SemiExpanded, (int)FontManifestHelpers.FontMatchingResult.CandidateIsBetter)]
	[DataRow(FontStretch.SemiExpanded, FontStretch.Expanded, (int)FontManifestHelpers.FontMatchingResult.CandidateIsBetter)]
	public void When_FontStretch_Requested_Is_Condensed(FontStretch candidate, FontStretch bestSoFar, int expectedResult)
	{
		Assert.AreEqual((FontManifestHelpers.FontMatchingResult)expectedResult, FontManifestHelpers.IsBetterStretch(candidate, bestSoFar, FontStretch.Condensed));

		var reversedExpected = expectedResult switch
		{
			(int)FontManifestHelpers.FontMatchingResult.CandidateIsBetter => FontManifestHelpers.FontMatchingResult.CandidateIsWorse,
			(int)FontManifestHelpers.FontMatchingResult.CandidateIsWorse => FontManifestHelpers.FontMatchingResult.CandidateIsBetter,
			_ => FontManifestHelpers.FontMatchingResult.Equivalent
		};
		Assert.AreEqual(reversedExpected, FontManifestHelpers.IsBetterStretch(bestSoFar, candidate, FontStretch.Condensed));
	}

	// https://github.com/dotnet/roslyn/blob/19b5e961ecb97b008106f1b646c077e0bffde4a7/src/Compilers/Core/CodeAnalysisTest/Collections/TemporaryArrayTests.cs#L239
	private static void PermuteAndAssert(FontInfo[] values, Action<FontManifest> assert)
	{
		doPermute(0, values.Length - 1);

		void doPermute(int start, int end)
		{
			if (start == end)
			{
				// We have one of our possible n! solutions,
				// add it to the list.
				assert(new FontManifest() { Fonts = values });
			}
			else
			{
				for (var i = start; i <= end; i++)
				{
					(values[start], values[i]) = (values[i], values[start]);
					doPermute(start + 1, end);
					(values[start], values[i]) = (values[i], values[start]);
				}
			}
		}
	}
}
