/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerRegisterPointerEventsOnViewParams
{
	/* Pack=4 */
	public HtmlId : number;
	public static unmarshal(pData:number) : WindowManagerRegisterPointerEventsOnViewParams
	{
		const ret = new WindowManagerRegisterPointerEventsOnViewParams();
		
		{
			ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
		}
		return ret;
	}
}
