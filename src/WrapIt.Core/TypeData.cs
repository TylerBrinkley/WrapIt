using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace WrapIt
{
    internal class TypeData : IEquatable<TypeData>
    {
        public static readonly HashSet<TypeData> DefaultTypes = new HashSet<TypeData>
        {
            new TypeData(typeof(object), new TypeName("object")),
            new TypeData(typeof(string), new TypeName("string")),
            new TypeData(typeof(sbyte), new TypeName("sbyte")),
            new TypeData(typeof(byte), new TypeName("byte")),
            new TypeData(typeof(short), new TypeName("short")),
            new TypeData(typeof(ushort), new TypeName("ushort")),
            new TypeData(typeof(int), new TypeName("int")),
            new TypeData(typeof(uint), new TypeName("uint")),
            new TypeData(typeof(long), new TypeName("long")),
            new TypeData(typeof(ulong), new TypeName("ulong")),
            new TypeData(typeof(decimal), new TypeName("decimal")),
            new TypeData(typeof(float), new TypeName("float")),
            new TypeData(typeof(double), new TypeName("double")),
            new TypeData(typeof(char), new TypeName("char")),
            new TypeData(typeof(bool), new TypeName("bool")),
            new TypeData(typeof(void), new TypeName("void"))
        };

        public Type Type { get; }

        public TypeName ClassName { get; }

        public TypeName InterfaceName { get; }

        public TypeBuildStatus BuildStatus { get; set; }

        public HashSet<TypeData> DependentTypes { get; } = new HashSet<TypeData>();

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        public TypeData(Type type)
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        {
            Type = type;
        }

        public TypeData(Type type, TypeName name, TypeBuildStatus buildStatus = TypeBuildStatus.NotBuilding)
            : this(type, name, name, buildStatus)
        {
        }

        public TypeData(Type type, TypeName className, TypeName interfaceName, TypeBuildStatus buildStatus)
        {
            Type = type;
            ClassName = className;
            InterfaceName = interfaceName;
            BuildStatus = buildStatus;
        }

        public static bool operator ==(TypeData? left, TypeData? right) => left is null ? right is null : left.Equals(right);

        public static bool operator !=(TypeData? left, TypeData? right) => !(left == right);

        public bool Equals(TypeData? other) => Type == other?.Type;

        public override bool Equals(object? obj) => Equals(obj as TypeData);

        public override int GetHashCode() => Type.GetHashCode();

        public override string ToString() => Type.ToString();

        public virtual Task BuildAsync(WrapperBuilder builder, HashSet<TypeData> typeDatas, Func<Type, string, CancellationToken, Task<TextWriter>> writerProvider, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public virtual string GetCodeToConvertToActualType(string parameterName) => parameterName;

        public virtual string GetCodeToConvertToClassType(string parameterName) => ClassName != InterfaceName ? $"({ClassName}){parameterName}" : parameterName;

        public virtual IEnumerable<TypeData> GetPublicTypes()
        {
            yield return this;
        }
    }
}