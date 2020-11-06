// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Common;
using MUXControlsTestApp.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;

#if USING_TAEF
using WEX.TestExecution;
using WEX.TestExecution.Markup;
using WEX.Logging.Interop;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
#endif

using ItemsRepeater = Microsoft.UI.Xaml.Controls.ItemsRepeater;
using ElementFactory = Microsoft.UI.Xaml.Controls.ElementFactory;
using RecyclePool = Microsoft.UI.Xaml.Controls.RecyclePool;
using StackLayout = Microsoft.UI.Xaml.Controls.StackLayout;
using ItemsRepeaterScrollHost = Microsoft.UI.Xaml.Controls.ItemsRepeaterScrollHost;
using RepeaterTestHooks = Microsoft.UI.Private.Controls.RepeaterTestHooks;

namespace Windows.UI.Xaml.Tests.MUXControls.ApiTests.RepeaterTests
{
    using ElementFactoryGetArgs = Microsoft.UI.Xaml.Controls.ElementFactoryGetArgs;
    using ElementFactoryRecycleArgs = Microsoft.UI.Xaml.Controls.ElementFactoryRecycleArgs;

    // Bug 17377723: crash in CControlTemplate::CreateXBindConnector in RS5.
    [TestClass]
    public class PhasingTests : ApiTestBase
    {
        const int expectedLastRealizedIndex = 8;

