using System.Numerics;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Composition;

internal partial class Given_InteractionTracker
{
	private static class TrackerLogsConstructingHelper
	{
		public static string GetCustomAnimationStateEntered(Vector3 trackerPosition, int requestId)
			=> $"CustomAnimationStateEntered: Position: {trackerPosition}, RequestId: {requestId}";

		public static string GetIdleStateEntered(Vector3 trackerPosition, int requestId)
			=> $"IdleStateEntered: Position: {trackerPosition}, RequestId: {requestId}";

		public static string GetInertiaStateEntered(Vector3 trackerPosition, int requestId, Vector3 naturalRestingPosition, Vector3? modifiedRestingPosition, Vector3 positionVelocityInPixelsPerSecond)
			=> $"InertiaStateEntered: Position: {trackerPosition}, RequestId: {requestId}, NaturalRestingPosition: {naturalRestingPosition}, ModifiedRestingPosition: {modifiedRestingPosition}, PositionVelocityInPixelsPerSecond: {positionVelocityInPixelsPerSecond}";

		public static string GetInteractingStateEntered(Vector3 trackerPosition, int requestId)
			=> $"InteractingStateEntered: Position: {trackerPosition}, RequestId: {requestId}";

		public static string GetRequestIgnored(Vector3 trackerPosition, int requestId)
			=> $"RequestIgnored: Position: {trackerPosition}, RequestId: {requestId}";

		public static string GetValuesChanged(Vector3 trackerPosition, int requestId, Vector3 argsPosition)
			=> $"ValuesChanged: Position: {trackerPosition}, RequestId: {requestId}, argsPosition: {argsPosition}";
	}
}
