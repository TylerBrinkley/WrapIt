using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WrapIt
{
    internal class DictionaryData : InterfaceData
    {
        public TypeData KeyType { get; }

        public TypeData ValueType { get; }

        public DictionaryData(Type type, TypeName className, TypeName interfaceName, TypeData keyType, TypeData valueType)
            : base(type, className, interfaceName, TypeBuildStatus.NotYetBuilt)
        {
            KeyType = keyType;
            DependentTypes.UnionWith(keyType.GetPublicTypes());
            ValueType = valueType;
            DependentTypes.UnionWith(valueType.GetPublicTypes());
        }

        public override async Task BuildAsync(WrapperBuilder builder, HashSet<TypeData> typeDatas, Func<Type, string, CancellationToken, Task<TextWriter>> writerProvider, DocumentationProvider? documentationProvider, CancellationToken cancellationToken = default)
        {
            BuildStatus = TypeBuildStatus.Built;

            foreach (var dependentType in DependentTypes)
            {
                if (dependentType.BuildStatus == TypeBuildStatus.NotYetBuilt)
                {
                    await dependentType.BuildAsync(builder, typeDatas, writerProvider, documentationProvider, cancellationToken).ConfigureAwait(false);
                }
            }

            foreach (var dependentType in ClassDependentTypes)
            {
                if (dependentType.BuildStatus == TypeBuildStatus.NotYetBuilt)
                {
                    await dependentType.BuildAsync(builder, typeDatas, writerProvider, documentationProvider, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        public override string GetCodeToConvertToActualType(string parameterName) => ClassName != InterfaceName ? $"{parameterName}?.ToCollection()" : base.GetCodeToConvertToActualType(parameterName);

        public override string GetCodeToConvertToClassType(string parameterName) => ClassName != InterfaceName ? $"{ClassName}.Create({parameterName})" : base.GetCodeToConvertToClassType(parameterName);

        public override IEnumerable<TypeData> GetPublicTypes() => base.GetPublicTypes().Concat(KeyType.GetPublicTypes()).Concat(ValueType.GetPublicTypes());
    }
}