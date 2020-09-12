/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class ApplicationDataContainer_GetCountParams
{
	/* Pack=4 */
	public Locality : string;
	public static unmarshal(pData:number) : ApplicationDataContainer_GetCountParams
	{
		const ret = new ApplicationDataContainer_GetCountParams();
		
		{
			const ptr = Module.getValue(pData + 0, "*");
			if(ptr !== 0)
			{
				ret.Locality = String(Module.UTF8ToString(ptr));
			}
			else
			
			{
				ret.Locality = null;
			}
		}
		return ret;
	}
}
