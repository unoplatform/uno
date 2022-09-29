#nullable disable

using System;
using Android.Views;

namespace Uno.UI.Controls
{
	/// <summary>
	/// An abstraction for secondary view pool, for lists that use the RecyleBin pattern.
	/// </summary>
	public interface ISecondaryViewPool
	{
		/// <summary>
		/// Gets a view for the specified position.
		/// </summary>
		/// <param name="position">The position of the item in the list, to be used for ItemType selection.</param>
		/// <returns>A view to be used, otherwise null.</returns>
		View GetView(int position);

		/// <summary>
		/// Sets the specified view as returned by the adapter.
		/// </summary>
		/// <param name="position">The position of the view in th elist</param>
		/// <param name="view">The view to be marked</param>
		void SetActiveView(int position, View view);
		/// <summary>
		/// Returns all materialized views stored in the pool
		/// </summary>
		/// <returns></returns>
		View[] GetAllViews();

		/// <summary>
		/// Registers an action to be called when a view is recycled.
		/// </summary>
		/// <param name="container"></param>
		/// <param name="action"></param>
		void RegisterForRecycled(View container, Action action);
	}
}