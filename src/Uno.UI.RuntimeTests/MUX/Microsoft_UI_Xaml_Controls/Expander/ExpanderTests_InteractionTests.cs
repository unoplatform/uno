// // Copyright (c) Microsoft Corporation. All rights reserved.
// // Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference Expander/InteractionTests/ExpanderTests.cpp, tag winui3/release/1.4.2

// using System;
// using Common;
// using Windows.UI.Xaml.Tests.MUXControls.InteractionTests.Infra;
// using Windows.UI.Xaml.Tests.MUXControls.InteractionTests.Common;
// using System.Collections.Generic;
// using Windows.Foundation.Metadata;
//
// #if USING_TAEF
// using WEX.TestExecution;
// using WEX.TestExecution.Markup;
// using WEX.Logging.Interop;
// #else
// using Microsoft.VisualStudio.TestTools.UnitTesting;
// using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
// #endif
//
// using Microsoft.Windows.Apps.Test.Automation;
// using Microsoft.Windows.Apps.Test.Foundation;
// using Microsoft.Windows.Apps.Test.Foundation.Controls;
// using Microsoft.Windows.Apps.Test.Foundation.Patterns;
// using Microsoft.Windows.Apps.Test.Foundation.Waiters;
// using MUXTestInfra.Shared.Infra;
//
// namespace Windows.UI.Xaml.Tests.MUXControls.InteractionTests
// {
//     public class Expander : UIObject, IExpandCollapse
//     {
//         public Expander(UIObject uiObject)
//             : base(uiObject)
//         {
//             this.Initialize();
//         }
//
//         private void Initialize()
//         {
//             _expandCollapsePattern = new ExpandCollapseImplementation(this);
//         }
//
//         public void ExpandAndWait()
//         {
//             using (var waiter = GetExpandedWaiter())
//             {
//                 Expand();
//                 waiter.Wait();
//             }
//
//             Wait.ForIdle();
//         }
//
//         public void CollapseAndWait()
//         {
//             using (var waiter = GetCollapsedWaiter())
//             {
//                 Collapse();
//                 waiter.Wait();
//             }
//
//             Wait.ForIdle();
//         }
//
//         public void Expand()
//         {
//             _expandCollapsePattern.Expand();
//         }
//
//         public void Collapse()
//         {
//             _expandCollapsePattern.Collapse();
//         }
//
//         public UIEventWaiter GetExpandedWaiter()
//         {
//             return _expandCollapsePattern.GetExpandedWaiter();
//         }
//
//         public UIEventWaiter GetCollapsedWaiter()
//         {
//             return _expandCollapsePattern.GetCollapsedWaiter();
//         }
//
//         public ExpandCollapseState ExpandCollapseState
//         {
//             get { return _expandCollapsePattern.ExpandCollapseState; }
//         }
//
//         new public static IFactory<Expander> Factory
//         {
//             get
//             {
//                 if (null == Expander._factory)
//                 {
//                     Expander._factory = new ExpanderFactory();
//                 }
//                 return Expander._factory;
//             }
//         }
//
//         private IExpandCollapse _expandCollapsePattern;
//         private static IFactory<Expander> _factory = null;
//         private class ExpanderFactory : IFactory<Expander>
//         {
//             public Expander Create(UIObject element)
//             {
//                 return new Expander(element);
//             }
//         }
//     }
//
//     [TestClass]
//     public class ExpanderTests
//     {
//         [ClassInitialize]
//         [TestProperty("RunAs", "User")]
//         [TestProperty("Classification", "Integration")]
//         [TestProperty("Platform", "Any")]
//         [TestProperty("MUXControlsTestSuite", "SuiteB")]
//         public static void ClassInitialize(TestContext testContext)
//         {
//             TestEnvironment.Initialize(testContext);
//         }
//
//         public void TestCleanup()
//         {
//             TestCleanupHelper.Cleanup();
//         }
//
//         [TestMethod]
//         public void VerifyAxeScanPasses()
//         {
//             using (var setup = new TestSetupHelper("Expander-Axe"))
//             {
//                 AxeTestHelper.TestForAxeIssues();
//             }
//         }
//
//         [TestMethod]
//         public void ExpandCollapseAutomationTests()
//         {
//             using (var setup = new TestSetupHelper("Expander Tests"))
//             {
//                 Expander expander = FindElement.ByName<Expander>("ExpandedExpander");
//                 expander.SetFocus();
//                 Wait.ForIdle();
//
//                 Log.Comment("Collapse using keyboard space key.");
//                 KeyboardHelper.PressKey(Key.Space);
//                 Verify.AreEqual(expander.ExpandCollapseState, ExpandCollapseState.Collapsed);
//
//                 Log.Comment("Expand using keyboard space key.");
//                 KeyboardHelper.PressKey(Key.Space);
//                 Verify.AreEqual(expander.ExpandCollapseState, ExpandCollapseState.Expanded);
//
//                 Log.Comment("Collapse by clicking.");
//                 expander.Click();
//                 Verify.AreEqual(expander.ExpandCollapseState, ExpandCollapseState.Collapsed);
//
//                 Log.Comment("Expand by clicking.");
//                 expander.Click();
//                 Verify.AreEqual(expander.ExpandCollapseState, ExpandCollapseState.Expanded);
//
//                 Log.Comment("Collapse using UIA ExpandCollapse pattern");
//                 expander.CollapseAndWait();
//                 Verify.AreEqual(expander.ExpandCollapseState, ExpandCollapseState.Collapsed);
//
//                 Log.Comment("Expand using UIA ExpandCollapse pattern");
//                 expander.ExpandAndWait();
//                 Verify.AreEqual(expander.ExpandCollapseState, ExpandCollapseState.Expanded);
//             }
//         }
//
//         [TestMethod]
//         public void AutomationPeerTest()
//         {
//             using (var setup = new TestSetupHelper("Expander Tests"))
//             {
//                 Expander expander = FindElement.ByName<Expander>("ExpanderWithToggleSwitch");
//                 expander.SetFocus();
//                 Wait.ForIdle();
//
//                 // Verify ExpandedExpander header content AutomationProperties.Name properties are set
//                 VerifyElement.Found("This expander with ToggleSwitch is expanded by default.", FindBy.Name);
//                 VerifyElement.Found("This is the second line of text.", FindBy.Name);
//                 VerifyElement.Found("SettingsToggleSwitch", FindBy.Name);
//
//                 // Verify ExpandedExpander content AutomationProperties.Name property is set
//                 VerifyElement.Found("ExpanderWithToggleSwitch Content", FindBy.Name);
//
//                 Log.Comment("Collapse using keyboard space key.");
//                 KeyboardHelper.PressKey(Key.Space);
//                 Verify.AreEqual(expander.ExpandCollapseState, ExpandCollapseState.Collapsed);
//
//                 // Verify ExpandedExpander content AutomationProperties.Name property is not visible once collapsed
//                 VerifyElement.NotFound("ExpanderWithToggleSwitch Content", FindBy.Name);
//             }
//         }
//     }
// }
