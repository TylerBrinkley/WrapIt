using System;

namespace WrapIt
{
    [Flags]
    public enum TypeGeneration
    {
        None = 0,
        Instance = 1,
        Static = 2,
        StaticAndInstance = 3
    }
}