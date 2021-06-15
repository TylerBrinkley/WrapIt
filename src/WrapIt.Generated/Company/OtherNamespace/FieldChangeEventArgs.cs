using System;

namespace Company.OtherNamespace
{
    public class FieldChangeEventArgs : EventArgs
    {
        public int BorrowerPair { get; }

        public string FieldId { get; }

        public string NewValue { get; }

        public string PriorValue { get; }
    }
}