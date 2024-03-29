﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WrapIt
{
    internal class CollectionData : InterfaceData
    {
        public TypeData ElementType { get; }

        public CollectionData(Type type, TypeName className, TypeName interfaceName, TypeData elementType)
            : base(type, className, interfaceName, TypeBuildStatus.NotYetBuilt)
        {
            ElementType = elementType;
            DependentTypes.UnionWith(elementType.GetPublicTypes());
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

        public override string GetCodeToConvertFromActualType(string input) => Type.IsInterface ? $"{ClassName}.Create({input})" : input;

        public override string GetCodeToConvertFromActualTypeToInterface(string input) => $"{ClassName}.Create({input})";

        public override string GetActualName(bool inXmlComment = false)
        {
            if (Type.IsArray)
            {
                return $"{ElementType.GetActualName(inXmlComment)}[]";
            }
            else if (Type.IsGenericType && Type.GetGenericTypeDefinition() == typeof(List<>))
            {
                return $"List{(inXmlComment ? "{" : "<")}{ElementType.GetActualName(inXmlComment)}{(inXmlComment ? "}" : ">")}";
            }
            return base.GetActualName(inXmlComment);
        }

        public override IEnumerable<TypeData> GetPublicTypes() => base.GetPublicTypes().Concat(ElementType.GetPublicTypes());
    }
}