using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using WrapIt.Collections;

namespace WrapIt.Tests
{
    public abstract class CollectionWrapperTests : CollectionWrapperBaseTests<ICollection<User>, CollectionWrapper<User, UserWrapper, IUser>, ICollection<IUser>>
    {
        protected override CollectionWrapper<User, UserWrapper, IUser> Create(ICollection<IUser> users) => CollectionWrapper<User, UserWrapper, IUser>.Create(users);

        protected override ICollection<IUser> CreateNewInstance(ICollection<IUser> users) => users.ToList();

        public override void Setup()
        {
            base.Setup();
            Users = new List<User> { User1, User2 };
        }

        [Test]
        public void ToCollection() => CollectionAssert.AreEqual(Users, Wrapped.ToCollection());

        [Test]
        public void Add()
        {
            var newUser = (UserWrapper)new User("George", "Washington");
            WrappedInterface.Add(newUser);
            Assert.AreEqual(3, WrappedInterface.Count);
            Assert.IsTrue(WrappedInterface.Contains((UserWrapper)User1));
            Assert.IsTrue(WrappedInterface.Contains((UserWrapper)User2));
            Assert.IsTrue(WrappedInterface.Contains(newUser));
        }

        [Test]
        public void Remove()
        {
            WrappedInterface.Remove((UserWrapper)User1);
            Assert.AreEqual(1, WrappedInterface.Count);
        }

        [Test]
        public void Clear()
        {
            WrappedInterface.Clear();
            Assert.AreEqual(0, WrappedInterface.Count);
        }
    }

    public sealed class CollectionWrapperStandardTests : CollectionWrapperTests
    {
        [SetUp]
        public override void Setup()
        {
            base.Setup();
            Wrapped = new CollectionWrapper<User, UserWrapper, IUser>(Users);
        }
    }

    public sealed class CollectionWrapperCastedTests : CollectionWrapperTests
    {
        [SetUp]
        public override void Setup()
        {
            base.Setup();
            Wrapped = new CollectionWrapper<User, UserWrapper, IUser>(new CollectionWrapper<User, UserWrapper, IUser>(Users).ToList());
        }
    }
}
