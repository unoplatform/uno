using System;
using System.Linq;

namespace DirectUI
{
	internal interface ITreeBuilder
	{
		public bool IsBuildTreeSuspended { get; }

		public bool IsRegisteredForCallbacks { get; set; }

		public bool BuildTree();
	}
}
