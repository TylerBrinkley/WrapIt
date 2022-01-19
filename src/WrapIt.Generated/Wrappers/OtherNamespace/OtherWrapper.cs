using System;
using System.Threading;

namespace Wrappers.OtherNamespace
{
    public partial class OtherWrapper : IOther
    {
        /// <summary>
        /// The conversion operator for wrapping the <see cref="Company.OtherNamespace.Other"/> object.
        /// </summary>
        /// <param name="object">The object to wrap.</param>
        public static implicit operator OtherWrapper(Company.OtherNamespace.Other @object) => @object != null ? new OtherWrapper(@object) : null;

        /// <summary>
        /// The conversion operator for unwrapping the <see cref="Company.OtherNamespace.Other"/> object.
        /// </summary>
        /// <param name="object">The object to unwrap.</param>
        public static implicit operator Company.OtherNamespace.Other(OtherWrapper @object) => @object?.Object;

        /// <summary>
        /// The wrapped object.
        /// </summary>
        public Company.OtherNamespace.Other Object { get; private set; }

        public int? Count { get => Object.Count; set => Object.Count = value; }

        public DateTime? this[string name, int? index] { get => Object[name, index]; set => Object[name, index] = value; }

        public string this[BaseWrapper b] { set => Object[b] = value; }

        string IOther.this[IBase b] { set => this[(BaseWrapper)b] = value; }

        public string[] StringArray { get => Object.StringArray; set => Object.StringArray = value; }

        public event FieldChangeEventHandlerWrapper FieldChange
        {
            add
            {
                if (value == null)
                {
                    return;
                }
                FieldChangeEventHandlerWrapper handler;
                FieldChangeEventHandlerWrapper handler2 = _fieldChange;
                FieldChangeEventHandlerWrapper combined;
                do
                {
                    handler = handler2;
                    combined = (FieldChangeEventHandlerWrapper)Delegate.Combine(handler, value);
                    handler2 = Interlocked.CompareExchange(ref _fieldChange, combined, handler);
                } while (handler != handler2);
                if (handler == null)
                {
                    Object.FieldChange += FieldChangeHandler;
                }
            }
            remove
            {
                if (value == null)
                {
                    return;
                }
                FieldChangeEventHandlerWrapper handler;
                FieldChangeEventHandlerWrapper handler2 = _fieldChange;
                FieldChangeEventHandlerWrapper removed;
                do
                {
                    handler = handler2;
                    removed = (FieldChangeEventHandlerWrapper)Delegate.Remove(handler, value);
                    handler2 = Interlocked.CompareExchange(ref _fieldChange, removed, handler);
                } while (handler != handler2);
                if (removed == null)
                {
                    Object.FieldChange -= FieldChangeHandler;
                }
            }
        }

        private FieldChangeEventHandlerWrapper _fieldChange;

        private void FieldChangeHandler(object source, Company.OtherNamespace.FieldChangeEventArgs e) => _fieldChange?.Invoke(source is Company.OtherNamespace.Other o ? (OtherWrapper)o : source, (FieldChangeEventArgsWrapper)e);

        /// <summary>
        /// The wrapper constructor.
        /// </summary>
        /// <param name="object">The object to wrap.</param>
        public OtherWrapper(Company.OtherNamespace.Other @object)
        {
            Object = @object ?? throw new ArgumentNullException(nameof(@object));
        }

        public OtherWrapper()
            : this(new Company.OtherNamespace.Other())
        {
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) => Object.Equals(obj is OtherWrapper o ? o.Object : obj);

        /// <inheritdoc/>
        public override int GetHashCode() => Object.GetHashCode();

        public void InvokeFieldChange() => Object.InvokeFieldChange();

        public void Open(params int[] indices) => Object.Open(indices);
    }
}