using System;
using Windows.UI.Xaml.Automation.Provider;
using Windows.UI.Xaml.Controls.Primitives;

namespace Windows.UI.Xaml.Automation.Peers
{
	/// <summary>
	/// A base class that provides a Microsoft UI Automation peer implementation for types that derive from RangeBase.
	/// </summary>
	public partial class RangeBaseAutomationPeer : FrameworkElementAutomationPeer, IRangeValueProvider
	{
		private readonly RangeBase _owner;

		private bool m_isEnableValueChangedEventThrottling;
		private DateTimeOffset m_timePointOfLastValuePropertyChangedEvent = DateTimeOffset.MinValue;

		/// <summary>
		/// Initializes a new instance of the RangeBaseAutomationPeer class.
		/// </summary>
		/// <param name="owner">The owner element to create for.</param>
		public RangeBaseAutomationPeer(RangeBase owner) : base(owner)
		{
			_owner = owner ?? throw new ArgumentNullException(nameof(owner));
		}

		protected override object GetPatternCore(PatternInterface patternInterface)
		{
			if (patternInterface == PatternInterface.RangeValue)
			{
				return this;
			}

			return base.GetPatternCore(patternInterface);
		}

		/// <summary>
		/// Sets the value of the control, as an implementation of the IValueProvider pattern.
		/// </summary>
		/// <param name="value">The value to set.</param>
		public void SetValue(double value)
		{
			if (!IsEnabled())
			{
				throw new InvalidOperationException("Element not enabled");
			}

			var minValue = _owner.Minimum;
			var maxValue = _owner.Maximum;
			if (value < minValue || value > maxValue)
			{
				throw new ArgumentOutOfRangeException(nameof(value), "Value must be between Minimum and Maximum.");
			}

			_owner.Value = value;
		}

		/// <summary>
		/// Gets the value of the control.
		/// </summary>
		public double Value => _owner.Value;

		/// <summary>
		/// Gets a value that indicates whether the value of a control is read-only.
		/// </summary>
		public bool IsReadOnly => !IsEnabledCore();

		/// <summary>
		/// Gets the maximum range value that is supported by the control.
		/// </summary>
		public double Maximum => _owner.Maximum;

		/// <summary>
		/// Gets the minimum range value that is supported by the control.
		/// </summary>
		public double Minimum => _owner.Minimum;

		/// <summary>
		/// Gets the value that is added to or subtracted from the Value property when a large change is made, such as with the PAGE DOWN key.
		/// </summary>
		public double LargeChange => _owner.LargeChange;

		/// <summary>
		/// Gets the value that is added to or subtracted from the Value property when a small change is made, such as with an arrow key.
		/// </summary>
		public double SmallChange => _owner.SmallChange;

		internal void RaiseMinimumPropertyChangedEvent(object oldValue, object newValue)
		{
			var bAutomationListener = AutomationPeer.ListenerExistsHelper(AutomationEvents.PropertyChanged);
			if (bAutomationListener)
			{
				RaisePropertyChangedEvent(RangeValuePatternIdentifiers.MinimumProperty, oldValue, newValue);
			}
		}

		internal void RaiseMaximumPropertyChangedEvent(object oldValue, object newValue)
		{
			var bAutomationListener = AutomationPeer.ListenerExistsHelper(AutomationEvents.PropertyChanged);
			if (bAutomationListener)
			{
				RaisePropertyChangedEvent(RangeValuePatternIdentifiers.MaximumProperty, oldValue, newValue);
			}
		}

		internal void EnableValueChangedEventThrottling(bool value)
		{
			// This flag is enabled only in MediaTransportControls for timer based change events while playing video.
			m_isEnableValueChangedEventThrottling = value;
		}

		internal void RaiseValuePropertyChangedEvent(object oldValue, object newValue)
		{
			bool shouldRaiseEvent = true;

			if (m_isEnableValueChangedEventThrottling)
			{
				// In the case progress slider in MediaTransportControls for a playing video, Value property is constantly changing causing slutter in Narration 
				// we want to avoid raising so many PropertyChangedEvents that Narrator cannot keep up.
				// We always wait for a timeout before we allow another PropertyChangedEvent to get raised.
				// This flag is enabled only in MediaTransportControls for timer based change events while playing video.
				const int timeOutInMilliseconds = 5000;
				var now = DateTime.UtcNow;
				shouldRaiseEvent = (now - m_timePointOfLastValuePropertyChangedEvent) > TimeSpan.FromMilliseconds(timeOutInMilliseconds);
				if (shouldRaiseEvent)
				{
					m_timePointOfLastValuePropertyChangedEvent = now;
				}
			}

			var bAutomationListener = AutomationPeer.ListenerExistsHelper(AutomationEvents.PropertyChanged);
			if (bAutomationListener && shouldRaiseEvent)
			{
				RaisePropertyChangedEvent(RangeValuePatternIdentifiers.ValueProperty, oldValue, newValue);
			}
		}
	}
}
