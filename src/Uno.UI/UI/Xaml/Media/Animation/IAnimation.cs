using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.UI.Xaml.Media.Animation
{
	/// <summary>
	/// Contract for a <see cref="Timeline"/> animation which is backed by <see cref="Timeline.AnimationImplementation{T}"/>.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	internal interface IAnimation<T> where T : struct
	{
		T? To { get; }
		T? From { get; }
		T? By { get; }
		bool EnableDependentAnimation { get; }
		IEasingFunction EasingFunction { get; }
		T Subtract(T minuend, T subtrahend);
		T Add(T first, T second);
		T Multiply(float multiplier, T t);
		T Convert(object value);
	}
}
