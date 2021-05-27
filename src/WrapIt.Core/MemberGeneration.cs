namespace WrapIt
{
    public enum MemberGeneration
    {
        None = 0,
        OnlyInInterface = 1,
        Full = 2,
        WrapImplementationInCompilerFlag = 3,
        FullWithSafeCaching = 4,
        WrapEventHandlerInCompilerFlag = 5
    }
}