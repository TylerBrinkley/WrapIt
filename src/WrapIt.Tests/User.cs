using System;

namespace WrapIt.Tests
{
    public sealed class User
    {
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public User(string firstName, string lastName)
        {
            FirstName = firstName;
            LastName = lastName;
        }
    }

    public sealed class UserWrapper : IUser
    {
        public static implicit operator UserWrapper(User @object) => @object != null ? new UserWrapper(@object) : null;

        public static implicit operator User(UserWrapper @object) => @object?.Object;

        public User Object { get; private set; }

        public string FirstName { get => Object.FirstName; set => Object.FirstName = value; }

        public string LastName { get => Object.LastName; set => Object.LastName = value; }

        public UserWrapper(User @object)
        {
            Object = @object ?? throw new ArgumentNullException(nameof(@object));
        }

        public override bool Equals(object obj) => Object.Equals(obj is UserWrapper o ? o.Object : obj);

        public override int GetHashCode() => Object.GetHashCode();
    }

    public interface IUser
    {
        string FirstName { get; set; }
        string LastName { get; set; }
    }
}
