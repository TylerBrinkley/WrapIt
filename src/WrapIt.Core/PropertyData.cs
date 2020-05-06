using System;
using System.Collections.Generic;
using System.Linq;

namespace WrapIt
{
    internal class PropertyData : IEquatable<PropertyData>
    {
        public TypeData Type { get; }

        public string Name { get; }

        public bool HasGetter { get; }

        public bool HasSetter { get; }

        public List<ParameterData> Parameters { get; }

        public MemberGeneration Generation { get; }

        public TypeData? DeclaringInterfaceType { get; set; }

        public PropertyData(TypeData type, string name, bool hasGetter, bool hasSetter, List<ParameterData> parameters, MemberGeneration generation)
        {
            Type = type;
            Name = name;
            HasGetter = hasGetter;
            HasSetter = hasSetter;
            Parameters = parameters;
            Generation = generation;
        }

        public override int GetHashCode() => Name.GetHashCode() ^ Type.GetHashCode();

        public override bool Equals(object? obj) => Equals(obj as PropertyData);

        public bool Equals(PropertyData? other) => other != null && Name == other.Name && Type.Equals(other.Type) && HasGetter == other.HasGetter && HasSetter == other.HasSetter && Parameters.Count == other.Parameters.Count && Parameters.Select((p, i) => (p, i)).All(t => t.p.Equals(other.Parameters[t.i]));
    }
}