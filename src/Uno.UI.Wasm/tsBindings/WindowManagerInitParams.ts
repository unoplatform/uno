/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerInitParams
{
	/* Pack=4 */
	LocalFolderPath : string;
	IsHostedMode : boolean;
	IsLoadEventsEnabled : boolean;
	public static unmarshal(pData:number) : WindowManagerInitParams
	{
		let ret = new WindowManagerInitParams();
		
		{
			var ptr = Module.getValue(pData + 0, "*");
			if(ptr !== 0)
			{
				ret.LocalFolderPath = String(Module.UTF8ToString(ptr));
			}
			else
			
			{
				ret.LocalFolderPath = null;
			}
		}
		
		{
			ret.IsHostedMode = Boolean(Module.getValue(pData + 4, "i32"));
		}
		
		{
			ret.IsLoadEventsEnabled = Boolean(Module.getValue(pData + 8, "i32"));
		}
		return ret;
	}
}
