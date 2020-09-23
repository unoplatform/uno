#nullable enable

using System;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Input;

namespace Windows.ApplicationModel.DataTransfer.DragDrop.Core
{
	public partial class CoreDragInfo 
	{
		private readonly CoreWindow _window;
		private readonly Action<DataPackageOperation> _complete;

		internal CoreDragInfo(
			CoreWindow window,
			DataPackageView data,
			DataPackageOperation allowedOperations,
			Action<DataPackageOperation> complete)
		{
			_window = window;
			_complete = complete;
			Data = data;
			AllowedOperations = allowedOperations;
		}

		public DataPackageView Data { get; }

		public DataPackageOperation AllowedOperations { get; }

		public DragDropModifiers Modifiers
		{
			get
			{
				var mods = DragDropModifiers.None;
				if (_window.LastPointerEvent is {} args)
				{
					var props = args.GetLocation(null).Properties;
					if (props.IsLeftButtonPressed)
					{
						mods |= DragDropModifiers.LeftButton;
					}
					if (props.IsMiddleButtonPressed)
					{
						mods |= DragDropModifiers.MiddleButton;
					}
					if (props.IsRightButtonPressed)
					{
						mods |= DragDropModifiers.RightButton;
					}
				}

				if (_window.GetAsyncKeyState(VirtualKey.Shift) == CoreVirtualKeyStates.Down)
				{
					mods |= DragDropModifiers.Shift;
				}
				if (_window.GetAsyncKeyState(VirtualKey.Control) == CoreVirtualKeyStates.Down)
				{
					mods |= DragDropModifiers.Control;
				}
				if (_window.GetAsyncKeyState(VirtualKey.Menu) == CoreVirtualKeyStates.Down)
				{
					mods |= DragDropModifiers.Alt;
				}

				return mods;
			}
		}

		public Point Position => _window.PointerPosition;

		internal void Complete(DataPackageOperation result)
			=> _complete(result);
	}
}
