using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace WrapIt
{
    internal class EventData : IEquatable<EventData>
    {
        public DelegateData Type { get; }

        public string Name { get; }

        public MemberGeneration Generation { get; }

        public TypeData? DeclaringInterfaceType { get; set; }

        public IEnumerable<XElement> Documentation { get; set; } = Enumerable.Empty<XElement>();

        public string? ObsoleteMessage { get; set; }

        public EventData(DelegateData type, string name, MemberGeneration generation)
        {
            Type = type;
            Name = name;
            Generation = generation;
        }

        public override int GetHashCode() => Name.GetHashCode() ^ Type.GetHashCode();

        public override bool Equals(object? obj) => Equals(obj as EventData);

        public bool Equals(EventData? other) => other != null && Name == other.Name && Type.Equals(other.Type);
    }
}