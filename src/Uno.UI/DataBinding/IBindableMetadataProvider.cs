#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI.DataBinding
{
	/// <summary>
	/// An interface that provides bindable types metadata.
	/// </summary>
	public interface IBindableMetadataProvider
	{
		/// <summary>
		/// Allows to get a Bindable type definition through a System.Type.
		/// </summary>
		/// <param name="type">The type to lookup</param>
		/// <returns>A bindable type instance, otherwise null.</returns>
		IBindableType GetBindableTypeByType(Type type);

		/// <summary>
		/// Allows to get a Bindable type definition through a string representing the full type name.
		/// </summary>
		/// <param name="fullName">The type to lookup</param>
		/// <returns>A bindable type instance, otherwise null.</returns>
		IBindableType GetBindableTypeByFullName(string fullName);
	}
}
