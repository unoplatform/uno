using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Windows.UI.Xaml
{
	/// <summary>
	/// Defines an object that has a Binding DataContext
	/// </summary>
	public interface IDataContextProvider
	{
		object DataContext { get; set; }
	}
}
