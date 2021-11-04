using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace WrapIt
{
    internal class MethodData : IEquatable<MethodData>
    {
        public string Name { get; }

        public TypeData ReturnType { get; }

        public List<ParameterData> Parameters { get; } = new List<ParameterData>();

        public bool OverrideObject { get; }

        public MemberGeneration Generation { get; }

        public bool IsStatic { get; }

        public TypeData? DeclaringInterfaceType { get; set; }

        public IEnumerable<XElement> Documentation { get; set; } = Enumerable.Empty<XElement>();

        public string? ObsoleteMessage { get; set; }

        public MethodData(string name, TypeData returnType, List<ParameterData> parameters, bool overrideObject, MemberGeneration generation, bool isStatic)
        {
            Name = name;
            ReturnType = returnType;
            Parameters = parameters;
            OverrideObject = overrideObject;
            Generation = generation;
            IsStatic = isStatic;
        }

        public override int GetHashCode() => Name.GetHashCode() ^ ReturnType.GetHashCode();

        public override bool Equals(object? obj) => Equals(obj as MethodData);

        public bool Equals(MethodData? other) => Equals(other, checkReturnType: true);

        public bool Equals(MethodData? other, bool checkReturnType) => other != null && Name == other.Name && (!checkReturnType || ReturnType.Equals(other.ReturnType)) && Parameters.Count == other.Parameters.Count && Parameters.Select((p, i) => (p, i)).All(t => t.p.Equals(other.Parameters[t.i]));
    }
}