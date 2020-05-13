using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using WrapIt.Collections;

namespace WrapIt.Tests
{
    public abstract class ArrayWrapperTests : ListWrapperBaseTests<User[], ArrayWrapper<User, UserWrapper, IUser>>
    {
        protected override ArrayWrapper<User, UserWrapper, IUser> Create(IList<IUser> users) => ArrayWrapper<User, UserWrapper, IUser>.Create(users);

        protected override IList<IUser> CreateNewInstance(IList<IUser> users) => users.ToList();

        public override void Setup()
        {
            base.Setup();
            Users = new[] { User1, User2 };
        }

        [Test]
        public void ToCollection() => CollectionAssert.AreEqual(Users, Wrapped.ToCollection());

        [Test]
        public void Add_Throws() => Assert.Throws<NotSupportedException>(() => WrappedInterface.Add((UserWrapper)User1));

        [Test]
        public void Insert_Throws() => Assert.Throws<NotSupportedException>(() => WrappedInterface.Insert(0, (UserWrapper)User1));

        [Test]
        public void Remove_Throws() => Assert.Throws<NotSupportedException>(() => WrappedInterface.Remove((UserWrapper)User1));

        [Test]
        public void RemoveAt_Throws() => Assert.Throws<NotSupportedException>(() => WrappedInterface.RemoveAt(0));

        [Test]
        public void Clear_Throws() => Assert.Throws<NotSupportedException>(() => WrappedInterface.Clear());
    }

    public class ArrayWrapperStandardTests : ArrayWrapperTests
    {
        [SetUp]
        public override void Setup()
        {
            base.Setup();
            Wrapped = ArrayWrapper<User, UserWrapper, IUser>.Create(Users);
        }
    }

    public class ArrayWrapperCastedTests : ArrayWrapperTests
    {
        [SetUp]
        public override void Setup()
        {
            base.Setup();
            Wrapped = ArrayWrapper<User, UserWrapper, IUser>.Create(ArrayWrapper<User, UserWrapper, IUser>.Create(Users).ToList());
        }
    }
}
