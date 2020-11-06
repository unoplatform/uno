// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Windows.UI.Xaml.Tests.MUXControls.ApiTests.RepeaterTests.Common;
using MUXControlsTestApp.Utilities;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Windows.Foundation.Collections;
using Windows.UI.Xaml.Controls;
using Common;
using System;
using Microsoft.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using MUXControlsTestApp.Utils;

#if USING_TAEF
using WEX.TestExecution;
using WEX.TestExecution.Markup;
using WEX.Logging.Interop;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
#endif

namespace Windows.UI.Xaml.Tests.MUXControls.ApiTests.RepeaterTests
{
    using ItemsSourceView = Microsoft.UI.Xaml.Controls.ItemsSourceView;
    using IKeyIndexMapping = Microsoft.UI.Xaml.Controls.IKeyIndexMapping;

    [TestClass]
    public class InspectingDataSourceTests : ApiTestBase
    {
        [TestMethod]
        public void CanCreateFromIBindableIterable()
        {
            RunOnUIThread.Execute(() =>
            {
                var dataSource = new ItemsSourceView(Enumerable.Range(0, 100));
                Verify.AreEqual(100, dataSource.Count);
                Verify.AreEqual(4, dataSource.GetAt(4));
            });
        }

        [TestMethod]
        public void CanCreateFromInccIBindableVector()
        {
            RunOnUIThread.Execute(() =>
            {
                var data = new ObservableCollection<string>(Enumerable.Range(0, 100).Select(i => string.Format("Item #{0}", i)));
                var dataSource = new ItemsSourceView(data);
                var recorder = new CollectionChangeRecorder(dataSource);
                Verify.AreEqual(100, dataSource.Count);
                Verify.AreEqual("Item #4", (string)dataSource.GetAt(4));

                data.Insert(4, "Inserted Item");
                data.RemoveAt(7);
                data[15] = "Replaced Item";
                data.Clear();

                VerifyRecordedCollectionChanges(
                    expected: new NotifyCollectionChangedEventArgs[]
                    {
                        CollectionChangeEventArgsConverters.CreateNotifyArgs(NotifyCollectionChangedAction.Add, -1, 0, 4, 1),
                        CollectionChangeEventArgsConverters.CreateNotifyArgs(NotifyCollectionChangedAction.Remove, 7, 1, -1, 0),
                        CollectionChangeEventArgsConverters.CreateNotifyArgs(NotifyCollectionChangedAction.Replace, 15, 1, 15, 1),
                        CollectionChangeEventArgsConverters.CreateNotifyArgs(NotifyCollectionChangedAction.Reset, -1, 0, -1, 0)
                    },
                    actual: recorder.RecordedArgs);
            });
        }

        [TestMethod]
        public void CanCreateFromIObservableVector()
        {
            RunOnUIThread.Execute(() =>
            {
                var data = new WinRTObservableVector(Enumerable.Range(0, 100).Select(i => string.Format("Item #{0}", i)));
                var dataSource = new ItemsSourceView(data);
                var recorder = new CollectionChangeRecorder(dataSource);
                Verify.AreEqual(100, dataSource.Count);
                Verify.AreEqual("Item #4", (string)dataSource.GetAt(4));
                Verify.IsFalse(dataSource.HasKeyIndexMapping);

                data.Insert(4, "Inserted Item");
                data.RemoveAt(7);
                data[15] = "Replaced Item";
                data.Clear();

                VerifyRecordedCollectionChanges(
                    expected: new NotifyCollectionChangedEventArgs[]
                    {
                     CollectionChangeEventArgsConverters.CreateNotifyArgs(NotifyCollectionChangedAction.Add, -1, 0, 4, 1),
                     CollectionChangeEventArgsConverters.CreateNotifyArgs(NotifyCollectionChangedAction.Remove, 7, 1, -1, 0),
                     CollectionChangeEventArgsConverters.CreateNotifyArgs(NotifyCollectionChangedAction.Replace, 15, 1, 15, 1),
                     CollectionChangeEventArgsConverters.CreateNotifyArgs(NotifyCollectionChangedAction.Reset, -1, 0, -1, 0),
                    },
                    actual: recorder.RecordedArgs);
            });
        }

        [TestMethod]
        public void VerifyUniqueIdMappingInterface()
        {
            RunOnUIThread.Execute(() =>
            {
                var data = new ObservableVectorWithUniqueIds(Enumerable.Range(0, 10));
                var dataSource = new ItemsSourceView(data);
                Verify.AreEqual(10, dataSource.Count);
                Verify.AreEqual(true, dataSource.HasKeyIndexMapping);
                Verify.AreEqual(5, dataSource.IndexFromKey("5"));
                Verify.AreEqual("5", dataSource.KeyFromIndex(5));
            });
        }

