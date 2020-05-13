using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using WrapIt.Collections;

namespace WrapIt.Tests
{
    public abstract class ReadOnlyDictionaryWrapperTests
    {
        protected User User1;
        protected User User2;
        protected KeyValuePair<string, User>[] UsersArray;
        protected IReadOnlyDictionary<string, User> Users;
        protected ReadOnlyDictionaryWrapper<string, User, UserWrapper, IUser> Wrapped;
        protected IReadOnlyDictionary<string, IUser> WrappedInterface => Wrapped;

        public virtual void Setup()
        {
            User1 = new User("John", "Doe");
            User2 = new User("James", "Horner");
            Users = new Dictionary<string, User> { { "jdoe", User1 }, { "jhorner", User2 } };
            UsersArray = Users.ToArray();
        }

        [Test]
        public void ToCollection() => CollectionAssert.AreEqual(Users, Wrapped.ToCollection());

        [Test]
        public void CreateIsSame() => Assert.AreSame(WrappedInterface, ReadOnlyDictionaryWrapper<string, User, UserWrapper, IUser>.Create(WrappedInterface));

        [Test]
        public void CreateFromNewInstanceIsNotSame() => Assert.AreNotSame(WrappedInterface, ReadOnlyDictionaryWrapper<string, User, UserWrapper, IUser>.Create(WrappedInterface.ToDictionary(p => p.Key, p => p.Value)));

        [Test]
        public void Enumerate_Wrapper()
        {
            var count = 0;
            foreach (var item in Wrapped)
            {
                Assert.AreEqual(UsersArray[count].Key, item.Key);
                Assert.AreEqual(UsersArray[count].Value.FirstName, item.Value.FirstName);
                Assert.AreEqual(UsersArray[count].Value.LastName, item.Value.LastName);
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
                Assert.AreEqual(UsersArray[count].Key, item.Key);
                Assert.AreEqual(UsersArray[count].Value.FirstName, item.Value.FirstName);
                Assert.AreEqual(UsersArray[count].Value.LastName, item.Value.LastName);
                ++count;
            }
            Assert.AreEqual(2, count);
        }

        [Test]
        public void Count_Wrapper() => Assert.AreEqual(2, Wrapped.Count);

        [Test]
        public void Count_Interface() => Assert.AreEqual(2, WrappedInterface.Count);

        [Test]
        public void IndexerGet_Wrapper()
        {
            Assert.AreEqual(User1.FirstName, Wrapped["jdoe"].FirstName);
            Assert.AreEqual(User1.LastName, Wrapped["jdoe"].LastName);
            Assert.AreEqual(User2.FirstName, Wrapped["jhorner"].FirstName);
            Assert.AreEqual(User2.LastName, Wrapped["jhorner"].LastName);
        }

        [Test]
        public void IndexerGet_Interface()
        {
            Assert.AreEqual(User1.FirstName, WrappedInterface["jdoe"].FirstName);
            Assert.AreEqual(User1.LastName, WrappedInterface["jdoe"].LastName);
            Assert.AreEqual(User2.FirstName, WrappedInterface["jhorner"].FirstName);
            Assert.AreEqual(User2.LastName, WrappedInterface["jhorner"].LastName);
        }

        [Test]
        public void Keys_Wrapper() => CollectionAssert.AreEqual(Users.Keys, Wrapped.Keys);

        [Test]
        public void Keys_Interface() => CollectionAssert.AreEqual(Users.Keys, WrappedInterface.Keys);

        [Test]
        public void Values_Wrapper() => CollectionAssert.AreEqual(new EnumerableWrapper<User, UserWrapper, IUser>(Users.Values), Wrapped.Values);

        [Test]
        public void Values_Interface() => CollectionAssert.AreEqual(new EnumerableWrapper<User, UserWrapper, IUser>(Users.Values), WrappedInterface.Values);

        [Test]
        public void ContainsKey_Wrapper_True()
        {
            Assert.IsTrue(Wrapped.ContainsKey("jdoe"));
            Assert.IsTrue(Wrapped.ContainsKey("jhorner"));
        }

        [Test]
        public void ContainsKey_Wrapper_False() => Assert.IsFalse(Wrapped.ContainsKey("gwashington"));

        [Test]
        public void ContainsKey_Interface_True()
        {
            Assert.IsTrue(WrappedInterface.ContainsKey("jdoe"));
            Assert.IsTrue(WrappedInterface.ContainsKey("jhorner"));
        }

        [Test]
        public void ContainsKey_Interface_False() => Assert.IsFalse(WrappedInterface.ContainsKey("gwashington"));

        [Test]
        public void TryGetValue_Wrapper_True()
        {
            Assert.IsTrue(Wrapped.TryGetValue("jdoe", out var user));
            Assert.AreEqual((UserWrapper)User1, user);
            Assert.IsTrue(Wrapped.TryGetValue("jhorner", out user));
            Assert.AreEqual((UserWrapper)User2, user);
        }

        [Test]
        public void TryGetValue_Wrapper_False() => Assert.IsFalse(Wrapped.TryGetValue("gwashington", out _));

        [Test]
        public void TryGetValue_Interface_True()
        {
            Assert.IsTrue(WrappedInterface.TryGetValue("jdoe", out var user));
            Assert.AreEqual((UserWrapper)User1, user);
            Assert.IsTrue(WrappedInterface.TryGetValue("jhorner", out user));
            Assert.AreEqual((UserWrapper)User2, user);
        }

        [Test]
        public void TryGetValue_Interface_False() => Assert.IsFalse(WrappedInterface.TryGetValue("gwashington", out _));
    }

    public sealed class ReadOnlyDictionaryWrapperStandardTests : ReadOnlyDictionaryWrapperTests
    {
        [SetUp]
        public override void Setup()
        {
            base.Setup();
            Wrapped = ReadOnlyDictionaryWrapper<string, User, UserWrapper, IUser>.Create(Users);
        }
    }

    public sealed class ReadOnlyDictionaryWrapperCastedTests : ReadOnlyDictionaryWrapperTests
    {
        [SetUp]
        public override void Setup()
        {
            base.Setup();
            Wrapped = ReadOnlyDictionaryWrapper<string, User, UserWrapper, IUser>.Create(ReadOnlyDictionaryWrapper<string, User, UserWrapper, IUser>.Create(Users).ToDictionary(p => p.Key, p => p.Value));
        }
    }
}
