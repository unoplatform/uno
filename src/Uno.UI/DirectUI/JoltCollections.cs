//// Copyright (c) Microsoft Corporation. All rights reserved.
//// Licensed under the MIT License. See LICENSE in the project root for license information.


//using namespace DirectUI;

//#pragma region View And Non-DO-Backed Collections

//View<DependencyObject>.IndexOf(
//     DependencyObject value, 
//    out Uint index, 
//    out bool found)
//{
//    std.list<DependencyObject>.iterator it;
//    Uint nPosition = 0;

//    CheckThread();
//    for (it = m_list.begin(); it != m_list.end(); ++nPosition, ++it)
//    {
//        bool areEqual = false;
//        areEqual = PropertyValue.AreEqual(value, it);
//        if (areEqual)
//        {
//            index = nPosition;
//            found = true;
//            goto Cleanup;
//        }
//    }

//    index = 0;
//    found = false;
//    S_false;

//}

//#pragma endregion

//#pragma region DO-backed Value Collection Specializations
//IFACEMETHODIMP
//PresentationFrameworkCollection<FLOAT>.Append( FLOAT item)
//{
//    CValue boxedValue;

//    CheckThread();
//    CValueBoxer.BoxValue(&boxedValue, item);

//    IFC(Collection_Add(
//        (CCollection)(GetHandle()),
//        &boxedValue));

//}


//PresentationFrameworkCollection<FLOAT>.GetAt(
//     Uint index, 
//    out FLOAT item)
//{
//    CValue value;
//    Xint nIndex = (XINT32)(index);
    
//    CheckThread();
//    value = .Collection_GetItem((CCollection)(GetHandle()), nIndex);
//    CValueBoxer.UnboxValue(&value, item);

//}

//IFACEMETHODIMP
//PresentationFrameworkCollection<FLOAT>.IndexOf( FLOAT value, out Uint index, out bool found)
//{
//    HRESULT hr = S_false;
//    CValue boxedValue;
//    Xint coreIndex = -1;

//    CheckThread();
//    CValueBoxer.BoxValue(&boxedValue, value);

//    if (SUCCEEDED(Collection_IndexOf(
//        (CCollection)(GetHandle()),
//        &boxedValue,
//        &coreIndex)))
//    {
//        index = (UINT)(coreIndex);
//    }
//    else
//    {
//        S_false;
//    }

//    found = coreIndex != -1;

//}

//IFACEMETHODIMP
//PresentationFrameworkCollection<FLOAT>.InsertAt( Uint index,  FLOAT item)
//{
//    CValue boxedValue;

//    CheckThread();
//    CValueBoxer.BoxValue(&boxedValue, item);

//    IFC(Collection_Insert(
//        (CCollection)(GetHandle()),
//        index,
//        &boxedValue));
            
//}

//IFACEMETHODIMP
//PresentationFrameworkCollection<DOUBLE>.Append( DOUBLE item)
//{
//    CValue boxedValue;

//    CheckThread();
//    CValueBoxer.BoxValue(&boxedValue, item);

//    IFC(Collection_Add(
//        (CCollection)(GetHandle()),
//        &boxedValue));

//}


//PresentationFrameworkCollection<DOUBLE>.GetAt(
//     Uint index, 
//    out DOUBLE item)
//{
//    CValue value;
//    Xint nIndex = (XINT32)(index);
           
//    CheckThread();
//    value = .Collection_GetItem((CCollection)(GetHandle()), nIndex);
//    CValueBoxer.UnboxValue(&value, item);

//}

//IFACEMETHODIMP
//PresentationFrameworkCollection<DOUBLE>.IndexOf( DOUBLE value, out Uint index, out bool found)
//{
//    HRESULT hr = S_false;
//    CValue boxedValue;
//    Xint coreIndex = -1;

//    CheckThread();
//    CValueBoxer.BoxValue(&boxedValue, value);
                
//    if (SUCCEEDED(Collection_IndexOf(
//        (CCollection)(GetHandle()),
//        &boxedValue,
//        &coreIndex)))
//    {
//        index = (UINT)(coreIndex);
//    }
//    else
//    {
//        S_false;
//    }

//    found = coreIndex != -1;

//}

//IFACEMETHODIMP
//PresentationFrameworkCollection<DOUBLE>.InsertAt( Uint index,  DOUBLE item)
//{
//    CValue boxedValue;

//    CheckThread();
//    CValueBoxer.BoxValue(&boxedValue, item);

//    IFC(Collection_Insert(
//        (CCollection)(GetHandle()),
//        index,
//        &boxedValue));
            
//}

//IFACEMETHODIMP
//PresentationFrameworkCollection<wf.Point>.Append( wf.Point item)
//{
//    CValue boxedValue;
//    BoxerBuffer buffer;

