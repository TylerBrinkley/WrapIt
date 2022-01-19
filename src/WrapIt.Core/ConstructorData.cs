using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace WrapIt
{
    internal class ConstructorData : IEquatable<ConstructorData>
    {
        public List<ParameterData> Parameters { get; } = new List<ParameterData>();

        public MemberGeneration Generation { get; }

        public IEnumerable<XElement> Documentation { get; set; } = Enumerable.Empty<XElement>();

        public string? ObsoleteMessage { get; set; }

        public ConstructorData(List<ParameterData> parameters)
        {
            Parameters = parameters;
        }

        public override int GetHashCode() => Parameters.Count;

        public override bool Equals(object? obj) => Equals(obj as ConstructorData);

        public bool Equals(ConstructorData? other) => other != null && Parameters.Count == other.Parameters.Count && Parameters.Select((p, i) => (p, i)).All(t => t.p.Equals(other.Parameters[t.i]));
    }
}