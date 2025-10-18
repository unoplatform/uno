using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Foundation;
using StoreKit;
using Windows.Services.Store;

namespace Uno.Services.Store.Internal
{
    internal static class SKProductExtensions
    {
        public static StoreProduct ToStoreProduct(this SKProduct skProduct, StoreProductKind kind)
		{
			var product = new StoreProduct()
			{
				StoreId = skProduct.ProductIdentifier,
				Description = skProduct.LocalizedDescription ?? skProduct.Description,
				Title = skProduct.LocalizedTitle,
				Price = skProduct.GetStorePrice(kind),
				ProductKind = kind.ToString("g", CultureInfo.InvariantCulture)
			};
			return product;
		}

		private static StorePrice GetStorePrice(this SKProduct skProduct, StoreProductKind kind)
		{
			skProduct.Consum
		}

		private string GetFormattedPrice(this SKProduct product)
		{
			if (product?.PriceLocale == null)
				return string.Empty;

			var formatter = new NSNumberFormatter()
			{
				FormatterBehavior = NSNumberFormatterBehavior.Version_10_4,
				NumberStyle = NSNumberFormatterStyle.Currency,
				Locale = product.PriceLocale
			};
			var formattedString = formatter.StringFromNumber(product.Price);
			return formattedString;
		}
	}
}
