using System;

namespace Company.OtherNamespace
{
    public class Other
    {
        public DateTime? this[string name, int? index]
        {
            get => null;
            set { }
        }

        public string this[Base b] { set { } }

        public int? Count { get; set; }

        public string[] StringArray { get; set; }

        public event FieldChangeEventHandler FieldChange;

        public void InvokeFieldChange()
        {
            FieldChange?.Invoke(this, new FieldChangeEventArgs());
        }

        public void Open(params int[] indices)
        {
        }
    }
}