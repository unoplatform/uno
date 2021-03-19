// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.


#pragma region ISwipeItemInvokedEventArgs
winrt.SwipeControl SwipeControl()
{
    return m_swipeControl;
}
#pragma endregion

void SwipeControl( winrt.SwipeControl& value)
{
    m_swipeControl = value;
}
