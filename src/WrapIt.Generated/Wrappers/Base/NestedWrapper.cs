using System;

namespace Wrappers.Base
{
    /// <inheritdoc cref="INested"/>
    public partial class NestedWrapper : INested
    {
        /// <summary>
        /// The conversion operator for wrapping the <see cref="Company.Base.Nested"/> object.
        /// </summary>
        /// <param name="object">The object to wrap.</param>
        public static implicit operator NestedWrapper(Company.Base.Nested @object) => @object != null ? new NestedWrapper(@object) : null;

        /// <summary>
        /// The conversion operator for unwrapping the <see cref="Company.Base.Nested"/> object.
        /// </summary>
        /// <param name="object">The object to unwrap.</param>
        public static implicit operator Company.Base.Nested(NestedWrapper @object) => @object?.Object;

        /// <summary>
        /// The wrapped object.
        /// </summary>
        public Company.Base.Nested Object { get; private set; }

        /// <inheritdoc/>
        public DateTimeKind Kind { get => Object.Kind; set => Object.Kind = value; }

        /// <summary>
        /// The wrapper constructor.
        /// </summary>
        /// <param name="object">The object to wrap.</param>
        public NestedWrapper(Company.Base.Nested @object)
        {
            Object = @object ?? throw new ArgumentNullException(nameof(@object));
        }

        public NestedWrapper()
            : this(new Company.Base.Nested())
        {
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) => Object.Equals(obj is NestedWrapper o ? o.Object : obj);

        /// <inheritdoc/>
        public override int GetHashCode() => Object.GetHashCode();
    }
}