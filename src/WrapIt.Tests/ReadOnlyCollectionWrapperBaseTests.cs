using System.Collections.Generic;
using NUnit.Framework;
using WrapIt.Collections;

namespace WrapIt.Tests
{
    public abstract class ReadOnlyCollectionWrapperBaseTests<T, TWrapped, TInterface> : EnumerableWrapperBaseTests<T, TWrapped, TInterface>
        where T : IReadOnlyCollection<User>
        where TWrapped : ReadOnlyCollectionWrapper<User, UserWrapper, IUser>, TInterface
        where TInterface : IReadOnlyCollection<IUser>
    {
        [Test]
        public void Count_Wrapper() => Assert.AreEqual(2, Wrapped.Count);

        [Test]
        public void Count_Interface() => Assert.AreEqual(2, WrappedInterface.Count);
    }
}
