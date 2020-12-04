// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.


using namespace DirectUI;
using namespace DirectUISynonyms;

// Work around disruptive max/min macros
#undef max
#undef min

// DataVirtualization - to avoid creating large number of calendar items,
// we only implement GetAt and get_Size.

// IVector<DependencyObject> implementation
CalendarViewGeneratorHost.GetAt( Uint index, out  DependencyObject item)
{
    DateTime date;

    date = GetDateAt(index);

    PropertyValue.CreateFromDateTime(date, item);

}

CalendarViewGeneratorHost.get_Size(out UINT value)
{
    value = m_size;
    return;
}

CalendarViewGeneratorHost.GetView(out result_maybenull_ wfc.IVectorView<DependencyObject>** view)
{
    wfc.IVectorView<DependencyObject> spResult;

    CheckThread();
    ARG_VALIDRETURNPOINTER(view);

    ctl.ComObject<TrackerView<DependencyObject>>.CreateInstance(spResult.ReleaseAn());
    spResult as TrackerView<DependencyObject>.SetCollection(this);

    view = spResult.Detach();

}

CalendarViewGeneratorHost.IndexOf( DependencyObject value, out UINT index, out BOOLEAN found)
{
    throw new NotImplementedException();
}

CalendarViewGeneratorHost.SetAt( Uint index,  DependencyObject item)
{
    throw new NotImplementedException();
}

CalendarViewGeneratorHost.InsertAt( Uint index,  DependencyObject item)
{
    throw new NotImplementedException();
}

CalendarViewGeneratorHost.RemoveAt( Uint index)
{
    throw new NotImplementedException();
}

CalendarViewGeneratorHost.Append( DependencyObject item)
{
    throw new NotImplementedException();
}

CalendarViewGeneratorHost.RemoveAtEnd()
{
    throw new NotImplementedException();
}

CalendarViewGeneratorHost.Clear()
{
    throw new NotImplementedException();
}
