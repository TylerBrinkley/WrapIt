using System.Collections.Generic;
using NUnit.Framework;
using WrapIt.Collections;

namespace WrapIt.Tests
{
    public abstract class EnumerableWrapperBaseTests<T, TWrapped, TInterface>
        where T : IEnumerable<User>
        where TWrapped : EnumerableWrapper<User, UserWrapper, IUser>, TInterface
        where TInterface : IEnumerable<IUser>
    {
        protected User User1;
        protected User User2;
        protected User[] UsersArray;
        protected T Users;
        protected TWrapped Wrapped;
        protected TInterface WrappedInterface => Wrapped;

        public virtual void Setup()
        {
            User1 = new User("John", "Doe");
            User2 = new User("James", "Horner");
            UsersArray = new[] { User1, User2 };
        }

        protected abstract TWrapped Create(TInterface users);

        protected abstract TInterface CreateNewInstance(TInterface users);

        [Test]
        public void CreateIsSame() => Assert.AreSame(WrappedInterface, Create(WrappedInterface));

        [Test]
        public void CreateFromNewInstanceIsNotSame() => Assert.AreNotSame(WrappedInterface, Create(CreateNewInstance(WrappedInterface)));

        [Test]
        public void Enumerate_Wrapper()
        {
            var count = 0;
            foreach (var item in Wrapped)
            {
                Assert.AreEqual(UsersArray[count].FirstName, item.FirstName);
                Assert.AreEqual(UsersArray[count].LastName, item.LastName);
                ++count;
            }
            Assert.AreEqual(2, count);
        }

        [Test]
        public void Enumerate_Interface()
        {
            var count = 0;
            foreach (var item in WrappedInterface)
            {
                Assert.AreEqual(UsersArray[count].FirstName, item.FirstName);
                Assert.AreEqual(UsersArray[count].LastName, item.LastName);
                ++count;
            }
            Assert.AreEqual(2, count);
        }
    }
}
