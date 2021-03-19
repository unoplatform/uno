// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#pragma once

namespace Windows.UI.Xaml.Controls
{
	internal partial class SwipeItems
	{
		class SwipeItems :
    public ReferenceTracker<SwipeItems, winrt.implementation.SwipeItemsT, winrt.IObservableVector<winrt.SwipeItem>>,
    public SwipeItemsProperties
{
public:
    SwipeItems();

#pragma region IVector
    winrt.SwipeItem GetAt(uint index);
    uint Size();
    winrt.IVectorView<winrt.SwipeItem> GetView();
    bool IndexOf(winrt.SwipeItem & value, uint32_t& index);
    void SetAt(uint index, winrt.SwipeItem & value);
    void InsertAt(uint index, winrt.SwipeItem & value);
    void RemoveAt(uint index);
    void Append(winrt.SwipeItem & value);
    void RemoveAtEnd();
    void Clear();

    // TODO:
    winrt.IIterator<winrt.SwipeItem> First() { throw winrt.hresult_not_implemented(); }
    uint GetMany(uint startIndex, winrt.array_view<winrt.SwipeItem> items) { throw winrt.hresult_not_implemented(); }
    void ReplaceAll(winrt.array_view<winrt.SwipeItem > items) { throw winrt.hresult_not_implemented(); }
#pragma endregion

#pragma region IObservableVector
    winrt.event_token VectorChanged(winrt.VectorChangedEventHandler<winrt.SwipeItem> & handler);
    void VectorChanged(winrt.event_token  token);
#pragma endregion

    void OnPropertyChanged( winrt.DependencyPropertyChangedEventArgs& args);

private:
    void put_Items( winrt.Collections.IVector<winrt.SwipeItem>& value);
    tracker_ref<winrt.Collections.IVector<winrt.SwipeItem>> m_items{ this };

    event_source<winrt.VectorChangedEventHandler<winrt.SwipeItem>> m_vectorChangedEventSource{ this };
};

	}
}
