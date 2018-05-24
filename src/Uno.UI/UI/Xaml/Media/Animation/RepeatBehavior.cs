using Uno.UI.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Windows.UI.Xaml.Media.Animation
{
	public partial struct RepeatBehavior : IEquatable<RepeatBehavior>
	{
		private static readonly string __forever = "Forever";
        private static readonly string __count = "Count";
        private double _count;

        public RepeatBehavior(double count)
		{
			if (count <= 0)
			{
				throw new ArgumentOutOfRangeException("count", "Count must be greater than zero.");
			}

			Type = RepeatBehaviorType.Count;
			_count = count;
			Duration = default(TimeSpan);
		}

		public RepeatBehavior(TimeSpan duration)
		{
			this.Type = RepeatBehaviorType.Duration;
			this.Duration = duration;
            _count = 1;
		}


        
        public double Count
        {
            get
            {
                if (double.IsInfinity(_count)
                    || double.IsNaN(_count)
                    || _count < 0.0)
                {
                    throw new InvalidOperationException("Count is not set");
                }
                return _count;
            }
            set { _count = value; }
        }

		public TimeSpan Duration { get; set; }

		public RepeatBehaviorType Type { get; set; }

		public bool HasCount
		{
			get
			{
				return this.Type == RepeatBehaviorType.Count && 
					this.Count > 0;
			}
		}

		public bool HasDuration
		{
			get
			{
				return this.Type == RepeatBehaviorType.Duration && 
					this.Duration.CompareTo(TimeSpan.Zero) > 0;
			}
		}

		public static RepeatBehavior Forever
		{
			get
			{
				return new RepeatBehavior() { Type = RepeatBehaviorType.Forever };
			}
		}

		public override int GetHashCode()
		{
			return this.Count.GetHashCode() ^
				this.Duration.GetHashCode() ^
				this.Type.GetHashCode();
		}

		public override bool Equals(object value)
		{
            if (value == null)
            {
                return false;
            }
            else if (value is RepeatBehavior)
            {
                return Equals((RepeatBehavior)value);
            }
            return false;            
		}

		public bool Equals(RepeatBehavior other)
        {
            if (Type != other.Type)
            {
                return false;
            }

            switch (Type)
            {
                case RepeatBehaviorType.Forever:
                    return true;
                case RepeatBehaviorType.Count:
                    return Count == other.Count;
                case RepeatBehaviorType.Duration:
                    return Duration == other.Duration;
                default:
                    return false;
            }          
        }

		public static bool operator ==(RepeatBehavior first, RepeatBehavior second)
		{
			return first.Equals(second);
		}

		public static bool operator !=(RepeatBehavior first, RepeatBehavior second)
		{
			return !RepeatBehavior.Equals(first, second);
		}

		public static bool Equals(RepeatBehavior first, RepeatBehavior second)
		{
			return first.Type.Equals(second.Type)
				&& first.Count.Equals(second.Count)
				&& first.Duration.Equals(second.Duration);
		}

		public override string ToString()
		{
			return this.ToString(CultureInfo.InvariantCulture);
		}

		public string ToString(IFormatProvider provider)
		{
			switch (this.Type)
			{
				case RepeatBehaviorType.Count:
					return this.Count.ToString(provider);
				case RepeatBehaviorType.Duration:
					return this.Duration.ToXamlString(provider);
				case RepeatBehaviorType.Forever:
					return __forever;
				default:
					// Should never be reached
					throw new NotSupportedException("this RepeatBehavior type is not supported.");
			}
		}

        static public implicit operator RepeatBehavior(string str)
        {
            RepeatBehaviorType type = RepeatBehaviorType.Duration;

            if(string.Equals(str, __forever, StringComparison.InvariantCultureIgnoreCase))
            {
                type = RepeatBehaviorType.Forever;
            }
            else if (string.Equals(str, __count, StringComparison.InvariantCultureIgnoreCase))
            {
                type = RepeatBehaviorType.Count;
            }
            return new RepeatBehavior() { Type = type };
        }

    }
}
