using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using WrapIt.Collections;

namespace WrapIt.Tests
{
    public abstract class ReadOnlyCollectionWrapperTests : ReadOnlyCollectionWrapperBaseTests<IReadOnlyCollection<User>, ReadOnlyCollectionWrapper<User, UserWrapper, IUser>, IReadOnlyCollection<IUser>>
    {
        protected override ReadOnlyCollectionWrapper<User, UserWrapper, IUser> Create(IReadOnlyCollection<IUser> users) => ReadOnlyCollectionWrapper<User, UserWrapper, IUser>.Create(users);

        protected override IReadOnlyCollection<IUser> CreateNewInstance(IReadOnlyCollection<IUser> users) => users.ToList();

        public override void Setup()
        {
            base.Setup();
            Users = new List<User> { User1, User2 };
        }

        [Test]
        public void ToCollection() => CollectionAssert.AreEqual(Users, Wrapped.ToCollection());
    }

    public sealed class ReadOnlyCollectionWrapperStandardTests : ReadOnlyCollectionWrapperTests
    {
        [SetUp]
        public override void Setup()
        {
            base.Setup();
            Wrapped = new ReadOnlyCollectionWrapper<User, UserWrapper, IUser>(Users);
        }
    }

    public sealed class ReadOnlyCollectionWrapperCastedTests : ReadOnlyCollectionWrapperTests
    {
        [SetUp]
        public override void Setup()
        {
            base.Setup();
            Wrapped = new ReadOnlyCollectionWrapper<User, UserWrapper, IUser>(new ReadOnlyCollectionWrapper<User, UserWrapper, IUser>(Users).ToList());
        }
    }
}
