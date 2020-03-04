using System;
using System.Collections.Generic;
using System.Linq;

namespace WrapIt
{
    internal class TypeName : IEquatable<TypeName>
    {
        public string? Namespace { get; }

        public string Name { get; }

        public virtual string FullName => Namespace != null ? $"{Namespace}.{Name}" : Name;

        public bool UseFullName { get; }

        public TypeName(string name)
            : this(null, name)
        {
        }

        public TypeName(string? @namespace, string name, bool useFullName = false)
        {
            Namespace = @namespace;
            Name = name;
            UseFullName = useFullName;
        }

        public virtual IEnumerable<string> GetNamespaces()
        {
            if (Namespace != null && !UseFullName)
            {
                yield return Namespace;
            }
        }

        public override string ToString() => UseFullName ? FullName : Name;

        public static bool operator ==(TypeName? left, TypeName? right) => left is null ? right is null : left.Equals(right);

        public static bool operator !=(TypeName? left, TypeName? right) => !(left == right);

        public override bool Equals(object obj) => Equals(obj as TypeName);

        public bool Equals(TypeName? other) => FullName == other?.FullName;

        public override int GetHashCode() => FullName.GetHashCode();
    }

    internal sealed class GenericTypeName : TypeName
    {
        public IReadOnlyList<TypeName> GenericTypeArguments { get; }

        public override string FullName => $"{base.FullName}<{string.Join(", ", GenericTypeArguments.Select(a => a.FullName))}>";

        public GenericTypeName(string @namespace, string name, IEnumerable<TypeName> genericTypeArguments)
            : base(@namespace, name)
        {
            GenericTypeArguments = genericTypeArguments.ToList().AsReadOnly();
        }

        public override IEnumerable<string> GetNamespaces()
        {
            var namespaces = base.GetNamespaces();
            foreach (var genericTypeArg in GenericTypeArguments)
            {
                namespaces = namespaces.Concat(genericTypeArg.GetNamespaces());
            }
            return namespaces;
        }

        public override string ToString() => UseFullName ? FullName : $"{Name}<{string.Join(", ", GenericTypeArguments)}>";
    }

    internal sealed class NullableTypeName : TypeName
    {
        public TypeName NonNullableTypeName { get; }

        public override string FullName => $"{NonNullableTypeName.FullName}?";

        public NullableTypeName(TypeName nonNullableTypeName)
            : base(nonNullableTypeName.Namespace, $"{nonNullableTypeName.Name}?")
        {
            NonNullableTypeName = nonNullableTypeName;
        }

        public override IEnumerable<string> GetNamespaces() => NonNullableTypeName.GetNamespaces();

        public override string ToString() => UseFullName ? FullName : $"{NonNullableTypeName}?";
    }

    internal sealed class RefTypeName : TypeName
    {
        public TypeName NonRefTypeName { get; }

        public override string FullName => $"{NonRefTypeName.FullName}&";

        public RefTypeName(TypeName nonRefTypeName)
            : base(nonRefTypeName.Namespace, $"{nonRefTypeName.Name}&")
        {
            NonRefTypeName = nonRefTypeName;
        }

        public override IEnumerable<string> GetNamespaces() => NonRefTypeName.GetNamespaces();

        public override string ToString() => UseFullName ? FullName : NonRefTypeName.ToString();
    }

    internal sealed class ArrayTypeName : TypeName
    {
        public TypeName ElementTypeName { get; }

        public override string FullName => $"{ElementTypeName.FullName}[]";

        public ArrayTypeName(TypeName elementTypeName)
            : base(elementTypeName.Namespace, $"{elementTypeName.Name}[]")
        {
            ElementTypeName = elementTypeName;
        }

        public override IEnumerable<string> GetNamespaces() => ElementTypeName.GetNamespaces();

        public override string ToString() => UseFullName ? FullName : $"{ElementTypeName}[]";
    }
}