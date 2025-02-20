namespace Windows.Services.Store
{
	public partial class StorePrice
	{
		internal StorePrice()
		{
		}

		public string FormattedBasePrice { get; internal set; }

		public string FormattedPrice { get; internal set; }

		public string CurrencyCode { get; internal set; }

		public bool IsOnSale { get; internal set; }
	}
}
