using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using Uno.UI.Xaml;

namespace Microsoft.UI.Xaml
{
	[DebuggerDisplay("{" + nameof(Name) + "}")]
	public partial class RoutedEvent
	{
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
			IsDragAndDropEvent = flag.IsDragAndDropEvent();

			IsTunnelingEvent = flag.IsTunnelingEvent();
		}

		[Pure]
		internal string Name { get; }

		[Pure]
		internal RoutedEventFlag Flag { get; }

		/// <summary>
		/// Determines if this event is bubbled to the parent, not matter is a parent is subscribed
		/// to the handler with the flag handledEventToo or not.
		/// </summary>
		/// <remarks>
		/// For some events like PointerEvent, we always needs to get the full events sequence to maintain the
		/// internal state, so we always needs the handled events too.
		/// 
		/// This flag avoids the complex update of the SubscribedToHandledEventsToo property coercing and inheritance
		/// for those kind of well-known events.
		/// 
		/// Basically all routed events that are implementing the 'PrepareManaged***EventBubbling' to maintain local
		/// state should opt-in for that.
		/// </remarks>
		[Pure]
		internal bool IsTunnelingEvent { get; }
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
		[Pure]
		internal bool IsDragAndDropEvent { get; }

		/// <inheritdoc />
		public override string ToString()
			=> Name;
	}
}
