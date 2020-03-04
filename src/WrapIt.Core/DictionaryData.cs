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

        public DictionaryData(Type type, TypeName className, TypeName interfaceName, TypeData keyType, TypeData valueType, WrapperBuilder builder, HashSet<TypeData> typeDatas)
            : base(type, className, interfaceName, TypeBuildStatus.NotYetBuilt)
        {
            var dictionaryWrapperData = builder.GetTypeData(typeof(DictionaryWrapperData), typeDatas);
            if (dictionaryWrapperData.BuildStatus == TypeBuildStatus.NotBuilding)
            {
                dictionaryWrapperData.BuildStatus = TypeBuildStatus.NotYetBuilt;
            }
            DependentTypes.UnionWith(dictionaryWrapperData.GetPublicTypes());
            KeyType = keyType;
            DependentTypes.UnionWith(keyType.GetPublicTypes());
            ValueType = valueType;
            DependentTypes.UnionWith(valueType.GetPublicTypes());
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

        public override string GetCodeToConvertToActualType(string parameterName) => ClassName != InterfaceName ? $"{parameterName}?.ToDictionary()" : base.GetCodeToConvertToActualType(parameterName);

        public override string GetCodeToConvertToClassType(string parameterName) => ClassName != InterfaceName ? $"{ClassName}.Create({parameterName})" : base.GetCodeToConvertToClassType(parameterName);

        public override IEnumerable<TypeData> GetPublicTypes() => base.GetPublicTypes().Concat(KeyType.GetPublicTypes()).Concat(ValueType.GetPublicTypes());
    }
}