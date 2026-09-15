#nullable enable

using System;
using System.Numerics;
using System.Threading;
using Uno.UI.Dispatching;
using Windows.Foundation;

using static Microsoft.UI.Composition.SubPropertyHelpers;

namespace Microsoft.UI.Composition.Interactions;

public partial class InteractionTracker : CompositionObject
{
	private InteractionTrackerState _state;
	private Vector3 _position;
	private float _scale = 1.0f;

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

	public float Scale => _scale;

	public Vector3 MinPosition { get; set; }

	public Vector3 MaxPosition { get; set; }

	public Vector3? PositionInertiaDecayRate { get; set; }

	public Vector3 Position => _position;

	public CompositionInteractionSourceCollection InteractionSources { get; }

	public int CurrentRequestId => _currentRequestId;

#if HAS_UNO
	internal InteractionTrackerState State => _state;
#endif

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
			var scale = _scale;
			NativeDispatcher.Main.Enqueue(() =>
			{
				Owner?.ValuesChanged(this, new InteractionTrackerValuesChangedArgs(newPosition, scale, requestId));
				OnPropertyChanged(nameof(Position), isSubPropertyChange: false);
			});
		}
	}

	internal void SetScale(float newScale, Vector3 centerPoint, int requestId)
	{
		var oldScale = _scale;
		var scaleChanged = !oldScale.Equals(newScale);
		var scaleRatio = oldScale == 0.0f ? 1.0f : newScale / oldScale;
		var newPosition = scaleRatio * (_position + centerPoint) - centerPoint;

		if (scaleChanged)
		{
			_scale = newScale;

			// Scale-dependent expression animations update the position boundaries synchronously.
			OnPropertyChanged(nameof(Scale), isSubPropertyChange: false);
		}

		newPosition = Vector3.Clamp(newPosition, MinPosition, MaxPosition);
		var positionChanged = _position != newPosition;

		if (!scaleChanged && !positionChanged)
		{
			return;
		}

		_position = newPosition;

		if (positionChanged)
		{
			OnPropertyChanged(nameof(Position), isSubPropertyChange: false);
		}

		NativeDispatcher.Main.Enqueue(() =>
			Owner?.ValuesChanged(this, new InteractionTrackerValuesChangedArgs(newPosition, newScale, requestId)));
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

	/// <summary>
	/// Tries to update the scale to the specified value.
	/// </summary>
	/// <param name="value">The new value for scale.</param>
	/// <param name="centerPoint">The new center point.</param>
	/// <returns>
	/// The request identifier. Request identifiers start at 1 and increase with each try call
	/// during the lifetime of the application.
	/// </returns>
	public int TryUpdateScale(float value, Vector3 centerPoint)
	{
		var id = Interlocked.Increment(ref _currentRequestId);
		_state.TryUpdateScale(value, centerPoint, id);
		return id;
	}

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
