using System;

namespace Wrappers.OtherNamespace
{
    /// <inheritdoc cref="IFieldChangeEventArgs"/>
    public partial class FieldChangeEventArgsWrapper : IFieldChangeEventArgs
    {
        /// <summary>
        /// The conversion operator for wrapping the <see cref="Company.OtherNamespace.FieldChangeEventArgs"/> object.
        /// </summary>
        /// <param name="object">The object to wrap.</param>
        public static implicit operator FieldChangeEventArgsWrapper(Company.OtherNamespace.FieldChangeEventArgs @object) => @object != null ? new FieldChangeEventArgsWrapper(@object) : null;

        /// <summary>
        /// The conversion operator for unwrapping the <see cref="Company.OtherNamespace.FieldChangeEventArgs"/> object.
        /// </summary>
        /// <param name="object">The object to unwrap.</param>
        public static implicit operator Company.OtherNamespace.FieldChangeEventArgs(FieldChangeEventArgsWrapper @object) => @object?.Object;

        /// <summary>
        /// The wrapped object.
        /// </summary>
        public Company.OtherNamespace.FieldChangeEventArgs Object { get; private set; }

        /// <inheritdoc/>
        public int BorrowerPair => Object.BorrowerPair;

        /// <inheritdoc/>
        public string FieldId => Object.FieldId;

        /// <inheritdoc/>
        public string NewValue => Object.NewValue;

        /// <inheritdoc/>
        public string PriorValue => Object.PriorValue;

        /// <summary>
        /// The wrapper constructor.
        /// </summary>
        /// <param name="object">The object to wrap.</param>
        public FieldChangeEventArgsWrapper(Company.OtherNamespace.FieldChangeEventArgs @object)
        {
            Object = @object ?? throw new ArgumentNullException(nameof(@object));
        }

        public FieldChangeEventArgsWrapper()
            : this(new Company.OtherNamespace.FieldChangeEventArgs())
        {
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) => Object.Equals(obj is FieldChangeEventArgsWrapper o ? o.Object : obj);

        /// <inheritdoc/>
        public override int GetHashCode() => Object.GetHashCode();
    }
}