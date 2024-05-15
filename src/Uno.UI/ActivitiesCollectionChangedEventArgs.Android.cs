using System;
using System.Collections.Immutable;
using System.Linq;

namespace Uno.UI
{
	public class ActivitiesCollectionChangedEventArgs : EventArgs
	{
		internal static ActivitiesCollectionChangedEventArgs Added(int id, IImmutableDictionary<int, BaseActivity> instances)
		{
			return new ActivitiesCollectionChangedEventArgs
			{
				Instances = instances,
				AddedId = id,
			};
		}

		internal static ActivitiesCollectionChangedEventArgs Removed(int id, IImmutableDictionary<int, BaseActivity> instances)
		{
			return new ActivitiesCollectionChangedEventArgs
			{
				Instances = instances,
				RemovedId = id,
			};
		}

		private ActivitiesCollectionChangedEventArgs() { }

		/// <summary>
		/// Capture of the updated instances collection
		/// </summary>
		public IImmutableDictionary<int, BaseActivity> Instances { get; private set; }

		/// <summary>
		/// Id of the added instance, if any
		/// </summary>
		public int? AddedId { get; private set; }

		/// <summary>
		/// Id of the removed instance, if any
		/// </summary>
		public int? RemovedId { get; private set; }
	}
}
