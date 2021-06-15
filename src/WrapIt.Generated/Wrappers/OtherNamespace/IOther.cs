using System;

namespace Wrappers.OtherNamespace
{
    public partial interface IOther
    {
        int? Count { get; set; }
        DateTime? this[string name, int? index] { get; set; }
        string this[IBase b] { set; }
        string[] StringArray { get; set; }

        event FieldChangeEventHandlerWrapper FieldChange;

        void InvokeFieldChange();
        void Open(params int[] indices);
    }
}