//    CheckThread();
//    buffer = CValueBoxer.BoxValue(&boxedValue, item);

//    IFC(Collection_Add(
//        (CCollection)(GetHandle()),
//        &boxedValue));

//}


//PresentationFrameworkCollection<wf.Point>.GetAt(
//     Uint index, 
//    out wf.Point item)
//{
//    CValue value;
//    Xint nIndex = (XINT32)(index);

//    CheckThread();
//    value = .Collection_GetItem((CCollection)(GetHandle()), nIndex);
//    CValueBoxer.UnboxValue(&value, item);

//}

//IFACEMETHODIMP
//PresentationFrameworkCollection<wf.Point>.IndexOf( wf.Point value, out Uint index, out bool found)
//{
//    HRESULT hr = S_false;
//    CValue boxedValue;
//    BoxerBuffer buffer;
//    Xint coreIndex = -1;

//    CheckThread();
//    buffer = CValueBoxer.BoxValue(&boxedValue, value);

//    if (SUCCEEDED(Collection_IndexOf(
//        (CCollection)(GetHandle()),
//        &boxedValue,
//        &coreIndex)))
//    {
//        index = (UINT)(coreIndex);
//    }
//    else
//    {
//        S_false;
//    }

//    found = coreIndex != -1;

//}

//IFACEMETHODIMP
//PresentationFrameworkCollection<wf.Point>.InsertAt( Uint index,  wf.Point item)
//{
//    CValue boxedValue;
//    BoxerBuffer buffer;

//    CheckThread();
//    buffer = CValueBoxer.BoxValue(&boxedValue, item);

//    IFC(Collection_Insert(
//        (CCollection)(GetHandle()),
//        index,
//        &boxedValue));
            
//}

//IFACEMETHODIMP
//PresentationFrameworkCollection<xaml_docs.TextRange>.Append( xaml_docs.TextRange item)
//{
//    CValue boxedValue;
//    BoxerBuffer buffer;

//    CheckThread();
//    buffer = CValueBoxer.BoxValue(&boxedValue, item);

//    IFC(Collection_Add(
//        (CCollection)(GetHandle()),
//        &boxedValue));

//}

//IFACEMETHODIMP
//PresentationFrameworkCollection<xaml_docs.TextRange>.GetAt(
//     Uint index,
//    out xaml_docs.TextRange item)
//{
//    CValue value;
//    Xint nIndex = (XINT32)(index);

//    CheckThread();
//    value = .Collection_GetItem((CCollection)(GetHandle()), nIndex);
//    CValueBoxer.UnboxValue(&value, item);

//}

//IFACEMETHODIMP
//PresentationFrameworkCollection<xaml_docs.TextRange>.IndexOf( xaml_docs.TextRange value, out Uint index, out bool found)
//{
//    HRESULT hr = S_false;
//    CValue boxedValue;
//    BoxerBuffer buffer;
//    Xint coreIndex = -1;

//    CheckThread();
//    buffer = CValueBoxer.BoxValue(&boxedValue, value);

//    if (SUCCEEDED(Collection_IndexOf(
//        (CCollection)(GetHandle()),
//        &boxedValue,
//        &coreIndex)))
//    {
//        index = (UINT)(coreIndex);
//    }
//    else
//    {
//        S_false;
//    }

//    found = coreIndex != -1;

//}

//IFACEMETHODIMP
//PresentationFrameworkCollection<xaml_docs.TextRange>.InsertAt( Uint index,  xaml_docs.TextRange item)
//{
//    CValue boxedValue;
//    BoxerBuffer buffer;

//    CheckThread();
//    buffer = CValueBoxer.BoxValue(&boxedValue, item);

//    IFC(Collection_Insert(
//        (CCollection)(GetHandle()),
//        index,
//        &boxedValue));

//}

//namespace DirectUI
//{
//    bool UntypedTryGetIndexOf( IUntypedVector vector,  DependencyObject item, out uint * index)
//    {
//        index = 0;
//        wrl.ComPtr<DependencyObject> itemIdentity;
//        IFCFAILFAST(item.QueryInterface(itemIdentity.ReleaseAn()));

//        bool found = false;
//        uint  size = 0, untypedIndex = 0;
//        IFCFAILFAST(vector.UntypedGetSize(&size));
//        while (untypedIndex < size && !found)
//        {
//            wrl.ComPtr<DependencyObject> itemAt;
//            IFCFAILFAST(vector.UntypedGetAt(untypedIndex, &itemAt));
//            found = (itemAt == itemIdentity);
//            ++untypedIndex;
//        }

//        if (found)
//        {
//            index = untypedIndex - 1;
//        }

//        return found;
//    }
//}

//#pragma endregion
