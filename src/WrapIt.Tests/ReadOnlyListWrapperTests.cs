using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using WrapIt.Collections;

namespace WrapIt.Tests
{
    public abstract class ReadOnlyListWrapperTests : ReadOnlyCollectionWrapperBaseTests<IReadOnlyList<User>, ReadOnlyListWrapper<User, UserWrapper, IUser>, IReadOnlyList<IUser>>
    {
        protected override ReadOnlyListWrapper<User, UserWrapper, IUser> Create(IReadOnlyList<IUser> users) => ReadOnlyListWrapper<User, UserWrapper, IUser>.Create(users);

        protected override IReadOnlyList<IUser> CreateNewInstance(IReadOnlyList<IUser> users) => users.ToList();

        public override void Setup()
        {
            base.Setup();
            Users = new List<User> { User1, User2 };
        }

        [Test]
        public void ToCollection() => CollectionAssert.AreEqual(Users, Wrapped.ToCollection());

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
    }

    public class ReadOnlyListWrapperStandardTests : ReadOnlyListWrapperTests
    {
        [SetUp]
        public override void Setup()
        {
            base.Setup();
            Wrapped = ReadOnlyListWrapper<User, UserWrapper, IUser>.Create(Users);
        }
    }

    public class ReadOnlyListWrapperCastedTests : ReadOnlyListWrapperTests
    {
        [SetUp]
        public override void Setup()
        {
            base.Setup();
            Wrapped = ReadOnlyListWrapper<User, UserWrapper, IUser>.Create(ReadOnlyListWrapper<User, UserWrapper, IUser>.Create(Users).ToList());
        }
    }
}
