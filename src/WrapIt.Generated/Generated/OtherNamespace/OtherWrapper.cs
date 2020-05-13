using System;
using Company;

namespace OtherNamespace
{
    public partial class OtherWrapper : IOther
    {
        public static implicit operator OtherWrapper(OtherNamespace.Other @object) => @object != null ? new OtherWrapper(@object) : null;

        public static implicit operator OtherNamespace.Other(OtherWrapper @object) => @object?.Object;

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

        public OtherWrapper(OtherNamespace.Other @object)
        {
            Object = @object ?? throw new ArgumentNullException(nameof(@object));
        }

        public override bool Equals(object obj) => Object.Equals(obj is OtherWrapper o ? o.Object : obj);

        public override int GetHashCode() => Object.GetHashCode();
    }
}