using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.Services.Store
{
	public sealed partial class StoreProduct
	{
		internal StoreProduct() { }

		/// <summary>
		/// Gets the Store ID for this product.
		/// </summary>
		/// <remarks>
		/// For example (Twitter app):
		/// Windows: 9wzdncrfj140
		/// iOS: 333903271
		/// Android: com.twitter.android
		/// </remarks>
		public string StoreId { get; internal set; } = null!;

		/// <summary>
		/// Gets the URI to the Store listing for the product.
		/// </summary>
		/// <remarks>
		/// For example (Twitter app): 
		/// Windows: https://www.microsoft.com/store/apps/9wzdncrfj140
		/// iOS: https://itunes.apple.com/app/id333903271
		/// Android: https://play.google.com/store/apps/details?id=com.twitter.android
		/// </remarks>
		public Uri LinkUri { get; internal set; } = null!;
	}
}
