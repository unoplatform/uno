// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using Windows.UI.Xaml.Controls;
using System.Collections.Specialized;

namespace Windows.UI.Xaml.Tests.MUXControls.ApiTests.RepeaterTests.Common
{
    using ItemsSourceView = Microsoft.UI.Xaml.Controls.ItemsSourceView;
    using IKeyIndexMapping = Microsoft.UI.Xaml.Controls.IKeyIndexMapping;

    class CustomItemsSource : CustomItemsSourceView
    {
        List<int> _inner;
        public int GetAtCallCount { get; set; }

        public CustomItemsSource(List<int> source)
        {
            _inner = source;
        }

        public List<int> Inner { get { return _inner; } }

        public object GetAt(int index)
        {
            return GetAtCore(index);
        }

        public void Insert(int index, int count, bool reset, int valueStart = 1000)
        {
            for (int i = 0; i < count; i++)
            {
                Inner.Insert(index + i, valueStart + i);
            }

            if (reset)
            {
                OnItemsSourceChanged(CollectionChangeEventArgsConverters.CreateNotifyArgs(NotifyCollectionChangedAction.Reset, -1, -1, -1, -1));
            }
            else
            {
                OnItemsSourceChanged(CollectionChangeEventArgsConverters.CreateNotifyArgs(
                    NotifyCollectionChangedAction.Add,
                    oldStartingIndex: -1,
                    oldItemsCount: -1,
                    newStartingIndex: index,
                    newItemsCount: count));
            }
        }

        public void Remove(int index, int count, bool reset)
        {
            for (int i = 0; i < count; i++)
            {
                Inner.RemoveAt(index);
            }

            if (reset)
            {
                OnItemsSourceChanged(CollectionChangeEventArgsConverters.CreateNotifyArgs(NotifyCollectionChangedAction.Reset, -1, -1, -1, -1));
            }
            else
            {
                OnItemsSourceChanged(CollectionChangeEventArgsConverters.CreateNotifyArgs(
                    NotifyCollectionChangedAction.Remove,
                    oldStartingIndex: index,
                    oldItemsCount: count,
                    newStartingIndex: -1,
                    newItemsCount: -1));
            }
        }

        public void Replace(int index, int oldCount, int newCount, bool reset)
        {
            for (int i = 0; i < oldCount; i++)
            {
                Inner.RemoveAt(index);
            }

            for (int i = 0; i < newCount; i++)
            {
                Inner.Insert(index, 10000 + i);
            }

            if (reset)
            {
                OnItemsSourceChanged(CollectionChangeEventArgsConverters.CreateNotifyArgs(NotifyCollectionChangedAction.Reset, -1, -1, -1, -1));
            }
            else
            {
                OnItemsSourceChanged(CollectionChangeEventArgsConverters.CreateNotifyArgs(
                    NotifyCollectionChangedAction.Replace,
                    oldStartingIndex: index,
                    oldItemsCount: oldCount,
                    newStartingIndex: index,
                    newItemsCount: newCount));
            }
        }

        public void Move(int oldIndex, int newIndex, int count, bool reset)
        {
            var items = Inner.GetRange(oldIndex, count);
            Inner.RemoveRange(oldIndex, count);
            Inner.InsertRange(newIndex, items);

            if (reset)
            {
                OnItemsSourceChanged(CollectionChangeEventArgsConverters.CreateNotifyArgs(NotifyCollectionChangedAction.Reset, -1, -1, -1, -1));
            }
            else
            {
                OnItemsSourceChanged(CollectionChangeEventArgsConverters.CreateNotifyArgs(
                    NotifyCollectionChangedAction.Move,
                    oldStartingIndex: oldIndex,
                    oldItemsCount: count,
                    newStartingIndex: newIndex,
                    newItemsCount: count));
            }
        }

        public void Reset()
        {
            Random rand = new Random(123);
            for (int i = 0; i < 10; i++)
            {
                int from = rand.Next(0, Inner.Count - 1);
                var value = Inner[from];
                Inner.RemoveAt(from);
                int to = rand.Next(0, Inner.Count - 1);
                Inner.Insert(to, value);
            }

            // something changed, but i don't want to tell you the 
            // exact changes 
            OnItemsSourceChanged(CollectionChangeEventArgsConverters.CreateNotifyArgs(NotifyCollectionChangedAction.Reset, -1, -1, -1, -1));
        }

        public new void Clear()
        {
            Inner.Clear();
            // something changed, but i don't want to tell you the exact changes 
            OnItemsSourceChanged(CollectionChangeEventArgsConverters.CreateNotifyArgs(NotifyCollectionChangedAction.Reset, -1, -1, -1, -1));
        }

        protected override int GetSizeCore()
        {
            return Inner.Count;
        }

        protected override object GetAtCore(int index)
        {
            GetAtCallCount++;
            return Inner[index];
        }
    }

    class CustomItemsSourceWithUniqueId : CustomItemsSource, IKeyIndexMapping
    {
        public CustomItemsSourceWithUniqueId(List<int> source) : base(source)
        { }

        public string KeyFromIndex(int index)
        {
            return Inner[index].ToString();
        }

        public int IndexFromKey(string id)
        {
            throw new NotImplementedException();
        }
    }
}
