using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using WrapIt.Collections;

namespace WrapIt.Tests
{
    public abstract class CollectionWrapperBaseTests<T, TWrapped, TInterface> : EnumerableWrapperBaseTests<T, TWrapped, TInterface>
        where T : ICollection<User>
        where TWrapped : CollectionWrapper<User, UserWrapper, IUser>, TInterface
        where TInterface : ICollection<IUser>
    {
        [Test]
        public void Count_Wrapper() => Assert.AreEqual(2, Wrapped.Count);

        [Test]
        public void Count_Interface() => Assert.AreEqual(2, WrappedInterface.Count);

        [Test]
        public void IsReadOnly() => Assert.AreEqual(Users.IsReadOnly, WrappedInterface.IsReadOnly);

        [Test]
        public void Contains_Wrapper_Existing() => Assert.True(Wrapped.Contains(User1));

        [Test]
        public void Contains_Interface_Existing() => Assert.True(WrappedInterface.Contains((UserWrapper)User1));

        [Test]
        public void Contains_Wrapper_NonExisting() => Assert.False(Wrapped.Contains(new User("Stan", "Musial")));

        [Test]
        public void Contains_Interface_NonExisting() => Assert.False(WrappedInterface.Contains((UserWrapper)new User("Stan", "Musial")));

        [Test]
        public void CopyTo()
        {
            var array = new IUser[2];
            WrappedInterface.CopyTo(array, 0);
            CollectionAssert.AreEqual(UsersArray.Select(u => (UserWrapper)u).ToArray<IUser>(), array);
        }
    }
}
