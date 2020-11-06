// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Windows.UI.Xaml.Controls;
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

using IndexPath = Microsoft.UI.Xaml.Controls.IndexPath;

namespace Windows.UI.Xaml.Tests.MUXControls.ApiTests.RepeaterTests
{
    [TestClass]
    public class IndexPathTests : ApiTestBase
    {
        [TestMethod]
        public void ValidateIndexPath()
        {
            RunOnUIThread.Execute(() =>
            {
                IndexPath path = IndexPath.CreateFromIndices(null);
                Verify.AreEqual(0, path.GetSize());

                path = IndexPath.CreateFrom(5);
                Verify.AreEqual(1, path.GetSize());
                Verify.AreEqual(5, path.GetAt(0));

                path = IndexPath.CreateFrom(1, 2);
                Verify.AreEqual(2, path.GetSize());
                Verify.AreEqual(1, path.GetAt(0));
                Verify.AreEqual(2, path.GetAt(1));
                
                Verify.AreEqual(0, IndexPath.CreateFrom(0, 1).CompareTo(IndexPath.CreateFrom(0, 1)));
                Verify.AreEqual(-1, IndexPath.CreateFrom(0, 1).CompareTo(IndexPath.CreateFrom(1, 0)));
                Verify.AreEqual(1, IndexPath.CreateFrom(0, 1).CompareTo(IndexPath.CreateFrom(0, 0)));
                
                Verify.AreEqual(-1, IndexPath.CreateFrom(1, 0).CompareTo(IndexPath.CreateFrom(1, 1)));
                Verify.AreEqual(0, IndexPath.CreateFrom(1, 0).CompareTo(IndexPath.CreateFrom(1, 0)));
                Verify.AreEqual(1, IndexPath.CreateFrom(1, 1).CompareTo(IndexPath.CreateFrom(1, 0)));
                

                var emptyPath = IndexPath.CreateFromIndices(null);
                Verify.AreEqual(0, emptyPath.CompareTo(emptyPath));
                var path1 = IndexPath.CreateFrom(1);
                Verify.AreEqual(-1, emptyPath.CompareTo(path1));
                Verify.AreEqual(1, path1.CompareTo(emptyPath));
                var path12 = IndexPath.CreateFrom(1, 2);
                Verify.AreEqual(-1, path1.CompareTo(path12));
                Verify.AreEqual(1, path12.CompareTo(path1));
            });
        }
    }
}
