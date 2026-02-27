// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference LinedFlowLayoutTests.cs, tag winui3/release/1.8.4

using Microsoft.UI.Xaml.Controls;
using MUXControlsTestApp.Utilities;
using Common;

#if USING_TAEF
using WEX.TestExecution;
using WEX.TestExecution.Markup;
using WEX.Logging.Interop;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
#endif

namespace Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.RepeaterTests
{
	[TestClass]
	public class LinedFlowLayoutTests : MUXApiTestBase
	{
		[TestMethod]
		public void VerifyDefaultPropertyValues()
		{
			RunOnUIThread.Execute(() =>
			{
				LinedFlowLayout linedFlowLayout = new LinedFlowLayout();
				Verify.IsNotNull(linedFlowLayout);

				Log.Comment("Verifying LinedFlowLayout default property values");
				Verify.AreEqual(0.0, linedFlowLayout.ActualLineHeight);
				Verify.IsTrue(double.IsNaN(linedFlowLayout.LineHeight), "LineHeight should be NaN by default");
				Verify.AreEqual(0.0, linedFlowLayout.LineSpacing);
				Verify.AreEqual(0.0, linedFlowLayout.MinItemSpacing);
				Verify.AreEqual(LinedFlowLayoutItemsJustification.Start, linedFlowLayout.ItemsJustification);
				Verify.AreEqual(LinedFlowLayoutItemsStretch.None, linedFlowLayout.ItemsStretch);
			});
		}
	}
}
