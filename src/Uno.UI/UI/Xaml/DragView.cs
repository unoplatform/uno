#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;

namespace Windows.UI.Xaml
{
	internal class DragView : Control
	{
		private readonly Stack<DragUIOverride> _overrides = new Stack<DragUIOverride>();

		public DragView(DragUI? ui)
		{
			
		}

		public void SetLocation(Point location)
		{

		}

		public void Stack(DragUIOverride @override)
		{
			_overrides.Push(@override);
		}

		public void Pop()
		{
			_overrides.Pop();
		}
	}
}
