/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class When_IntPtrParams
{
	/* Pack=1 */
	public Id : number;
	public static unmarshal(pData:number) : When_IntPtrParams
	{
		const ret = new When_IntPtrParams();
		
		{
			ret.Id = Number(Module.getValue(pData + 0, "*"));
		}
		return ret;
	}
}
