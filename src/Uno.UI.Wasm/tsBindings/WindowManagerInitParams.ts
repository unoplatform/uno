/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerInitParams
{
	/* Pack=4 */
	IsHostedMode : boolean;
	IsLoadEventsEnabled : boolean;
	public static unmarshal(pData:number) : WindowManagerInitParams
	{
		let ret = new WindowManagerInitParams();
		
		{
			ret.IsHostedMode = Boolean(Module.getValue(pData + 0, "i32"));
		}
		
		{
			ret.IsLoadEventsEnabled = Boolean(Module.getValue(pData + 4, "i32"));
		}
		return ret;
	}
}
