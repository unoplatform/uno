using System;
using System.Linq;

namespace Windows.ApplicationModel.DataTransfer.DragDrop.Core
{
	internal interface IDragDropExtension
	{
		void StartNativeDrag(CoreDragInfo info);
	}
}