        [TestMethod]
        public void VerifyIndexOfBehavior()
        {
            RunOnUIThread.Execute(() =>
            {
                var collections = new List<IEnumerable>();
                collections.Add(new ObservableVectorWithUniqueIds(Enumerable.Range(0, 10)));
                collections.Add(new ObservableCollection<int>(Enumerable.Range(0,10)));

                foreach(var collection in collections)
                {
                    var dataSource = new ItemsSourceView(collection);
                    foreach(int i in collection)
                    {
                        Verify.AreEqual(i, dataSource.IndexOf(i));
                    }

                    Verify.AreEqual(-1, dataSource.IndexOf(11));
                }

                // Enumerabl.Range returns IEnumerable which does not provide IndexOf
                var testingItemsSourceView = new ItemsSourceView(Enumerable.Range(0, 10));
                var index = -1;
                try
                {
                    index = testingItemsSourceView.IndexOf(0);
                }catch(Exception){ }
                Verify.AreEqual(-1, index);


                var nullContainingEnumerable = new CustomEnumerable();
                testingItemsSourceView = new ItemsSourceView(nullContainingEnumerable);

                Verify.AreEqual(1,testingItemsSourceView.IndexOf(null));

            });
        }

        // Calling Reset multiple times before layout runs causes a crash
        // in unique ids. We end up thinking we have multiple elements with the same id.
        [TestMethod]
        public void VerifyCallingResetMultipleTimesOnUniqueIdItemsSource()
        {
            RunOnUIThread.Execute(() =>
            {
                var data = new CustomItemsSourceWithUniqueId(Enumerable.Range(0, 5).ToList());
                var repeater = new ItemsRepeater() 
                {
                    ItemsSource = data,
                    Animator = new DefaultElementAnimator()
                };

                Content = new Windows.UI.Xaml.Controls.ScrollViewer() 
                {
                    Width=400,
                    Height=400,
                    Content = repeater
                };
                Content.UpdateLayout();

                data.Reset();
                data.Reset();

                Content.UpdateLayout();

                Verify.AreEqual(5, repeater.ItemsSourceView.Count);
                for(int i=0; i< 5; i++)
                {
                    Verify.IsNotNull(repeater.TryGetElement(i));
                }
            });
        }

        [TestMethod]
        public void ValidateSwitchingItemsSourceRefreshesElementsNonVirtualLayout()
        {
            ValidateSwitchingItemsSourceRefreshesElements(isVirtualLayout: false);
        }

        [TestMethod]
        public void ValidateSwitchingItemsSourceRefreshesElementsVirtualLayout()
        {
            ValidateSwitchingItemsSourceRefreshesElements(isVirtualLayout: true);
        }

        public void ValidateSwitchingItemsSourceRefreshesElements(bool isVirtualLayout)
        {
            RunOnUIThread.Execute(() =>
            {
                ItemsRepeater repeater = null;
                const int numItems = 10;

                repeater = new ItemsRepeater() {
                    ItemsSource = Enumerable.Range(0, numItems),
                };

                // By default we use stack layout that is virtualizing.
                if (!isVirtualLayout)
                {
                    repeater.Layout = new NonVirtualStackLayout();
                }

                Content = new ItemsRepeaterScrollHost() {
                    Width = 400,
                    Height = 400,
                    ScrollViewer = new Windows.UI.Xaml.Controls.ScrollViewer() {
                        Content = repeater
                    }
                };

                Content.UpdateLayout();
                for (int i = 0; i < numItems; i++)
                {
                    var element = (TextBlock)repeater.TryGetElement(i);
                    Verify.AreEqual(i.ToString(), element.Text);
                }

                repeater.ItemsSource = Enumerable.Range(20, numItems);
                Content.UpdateLayout();

                for (int i = 0; i < numItems; i++)
                {
                    var element = (TextBlock)repeater.TryGetElement(i);
                    Verify.AreEqual((i + 20).ToString(), element.Text);
                }
            });
        }

        private static void VerifyRecordedCollectionChanges(NotifyCollectionChangedEventArgs[] expected, List<NotifyCollectionChangedEventArgs> actual)
        {
            Verify.AreEqual(expected.Length, actual.Count);

            for (int i = 0; i < expected.Length; ++i)
            {
                Verify.AreEqual(expected[i].Action, actual[i].Action);
                Verify.AreEqual(expected[i].NewStartingIndex, actual[i].NewStartingIndex);
                Verify.AreEqual(expected[i].OldStartingIndex, actual[i].OldStartingIndex);
                Verify.AreEqual(GetCount(expected[i].NewItems), GetCount(actual[i].NewItems));
                Verify.AreEqual(GetCount(expected[i].OldItems), GetCount(actual[i].OldItems));
            }
        }

