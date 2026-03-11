using System.Diagnostics;
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
			IsContextEvent = flag.IsContextEvent();

			IsTunnelingEvent = flag.IsTunnelingEvent();
		}

		internal string Name { get; }

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
		internal bool IsTunnelingEvent { get; }
		internal bool IsPointerEvent { get; }
		internal bool IsKeyEvent { get; }
		internal bool IsFocusEvent { get; }
		internal bool IsManipulationEvent { get; }
		internal bool IsGestureEvent { get; }
		internal bool IsDragAndDropEvent { get; }
		internal bool IsContextEvent { get; }

		/// <inheritdoc />
		public override string ToString()
			=> Name;
	}
}
