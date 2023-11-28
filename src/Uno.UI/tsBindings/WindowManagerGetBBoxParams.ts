/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerGetBBoxParams
{
	/* Pack=4 */
	public HtmlId : number;
	public static unmarshal(pData:number) : WindowManagerGetBBoxParams
	{
		const ret = new WindowManagerGetBBoxParams();
		
		{
			ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
		}
		return ret;
	}
}
