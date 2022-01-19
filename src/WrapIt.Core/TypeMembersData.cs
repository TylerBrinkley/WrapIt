using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace WrapIt
{
    internal abstract class TypeMembersData : TypeData
    {
        protected bool _initialized;

        public IEnumerable<InterfaceData> Interfaces => ImplicitInterfaces.Concat(ExplicitInterfaces);

        public List<InterfaceData> ImplicitInterfaces { get; private set; } = new List<InterfaceData>();

        public List<InterfaceData> ExplicitInterfaces { get; private set; } = new List<InterfaceData>();

        public List<PropertyData> Properties { get; private set; } = new List<PropertyData>();

        public List<MethodData> Methods { get; private set; } = new List<MethodData>();

        public List<EventData> Events { get; private set; } = new List<EventData>();

        public IEnumerable<XElement> Documentation { get; set; } = Enumerable.Empty<XElement>();

        public string? ObsoleteMessage { get; set; }

        public TypeMembersData? BaseType { get; internal set; }

        protected TypeMembersData(Type type, TypeName name)
            : base(type, name)
        {
        }

        protected TypeMembersData(Type type, TypeName name, TypeBuildStatus buildStatus = TypeBuildStatus.NotBuilding)
            : base(type, name, buildStatus)
        {
        }

        protected TypeMembersData(Type type, TypeName className, TypeName interfaceName, TypeBuildStatus buildStatus)
            : base(type, className, interfaceName, buildStatus)
        {
        }

        internal virtual Type[] GetInterfaces() => Type.GetInterfaces();

        public virtual void Initialize(WrapperBuilder builder, DocumentationProvider? documentationProvider, HashSet<TypeData> typeDatas, BindingFlags bindingFlags)
        {
            if (_initialized)
            {
                return;
            }

            if (documentationProvider != null)
            {
                Documentation = documentationProvider.GetDocumentation(Type);
            }
            var obsoleteAttribute = Type.GetCustomAttribute<ObsoleteAttribute>();
            if (obsoleteAttribute != null)
            {
                ObsoleteMessage = obsoleteAttribute.Message?.Replace("\"", "\\\"") ?? string.Empty;
            }

            var isIEnumerable = false;
            var isGenericIEnumerable = false;
            foreach (var @interface in GetInterfaces())
            {
                if (@interface.IsPublic && !builder.AssembliesWithTypesToWrap.Contains(@interface.Assembly) && builder.InterfaceResolver?.Invoke(Type, @interface) != false)
                {
                    if (!isIEnumerable && @interface == typeof(IEnumerable))
                    {
                        isIEnumerable = true;
                    }
                    if (!isGenericIEnumerable && @interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    {
                        isGenericIEnumerable = true;
                    }
                    var interfaceTypeData = (InterfaceData)builder.GetTypeData(@interface, typeDatas);
                    interfaceTypeData.Initialize(builder, documentationProvider, typeDatas, bindingFlags);
                    if (@interface == typeof(IList) || @interface == typeof(ICollection))
                    {
                        if (@interface == typeof(IList) || (@interface == typeof(ICollection) && BaseType?.ExplicitInterfaces.Any(i => i.Type == @interface) != true))
                        {
                            ExplicitInterfaces.Add(interfaceTypeData);
                        }
                    }
                    else
                    {
                        ImplicitInterfaces.Add(interfaceTypeData);
                    }
                }
            }

            var propertyInfos = Type.GetProperties(bindingFlags);
            foreach (var property in propertyInfos)
            {
                if (property.GetAccessors().All(a => IncludeMethod(builder, a, typeDatas, out _)))
                {
                    var generation = builder.PropertyResolver?.Invoke(Type, property) ?? MemberGeneration.Full;
                    if (generation != MemberGeneration.None)
                    {
                        var propertyType = property.PropertyType;
                        var propertyTypeData = builder.GetTypeData(propertyType, typeDatas);
                        DependentTypes.UnionWith(propertyTypeData.GetPublicTypes());
                        var indexParameters = property.GetIndexParameters();
                        var parameters = new List<ParameterData>();
                        if (indexParameters?.Length > 0)
                        {
                            foreach (var parameter in indexParameters)
                            {
                                var parameterType = parameter.ParameterType;
                                var parameterTypeData = builder.GetTypeData(parameterType, typeDatas);
                                DependentTypes.UnionWith(parameterTypeData.GetPublicTypes());
                                parameters.Add(new ParameterData(parameterTypeData, parameter.Name, parameter.IsOut, parameter.GetCustomAttribute<ParamArrayAttribute>() != null));
                            }
                        }

                        var getMethod = property.GetGetMethod();
                        var setMethod = property.GetSetMethod();
                        var propertyData = new PropertyData(propertyTypeData, property.Name, getMethod != null, setMethod != null, parameters, generation, getMethod?.IsStatic ?? setMethod!.IsStatic);
                        foreach (var @interface in Interfaces)
                        {
                            if (@interface.Properties.Any(p => p.Equals(propertyData)))
                            {
                                propertyData.DeclaringInterfaceType = @interface;
                            }
                        }
                        if (documentationProvider != null)
                        {
                            propertyData.Documentation = documentationProvider.GetDocumentation(property);
                        }
                        obsoleteAttribute = property.GetCustomAttribute<ObsoleteAttribute>();
                        if (obsoleteAttribute != null)
                        {
                            propertyData.ObsoleteMessage = obsoleteAttribute.Message?.Replace("\"", "\\\"") ?? string.Empty;
                        }
                        Properties.Add(propertyData);
                    }
                }
            }

            var fieldInfos = Type.GetFields(bindingFlags);
            foreach (var field in fieldInfos)
            {
                var generation = builder.FieldResolver?.Invoke(Type, field) ?? MemberGeneration.Full;
                if (generation != MemberGeneration.None)
                {
                    var fieldType = field.FieldType;
                    var fieldTypeData = builder.GetTypeData(fieldType, typeDatas);
                    DependentTypes.UnionWith(fieldTypeData.GetPublicTypes());
                    var propertyData = new PropertyData(fieldTypeData, field.Name, true, !field.IsInitOnly && !field.IsLiteral, new List<ParameterData>(), generation, field.IsStatic);
                    if (documentationProvider != null)
                    {
                        propertyData.Documentation = documentationProvider.GetDocumentation(field);
                    }
                    obsoleteAttribute = field.GetCustomAttribute<ObsoleteAttribute>();
                    if (obsoleteAttribute != null)
                    {
                        propertyData.ObsoleteMessage = obsoleteAttribute.Message?.Replace("\"", "\\\"") ?? string.Empty;
                    }
                    Properties.Add(propertyData);
                }
            }

            Properties = Properties.Distinct().OrderBy(p => p.Name).ToList();

            var isEquatable = false;
            var methodInfos = Type.GetMethods(bindingFlags);
            foreach (var method in methodInfos)
            {
                if ((method.DeclaringType != typeof(object) || method.Name == "Equals" || method.Name == "GetHashCode") && !method.IsSpecialName && IncludeMethod(builder, method, typeDatas, out var overrideObject))
                {
                    var generation = builder.MethodResolver?.Invoke(Type, method) ?? MemberGeneration.Full;
                    if (generation != MemberGeneration.None)
                    {
                        var returnType = method.ReturnType;

                        var returnTypeData = builder.GetTypeData(returnType, typeDatas);
                        DependentTypes.UnionWith(returnTypeData.GetPublicTypes());
                        var parameterInfos = method.GetParameters();
                        if (!isEquatable && method.Name == "Equals" && returnType == typeof(bool) && parameterInfos.Length == 1 && parameterInfos[0].ParameterType == typeof(object))
                        {
                            isEquatable = true;
                        }
                        if (!isIEnumerable && method.Name == "GetEnumerator" && returnType == typeof(IEnumerator) && Type != typeof(IEnumerable))
                        {
                            isIEnumerable = true;
                            var interfaceTypeData = (InterfaceData)builder.GetTypeData(typeof(IEnumerable), typeDatas);
                            interfaceTypeData.Initialize(builder, documentationProvider, typeDatas, bindingFlags);
                            ImplicitInterfaces.Add(interfaceTypeData);
                        }
                        var parameters = new List<ParameterData>();
                        if (parameterInfos?.Length > 0)
                        {
                            foreach (var parameter in parameterInfos)
                            {
                                var parameterType = parameter.ParameterType;
                                var parameterTypeData = builder.GetTypeData(parameterType, typeDatas);
                                DependentTypes.UnionWith(parameterTypeData.GetPublicTypes());
                                parameters.Add(new ParameterData(parameterTypeData, parameter.Name, parameter.IsOut, parameter.GetCustomAttribute<ParamArrayAttribute>() != null));
                            }
                        }

                        var methodData = new MethodData(method.Name, returnTypeData, parameters, overrideObject, generation, method.IsStatic);
                        foreach (var @interface in Interfaces)
                        {
                            if (@interface.Methods.Any(p => p.Equals(methodData)))
                            {
                                methodData.DeclaringInterfaceType = @interface;
                            }
                        }
                        if (documentationProvider != null)
                        {
                            methodData.Documentation = documentationProvider.GetDocumentation(method);
                        }
                        obsoleteAttribute = method.GetCustomAttribute<ObsoleteAttribute>();
                        if (obsoleteAttribute != null)
                        {
                            methodData.ObsoleteMessage = obsoleteAttribute.Message?.Replace("\"", "\\\"") ?? string.Empty;
                        }
                        Methods.Add(methodData);
                    }
                }
            }

            if (!Type.IsInterface && isIEnumerable && !isGenericIEnumerable)
            {
                var addMethods = Methods.Where(m => m.Name == "Add").ToList();
                var specificAddMethods = addMethods.Where(m => m.Parameters.Count == 1 && (m.ReturnType.Type == typeof(void) || m.ReturnType.Type == typeof(bool) || m.ReturnType.Type == typeof(int))).ToList();
                var addMethod = specificAddMethods.Count == 1 ? specificAddMethods[0] : null;
                TypeData? genericArg = null;
                var indexers = Properties.Where(p => p.Name == "Item").ToList();
                var removeMethods = Methods.Where(m => m.Name == "Remove").ToList();
                var specificRemoveMethods = removeMethods.Where(m => m.Parameters.Count == 1 && (m.ReturnType.Type == typeof(void) || m.ReturnType.Type == typeof(bool) || m.ReturnType.Type == typeof(int))).ToList();
                var removeMethod = specificRemoveMethods.Count == 1 ? specificRemoveMethods[0] : null;
                if (indexers.Count > 0 && indexers.All(i => i.Type.Equals(indexers[0].Type)))
                {
                    genericArg = indexers[0].Type;
                }
                else if (addMethod != null)
                {
                    genericArg = addMethod.Parameters[0].Type;
                }
                else if (removeMethod != null)
                {
                    genericArg = removeMethod.Parameters[0].Type;
                }
                else if (addMethods.Count > 0)
                {
                    var returnType = addMethods[0].ReturnType;
                    if (addMethods.All(m => m.ReturnType.Equals(returnType)))
                    {
                        genericArg = returnType;
                    }
                }

                if (genericArg != null)
                {
                    var interfaceTypeData = (InterfaceData)builder.GetTypeData(typeof(IEnumerable<>).MakeGenericType(genericArg.Type), typeDatas);
                    interfaceTypeData.Initialize(builder, documentationProvider, typeDatas, bindingFlags);
                    ImplicitInterfaces.Add(interfaceTypeData);
                    for (var i = 0; i < Methods.Count; ++i)
                    {
                        if (Methods[i].Name == "GetEnumerator")
                        {
                            Methods.RemoveAt(i);
                            break;
                        }
                    }

                    var iCollectionInterface = ExplicitInterfaces.FirstOrDefault(i => i.Type == typeof(ICollection));
                    var iListInterface = ExplicitInterfaces.FirstOrDefault(i => i.Type == typeof(IList));

                    if (iListInterface != null)
                    {
                        interfaceTypeData = (InterfaceData)builder.GetTypeData(typeof(ICollection<>).MakeGenericType(genericArg.Type), typeDatas);
                        interfaceTypeData.Initialize(builder, documentationProvider, typeDatas, bindingFlags);
                        ImplicitInterfaces.Add(interfaceTypeData);
                        AssignDeclaringInterfaceType(interfaceTypeData);

                        interfaceTypeData = (InterfaceData)builder.GetTypeData(typeof(IList<>).MakeGenericType(genericArg.Type), typeDatas);
                        interfaceTypeData.Initialize(builder, documentationProvider, typeDatas, bindingFlags);
                        ImplicitInterfaces.Add(interfaceTypeData);
                        AssignDeclaringInterfaceType(interfaceTypeData);

                        RemoveNonGenericMembers();
                    }

                    if (iCollectionInterface != null || HasProperty(p => p.Name == "Count" && p.Type.Type == typeof(int) && p.Parameters.Count == 0))
                    {
                        interfaceTypeData = (InterfaceData)builder.GetTypeData(typeof(IReadOnlyCollection<>).MakeGenericType(genericArg.Type), typeDatas);
                        interfaceTypeData.Initialize(builder, documentationProvider, typeDatas, bindingFlags);
                        if (iListInterface != null)
                        {
                            ExplicitInterfaces.Add(interfaceTypeData);
                        }
                        else
                        {
                            ImplicitInterfaces.Add(interfaceTypeData);
                        }
                        AssignDeclaringInterfaceType(interfaceTypeData);

                        if (iListInterface != null || HasProperty(p => p.Name == "Item" && p.Type == genericArg && p.Parameters.Count == 1 && p.Parameters[0].Type.Type == typeof(int)))
                        {
                            interfaceTypeData = (InterfaceData)builder.GetTypeData(typeof(IReadOnlyList<>).MakeGenericType(genericArg.Type), typeDatas);
                            interfaceTypeData.Initialize(builder, documentationProvider, typeDatas, bindingFlags);
                            if (iListInterface != null)
                            {
                                ExplicitInterfaces.Add(interfaceTypeData);
                            }
                            else
                            {
                                ImplicitInterfaces.Add(interfaceTypeData);
                            }
                            AssignDeclaringInterfaceType(interfaceTypeData);
                        }
                    }
                }
            }

            ImplicitInterfaces.Sort((x, y) => string.Compare(x.InterfaceName.Name, y.InterfaceName.Name));
            ExplicitInterfaces.Sort((x, y) => string.Compare(x.InterfaceName.Name, y.InterfaceName.Name));

            Methods = Methods.Distinct().OrderBy(m => m.Name).ToList();

            var eventInfos = Type.GetEvents(bindingFlags);
            foreach (var @event in eventInfos)
            {
                if (IncludeMethod(builder, @event.AddMethod, typeDatas, out _) && IncludeMethod(builder, @event.RemoveMethod, typeDatas, out _))
                {
                    var generation = builder.EventResolver?.Invoke(Type, @event) ?? MemberGeneration.Full;
                    if (generation != MemberGeneration.None)
                    {
                        var eventHandlerType = @event.EventHandlerType;
                        var eventHandlerTypeData = (DelegateData)builder.GetTypeData(eventHandlerType, typeDatas);
                        DependentTypes.UnionWith(eventHandlerTypeData.GetPublicTypes());
                        var invokeMethod = eventHandlerType.GetMethod("Invoke");
                        var returnType = invokeMethod.ReturnType;
                        var returnTypeData = builder.GetTypeData(returnType, typeDatas);
                        DependentTypes.UnionWith(returnTypeData.GetPublicTypes());
                        var parameterInfos = invokeMethod.GetParameters();
                        var parameters = new List<ParameterData>();
                        if (parameterInfos?.Length > 0)
                        {
                            foreach (var parameter in parameterInfos)
                            {
                                var parameterType = parameter.ParameterType;
                                var parameterTypeData = builder.GetTypeData(parameterType, typeDatas);
                                DependentTypes.UnionWith(parameterTypeData.GetPublicTypes());
                                parameters.Add(new ParameterData(parameterTypeData, parameter.Name, parameter.IsOut, parameter.GetCustomAttribute<ParamArrayAttribute>() != null));
                            }
                        }

                        var eventData = new EventData(eventHandlerTypeData, @event.Name, generation, @event.GetAddMethod().IsStatic);
                        foreach (var @interface in Interfaces)
                        {
                            if (@interface.Events.Any(e => e.Equals(eventData)))
                            {
                                eventData.DeclaringInterfaceType = @interface;
                            }
                        }
                        if (documentationProvider != null)
                        {
                            eventData.Documentation = documentationProvider.GetDocumentation(@event);
                        }
                        obsoleteAttribute = @event.GetCustomAttribute<ObsoleteAttribute>();
                        if (obsoleteAttribute != null)
                        {
                            eventData.ObsoleteMessage = obsoleteAttribute.Message?.Replace("\"", "\\\"") ?? string.Empty;
                        }
                        Events.Add(eventData);
                    }
                }
            }

            Events = Events.Distinct().OrderBy(e => e.Name).ToList();

            _initialized = true;
        }

        protected abstract bool IncludeMethod(WrapperBuilder builder, MethodInfo method, HashSet<TypeData> typeDatas, out bool overrideObject);

        private void AssignDeclaringInterfaceType(InterfaceData @interface)
        {
            foreach (var property in @interface.Properties)
            {
                var p = Properties.FirstOrDefault(p => p.Equals(property));
                if (p != null && p.DeclaringInterfaceType == null)
                {
                    p.DeclaringInterfaceType = @interface;
                }
            }

            foreach (var method in @interface.Methods)
            {
                var m = Methods.FirstOrDefault(m => m.Equals(method));
                if (m != null && m.DeclaringInterfaceType == null)
                {
                    m.DeclaringInterfaceType = @interface;
                }
            }

            foreach (var @event in @interface.Events)
            {
                var e = Events.FirstOrDefault(e => e.Equals(@event));
                if (e != null && e.DeclaringInterfaceType == null)
                {
                    e.DeclaringInterfaceType = @interface;
                }
            }
        }

        private void RemoveNonGenericMembers()
        {
            for (var i = Properties.Count - 1; i >= 0; --i)
            {
                var property = Properties[i];
                var declaringInterfaceType = property.DeclaringInterfaceType?.Type;
                if ((declaringInterfaceType == typeof(IList) || declaringInterfaceType == typeof(ICollection)) && (property.Type.Type == typeof(object) || property.Parameters.Any(p => p.Type.Type == typeof(object) || p.Type.Type == typeof(Array))) && property.Name != "SyncRoot")
                {
                    Properties.RemoveAt(i);
                }
            }

            for (var i = Methods.Count - 1; i >= 0; --i)
            {
                var method = Methods[i];
                var declaringInterfaceType = method.DeclaringInterfaceType?.Type;
                if ((declaringInterfaceType == typeof(IList) || declaringInterfaceType == typeof(ICollection)) && (method.ReturnType.Type == typeof(object) || method.Parameters.Any(p => p.Type.Type == typeof(object) || p.Type.Type == typeof(Array))))
                {
                    Methods.RemoveAt(i);
                }
            }
        }

        internal bool HasInterface(Func<InterfaceData, bool> predicate) => Interfaces.Any(predicate) || BaseType?.HasInterface(predicate) == true;

        internal bool HasProperty(Func<PropertyData, bool> predicate) => Properties.Any(predicate) || BaseType?.HasProperty(predicate) == true;

        internal bool HasMethod(Func<MethodData, bool> predicate) => Methods.Any(predicate) || BaseType?.HasMethod(predicate) == true;
    }
}