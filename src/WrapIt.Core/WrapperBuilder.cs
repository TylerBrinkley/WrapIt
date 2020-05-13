﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace WrapIt
{
    public sealed class WrapperBuilder
    {
        private HashSet<Type> _rootTypes = new HashSet<Type>();
        private HashSet<Assembly> _assembliesWithTypesToWrap = new HashSet<Assembly>();

        public ICollection<Type> RootTypes { get => _rootTypes; set => _rootTypes = new HashSet<Type>(value ?? throw new ArgumentNullException(nameof(value))); }

        public ICollection<Assembly> AssembliesWithTypesToWrap { get => _assembliesWithTypesToWrap; set => _assembliesWithTypesToWrap = new HashSet<Assembly>(value ?? throw new ArgumentNullException(nameof(value))); }

        /// <summary>
        /// If <c>null</c> defaults to "{0}.{1}Wrapper" where {0} is Type.Namespace and {1} is Type.Name.
        /// </summary>
        public Func<string, string, string>? ClassFullNameFormat { get; set; }

        /// <summary>
        /// If <c>null</c> defaults to "{0}.I{1}" where {0} is Type.Namespace and {1} is Type.Name.
        /// </summary>
        public Func<string, string, string>? InterfaceFullNameFormat { get; set; }

        /// <summary>
        /// If <c>null</c> defaults to "{0}.{1}Wrapper" where {0} is Type.Namespace and {1} is Type.Name.
        /// </summary>
        public Func<string, string, string>? DelegateFullNameFormat { get; set; }

        /// <summary>
        /// If <c>null</c> enums are not wrapped.
        /// </summary>
        public Func<string, string, string>? EnumFullNameFormat { get; set; }

        public Func<Type, PropertyInfo, MemberGeneration>? PropertyResolver { get; set; }

        public Func<Type, MethodInfo, MemberGeneration>? MethodResolver { get; set; }

        public Func<Type, EventInfo, MemberGeneration>? EventResolver { get; set; }

        public Func<Type, Type, bool>? InterfaceResolver { get; set; }

        public Func<Type, bool>? TypeResolver { get; set; }

        public decimal MinCSharpVersion { get; set; } = decimal.MaxValue;

        /// <summary>
        /// For use with MemberGeneration.WrapImplementationInCompilerFlag. Default is "WRAP_IT_DEFAULT_MEMBER_GENERATION".
        /// </summary>
        public string DefaultMemberGenerationCompilerFlag { get; set; } = "WRAP_IT_DEFAULT_MEMBER_GENERATION";

        public async Task BuildAsync(Func<Type, string, CancellationToken, Task<TextWriter>> writerProvider, CancellationToken cancellationToken = default)
        {
            if (writerProvider is null)
            {
                throw new ArgumentNullException(nameof(writerProvider));
            }

            foreach (var rootType in _rootTypes)
            {
                AssembliesWithTypesToWrap.Add(rootType.Assembly);
            }
            var typeDatas = new HashSet<TypeData>(TypeData.DefaultTypes);
            foreach (var rootType in _rootTypes)
            {
                var typeData = GetTypeData(rootType, typeDatas);
                await typeData.BuildAsync(this, typeDatas, writerProvider, cancellationToken).ConfigureAwait(false);
            }
        }

        internal TypeData GetTypeData(Type type, HashSet<TypeData> typeDatas)
        {
            if (!typeDatas.TryGetValue(new TypeData(type), out var typeData))
            {
                var typeName = type.Name;
                var isEnum = type.IsEnum;
                if ((!type.IsValueType || (isEnum && EnumFullNameFormat != null)) && _assembliesWithTypesToWrap.Contains(type.Assembly) && TypeResolver?.Invoke(type) != false)
                {
                    var typeNamespace = type.Namespace;
                    var baseType = type.BaseType;
                    if (isEnum)
                    {
                        var enumFullName = EnumFullNameFormat!(type.Namespace, type.Name);
                        typeData = new EnumData(type, GetTypeName(enumFullName));
                        typeDatas.Add(typeData);
                    }
                    else if (type.IsArray && type.GetArrayRank() == 1)
                    {
                        var elementType = type.GetElementType();
                        var elementTypeData = GetTypeData(elementType, typeDatas);
                        var className = new GenericTypeName("WrapIt.Collections", "ArrayWrapper", new[] { new TypeName(elementType.Namespace, elementType.Name, true), elementTypeData.ClassName, elementTypeData.InterfaceName });
                        var interfaceName = new GenericTypeName("System.Collections.Generic", "IList", new[] { elementTypeData.InterfaceName });
                        typeData = new CollectionData(type, className, interfaceName, elementTypeData);
                        typeDatas.Add(typeData);
                    }
                    else if (baseType == typeof(MulticastDelegate))
                    {
                        var delegateFullName = DelegateFullNameFormat?.Invoke(typeNamespace, typeName) ?? $"{typeNamespace}.{typeName}Wrapper";
                        typeData = new DelegateData(type, GetOtherTypeData(type, typeDatas).ClassName, GetTypeName(delegateFullName), TypeBuildStatus.NotYetBuilt, this, typeDatas);
                        typeDatas.Add(typeData);
                    }
                    else
                    {
                        ClassData? baseTypeData = null;
                        if (baseType != null && _assembliesWithTypesToWrap.Contains(baseType.Assembly) && TypeResolver?.Invoke(baseType) != false)
                        {
                            baseTypeData = (ClassData)GetTypeData(baseType, typeDatas);
                            if (typeDatas.TryGetValue(new TypeData(type), out typeData))
                            {
                                return typeData;
                            }
                        }
                        var classFullName = ClassFullNameFormat?.Invoke(typeNamespace, typeName) ?? $"{typeNamespace}.{typeName}Wrapper";
                        var interfaceFullName = InterfaceFullNameFormat?.Invoke(typeNamespace, typeName) ?? $"{typeNamespace}.I{typeName}";
                        typeData = new ClassData(type, GetTypeName(classFullName), GetTypeName(interfaceFullName), TypeBuildStatus.NotYetBuilt, baseTypeData);
                        typeDatas.Add(typeData);
                        if (baseTypeData != null)
                        {
                            baseTypeData.DirectDerivedTypes.Add((ClassData)typeData);
                        }
                        if (!type.IsSealed)
                        {
                            foreach (var assembly in _assembliesWithTypesToWrap)
                            {
                                foreach (var t in assembly.GetExportedTypes())
                                {
                                    if (type.IsAssignableFrom(t) && t != type)
                                    {
                                        typeData.DependentTypes.Add(GetTypeData(t, typeDatas));
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    typeData = GetOtherTypeData(type, typeDatas);
                    typeDatas.Add(typeData);
                }
            }
            return typeData;
        }

        private TypeData GetOtherTypeData(Type type, HashSet<TypeData> typeDatas)
        {
            TypeName className;
            TypeName? interfaceName = null;
            if (type.IsValueType && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                var nonNullableClassName = GetTypeData(type.GenericTypeArguments[0], typeDatas).ClassName;
                className = new NullableTypeName(nonNullableClassName);
            }
            else if (type.IsArray && type.GetArrayRank() == 1)
            {
                var elementType = type.GetElementType();
                var elementTypeData = GetTypeData(elementType, typeDatas);
                className = new ArrayTypeName(elementTypeData.ClassName);
            }
            else if (type.IsGenericType)
            {
                var genericTypeDefinition = type.GetGenericTypeDefinition();
                if (genericTypeDefinition == typeof(IEnumerable<>))
                {
                    var elementType = type.GenericTypeArguments[0];
                    var elementTypeData = GetTypeData(elementType, typeDatas);
                    if (elementTypeData.BuildStatus != TypeBuildStatus.NotBuilding)
                    {
                        className = new GenericTypeName("WrapIt.Collections", "EnumerableWrapper", new[] { new TypeName(elementType.Namespace, elementType.Name, true), elementTypeData.ClassName, elementTypeData.InterfaceName });
                        interfaceName = new GenericTypeName("System.Collections.Generic", "IEnumerable", new[] { elementTypeData.InterfaceName });
                        return new CollectionData(type, className, interfaceName, elementTypeData);
                    }
                }
                else if (genericTypeDefinition == typeof(ICollection<>))
                {
                    var elementType = type.GenericTypeArguments[0];
                    var elementTypeData = GetTypeData(elementType, typeDatas);
                    if (elementTypeData.BuildStatus != TypeBuildStatus.NotBuilding)
                    {
                        className = new GenericTypeName("WrapIt.Collections", "CollectionWrapper", new[] { new TypeName(elementType.Namespace, elementType.Name, true), elementTypeData.ClassName, elementTypeData.InterfaceName });
                        interfaceName = new GenericTypeName("System.Collections.Generic", "ICollection", new[] { elementTypeData.InterfaceName });
                        return new CollectionData(type, className, interfaceName, elementTypeData);
                    }
                }
                else if (genericTypeDefinition == typeof(List<>) || genericTypeDefinition == typeof(IList<>))
                {
                    var elementType = type.GenericTypeArguments[0];
                    var elementTypeData = GetTypeData(elementType, typeDatas);
                    if (elementTypeData.BuildStatus != TypeBuildStatus.NotBuilding)
                    {
                        className = new GenericTypeName("WrapIt.Collections", "ListWrapper", new[] { new TypeName(elementType.Namespace, elementType.Name, true), elementTypeData.ClassName, elementTypeData.InterfaceName });
                        interfaceName = new GenericTypeName("System.Collections.Generic", "IList", new[] { elementTypeData.InterfaceName });
                        return new CollectionData(type, className, interfaceName, elementTypeData);
                    }
                }
                else if (genericTypeDefinition == typeof(HashSet<>) || genericTypeDefinition == typeof(ISet<>))
                {
                    var elementType = type.GenericTypeArguments[0];
                    var elementTypeData = GetTypeData(elementType, typeDatas);
                    if (elementTypeData.BuildStatus != TypeBuildStatus.NotBuilding)
                    {
                        className = new GenericTypeName("WrapIt.Collections", "SetWrapper", new[] { new TypeName(elementType.Namespace, elementType.Name, true), elementTypeData.ClassName, elementTypeData.InterfaceName });
                        interfaceName = new GenericTypeName("System.Collections.Generic", "ISet", new[] { elementTypeData.InterfaceName });
                        return new CollectionData(type, className, interfaceName, elementTypeData);
                    }
                }
                else if (genericTypeDefinition == typeof(Dictionary<,>) || genericTypeDefinition == typeof(IDictionary<,>))
                {
                    var genericTypeArguments = type.GenericTypeArguments;
                    var keyType = genericTypeArguments[0];
                    var keyTypeData = GetTypeData(keyType, typeDatas);
                    var valueType = genericTypeArguments[1];
                    var valueTypeData = GetTypeData(valueType, typeDatas);
                    if (keyTypeData.BuildStatus == TypeBuildStatus.NotBuilding && valueTypeData.BuildStatus != TypeBuildStatus.NotBuilding)
                    {
                        className = new GenericTypeName("WrapIt.Collections", "DictionaryWrapper", new[] { keyTypeData.ClassName, new TypeName(valueType.Namespace, valueType.Name, true), valueTypeData.ClassName, valueTypeData.InterfaceName });
                        interfaceName = new GenericTypeName("System.Collections.Generic", "IDictionary", new[] { keyTypeData.ClassName, valueTypeData.InterfaceName });
                        return new DictionaryData(type, className, interfaceName, keyTypeData, valueTypeData);
                    }
                }
                else if (genericTypeDefinition == typeof(IReadOnlyCollection<>))
                {
                    var elementType = type.GenericTypeArguments[0];
                    var elementTypeData = GetTypeData(elementType, typeDatas);
                    if (elementTypeData.BuildStatus != TypeBuildStatus.NotBuilding)
                    {
                        className = new GenericTypeName("WrapIt.Collections", "ReadOnlyCollectionWrapper", new[] { new TypeName(elementType.Namespace, elementType.Name, true), elementTypeData.ClassName, elementTypeData.InterfaceName });
                        interfaceName = new GenericTypeName("System.Collections.Generic", "IReadOnlyCollection", new[] { elementTypeData.InterfaceName });
                        return new CollectionData(type, className, interfaceName, elementTypeData);
                    }
                }
                else if (genericTypeDefinition == typeof(IReadOnlyList<>))
                {
                    var elementType = type.GenericTypeArguments[0];
                    var elementTypeData = GetTypeData(elementType, typeDatas);
                    if (elementTypeData.BuildStatus != TypeBuildStatus.NotBuilding)
                    {
                        className = new GenericTypeName("WrapIt.Collections", "ReadOnlyListWrapper", new[] { new TypeName(elementType.Namespace, elementType.Name, true), elementTypeData.ClassName, elementTypeData.InterfaceName });
                        interfaceName = new GenericTypeName("System.Collections.Generic", "IReadOnlyList", new[] { elementTypeData.InterfaceName });
                        return new CollectionData(type, className, interfaceName, elementTypeData);
                    }
                }
                else if (genericTypeDefinition == typeof(IReadOnlyDictionary<,>))
                {
                    var genericTypeArguments = type.GenericTypeArguments;
                    var keyType = genericTypeArguments[0];
                    var keyTypeData = GetTypeData(keyType, typeDatas);
                    var valueType = genericTypeArguments[1];
                    var valueTypeData = GetTypeData(valueType, typeDatas);
                    if (keyTypeData.BuildStatus == TypeBuildStatus.NotBuilding && valueTypeData.BuildStatus != TypeBuildStatus.NotBuilding)
                    {
                        className = new GenericTypeName("WrapIt.Collections", "ReadOnlyDictionaryWrapper", new[] { keyTypeData.ClassName, new TypeName(valueType.Namespace, valueType.Name, true), valueTypeData.ClassName, valueTypeData.InterfaceName });
                        interfaceName = new GenericTypeName("System.Collections.Generic", "IReadOnlyDictionary", new[] { keyTypeData.ClassName, valueTypeData.InterfaceName });
                        return new DictionaryData(type, className, interfaceName, keyTypeData, valueTypeData);
                    }
                }
                var typeName = type.Name.Substring(0, type.Name.IndexOf('`'));
                var genericTypeNames = type.GenericTypeArguments.Select(t => GetTypeData(t, typeDatas));
                className = new GenericTypeName(type.Namespace, typeName, genericTypeNames.Select(t => t.ClassName));
                interfaceName = new GenericTypeName(type.Namespace, typeName, genericTypeNames.Select(t => t.InterfaceName));
            }
            else if (type.IsByRef)
            {
                var nonByRefTypeClassName = GetTypeData(type.GetElementType(), typeDatas).ClassName;
                className = new RefTypeName(nonByRefTypeClassName);
            }
            else
            {
                className = new TypeName(type.Namespace, type.Name, AssembliesWithTypesToWrap.Contains(type.Assembly));
            }
            if (type.BaseType == typeof(MulticastDelegate))
            {
                return new DelegateData(type, className, interfaceName ?? className, TypeBuildStatus.NotBuilding, this, typeDatas);
            }
            else if (type.IsInterface)
            {
                return new InterfaceData(type, className, interfaceName ?? className, TypeBuildStatus.NotBuilding);
            }
            else
            {
                return new TypeData(type, className, interfaceName ?? className, TypeBuildStatus.NotBuilding);
            }
        }

        private TypeName GetTypeName(string fullName)
        {
            var lastPeriodIndex = fullName.LastIndexOf('.');
            return new TypeName(fullName.Substring(0, lastPeriodIndex), fullName.Substring(lastPeriodIndex + 1));
        }
    }
}