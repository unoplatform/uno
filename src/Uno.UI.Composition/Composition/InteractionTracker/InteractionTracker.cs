#nullable enable

using System;
using System.Numerics;
using System.Threading;
using Uno.UI.Dispatching;
using Windows.Foundation;

using static Windows.UI.Composition.SubPropertyHelpers;

namespace Windows.UI.Composition.Interactions;

public partial class InteractionTracker : CompositionObject
{
	private InteractionTrackerState _state;
	private Vector3 _position;

	private int _currentRequestId;

	private InteractionTracker(Compositor compositor, IInteractionTrackerOwner? owner = null) : base(compositor)
	{
		_state = new InteractionTrackerIdleState(this, 0, isInitialIdleState: true);
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

	internal void SetPosition(Vector3 newPosition, int requestId)
	{
		if (_position != newPosition)
		{
			_position = newPosition;
			NativeDispatcher.Main.Enqueue(() =>
			{
				Owner?.ValuesChanged(this, new InteractionTrackerValuesChangedArgs(newPosition, Scale, requestId));
				OnPropertyChanged(nameof(Position), isSubPropertyChange: false);
			});
		}
	}

	public int TryUpdatePositionWithAdditionalVelocity(Vector3 velocityInPixelsPerSecond)
	{
		var id = Interlocked.Increment(ref _currentRequestId);
		_state.TryUpdatePositionWithAdditionalVelocity(velocityInPixelsPerSecond, id);
		return id;
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

	internal void ReceivePointerWheel(int mouseWheelTicks, bool isHorizontal)
	{
		// On WinUI, this depends on mouse setting "how many lines to scroll each time"
		// The default Windows setting is 3 lines, and each line is 16px.
		// Note: the value for each line may vary depending on scaling.
		// For now, we just use 16*3=48.
		var delta = mouseWheelTicks * 48;
		_state.ReceivePointerWheel(-delta, isHorizontal);
	}

	public int TryUpdatePosition(Vector3 value)
		=> TryUpdatePosition(value, InteractionTrackerClampingOption.Auto);

	public int TryUpdatePositionBy(Vector3 amount)
		=> TryUpdatePosition(Position + amount);

	public int TryUpdatePosition(Vector3 value, InteractionTrackerClampingOption option)
	{
		var id = Interlocked.Increment(ref _currentRequestId);
		_state.TryUpdatePosition(value, option, id);
		return id;
	}

	public int TryUpdatePositionBy(Vector3 amount, InteractionTrackerClampingOption option)
		=> TryUpdatePosition(Position + amount, option);

	private protected override void SetAnimatableProperty(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> subPropertyName, object? propertyValue)
	{
		if (propertyName.Equals(nameof(MinPosition), StringComparison.OrdinalIgnoreCase))
		{
			MinPosition = UpdateVector3(subPropertyName, MinPosition, propertyValue);
		}
		else if (propertyName.Equals(nameof(MaxPosition), StringComparison.OrdinalIgnoreCase))
		{
			MaxPosition = UpdateVector3(subPropertyName, MaxPosition, propertyValue);
		}
		else if (propertyName.Equals(nameof(MinScale), StringComparison.OrdinalIgnoreCase))
		{
			MinScale = ValidateValue<float>(propertyValue);
		}
		else if (propertyName.Equals(nameof(MaxScale), StringComparison.OrdinalIgnoreCase))
		{
			MaxScale = ValidateValue<float>(propertyValue);
		}
		else
		{
			base.SetAnimatableProperty(propertyName, subPropertyName, propertyValue);
		}
	}

	internal override object GetAnimatableProperty(string propertyName, string subPropertyName)
	{
		if (propertyName.Equals(nameof(Position), StringComparison.OrdinalIgnoreCase))
		{
			return GetVector3(subPropertyName, Position);
		}
		else if (propertyName.Equals(nameof(MinPosition), StringComparison.OrdinalIgnoreCase))
		{
			return GetVector3(subPropertyName, MinPosition);
		}
		else if (propertyName.Equals(nameof(MaxPosition), StringComparison.OrdinalIgnoreCase))
		{
			return GetVector3(subPropertyName, MinPosition);
		}
		else if (propertyName.Equals(nameof(Scale), StringComparison.OrdinalIgnoreCase))
		{
			return ValidateValue<float>(Scale);
		}
		else if (propertyName.Equals(nameof(MinScale), StringComparison.OrdinalIgnoreCase))
		{
			return ValidateValue<float>(MinScale);
		}
		else if (propertyName.Equals(nameof(MaxScale), StringComparison.OrdinalIgnoreCase))
		{
			return ValidateValue<float>(MaxScale);
		}
		else
		{
			return base.GetAnimatableProperty(propertyName, subPropertyName);
		}
	}
}
