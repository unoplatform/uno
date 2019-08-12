using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Data
{
	/// <summary>
	/// A subject to route the availability of an element instance reference.
	/// </summary>
	/// <remarks>Used internally by the Xaml generator to perform late ElementName binding.</remarks>
	public class ElementNameSubject
	{
		private object _elementInstance;

		public ElementNameSubject() { }

		public ElementNameSubject(bool isRuntimeBound, string name)
		{
			IsLoadTimeBound = isRuntimeBound;
			Name = name;
		}

		/// <summary>
		/// An element name instance changed delegate.
		/// </summary>
		public delegate void ElementInstanceChangedHandler(object sender, object instance);

		/// <summary>
		/// An event raised when the ElementInstance property has changed.
		/// </summary>
		public event ElementInstanceChangedHandler ElementInstanceChanged;

		/// <summary>
		/// The element name instance
		/// </summary>
		public object ElementInstance
		{
			// element stubs are not returned, are only materialized
			// as part of FindName or Visibility binding
			get => _elementInstance is ElementStub ? null : _elementInstance;
			set
			{
				_elementInstance = value;

				if (!(_elementInstance is ElementStub))
				{
					// In the case of element stubs, the XAML generator sets the subject as part of the
					// materialization of the final element, raising this event.

					ElementInstanceChanged?.Invoke(this, _elementInstance);
				}
			}
		}

		internal object ActualElementInstance => _elementInstance;

		/// <summary>
		/// Should the ElementName binding be applied when the view loads? True when the named element is not in local scope.
		/// </summary>
		public bool IsLoadTimeBound { get; set; }

		/// <summary>
		/// The ElementName. 
		/// </summary>
		public string Name { get; set; }
	}
}
