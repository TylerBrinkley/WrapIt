using System;

namespace OtherNamespace
{
    public partial class FieldChangeEventArgsWrapper : IFieldChangeEventArgs
    {
        public static implicit operator FieldChangeEventArgsWrapper(OtherNamespace.FieldChangeEventArgs @object) => @object != null ? new FieldChangeEventArgsWrapper(@object) : null;

        public static implicit operator OtherNamespace.FieldChangeEventArgs(FieldChangeEventArgsWrapper @object) => @object?.Object;

        public OtherNamespace.FieldChangeEventArgs Object { get; private set; }

        public int BorrowerPair => Object.BorrowerPair;

        public string FieldId => Object.FieldId;

        public string NewValue => Object.NewValue;

        public string PriorValue => Object.PriorValue;

        public FieldChangeEventArgsWrapper(OtherNamespace.FieldChangeEventArgs @object)
        {
            Object = @object ?? throw new ArgumentNullException(nameof(@object));
        }

        public override bool Equals(object obj) => Object.Equals(obj is FieldChangeEventArgsWrapper o ? o.Object : obj);

        public override int GetHashCode() => Object.GetHashCode();
    }
}