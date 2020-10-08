using System;
using Company;

namespace OtherNamespace
{
    public partial class OtherWrapper : IOther
    {
        /// <summary>
        /// The conversion operator for wrapping the <see cref="OtherNamespace.Other"/> object.
        /// </summary>
        /// <param name="object">The object to wrap.</param>
        public static implicit operator OtherWrapper(OtherNamespace.Other @object) => @object != null ? new OtherWrapper(@object) : null;

        /// <summary>
        /// The conversion operator for unwrapping the <see cref="OtherNamespace.Other"/> object.
        /// </summary>
        /// <param name="object">The object to unwrap.</param>
        public static implicit operator OtherNamespace.Other(OtherWrapper @object) => @object?.Object;

        /// <summary>
        /// The wrapped object.
        /// </summary>
        public OtherNamespace.Other Object { get; private set; }

        public int? Count { get => Object.Count; set => Object.Count = value; }

        public DateTime? this[string name, int? index] { get => Object[name, index]; set => Object[name, index] = value; }

        public string this[BaseWrapper b] { set => Object[b] = value; }

        string IOther.this[IBase b] { set => this[(BaseWrapper)b] = value; }

        public string[] StringArray { get => Object.StringArray; set => Object.StringArray = value; }

        public event FieldChangeEventHandlerWrapper FieldChange
        {
            add => AddOrRemoveFieldChange(value, true);
            remove => AddOrRemoveFieldChange(value, false);
        }

        private void AddOrRemoveFieldChange(FieldChangeEventHandlerWrapper value, bool toAdd)
        {
            if (value != null)
            {
                OtherNamespace.FieldChangeEventHandler handler = (source, e) => value(source is OtherNamespace.Other o ? (OtherWrapper)o : source, (FieldChangeEventArgsWrapper)e);
                if (toAdd)
                {
                    Object.FieldChange += handler;
                }
                else
                {
                    Object.FieldChange -= handler;
                }
            }
        }

        /// <summary>
        /// The wrapper constructor.
        /// </summary>
        /// <param name="object">The object to wrap.</param>
        public OtherWrapper(OtherNamespace.Other @object)
        {
            Object = @object ?? throw new ArgumentNullException(nameof(@object));
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>
        public override bool Equals(object obj) => Object.Equals(obj is OtherWrapper o ? o.Object : obj);

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode() => Object.GetHashCode();

        public void Open(params int[] indices) => Object.Open(indices);
    }
}