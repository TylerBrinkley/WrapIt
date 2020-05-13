using System.Collections.Generic;
using NUnit.Framework;
using WrapIt.Collections;

namespace WrapIt.Tests
{
    public abstract class SetWrapperTests : CollectionWrapperBaseTests<HashSet<User>, SetWrapper<User, UserWrapper, IUser>, ISet<IUser>>
    {
        protected override SetWrapper<User, UserWrapper, IUser> Create(ISet<IUser> users) => SetWrapper<User, UserWrapper, IUser>.Create(users);

        protected override ISet<IUser> CreateNewInstance(ISet<IUser> users) => new HashSet<IUser>(users);

        public override void Setup()
        {
            base.Setup();
            Users = new HashSet<User> { User1, User2 };
        }

        [Test]
        public void ToCollection() => CollectionAssert.AreEqual(Users, Wrapped.ToCollection());

        [Test]
        public void Add()
        {
            var newUser = new User("George", "Washington");
            Assert.IsTrue(Wrapped.Add(newUser));
            Assert.AreEqual(3, Wrapped.Count);
            Assert.IsTrue(Wrapped.Contains(User1));
            Assert.IsTrue(Wrapped.Contains(User2));
            Assert.IsTrue(Wrapped.Contains(newUser));
        }

        [Test]
        public void Remove()
        {
            Assert.IsTrue(Wrapped.Remove(User1));
            Assert.AreEqual(1, Wrapped.Count);
            Assert.IsTrue(Wrapped.Contains(User2));
        }

        [Test]
        public void Clear()
        {
            Wrapped.Clear();
            Assert.AreEqual(0, Wrapped.Count);
        }

        [Test]
        public void ExceptWith()
        {
            var newUser = new User("George", "Washington");
            Wrapped.ExceptWith(EnumerableWrapper<User, UserWrapper, IUser>.Create(new[] { User1, newUser }));
            Assert.AreEqual(1, Wrapped.Count);
            Assert.IsFalse(Wrapped.Contains(User1));
            Assert.IsTrue(Wrapped.Contains(User2));
            Assert.IsFalse(Wrapped.Contains(newUser));
        }

        [Test]
        public void IntersectWith()
        {
            var newUser = new User("George", "Washington");
            Wrapped.IntersectWith(EnumerableWrapper<User, UserWrapper, IUser>.Create(new[] { User1, newUser }));
            Assert.AreEqual(1, Wrapped.Count);
            Assert.IsTrue(Wrapped.Contains(User1));
            Assert.IsFalse(Wrapped.Contains(User2));
            Assert.IsFalse(Wrapped.Contains(newUser));
        }

        [Test]
        public void IsProperSubsetOf_True() => Assert.IsTrue(Wrapped.IsProperSubsetOf(EnumerableWrapper<User, UserWrapper, IUser>.Create(new[] { User1, User2, new User("George", "Washington") })));

        [Test]
        public void IsProperSubsetOf_False() => Assert.IsFalse(Wrapped.IsProperSubsetOf(EnumerableWrapper<User, UserWrapper, IUser>.Create(new[] { User1, User2 })));

        [Test]
        public void IsProperSupersetOf_True() => Assert.IsTrue(Wrapped.IsProperSupersetOf(EnumerableWrapper<User, UserWrapper, IUser>.Create(new[] { User1 })));

        [Test]
        public void IsProperSupersetOf_False() => Assert.IsFalse(Wrapped.IsProperSupersetOf(EnumerableWrapper<User, UserWrapper, IUser>.Create(new[] { User1, User2 })));

        [Test]
        public void IsSubsetOf_True() => Assert.IsTrue(Wrapped.IsSubsetOf(EnumerableWrapper<User, UserWrapper, IUser>.Create(new[] { User1, User2 })));

        [Test]
        public void IsSubsetOf_False() => Assert.IsFalse(Wrapped.IsSubsetOf(EnumerableWrapper<User, UserWrapper, IUser>.Create(new[] { User1 })));

        [Test]
        public void IsSupersetOf_True() => Assert.IsTrue(Wrapped.IsSupersetOf(EnumerableWrapper<User, UserWrapper, IUser>.Create(new[] { User1, User2 })));

        [Test]
        public void IsSupersetOf_False() => Assert.IsFalse(Wrapped.IsSupersetOf(EnumerableWrapper<User, UserWrapper, IUser>.Create(new[] { User1, User2, new User("George", "Washington") })));

        [Test]
        public void Overlaps_True() => Assert.IsTrue(Wrapped.Overlaps(EnumerableWrapper<User, UserWrapper, IUser>.Create(new[] { User1, new User("George", "Washington") })));

        [Test]
        public void Overlaps_False() => Assert.IsFalse(Wrapped.Overlaps(EnumerableWrapper<User, UserWrapper, IUser>.Create(new[] { new User("George", "Washington") })));

        [Test]
        public void SetEquals_True() => Assert.IsTrue(Wrapped.SetEquals(EnumerableWrapper<User, UserWrapper, IUser>.Create(new[] { User1, User2 })));

        [Test]
        public void SetEquals_False() => Assert.IsFalse(Wrapped.SetEquals(EnumerableWrapper<User, UserWrapper, IUser>.Create(new[] { User1 })));

        [Test]
        public void SymmetricExceptWith()
        {
            var newUser = new User("George", "Washington");
            Wrapped.SymmetricExceptWith(EnumerableWrapper<User, UserWrapper, IUser>.Create(new[] { User1, newUser }));
            Assert.AreEqual(2, Wrapped.Count);
            Assert.IsFalse(Wrapped.Contains(User1));
            Assert.IsTrue(Wrapped.Contains(User2));
            Assert.IsTrue(Wrapped.Contains(newUser));
        }

        [Test]
        public void UnionWith()
        {
            var newUser = new User("George", "Washington");
            Wrapped.UnionWith(EnumerableWrapper<User, UserWrapper, IUser>.Create(new[] { User1, newUser }));
            Assert.AreEqual(3, Wrapped.Count);
            Assert.IsTrue(Wrapped.Contains(User1));
            Assert.IsTrue(Wrapped.Contains(User2));
            Assert.IsTrue(Wrapped.Contains(newUser));
        }
    }

    public class SetWrapperStandardTests : SetWrapperTests
    {
        [SetUp]
        public override void Setup()
        {
            base.Setup();
            Wrapped = SetWrapper<User, UserWrapper, IUser>.Create(Users);
        }
    }

    public class SetWrapperCastedTests : SetWrapperTests
    {
        [SetUp]
        public override void Setup()
        {
            base.Setup();
            Wrapped = SetWrapper<User, UserWrapper, IUser>.Create(new HashSet<IUser>(SetWrapper<User, UserWrapper, IUser>.Create(Users)));
        }
    }
}
