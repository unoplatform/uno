#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Input.Preview.Injection;

namespace Uno.UITest;

public partial class RuntimeTestsApp
{
	private class MouseHelper
	{
		private readonly InputInjector _input;

		public MouseHelper(InputInjector input)
		{
			_input = input;
		}

		public InjectedInputMouseInfo Press()
			=> new()
			{
				TimeOffsetInMilliseconds = 1,
				MouseOptions = InjectedInputMouseOptions.LeftDown,
			};

		public InjectedInputMouseInfo Release()
			=> new()
			{
				TimeOffsetInMilliseconds = 1,
				MouseOptions = InjectedInputMouseOptions.LeftUp,
			};

		public InjectedInputMouseInfo? ReleaseAny()
		{
			var options = default(InjectedInputMouseOptions);

#if HAS_UNO
			var current = _input.Mouse;
			if (current.Properties.IsLeftButtonPressed)
			{
				options |= InjectedInputMouseOptions.LeftUp;
			}

			if (current.Properties.IsMiddleButtonPressed)
			{
				options |= InjectedInputMouseOptions.MiddleUp;
			}

			if (current.Properties.IsRightButtonPressed)
			{
				options |= InjectedInputMouseOptions.RightUp;
			}

			if (current.Properties.IsXButton1Pressed)
			{
				options |= InjectedInputMouseOptions.XUp;
			}
#else
			options = InjectedInputMouseOptions.LeftUp
				| InjectedInputMouseOptions.MiddleUp
				| InjectedInputMouseOptions.RightUp
				| InjectedInputMouseOptions.XUp;
#endif

			return options is default(InjectedInputMouseOptions)
				? null
				: new()
				{
					TimeOffsetInMilliseconds = 1,
					MouseOptions = options
				};
		}

		public InjectedInputMouseInfo MoveBy(int deltaX, int deltaY)
			=> new()
			{
				DeltaX = deltaX,
				DeltaY = deltaY,
				TimeOffsetInMilliseconds = 1,
				MouseOptions = InjectedInputMouseOptions.MoveNoCoalesce,
			};

		public IEnumerable<InjectedInputMouseInfo> MoveTo(double x, double y, int? steps = null)
		{
			Point Current()
#if HAS_UNO
				=> _input.Mouse.Position;
#else
				=> CoreWindow.GetForCurrentThread().PointerPosition;
#endif

			var x0 = Current().X;
			var y0 = Current().Y;
			var deltaX = x - x0;
			var deltaY = y - y0;

			steps ??= (int)Math.Min(Math.Max(Math.Abs(deltaX), Math.Abs(deltaY)), 512);
			if (steps is 0)
			{
				yield break;
			}

			// Could probably use Bresenham's algorithm if performance issues appear
			var stepX = deltaX / steps.Value;
			var stepY = deltaY / steps.Value;

			var prevPositionX = (int)Math.Round(x0);
			var prevPositionY = (int)Math.Round(y0);

			for (var i = 1; i <= steps; i++)
			{
				var newPositionX = (int)Math.Round(x0 + i * stepX);
				var newPositionY = (int)Math.Round(y0 + i * stepY);

				yield return MoveBy(newPositionX - prevPositionX, newPositionY - prevPositionY);

				prevPositionX = newPositionX;
				prevPositionY = newPositionY;
			}
		}
	}
}
