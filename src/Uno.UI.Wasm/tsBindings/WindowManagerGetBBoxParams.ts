/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerGetBBoxParams
{
	/* Pack=4 */
	HtmlId : number;
	public static unmarshal(pData:number) : WindowManagerGetBBoxParams
	{
		let ret = new WindowManagerGetBBoxParams();
		
		{
			ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
		}
		return ret;
	}
}
