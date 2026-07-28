#nullable enable

using System;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	public partial class Given_RichEditBox
	{
		[TestMethod]
		public void When_Advanced_Destinations_RoundTrip_Without_Unsafe_Payloads()
		{
			const string rtf = @"{\rtf1"
				+ @"{\header Head {\b nested}}"
				+ @"{\footnote Note}"
				+ @"{\*\bkmkstart mark}body{\*\bkmkend mark}"
				+ @"{\*\vendorx escaped \{brace\}{\*\nested value}\bin3 {\}}"
				+ @"{\*\objdata 010203}{\*\passwordhash secret}}";
			var document = new RichEditBox().Document;

			document.SetText(TextSetOptions.FormatRtf, rtf);
			document.GetText(TextGetOptions.FormatRtf, out var exported);

			StringAssert.Contains(exported, @"{\header Head {\b nested}}");
			StringAssert.Contains(exported, @"{\footnote Note}");
			StringAssert.Contains(exported, @"{\*\bkmkstart mark}");
			StringAssert.Contains(exported, @"{\*\bkmkend mark}");
			StringAssert.Contains(exported, @"{\*\vendorx escaped \{brace\}{\*\nested value}\bin3 {\}}");
			Assert.IsFalse(exported.Contains(@"\objdata", StringComparison.Ordinal));
			Assert.IsFalse(exported.Contains(@"\passwordhash", StringComparison.Ordinal));
		}

		[TestMethod]
		public void When_Unsafe_Destination_Is_Nested_The_Outer_Group_Is_Not_Preserved()
		{
			const string rtf = @"{\rtf1{\*\vendorx safe {\*\objdata 010203}{\*\passwordhash secret}}body}";
			var document = new RichEditBox().Document;

			document.SetText(TextSetOptions.FormatRtf, rtf);
			document.GetText(TextGetOptions.FormatRtf, out var exported);

			Assert.IsFalse(exported.Contains(@"\vendorx", StringComparison.Ordinal));
			Assert.IsFalse(exported.Contains(@"\objdata", StringComparison.Ordinal));
			Assert.IsFalse(exported.Contains(@"\passwordhash", StringComparison.Ordinal));
		}

		[TestMethod]
		public void When_Active_Field_Is_Nested_The_Outer_Group_Is_Not_Preserved()
		{
			const string rtf = @"{\rtf1{\*\vendorx safe {\field{\*\fldinst DDEAUTO cmd.exe}}}body}";
			var document = new RichEditBox().Document;

			document.SetText(TextSetOptions.FormatRtf, rtf);
			document.GetText(TextGetOptions.FormatRtf, out var exported);

			Assert.IsFalse(exported.Contains(@"\vendorx", StringComparison.Ordinal));
			Assert.IsFalse(exported.Contains(@"\field", StringComparison.Ordinal));
			Assert.IsFalse(exported.Contains("DDEAUTO", StringComparison.OrdinalIgnoreCase));
		}

		[TestMethod]
		public void When_Table_Is_Untouched_Row_And_Cell_Descriptors_RoundTrip()
		{
			const string rtf = @"{\rtf1\trowd\trgaph108\clvertalc\cellx1200"
				+ @"\clvertalb\cellx2400\intbl first\cell second\cell\row tail}";
			var document = new RichEditBox().Document;

			document.SetText(TextSetOptions.FormatRtf, rtf);
			document.GetText(TextGetOptions.None, out var text);
			document.GetText(TextGetOptions.FormatRtf, out var exported);

			StringAssert.StartsWith(text, "first\tsecond\t\rtail");
			StringAssert.Contains(exported, @"\trowd");
			StringAssert.Contains(exported, @"\trgaph108");
			StringAssert.Contains(exported, @"\clvertalc\cellx1200");
			StringAssert.Contains(exported, @"\clvertalb\cellx2400");
			StringAssert.Contains(exported, @"\cell");
			StringAssert.Contains(exported, @"\row");
		}

		[TestMethod]
		public void When_Nested_Table_Is_Untouched_Nested_Boundaries_RoundTrip()
		{
			const string rtf = @"{\rtf1\trowd\cellx3000\intbl outer "
				+ @"{\trowd\itap2\cellx1000 nested\cell\nestrow}"
				+ @" after\cell\row}";
			var document = new RichEditBox().Document;

			document.SetText(TextSetOptions.FormatRtf, rtf);
			document.GetText(TextGetOptions.FormatRtf, out var exported);

			StringAssert.Contains(exported, @"\itap2");
			StringAssert.Contains(exported, @"\nestrow");
			Assert.AreEqual(2, Count(exported, @"\trowd"));
		}

		[TestMethod]
		public void When_Edit_Is_Outside_Table_Preservation_Remains()
		{
			const string rtf = @"{\rtf1\trowd\cellx1000\intbl A\cell\row tail}";
			var document = new RichEditBox().Document;
			document.SetText(TextSetOptions.FormatRtf, rtf);

			var end = document.GetRange(0, int.MaxValue).EndPosition;
			document.GetRange(end, end).SetText(TextSetOptions.None, "!");
			document.GetText(TextGetOptions.FormatRtf, out var exported);

			StringAssert.Contains(exported, @"\trowd");
			StringAssert.Contains(exported, @"\cellx1000");
			StringAssert.Contains(exported, @"\cell");
			StringAssert.Contains(exported, @"\row");
		}

		[TestMethod]
		public void When_Edit_Is_Inside_Table_Invalidated_Metadata_Is_Dropped()
		{
			const string rtf = @"{\rtf1\trowd\cellx1000\intbl AB\cell\row tail}";
			var document = new RichEditBox().Document;
			document.SetText(TextSetOptions.FormatRtf, rtf);

			document.GetRange(1, 1).SetText(TextSetOptions.None, "x");
			document.GetText(TextGetOptions.FormatRtf, out var exported);

			Assert.IsFalse(exported.Contains(@"\trowd", StringComparison.Ordinal));
			Assert.IsFalse(exported.Contains(@"\cellx", StringComparison.Ordinal));
			Assert.IsFalse(exported.Contains(@"\intbl", StringComparison.Ordinal));
			Assert.IsFalse(exported.Contains(@"\row", StringComparison.Ordinal));
		}

		[TestMethod]
		public void When_Outer_Table_Is_Invalidated_Nested_Table_Metadata_Is_Dropped()
		{
			const string rtf = @"{\rtf1\trowd\cellx3000\intbl outer "
				+ @"{\trowd\itap2\cellx1000 nested\cell\nestrow}"
				+ @" after\cell\row}";
			var document = new RichEditBox().Document;
			document.SetText(TextSetOptions.FormatRtf, rtf);

			document.GetRange(1, 1).SetText(TextSetOptions.None, "x");
			document.GetText(TextGetOptions.FormatRtf, out var exported);

			Assert.IsFalse(exported.Contains(@"\trowd", StringComparison.Ordinal));
			Assert.IsFalse(exported.Contains(@"\itap2", StringComparison.Ordinal));
			Assert.IsFalse(exported.Contains(@"\nestrow", StringComparison.Ordinal));
		}

		[TestMethod]
		public void When_Table_Invalidation_Is_Undone_Preserved_Metadata_Is_Restored()
		{
			const string rtf = @"{\rtf1\trowd\cellx1000\intbl AB\cell\row tail}";
			var document = new RichEditBox().Document;
			document.SetText(TextSetOptions.FormatRtf, rtf);
			document.ClearUndoRedoHistory();

			document.GetRange(1, 1).SetText(TextSetOptions.None, "x");
			document.Undo();
			document.GetText(TextGetOptions.FormatRtf, out var undone);
			document.Redo();
			document.GetText(TextGetOptions.FormatRtf, out var redone);

			StringAssert.Contains(undone, @"\trowd");
			StringAssert.Contains(undone, @"\cellx1000");
			Assert.IsFalse(redone.Contains(@"\trowd", StringComparison.Ordinal));
		}

		[TestMethod]
		public void When_Text_Is_Inserted_At_Table_Start_Metadata_Rebases_After_The_Insert()
		{
			const string rtf = @"{\rtf1\trowd\cellx1000\intbl AB\cell\row tail}";
			var document = new RichEditBox().Document;
			document.SetText(TextSetOptions.FormatRtf, rtf);

			document.GetRange(0, 0).SetText(TextSetOptions.None, "x");
			document.GetText(TextGetOptions.FormatRtf, out var exported);

			StringAssert.Contains(exported, @"x\trowd");
			StringAssert.Contains(exported, @"\cellx1000");
		}

		[TestMethod]
		public void When_Opaque_Destination_Exceeds_Budget_Import_Is_Rejected()
		{
			var rtf = @"{\rtf1{\*\vendorx " + new string('x', 256 * 1024 + 1) + "}}";
			var document = new RichEditBox().Document;

			Assert.ThrowsExactly<ArgumentException>(() => document.SetText(TextSetOptions.FormatRtf, rtf));
		}

		private static int Count(string value, string token)
		{
			var count = 0;
			var position = 0;
			while ((position = value.IndexOf(token, position, StringComparison.Ordinal)) >= 0)
			{
				count++;
				position += token.Length;
			}
			return count;
		}
	}
}
