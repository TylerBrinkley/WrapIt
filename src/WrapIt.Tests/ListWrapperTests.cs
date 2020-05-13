using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using WrapIt.Collections;

namespace WrapIt.Tests
{
    public abstract class ListWrapperTests : ListWrapperBaseTests<List<User>, ListWrapper<User, UserWrapper, IUser>>
    {
        protected override ListWrapper<User, UserWrapper, IUser> Create(IList<IUser> users) => ListWrapper<User, UserWrapper, IUser>.Create(users);

        protected override IList<IUser> CreateNewInstance(IList<IUser> users) => users.ToList();

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
            var newUser = new User("George", "Washington");
            Wrapped.Add(newUser);
            Assert.AreEqual(3, Wrapped.Count);
            Assert.AreEqual(0, Wrapped.IndexOf(User1));
            Assert.AreEqual(1, Wrapped.IndexOf(User2));
            Assert.AreEqual(2, Wrapped.IndexOf(newUser));
        }

        [Test]
        public void Insert()
        {
            var newUser = new User("George", "Washington");
            Wrapped.Insert(0, newUser);
            Assert.AreEqual(3, Wrapped.Count);
            Assert.AreEqual(0, Wrapped.IndexOf(newUser));
            Assert.AreEqual(1, Wrapped.IndexOf(User1));
            Assert.AreEqual(2, Wrapped.IndexOf(User2));
        }

        [Test]
        public void Remove()
        {
            Assert.IsTrue(Wrapped.Remove(User1));
            Assert.AreEqual(1, Wrapped.Count);
            Assert.AreEqual((UserWrapper)User2, Wrapped[0]);
        }

        [Test]
        public void RemoveAt()
        {
            Wrapped.RemoveAt(0);
            Assert.AreEqual(1, Wrapped.Count);
            Assert.AreEqual((UserWrapper)User2, Wrapped[0]);
        }

        [Test]
        public void Clear()
        {
            Wrapped.Clear();
            Assert.AreEqual(0, Wrapped.Count);
        }
    }

    public class ListWrapperStandardTests : ListWrapperTests
    {
        [SetUp]
        public override void Setup()
        {
            base.Setup();
            Wrapped = ListWrapper<User, UserWrapper, IUser>.Create(Users);
        }
    }

    public class ListWrapperCastedTests : ListWrapperTests
    {
        [SetUp]
        public override void Setup()
        {
            base.Setup();
            Wrapped = ListWrapper<User, UserWrapper, IUser>.Create(ListWrapper<User, UserWrapper, IUser>.Create(Users).ToList());
        }
    }
}
