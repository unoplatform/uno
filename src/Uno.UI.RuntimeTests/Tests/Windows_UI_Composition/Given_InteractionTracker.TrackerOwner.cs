using System.Text;
using Windows.UI.Composition.Interactions;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Composition;

public partial class Given_InteractionTracker
{
	private sealed class TrackerOwner : IInteractionTrackerOwner
	{
		private readonly StringBuilder _log = new();

		public void CustomAnimationStateEntered(InteractionTracker sender, InteractionTrackerCustomAnimationStateEnteredArgs args)
			=> _log.AppendLine(TrackerLogsConstructingHelper.GetCustomAnimationStateEntered(sender.Position, args.RequestId));

		public void IdleStateEntered(InteractionTracker sender, InteractionTrackerIdleStateEnteredArgs args)
			=> _log.AppendLine(TrackerLogsConstructingHelper.GetIdleStateEntered(sender.Position, args.RequestId));

		public void InertiaStateEntered(InteractionTracker sender, InteractionTrackerInertiaStateEnteredArgs args)
			=> _log.AppendLine(TrackerLogsConstructingHelper.GetInertiaStateEntered(sender.Position, args.RequestId, args.NaturalRestingPosition, args.ModifiedRestingPosition, args.PositionVelocityInPixelsPerSecond));

		public void InteractingStateEntered(InteractionTracker sender, InteractionTrackerInteractingStateEnteredArgs args)
			=> _log.AppendLine(TrackerLogsConstructingHelper.GetInteractingStateEntered(sender.Position, args.RequestId));

		public void RequestIgnored(InteractionTracker sender, InteractionTrackerRequestIgnoredArgs args)
			=> _log.AppendLine(TrackerLogsConstructingHelper.GetRequestIgnored(sender.Position, args.RequestId));

		public void ValuesChanged(InteractionTracker sender, InteractionTrackerValuesChangedArgs args)
			=> _log.AppendLine(TrackerLogsConstructingHelper.GetValuesChanged(sender.Position, args.RequestId, args.Position));

		public string GetLogs() => _log.ToString();
	}
}
