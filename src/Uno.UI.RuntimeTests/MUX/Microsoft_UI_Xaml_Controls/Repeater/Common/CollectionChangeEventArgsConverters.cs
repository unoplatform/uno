// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Windows.Foundation.Collections;
using Windows.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Tests.MUXControls.ApiTests.RepeaterTests.Common
{
    public static class CollectionChangeEventArgsConverters
    {
        public static NotifyCollectionChangedEventArgs ConvertToDataSourceChangedEventArgs(this IVectorChangedEventArgs args)
        {
            NotifyCollectionChangedEventArgs newArgs = null;

            switch (args.CollectionChange)
            {
                case CollectionChange.ItemInserted:
                    List<object> addedItems = new List<object>();
                    addedItems.Add(null);
                    newArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, addedItems, args.Index);
                    break;
                case CollectionChange.ItemRemoved:
                    List<object> removedItems = new List<object>();
                    removedItems.Add(null);
                    newArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removedItems, args.Index);
                    break;
                case CollectionChange.ItemChanged:
                    newArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, null, args.Index);
                    break;
                case CollectionChange.Reset:
                    newArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
                    break;
                default:
                    throw new InvalidOperationException();
            }

            return newArgs;
        }

        public static NotifyCollectionChangedEventArgs CreateNotifyArgs(
            NotifyCollectionChangedAction action,
            int oldStartingIndex,
            int oldItemsCount,
            int newStartingIndex,
            int newItemsCount)
        {
            NotifyCollectionChangedEventArgs newArgs = null;

            switch (action)
            {
                case NotifyCollectionChangedAction.Add:
                    {
                        List<object> addedItems = new List<object>();
                        for (int i = 0; i < newItemsCount; i++)
                        {
                            addedItems.Add(null);
                        }

                        newArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, addedItems, newStartingIndex);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    {
                        List<object> removedItems = new List<object>();
                        for (int i = 0; i < oldItemsCount; i++)
                        {
                            removedItems.Add(null);
                        }
                        newArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removedItems, oldStartingIndex);
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    {
                        List<object> addedItems = new List<object>();
                        for (int i = 0; i < newItemsCount; i++)
                        {
                            addedItems.Add(null);
                        }
                        List<object> removedItems = new List<object>();
                        for (int i = 0; i < oldItemsCount; i++)
                        {
                            removedItems.Add(null);
                        }
                        newArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, addedItems, removedItems, oldStartingIndex);
                    }
                    break;
                case NotifyCollectionChangedAction.Move:
                    {
                        List<object> movedItems = new List<object>();
                        for (int i = 0; i < oldItemsCount; i++)
                        {
                            movedItems.Add(null);
                        }
                        newArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, movedItems, newStartingIndex, oldStartingIndex);
                    }
                  break;
                case NotifyCollectionChangedAction.Reset:
                    newArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
                    break;
                default:
                    throw new InvalidOperationException();
            }

            return newArgs;
        }
    }
}
