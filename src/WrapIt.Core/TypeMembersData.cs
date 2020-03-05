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
                    if (!isGenericIEnumerable && @interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    {
                        isGenericIEnumerable = true;
                    }
                    var interfaceTypeData = (InterfaceData)builder.GetTypeData(@interface, typeDatas);
                    interfaceTypeData.Initialize(builder, typeDatas, bindingFlags);
                    Interfaces.Add(interfaceTypeData);
                }
            }

            var propertyInfos = Type.GetProperties(bindingFlags);
            foreach (var property in propertyInfos)
            {
                if (property.GetAccessors().All(a => IncludeMethod(builder, a, typeDatas, out _)) && builder.PropertyResolver?.Invoke(Type, property) != false)
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

                    var propertyData = new PropertyData(propertyTypeData, property.Name, property.GetGetMethod() != null, property.GetSetMethod() != null, parameters);
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

            Properties = Properties.Distinct().OrderBy(p => p.Name).ToList();

            var methodInfos = Type.GetMethods(bindingFlags);
            foreach (var method in methodInfos)
            {
                if (method.DeclaringType != typeof(object) && !method.IsSpecialName && IncludeMethod(builder, method, typeDatas, out var overrideObject) && builder.MethodResolver?.Invoke(Type, method) != false)
                {
                    var returnType = method.ReturnType;
                    var returnTypeData = builder.GetTypeData(returnType, typeDatas);
                    DependentTypes.UnionWith(returnTypeData.GetPublicTypes());
                    var parameterInfos = method.GetParameters();
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

                    var methodData = new MethodData(method.Name, returnTypeData, parameters, overrideObject);
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

            if (!Type.IsInterface && isIEnumerable && !isGenericIEnumerable)
            {
                var addMethod = Methods.SingleOrDefault(m => m.Name == "Add" && m.Parameters.Count == 1);
                TypeData? genericArg = null;
                if (addMethod != null)
                {
                    genericArg = addMethod.Parameters[0].Type;
                }
                var indexers = Properties.Where(p => p.Name == "Item").ToList();
                if (genericArg != null || indexers.Count > 0)
                {
                    genericArg ??= indexers[0].Type;
                    if (indexers.All(i => i.Type.Equals(genericArg)))
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
                if (IncludeMethod(builder, @event.AddMethod, typeDatas, out _) && IncludeMethod(builder, @event.RemoveMethod, typeDatas, out _) && builder.EventResolver?.Invoke(Type, @event) != false)
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

                    var eventData = new EventData(eventHandlerTypeData, @event.Name);
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

            Events = Events.Distinct().OrderBy(e => e.Name).ToList();

            _initialized = true;
        }

        protected abstract bool IncludeMethod(WrapperBuilder builder, MethodInfo method, HashSet<TypeData> typeDatas, out bool overrideObject);
    }
}