#nullable enable

using System;
using System.Numerics;

namespace Microsoft.UI.Composition.Interactions;

internal sealed partial class InteractionTrackerActiveInputInertiaHandler : InteractionTrackerFrameInertiaHandler
{
	private readonly AxisHelper _xHelper;
	private readonly AxisHelper _yHelper;
	private readonly AxisHelper _zHelper;

	/// <summary>Seconds since the motion started, as of the frame being processed.</summary>
	internal float ElapsedInSeconds { get; private set; }

	public override Vector3 InitialVelocity => new Vector3(_xHelper.InitialVelocity, _yHelper.InitialVelocity, _zHelper.InitialVelocity);
	public override Vector3 FinalPosition => new Vector3(_xHelper.FinalValue, _yHelper.FinalValue, _zHelper.FinalValue);
	public override Vector3 FinalModifiedPosition => new Vector3(_xHelper.FinalModifiedValue, _yHelper.FinalModifiedValue, _zHelper.FinalModifiedValue);

	public InteractionTrackerActiveInputInertiaHandler(InteractionTracker interactionTracker, Vector3 translationVelocities, int requestId)
		: base(interactionTracker, requestId)
	{
		_xHelper = new AxisHelper(this, translationVelocities, Axis.X);
		_yHelper = new AxisHelper(this, translationVelocities, Axis.Y);
		_zHelper = new AxisHelper(this, translationVelocities, Axis.Z);
	}

	protected override void Advance(long elapsedTicks)
	{
		ElapsedInSeconds = (float)(elapsedTicks / (double)TimeSpan.TicksPerSecond);

		if (_xHelper.HasCompleted && _yHelper.HasCompleted && _zHelper.HasCompleted)
		{
			Complete();
			return;
		}

		var newPosition = new Vector3(
			_xHelper.GetPosition(ElapsedInSeconds),
			_yHelper.GetPosition(ElapsedInSeconds),
			_zHelper.GetPosition(ElapsedInSeconds));

		InteractionTracker.SetPosition(newPosition, RequestId);
	}
}
