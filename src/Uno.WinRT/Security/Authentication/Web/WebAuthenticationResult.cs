#nullable enable

namespace Windows.Security.Authentication.Web
{
	public partial class WebAuthenticationResult
	{
		public WebAuthenticationResult(string? responseData, uint responseErrorDetail, WebAuthenticationStatus responseStatus)
		{
			ResponseData = responseData;
			ResponseErrorDetail = responseErrorDetail;
			ResponseStatus = responseStatus;
		}

		public string? ResponseData
		{
			get;
		}

		public uint ResponseErrorDetail
		{
			get;
		}

		public WebAuthenticationStatus ResponseStatus
		{
			get;
		}
	}
}
