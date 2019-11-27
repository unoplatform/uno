using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using Uno.UI.Xaml;

namespace Windows.UI.Xaml
{
	[DebuggerDisplay("{" + nameof(Name) + "}")]
	public partial class RoutedEvent
	{
		public RoutedEvent([CallerMemberName] string name = null)
			: this(RoutedEventFlag.None, name)
		{
		}

		internal RoutedEvent(
			RoutedEventFlag flag,
			[CallerMemberName] string name = null)
		{
			Flag = flag;
			Name = name;

			IsPointerEvent = flag.IsPointerEvent();
			IsKeyEvent = flag.IsKeyEvent();
			IsFocusEvent = flag.IsFocusEvent();
			IsManipulationEvent = flag.IsManipulationEvent();
			IsGestureEvent = flag.IsGestureEvent();
		}

		internal string Name { get; }

		internal RoutedEventFlag Flag { get; }

		[Pure]
		internal bool IsPointerEvent { get; }
		[Pure]
		internal bool IsKeyEvent { get; }
		[Pure]
		internal bool IsFocusEvent { get; }
		[Pure]
		internal bool IsManipulationEvent { get; }
		[Pure]
		internal bool IsGestureEvent { get; }
	}
}
