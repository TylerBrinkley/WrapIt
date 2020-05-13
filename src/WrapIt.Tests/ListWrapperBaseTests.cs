using System.Collections.Generic;
using NUnit.Framework;
using WrapIt.Collections;

namespace WrapIt.Tests
{
    public abstract class ListWrapperBaseTests<T, TWrapped> : CollectionWrapperBaseTests<T, TWrapped, IList<IUser>>
        where T : IList<User>
        where TWrapped : ListWrapperBase<User, UserWrapper, IUser>
    {
        [Test]
        public void IndexerGet_Wrapper()
        {
            Assert.AreEqual(User1.FirstName, Wrapped[0].FirstName);
            Assert.AreEqual(User1.LastName, Wrapped[0].LastName);
            Assert.AreEqual(User2.FirstName, Wrapped[1].FirstName);
            Assert.AreEqual(User2.LastName, Wrapped[1].LastName);
        }

        [Test]
        public void IndexerGet_Interface()
        {
            Assert.AreEqual(User1.FirstName, WrappedInterface[0].FirstName);
            Assert.AreEqual(User1.LastName, WrappedInterface[0].LastName);
            Assert.AreEqual(User2.FirstName, WrappedInterface[1].FirstName);
            Assert.AreEqual(User2.LastName, WrappedInterface[1].LastName);
        }

        [Test]
        public void IndexerSet_Wrapper()
        {
            Wrapped[0] = new User("George", "Washington");
            Assert.AreEqual("George", Wrapped[0].FirstName);
            Assert.AreEqual("Washington", Wrapped[0].LastName);
        }

        [Test]
        public void IndexerSet_Interface()
        {
            WrappedInterface[0] = (UserWrapper)new User("George", "Washington");
            Assert.AreEqual("George", WrappedInterface[0].FirstName);
            Assert.AreEqual("Washington", WrappedInterface[0].LastName);
        }

        [Test]
        public void IndexOf_Wrapper_Existing() => Assert.AreEqual(0, Wrapped.IndexOf(User1));

        [Test]
        public void IndexOf_Interface_Existing() => Assert.AreEqual(0, WrappedInterface.IndexOf((UserWrapper)User1));

        [Test]
        public void IndexOf_Wrapper_NonExisting() => Assert.AreEqual(-1, Wrapped.IndexOf(new User("Stan", "Musial")));

        [Test]
        public void IndexOf_Interface_NonExisting() => Assert.AreEqual(-1, WrappedInterface.IndexOf((UserWrapper)new User("Stan", "Musial")));
    }
}
