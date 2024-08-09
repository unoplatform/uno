using System;
using Uno.Extensions;
using Uno.Foundation.Logging;

namespace Windows.UI.Xaml
{
	/// <summary>
	/// Defines a property assignation in a style or story board.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class Setter<T> : SetterBase, ICSharpPropertySetter
	{
		/// <summary>
		/// Creates a setter using the specified action to set a named property.
		/// </summary>
		/// <param name="property">The name of the property on the target object.</param>
		/// <param name="action"></param>
		public Setter(string property, Action<T> action)
		{
			Property = property;
			Action = action;
		}

		/// <summary>
		/// The name of the string to set
		/// </summary>
		public string Property { get; set; }

		internal override void OnStringPropertyChanged(string name) => Property = name;

		internal override void ApplyTo(DependencyObject o)
		{
			if (!(o is T))
			{
				this.Log().Error($"The provided instance [{o?.GetType()}] does not match the setter's target type [{typeof(T)}]");
			}
			else
			{
				Action?.Invoke((T)o);
			}
		}

		/// <summary>
		/// The action to be executed when the setter is applied
		/// </summary>
		public Action<T> Action { get; }
	}

	/// <summary>
	/// A marking interface used to determine if a <see cref="SetterBase"/> instance is a <see cref="Setter{T}"/>. 
	/// This is used for binary backward compatitility
	/// </summary>
	internal interface ICSharpPropertySetter
	{
		string Property { get; }
	}
}

