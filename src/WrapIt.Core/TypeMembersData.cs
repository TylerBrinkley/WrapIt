using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace WrapIt
{
    internal abstract class TypeMembersData : TypeData
    {
        private bool _initialized;

        public List<InterfaceData> Interfaces { get; private set; } = new List<InterfaceData>();

        public List<PropertyData> Properties { get; private set; } = new List<PropertyData>();

        public List<MethodData> Methods { get; private set; } = new List<MethodData>();

        public List<EventData> Events { get; private set; } = new List<EventData>();

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

        public void Initialize(WrapperBuilder builder, HashSet<TypeData> typeDatas, BindingFlags bindingFlags)
        {
            if (_initialized)
            {
                return;
            }

            var isIEnumerable = false;
            var isGenericIEnumerable = false;
            foreach (var @interface in Type.GetInterfaces())
            {
                if (@interface.IsPublic && !builder.AssembliesWithTypesToWrap.Contains(@interface.Assembly) && builder.InterfaceResolver?.Invoke(Type, @interface) != false)
                {
                    if (!isIEnumerable && @interface == typeof(IEnumerable))
                    {
                        isIEnumerable = true;
                    }
                    if (@interface.IsGenericType)
                    {
                        var genericTypeDefinition = @interface.GetGenericTypeDefinition();
                        if (!isGenericIEnumerable && genericTypeDefinition == typeof(IEnumerable<>))
                        {
                            isGenericIEnumerable = true;
                        }
                    }
                    var interfaceTypeData = (InterfaceData)builder.GetTypeData(@interface, typeDatas);
                    interfaceTypeData.Initialize(builder, typeDatas, bindingFlags);
                    Interfaces.Add(interfaceTypeData);
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
                                parameters.Add(new ParameterData(parameterTypeData, parameter.Name, parameter.IsOut));
                            }
                        }

                        var propertyData = new PropertyData(propertyTypeData, property.Name, property.GetGetMethod() != null, property.GetSetMethod() != null, parameters, generation);
                        foreach (var @interface in Interfaces)
                        {
                            if (@interface.Properties.Any(p => p.Equals(propertyData)))
                            {
                                propertyData.DeclaringInterfaceType = @interface;
                            }
                        }
                        Properties.Add(propertyData);
                    }
                }
            }

            Properties = Properties.Distinct().OrderBy(p => p.Name).ToList();

            var isEquatable = false;
            var methodInfos = Type.GetMethods(bindingFlags);
            foreach (var method in methodInfos)
            {
                if (method.DeclaringType != typeof(object) && !method.IsSpecialName && IncludeMethod(builder, method, typeDatas, out var overrideObject))
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
                        var parameters = new List<ParameterData>();
                        if (parameterInfos?.Length > 0)
                        {
                            foreach (var parameter in parameterInfos)
                            {
                                var parameterType = parameter.ParameterType;
                                var parameterTypeData = builder.GetTypeData(parameterType, typeDatas);
                                DependentTypes.UnionWith(parameterTypeData.GetPublicTypes());
                                parameters.Add(new ParameterData(parameterTypeData, parameter.Name, parameter.IsOut));
                            }
                        }

                        var methodData = new MethodData(method.Name, returnTypeData, parameters, overrideObject, generation);
                        foreach (var @interface in Interfaces)
                        {
                            if (@interface.Methods.Any(p => p.Equals(methodData)))
                            {
                                methodData.DeclaringInterfaceType = @interface;
                            }
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
                if (addMethod != null)
                {
                    genericArg = addMethod.Parameters[0].Type;
                }
                else if (addMethods.Count > 0)
                {
                    var returnType = addMethods[0].ReturnType;
                    if (addMethods.All(m => m.ReturnType.Equals(returnType)))
                    {
                        genericArg = returnType;
                    }
                }
                var indexers = Properties.Where(p => p.Name == "Item").ToList();
                if (genericArg != null || indexers.Count > 0)
                {
                    var indexerType = indexers[0].Type;
                    if (genericArg == null || !indexers.All(i => i.Type.Equals(genericArg)))
                    {
                        genericArg = indexerType;
                        if (!indexers.All(i => i.Type.Equals(genericArg)) || (addMethods.Count > 0 && !addMethods.Any(m => m.Parameters.Count == 1 && m.Parameters[0].Type.Equals(genericArg))))
                        {
                            genericArg = null;
                        }
                    }
                    if (genericArg != null)
                    {
                        var interfaceTypeData = (InterfaceData)builder.GetTypeData(typeof(IEnumerable<>).MakeGenericType(genericArg.Type), typeDatas);
                        interfaceTypeData.Initialize(builder, typeDatas, bindingFlags);
                        Interfaces.Add(interfaceTypeData);
                        for (var i = 0; i < Methods.Count; ++i)
                        {
                            if (Methods[i].Name == "GetEnumerator")
                            {
                                Methods.RemoveAt(i);
                                break;
                            }
                        }
                    }
                }
            }

            Interfaces.Sort((x, y) => string.Compare(x.InterfaceName.Name, y.InterfaceName.Name));

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
                                parameters.Add(new ParameterData(parameterTypeData, parameter.Name, parameter.IsOut));
                            }
                        }

                        var eventData = new EventData(eventHandlerTypeData, @event.Name, generation);
                        foreach (var @interface in Interfaces)
                        {
                            if (@interface.Events.Any(e => e.Equals(eventData)))
                            {
                                eventData.DeclaringInterfaceType = @interface;
                            }
                        }
                        Events.Add(eventData);
                    }
                }
            }

            Events = Events.Distinct().OrderBy(e => e.Name).ToList();

            _initialized = true;
        }

        protected abstract bool IncludeMethod(WrapperBuilder builder, MethodInfo method, HashSet<TypeData> typeDatas, out bool overrideObject);
    }
}