using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WrapIt
{
    internal class ArrayData : TypeData
    {
        public TypeData ElementType { get; }

        public ArrayData(Type type, TypeName className, TypeName interfaceName, TypeData elementType, WrapperBuilder builder, HashSet<TypeData> typeDatas)
            : base(type, className, interfaceName, TypeBuildStatus.NotYetBuilt)
        {
            ElementType = elementType;
            DependentTypes.UnionWith(elementType.GetPublicTypes());
        }

        public override async Task BuildAsync(WrapperBuilder builder, HashSet<TypeData> typeDatas, Func<Type, string, CancellationToken, Task<TextWriter>> writerProvider, CancellationToken cancellationToken = default)
        {
            BuildStatus = TypeBuildStatus.Built;

            foreach (var dependentType in DependentTypes)
            {
                if (dependentType.BuildStatus == TypeBuildStatus.NotYetBuilt)
                {
                    await dependentType.BuildAsync(builder, typeDatas, writerProvider, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        public override string GetCodeToConvertToActualType(string parameterName) => ClassName != InterfaceName ? $"{parameterName}?.ToArray()" : base.GetCodeToConvertToActualType(parameterName);

        public override string GetCodeToConvertToClassType(string parameterName) => ClassName != InterfaceName ? $"{ClassName}.Create({parameterName})" : base.GetCodeToConvertToClassType(parameterName);

        public override IEnumerable<TypeData> GetPublicTypes() => base.GetPublicTypes().Concat(ElementType.GetPublicTypes());
    }
}