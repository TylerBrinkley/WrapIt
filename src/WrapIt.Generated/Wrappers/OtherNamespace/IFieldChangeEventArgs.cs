namespace Wrappers.OtherNamespace
{
    public partial interface IFieldChangeEventArgs
    {
        int BorrowerPair { get; }
        string FieldId { get; }
        string NewValue { get; }
        string PriorValue { get; }
    }
}