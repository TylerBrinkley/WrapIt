using System;

namespace WrapIt.Collections
{
    internal static class Conversion<T, TWrapped>
    {
        public static readonly Func<T, TWrapped> Wrap = (Func<T, TWrapped>)Delegate.CreateDelegate(typeof(Func<T, TWrapped>), typeof(TWrapped).GetMethod("op_Implicit", new[] { typeof(T) }));

        public static readonly Func<TWrapped, T> Unwrap = (Func<TWrapped, T>)Delegate.CreateDelegate(typeof(Func<TWrapped, T>), typeof(TWrapped).GetMethod("op_Implicit", new[] { typeof(TWrapped) }));
    }
}