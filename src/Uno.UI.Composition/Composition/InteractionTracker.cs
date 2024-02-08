#nullable enable

using System.Numerics;
using System.Threading;
using Uno.UI.Dispatching;
using Windows.Foundation;

namespace Microsoft.UI.Composition.Interactions;

public partial class InteractionTracker : CompositionObject
{
	private InteractionTrackerState _state;
	private Vector3 _position;

	private int _currentRequestId;

	private InteractionTracker(Compositor compositor, IInteractionTrackerOwner? owner = null) : base(compositor)
	{
		_state = new InteractionTrackerIdleState(this);
		Owner = owner;
		InteractionSources = new CompositionInteractionSourceCollection(compositor, this);
	}

	public IInteractionTrackerOwner? Owner { get; }

	public float MinScale { get; set; } = 1.0f;

	public float MaxScale { get; set; } = 1.0f;

	public float Scale { get; } = 1.0f;

	public Vector3 MinPosition { get; set; }

	public Vector3 MaxPosition { get; set; }

	public Vector3? PositionInertiaDecayRate { get; set; }

	public Vector3 Position => _position;

	public CompositionInteractionSourceCollection InteractionSources { get; }

	public int CurrentRequestId => _currentRequestId;

	public static InteractionTracker Create(Compositor compositor) => new InteractionTracker(compositor);

	public static InteractionTracker CreateWithOwner(Compositor compositor, IInteractionTrackerOwner owner) => new InteractionTracker(compositor, owner);

	internal void ChangeState(InteractionTrackerState newState)
	{
		_state.Dispose();
		_state = newState;
	}

	internal void SetPosition(Vector3 newPosition, bool isFromUserManipulation)
	{
		if (_position != newPosition)
		{
			_position = newPosition;
			int requestId = isFromUserManipulation ? 0 : _currentRequestId;
			if (Owner is { } owner)
			{
				NativeDispatcher.Main.Enqueue(() =>
				{
					owner.ValuesChanged(this, new InteractionTrackerValuesChangedArgs(Position, Scale, requestId));
				});
			}

			OnPropertyChanged(nameof(Position), isSubPropertyChange: false);
		}
	}

	public int TryUpdatePositionWithAdditionalVelocity(Vector3 velocityInPixelsPerSecond)
	{
		Interlocked.Increment(ref _currentRequestId);
		_state.TryUpdatePositionWithAdditionalVelocity(velocityInPixelsPerSecond);
		return _currentRequestId;
	}

	internal void StartUserManipulation()
	{
		_state.StartUserManipulation();
	}

	internal void CompleteUserManipulation(Vector3 linearVelocity)
	{
		_state.CompleteUserManipulation(-linearVelocity);
	}

	internal void ReceiveManipulationDelta(Point translationDelta)
	{
		_state.ReceiveManipulationDelta(-translationDelta);
	}

	internal void ReceiveInertiaStarting(Point linearVelocity)
	{
		_state.ReceiveInertiaStarting(-linearVelocity);
	}

	public int TryUpdatePosition(Vector3 value)
		=> TryUpdatePosition(value, InteractionTrackerClampingOption.Auto);

	public int TryUpdatePositionBy(Vector3 amount)
		=> TryUpdatePosition(Position + amount);

	public int TryUpdatePosition(Vector3 value, InteractionTrackerClampingOption option)
	{
		Interlocked.Increment(ref _currentRequestId);
		_state.TryUpdatePosition(value, option);
		return _currentRequestId;
	}

	public int TryUpdatePositionBy(Vector3 amount, InteractionTrackerClampingOption option)
		=> TryUpdatePosition(Position + amount, option);
}