        [TestMethod]
        public void ValidatePhaseInvokeAndOrdering()
        {
            if (!PlatformConfiguration.IsOsVersionGreaterThan(OSVersion.Redstone2))
            {
                Log.Warning("Skipping: GetAvailableSize API is only available in RS3 and above.");
                return;
            }

            ItemsRepeater repeater = null;
            int numPhases = 6; // 0 to 5
            ManualResetEvent buildTreeCompleted = new ManualResetEvent(false);

            RunOnUIThread.Execute(() =>
            {
                var itemTemplate = (DataTemplate)XamlReader.Load(
                       @"<DataTemplate  xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
                            <Button Width='100' Height='100'/>
                        </DataTemplate>");
                repeater = new ItemsRepeater()
                {
                    ItemsSource = Enumerable.Range(0, 10),
                    ItemTemplate = new CustomElementFactory(numPhases),
                    Layout = new StackLayout(),
                };
                
                repeater.ElementPrepared += (sender, args) =>
                {
                    if (args.Index == expectedLastRealizedIndex)
                    {
                        Log.Comment("Item 8 Created!" );
                        RepeaterTestHooks.BuildTreeCompleted += (sender1, args1) =>
                        {
                            buildTreeCompleted.Set();
                        };
                    }
                };

                Content = new ItemsRepeaterScrollHost()
                {
                    Width = 400,
                    Height = 400,
                    ScrollViewer = new ScrollViewer
                    {
                        Content = repeater
                    }
                };

                // CompositionTarget.Rendering += (sender, args) => { Log.Comment("Rendering"); }; // debugging aid
            });

            if(buildTreeCompleted.WaitOne(TimeSpan.FromMilliseconds(2000)))
            {
                RunOnUIThread.Execute(() =>
                {
                    var calls = ElementPhasingManager.ProcessedCalls;

                    Verify.AreEqual(9, calls.Count);
                    calls[0].RemoveAt(0); // Remove the create we did for first measure.
                    foreach (var index in calls.Keys)
                    {
                        var phases = calls[index];
                        Verify.AreEqual(6, phases.Count);
                        for (int i = 0; i < phases.Count; i++)
                        {
                            Verify.AreEqual(i, phases[i]);
                        }
                    }

                    ElementPhasingManager.ProcessedCalls.Clear();
                });
            }
            else
            {
                Verify.Fail("Failed on waiting on build tree.");
            }
        }

        [TestMethod]
        public void ValidateXBindWithoutPhasing()
        {
            ItemsRepeater repeater = null;
            int numPhases = 1; // Just Phase 0 for x:Bind
            ManualResetEvent ElementLoadedCompleted = new ManualResetEvent(false);

            RunOnUIThread.Execute(() =>
            {
                var itemTemplate = (DataTemplate)XamlReader.Load(
                       @"<DataTemplate  xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
                            <Button Width='100' Height='100'/>
                        </DataTemplate>");
                repeater = new ItemsRepeater()
                {
                    ItemsSource = Enumerable.Range(0, 10),
                    ItemTemplate = new CustomElementFactory(numPhases),
                    Layout = new StackLayout(),
                };

                repeater.ElementPrepared += (sender, args) =>
                {
                    if (args.Index == expectedLastRealizedIndex)
                    {
                        Log.Comment("Item 8 Created!");
                        ElementLoadedCompleted.Set();
                    }
                };

                Content = new ItemsRepeaterScrollHost()
                {
                    Width = 400,
                    Height = 400,
                    ScrollViewer = new ScrollViewer
                    {
                        Content = repeater
                    }
                };
            });

            if(ElementLoadedCompleted.WaitOne(TimeSpan.FromMilliseconds(2000)))
            {
                RunOnUIThread.Execute(() =>
                {
                    var calls = ElementPhasingManager.ProcessedCalls;

                    Verify.AreEqual(calls.Count, 9);
                    calls[0].RemoveAt(0); // Remove the create we did for first measure.
                    foreach (var index in calls.Keys)
                    {
                        var phases = calls[index];
                        Verify.AreEqual(1, phases.Count); // Just phase 0
                    }

                    ElementPhasingManager.ProcessedCalls.Clear();
                });
            }
            else
            {
                Verify.Fail("Failed on waiting on build tree.");
            }
        }

        private class CustomElementFactory : ElementFactory
        {
            private int _numPhases;
            private RecyclePool _recyclePool = new RecyclePool();
            private string key = "foobar";

            public CustomElementFactory(int numPhases)
            {
                _numPhases = numPhases;
            }

            protected override UIElement GetElementCore(ElementFactoryGetArgs args)
            {
                var element = _recyclePool.TryGetElement(key, args.Parent);
                if (element == null)
                {
                    element = new Button() { Width = 100, Height = 100 };
                }

                var elementManager = new ElementPhasingManager(_numPhases);
                XamlBindingHelper.SetDataTemplateComponent(element, elementManager);
                return element;
            }

            protected override void RecycleElementCore(ElementFactoryRecycleArgs args)
            {
                XamlBindingHelper.GetDataTemplateComponent(args.Element).Recycle();
                _recyclePool.PutElement(args.Element, key, args.Parent);
            }
        }

        private class ElementPhasingManager : IDataTemplateComponent
        {
            private int _numPhases = 1; // Default is just phase 0
            private int _data = -1;

            public List<int> Data { get; private set; }
            public bool IsCleared { get; private set; }

            // data index -> list<phases>
            public static Dictionary<int, List<int>> ProcessedCalls { get; set; }

            public ElementPhasingManager(int numPhases)
            {
                _numPhases = numPhases;
            }

            public void Recycle()
            {
                IsCleared = true;
                Log.Comment(string.Format("Recycle Index:{0}", _data));
            }

            public void ProcessBindings(object item, int itemIndex, int phase, out int nextPhase)
            {
                if (Data == null)
                {
                    Data = new List<int>();
                }

                if (ProcessedCalls == null)
                {
                    ProcessedCalls = new Dictionary<int, List<int>>();
                }

                _data = (int)item;
                Data.Add(_data);
                if (!ProcessedCalls.ContainsKey(_data))
                {
                    ProcessedCalls.Add(_data, new List<int>());
                }

                ProcessedCalls[_data].Add(phase);

                nextPhase = phase >= _numPhases -1 ? -1 : phase + 1;
                Log.Comment(string.Format("Index:{0}  Phase:{1}  NextPhase:{2}", item.ToString(), phase, nextPhase));
            }
        }
    }
}
