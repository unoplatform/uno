// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using static Microsoft/* UWP don't rename */.UI.Xaml.Controls._Tracing;

namespace Windows.UI.Xaml.Controls;

internal enum PointerOverStatus
{
	None = 0x00,
	Touch = 0x01,
	Pen = 0x02,
	Mouse = 0x04
}

internal enum PointerPressedStatus
{
	None = 0x00,
	LeftMouseButton = 0x01,
	RightMouseButton = 0x02,
	Pointer = 0x04
}


internal class PointerInfo<T>
{
	/* Use this constructor if pointer capturing is added through
       CapturedPointer/SetCapturedPointer/m_capturedPointer members below.
    public PointerInfo(const ITrackerHandleManager* owner) : m_owner(owner)
    {
    }
    */

	public PointerInfo()
	{
	}

	//~PointerInfo()
	//{
	//}

	public bool IsPointerOver()
	{
		return m_pointerOverStatus != PointerOverStatus.None;
	}

	public bool IsTouchPointerOver()
	{
		return (m_pointerOverStatus & PointerOverStatus.Touch) != 0;
	}

	public bool IsPenPointerOver()
	{
		return (m_pointerOverStatus & PointerOverStatus.Pen) != 0;
	}

	public bool IsMousePointerOver()
	{
		return (m_pointerOverStatus & PointerOverStatus.Mouse) != 0;
	}

	public void SetIsTouchPointerOver()
	{
		m_pointerOverStatus |= PointerOverStatus.Touch;
	}

	public void ResetIsTouchPointerOver()
	{
		m_pointerOverStatus &= ~PointerOverStatus.Touch;
	}

	public void SetIsPenPointerOver()
	{
		m_pointerOverStatus |= PointerOverStatus.Pen;
	}

	public void ResetIsPenPointerOver()
	{
		m_pointerOverStatus &= ~PointerOverStatus.Pen;
	}

	public void SetIsMousePointerOver()
	{
		m_pointerOverStatus |= PointerOverStatus.Mouse;
	}

	public void ResetIsMousePointerOver()
	{
		m_pointerOverStatus &= ~PointerOverStatus.Mouse;
	}

	public bool IsPressed()
	{
		return m_pointerPressedStatus != PointerPressedStatus.None;
	}

	public bool IsMouseButtonPressed(bool isForLeftMouseButton)
	{
		return isForLeftMouseButton ? (m_pointerPressedStatus & PointerPressedStatus.LeftMouseButton) != 0 : (m_pointerPressedStatus & PointerPressedStatus.RightMouseButton) != 0;
	}

	public void SetIsMouseButtonPressed(bool isForLeftMouseButton)
	{
		if (isForLeftMouseButton)
		{
			m_pointerPressedStatus |= PointerPressedStatus.LeftMouseButton;
		}
		else
		{
			m_pointerPressedStatus |= PointerPressedStatus.RightMouseButton;
		}
	}

	public void ResetIsMouseButtonPressed(bool isForLeftMouseButton)
	{
		if (isForLeftMouseButton)
		{
			m_pointerPressedStatus &= ~PointerPressedStatus.LeftMouseButton;
		}
		else
		{
			m_pointerPressedStatus &= ~PointerPressedStatus.RightMouseButton;
		}
	}

	public void SetPointerPressed()
	{
		m_pointerPressedStatus |= PointerPressedStatus.Pointer;
	}

	public void ResetPointerPressed()
	{
		m_pointerPressedStatus &= ~PointerPressedStatus.Pointer;
	}

	public bool IsTrackingPointer()
	{
		return m_trackedPointerId != 0;
	}

	public bool IsPointerIdTracked(uint pointerId)
	{
		return m_trackedPointerId == pointerId;
	}

	public void TrackPointerId(uint pointerId)
	{
		MUX_ASSERT(m_trackedPointerId == 0);

		m_trackedPointerId = pointerId;
	}

	public void ResetTrackedPointerId()
	{
		m_trackedPointerId = 0;
	}

	public void ResetAll()
	{
		m_trackedPointerId = 0;
		m_pointerOverStatus = PointerOverStatus.None;
		m_pointerPressedStatus = PointerPressedStatus.None;
	}

	/* Uncomment when pointer capturing becomes necessary.
	Pointer CapturedPointer() const
	{
		return m_capturedPointer;
	}

	void SetCapturedPointer(Pointer pointer)
	{
		m_capturedPointer = pointer;
	}
	*/

	//const ITrackerHandleManager* m_owner;
	//tracker_ref<winrt::Pointer> m_capturedPointer{ m_owner };
	private uint m_trackedPointerId;
	private PointerOverStatus m_pointerOverStatus = PointerOverStatus.None;
	private PointerPressedStatus m_pointerPressedStatus = PointerPressedStatus.None;
};
