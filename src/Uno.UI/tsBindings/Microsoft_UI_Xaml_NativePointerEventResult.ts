/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
namespace Microsoft.UI.Xaml
{
	export class NativePointerEventResult
	{
		/* Pack=4 */
		public Result : number;
		public static unmarshal(pData:number) : NativePointerEventResult
		{
			const ret = new NativePointerEventResult();
			
			{
				ret.Result = Number(Module.getValue(pData + 0, "i8"));
			}
			return ret;
		}
	}
}
