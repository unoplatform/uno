using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Controls
{
	//	[PortStatus("Jérôme's implementation", Complete = false)]

	[AttributeUsage(AttributeTargets.All)]
	public class PortStatusAttribute : Attribute
	{
		private string comments;
		private bool complete;

		public virtual string Comments
		{
			get { return comments; }
		}

		public virtual bool Complete
		{
			get { return complete; }
			set { complete = value; }
		}

		public PortStatusAttribute(string comments)
		{
			this.comments = comments;
			this.complete = false;
		}

		public PortStatusAttribute()
		{
			this.complete = false;
		}

	}
}
