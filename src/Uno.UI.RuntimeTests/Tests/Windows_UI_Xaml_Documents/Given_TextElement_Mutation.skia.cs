// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TextElement.cpp / InlineCollection.cpp / paragraph.cpp, tag winui3/release/1.8.2, commit 4a1c6184c

using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Private.Infrastructure.TestServices;

#nullable enable

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Documents
{
	[TestClass]
	public class Given_TextElement_Mutation
	{
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Run_Text_Shrinks_After_Load_Container_Refreshes()
		{
			var run = new Run { Text = "Hello World" };
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(run);
			var SUT = new RichTextBlock { IsTextSelectionEnabled = true };
			SUT.Blocks.Add(paragraph);

			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				await WindowHelper.WaitForIdle();

				// Populate the run-model position caches the way a selection / UIA read would.
				SUT.Blocks.GetPositionCount(out var before);
				Assert.AreEqual(15u, before); // paragraph = 2 + ("Hello World"=11 + 2)

				// Shrink the run. Before the fix the ancestor caches stayed stale and the next run-model
				// walk sliced past the shrunk text (ArgumentOutOfRangeException). MarkDirty must clear them.
				run.Text = "Hi";
				await WindowHelper.WaitForIdle();

				SUT.Blocks.GetPositionCount(out var after);
				Assert.AreEqual(6u, after); // paragraph = 2 + ("Hi"=2 + 2)

				// A run-model walk over the refreshed content must succeed and reflect the new text.
				SUT.Blocks.GetRun(2, out _, out _, out _, out var nested, out var characters, out _);
				Assert.AreSame(run, nested);
				Assert.AreEqual("Hi", characters.ToString());
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Empty_Paragraph_ContentEnd_Parent_Returns_Paragraph()
		{
			var paragraph = new Paragraph();
			var SUT = new RichTextBlock();
			SUT.Blocks.Add(paragraph);

			try
			{
				WindowHelper.WindowContent = SUT;
				// An empty RichTextBlock can settle to zero size; wait on IsLoaded rather than a non-zero rect.
				await WindowHelper.WaitForLoaded(SUT, x => x.IsLoaded);
				await WindowHelper.WaitForIdle();

				// ContentEnd sits at the interior position of the empty paragraph; TextPointer.Parent walks
				// GetContainingElement over an empty inline collection — it must return the paragraph, not NRE.
				var pointer = paragraph.GetContentEnd();
				Assert.IsNotNull(pointer);
				Assert.AreSame(paragraph, pointer!.Parent);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}
	}
}
