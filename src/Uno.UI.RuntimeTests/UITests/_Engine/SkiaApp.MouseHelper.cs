#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Input.Preview.Injection;

namespace Uno.UITest;

public partial class SkiaApp
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

			var deltaX = x - Current().X;
			var deltaY = y - Current().Y;

			steps ??= (int)Math.Min(Math.Max(Math.Abs(deltaX), Math.Abs(deltaY)), 512);
			if (steps is 0)
			{
				yield break;
			}

			var stepX = deltaX / steps.Value;
			var stepY = deltaY / steps.Value;

			stepX = stepX is > 0 ? Math.Ceiling(stepX) : Math.Floor(stepX);
			stepY = stepY is > 0 ? Math.Ceiling(stepY) : Math.Floor(stepY);

			for (var step = 0; step <= steps && (stepX is not 0 || stepY is not 0); step++)
			{
				yield return MoveBy((int)stepX, (int)stepY);

				if (Math.Abs(Current().X - x) < stepX)
				{
					stepX = 0;
				}

				if (Math.Abs(Current().Y - y) < stepY)
				{
					stepY = 0;
				}
			}
		}
	}
}