        private static int GetCount(IList list)
        {
            return list == null ? -1 : list.Count;
        }

        class CollectionChangeRecorder
        {
            private List<NotifyCollectionChangedEventArgs> _recordedArgs = new List<NotifyCollectionChangedEventArgs>();

            public List<NotifyCollectionChangedEventArgs> RecordedArgs { get { return _recordedArgs; } }

            public CollectionChangeRecorder(ItemsSourceView source)
            {
                source.CollectionChanged += (sender, args) => RecordedArgs.Add(Clone(args));
            }

            private static NotifyCollectionChangedEventArgs Clone(NotifyCollectionChangedEventArgs args)
            {
                return CollectionChangeEventArgsConverters.CreateNotifyArgs(
                    args.Action,
                    args.OldStartingIndex,
                    args.OldItems == null ? -1 : args.OldItems.Count,
                    args.NewStartingIndex,
                    args.NewItems == null ? -1 : args.NewItems.Count);
            }
        }

        class WinRTObservableVector : Windows.Foundation.Collections.IObservableVector<object>
        {
            private IList<object> _items;

            public object this[int index]
            {
                get { return _items[index]; }
                set
                {
                    _items[index] = value;
                    if (VectorChanged != null) { VectorChanged(this, new WinRTVectorChangedEventArgs(CollectionChange.ItemChanged, index)); }
                }
            }

            public int Count { get { return _items.Count; } }

            public bool IsReadOnly { get { return false; } }

            public event VectorChangedEventHandler<object> VectorChanged;

            public WinRTObservableVector()
            {
                _items = new List<object>();
            }

            public WinRTObservableVector(IEnumerable<object> items)
            {
                _items = new List<object>(items);
            }

            public void Add(object item)
            {
                _items.Add(item);
                if (VectorChanged != null) { VectorChanged(this, new WinRTVectorChangedEventArgs(CollectionChange.ItemInserted, _items.Count - 1)); }
            }

            public void Clear()
            {
                _items.Clear();
                if (VectorChanged != null) { VectorChanged(this, new WinRTVectorChangedEventArgs(CollectionChange.Reset, 0)); }
            }

            public bool Contains(object item)
            {
                return _items.Contains(item);
            }

            public void CopyTo(object[] array, int arrayIndex)
            {
                _items.CopyTo(array, arrayIndex);
            }

            public IEnumerator<object> GetEnumerator()
            {
                return _items.GetEnumerator();
            }

            public int IndexOf(object item)
            {
                return _items.IndexOf(item);
            }

            public void Insert(int index, object item)
            {
                _items.Insert(index, item);
                if (VectorChanged != null) { VectorChanged(this, new WinRTVectorChangedEventArgs(CollectionChange.ItemInserted, index)); }
            }

            public bool Remove(object item)
            {
                RemoveAt(_items.IndexOf(item));
                return true;
            }

            public void RemoveAt(int index)
            {
                _items.RemoveAt(index);
                if (VectorChanged != null) { VectorChanged(this, new WinRTVectorChangedEventArgs(CollectionChange.ItemRemoved, index)); }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _items.GetEnumerator();
            }

            private class WinRTVectorChangedEventArgs : IVectorChangedEventArgs
            {
                public CollectionChange CollectionChange { get; private set; }

                private uint _index;
                public uint Index
                {
                    get
                    {
                        if (CollectionChange == CollectionChange.Reset)
                        {
                            // C++/CX observable collection fails if accessing index 
                            // when the args is for a Reset, so emulating that behavior here.
                            throw new InvalidOperationException();
                        }

                        return _index;
                    }

                    private set
                    {
                        _index = value;
                    }
                }

                public WinRTVectorChangedEventArgs(CollectionChange change, int index)
                {
                    CollectionChange = change;
                    Index = (uint)index;
                }
            }
        }

        class ObservableVectorWithUniqueIds : ObservableCollection<int>, IKeyIndexMapping
        {
            public ObservableVectorWithUniqueIds(IEnumerable<int> data) : base(data) { }

            // Note: In real world scenarios the mapping would be based on the object,
            // but for testing purposes we can just use the index here.
            public string KeyFromIndex(int index)
            {
                return index.ToString();
            }

            public int IndexFromKey(string id)
            {
                return int.Parse(id);
            }
        }

        class CustomEnumerable : IEnumerable<object>
        {
            private List<string> myList = new List<string>();
            
            public CustomEnumerable()
            {
                myList.Add("text");
                myList.Add(null);
                myList.Add("foobar");
                myList.Add("WinUI is awesome");
            }

            public IEnumerator<object> GetEnumerator()
            {
                return myList.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return myList.GetEnumerator();
            }
        }
    }
}
