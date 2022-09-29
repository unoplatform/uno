#nullable disable

using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.Foundation
{
	public interface IClosable
	{
		/// <summary>Releases system resources that are exposed by a Windows Runtime object.</summary>
		void Close();
	}
}
