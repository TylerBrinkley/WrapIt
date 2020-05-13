using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using WrapIt.Collections;

namespace WrapIt.Tests
{
    public abstract class EnumerableWrapperTests : EnumerableWrapperBaseTests<IEnumerable<User>, EnumerableWrapper<User, UserWrapper, IUser>, IEnumerable<IUser>>
    {
        protected override EnumerableWrapper<User, UserWrapper, IUser> Create(IEnumerable<IUser> users) => CollectionWrapper<User, UserWrapper, IUser>.Create(users);

        protected override IEnumerable<IUser> CreateNewInstance(IEnumerable<IUser> users) => users.ToList();

        public override void Setup()
        {
            base.Setup();
            Users = new List<User> { User1, User2 };
        }

        [Test]
        public void ToCollection() => CollectionAssert.AreEqual(Users, Wrapped.ToCollection());
    }

    public sealed class EnumerableWrapperStandardTests : EnumerableWrapperTests
    {
        [SetUp]
        public override void Setup()
        {
            base.Setup();
            Wrapped = new EnumerableWrapper<User, UserWrapper, IUser>(Users);
        }
    }

    public sealed class EnumerableWrapperCastedTests : EnumerableWrapperTests
    {
        [SetUp]
        public override void Setup()
        {
            base.Setup();
            Wrapped = new EnumerableWrapper<User, UserWrapper, IUser>(new EnumerableWrapper<User, UserWrapper, IUser>(Users).ToList());
        }
    }
}
