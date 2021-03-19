// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.


SwipeItems()
{
    // create the Collection
    var collection = winrt.new Vector<winrt.SwipeItem, MakeVectorParam<VectorFlag.DependencyObjectBase>()>();

    put_Items(collection);
}

void OnPropertyChanged( winrt.DependencyPropertyChangedEventArgs& args)
{
    if (args.Property() == s_ModeProperty)
    {
        if (winrt.unbox_value<winrt.SwipeMode>(args.NewValue()) == winrt.SwipeMode.Execute && m_items.get().Size() > 1)
        {
            throw winrt.hresult_invalid_argument("Execute items should only have one item.");
        }
    }
}

void SwipeItems.put_Items(
     winrt.Collections.IVector<winrt.SwipeItem>& value)
{
    if (Mode() == winrt.SwipeMode.Execute && value.Size() > 1)
    {
        throw winrt.hresult_invalid_argument("Execute items should only have one item.");
    }

    m_items.set(value);
    m_vectorChangedEventSource(this, null);
}

winrt.SwipeItem GetAt(uint index)
{
    if (index >= m_items.get().Size())
    {
        throw winrt.hresult_out_of_bounds();
    }
    return m_items.get().GetAt(index);
}

uint Size()
{
    return m_items.get().Size();
}

bool IndexOf(winrt.SwipeItem & value, uint32_t& index)
{
    if (index >= m_items.get().Size())
    {
        throw winrt.hresult_out_of_bounds();
    }
    return m_items.get().IndexOf(value, index);
}

void SetAt(uint index, winrt.SwipeItem & value)
{
    if (index >= m_items.get().Size())
    {
        throw winrt.hresult_out_of_bounds();
    }
    m_items.get().SetAt(index, value);
    m_vectorChangedEventSource(this, null);
}

void InsertAt(uint index, winrt.SwipeItem & value)
{
    if (Mode() == winrt.SwipeMode.Execute && m_items.get().Size() > 0)
    {
        throw winrt.hresult_invalid_argument("Execute items should only have one item.");
    }
    if (index > m_items.get().Size())
    {
        throw winrt.hresult_out_of_bounds();
    }

    m_items.get().InsertAt(index, value);
    m_vectorChangedEventSource(this, null);
}

void RemoveAt(uint index)
{
    if (index >= m_items.get().Size())
    {
        throw winrt.hresult_out_of_bounds();
    }
    m_items.get().RemoveAt(index);
    m_vectorChangedEventSource(this, null);
}

void Append(winrt.SwipeItem & value)
{
    if (Mode() == winrt.SwipeMode.Execute && m_items.get().Size() > 0)
    {
        throw winrt.hresult_invalid_argument("Execute items should only have one item.");
    }
    
    m_items.get().Append(value);
    m_vectorChangedEventSource(this, null);
}

void RemoveAtEnd()
{
    m_items.get().RemoveAtEnd();
    m_vectorChangedEventSource(this, null);
}

void Clear()
{
    m_items.get().Clear();
    m_vectorChangedEventSource(this, null);
}

winrt.IVectorView<winrt.SwipeItem> GetView()
{
    return m_items.get().GetView();
}

winrt.event_token VectorChanged(winrt.VectorChangedEventHandler<winrt.SwipeItem> & handler)
{
    return m_vectorChangedEventSource.add(handler);
}

void VectorChanged(winrt.event_token  token)
{
    m_vectorChangedEventSource.remove(token);
}
