using System;
using Windows.Foundation;
using Windows.Foundation.Metadata;

namespace Windows.ApplicationModel.DataTransfer.DragDrop
{
	[Flags]
	public enum DragDropModifiers : uint
	{
		None = 0U,

		Shift = 1U,
		Control = 2U,
		Alt = 4U,

		LeftButton = 8U,
		MiddleButton = 16U,
		RightButton = 32U,
	}
}
