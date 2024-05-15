/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
namespace Microsoft.UI.Xaml
{
	export class NativePointerSubscriptionParams
	{
		/* Pack=4 */
		public HtmlId : number;
		public Events : number;
		public static unmarshal(pData:number) : NativePointerSubscriptionParams
		{
			const ret = new NativePointerSubscriptionParams();
			
			{
				ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
			}
			
			{
				ret.Events = Number(Module.getValue(pData + 4, "i8"));
			}
			return ret;
		}
	}
}
