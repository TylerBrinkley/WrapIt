﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace WrapIt
{
    internal class ClassData : TypeMembersData
    {
        public const string ObjectName = "Object";
        public const string ObjectParamName = "@object";

        public List<ClassData> DirectDerivedTypes { get; } = new List<ClassData>();

        public TypeGeneration TypeGeneration { get; }

        public bool IsValueType { get; }

        public List<ConstructorData> Constructors { get; } = new List<ConstructorData>();

        public ClassData(Type type, TypeName className, TypeName interfaceName, TypeBuildStatus buildStatus, ClassData? baseType, TypeGeneration typeGeneration)
            : base(type, className, interfaceName, buildStatus)
        {
            BaseType = baseType;
            TypeGeneration = typeGeneration;
            IsValueType = type.IsValueType;
        }

        internal override Type[] GetInterfaces() => TypeGeneration.HasFlag(TypeGeneration.Instance) ? base.GetInterfaces() : Array.Empty<Type>();

        public override async Task BuildAsync(WrapperBuilder builder, HashSet<TypeData> typeDatas, Func<Type, string, CancellationToken, Task<TextWriter>> writerProvider, DocumentationProvider? documentationProvider, CancellationToken cancellationToken = default)
        {
            BuildStatus = TypeBuildStatus.Building;

            var bindingFlags = BindingFlags.Public;
            if (TypeGeneration.HasFlag(TypeGeneration.Instance))
            {
                bindingFlags |= BindingFlags.Instance;
            }
            if (TypeGeneration.HasFlag(TypeGeneration.Static))
            {
                bindingFlags |= BindingFlags.Static;
            }
            if (BaseType != null)
            {
                bindingFlags |= BindingFlags.DeclaredOnly;
                if (BaseType.BuildStatus == TypeBuildStatus.NotYetBuilt)
                {
                    await BaseType.BuildAsync(builder, typeDatas, writerProvider, documentationProvider, cancellationToken).ConfigureAwait(false);
                }
            }

            Initialize(builder, documentationProvider, typeDatas, bindingFlags);

            var interfaceUsingDirectives = new HashSet<string>();
            var classUsingDirectives = new HashSet<string>();
            if (!(BaseType is null) && BaseType.Type != typeof(object))
            {
                interfaceUsingDirectives.UnionWith(BaseType.InterfaceName.GetNamespaces());
                classUsingDirectives.UnionWith(BaseType.ClassName.GetNamespaces());
            }
            else if (TypeGeneration.HasFlag(TypeGeneration.Instance) && !IsValueType)
            {
                // For ArgumentNullException
                classUsingDirectives.Add("System");
            }

            foreach (var dependentType in DependentTypes)
            {
                interfaceUsingDirectives.UnionWith(dependentType.InterfaceName.GetNamespaces());
                classUsingDirectives.UnionWith(dependentType.ClassName.GetNamespaces());
            }

            foreach (var classDependentType in ClassDependentTypes)
            {
                classUsingDirectives.UnionWith(classDependentType.ClassName.GetNamespaces());
            }

            foreach (var @interface in ImplicitInterfaces)
            {
                interfaceUsingDirectives.UnionWith(@interface.InterfaceName.GetNamespaces());
            }

            foreach (var @interface in ExplicitInterfaces)
            {
                classUsingDirectives.UnionWith(@interface.InterfaceName.GetNamespaces());
            }

            foreach (var derivedType in DirectDerivedTypes)
            {
                classUsingDirectives.UnionWith(derivedType.ClassName.GetNamespaces());
            }

            if (ObsoleteMessage != null)
            {
                interfaceUsingDirectives.Add("System");
            }

            foreach (var property in Properties)
            {
                if (property.ObsoleteMessage != null)
                {
                    interfaceUsingDirectives.Add("System");
                }
            }

            foreach (var @event in Events)
            {
                if (@event.ObsoleteMessage != null)
                {
                    interfaceUsingDirectives.Add("System");
                }
                if (!@event.IsStatic || @event.Type.ClassName != @event.Type.InterfaceName)
                {
                    // For Delegate
                    classUsingDirectives.Add("System");
                    // For Interlocked
                    classUsingDirectives.Add("System.Threading");
                }
            }

            foreach (var method in Methods)
            {
                if (method.ObsoleteMessage != null)
                {
                    interfaceUsingDirectives.Add("System");
                }
            }

            classUsingDirectives.UnionWith(interfaceUsingDirectives);

            await WriteInterfaceAsync(builder, writerProvider, interfaceUsingDirectives, cancellationToken).ConfigureAwait(false);

            await WriteClassAsync(builder, writerProvider, classUsingDirectives, cancellationToken).ConfigureAwait(false);

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

        public override void Initialize(WrapperBuilder builder, DocumentationProvider? documentationProvider, HashSet<TypeData> typeDatas, BindingFlags bindingFlags)
        {
            if (!_initialized)
            {
                base.Initialize(builder, documentationProvider, typeDatas, bindingFlags);

                if (!Type.IsAbstract)
                {
                    var constructorInfos = Type.GetConstructors();
                    foreach (var constructor in constructorInfos)
                    {
                        var generation = builder.ConstructorResolver?.Invoke(Type, constructor) ?? MemberGeneration.Full;
                        if (generation != MemberGeneration.None)
                        {
                            var parameterInfos = constructor.GetParameters();
                            var parameters = new List<ParameterData>();
                            if (parameterInfos?.Length > 0)
                            {
                                InterfaceData? genericIEnumerable = null;
                                if (parameterInfos.Length == 1 && typeof(IEnumerable).IsAssignableFrom(parameterInfos[0].ParameterType) && !parameterInfos[0].ParameterType.IsGenericType && (genericIEnumerable = Interfaces.FirstOrDefault(i => i.Type.IsGenericType && i.Type.GetGenericTypeDefinition() == typeof(IEnumerable<>))) != null)
                                {
                                    var parameter = parameterInfos[0];
                                    var parameterTypeData = builder.GetTypeData((parameter.ParameterType == typeof(ICollection) || parameter.ParameterType == typeof(IList) ? typeof(List<>) : typeof(IEnumerable<>)).MakeGenericType(genericIEnumerable.Type.GenericTypeArguments[0]), typeDatas);
                                    ClassDependentTypes.UnionWith(parameterTypeData.GetPublicTypes());
                                    parameters.Add(new ParameterData(parameterTypeData, parameter.Name, parameter.IsOut, parameter.GetCustomAttribute<ParamArrayAttribute>() != null));
                                }
                                else
                                {
                                    foreach (var parameter in parameterInfos)
                                    {
                                        var parameterTypeData = builder.GetTypeData(parameter.ParameterType, typeDatas);
                                        ClassDependentTypes.UnionWith(parameterTypeData.GetPublicTypes());
                                        parameters.Add(new ParameterData(parameterTypeData, parameter.Name, parameter.IsOut, parameter.GetCustomAttribute<ParamArrayAttribute>() != null));
                                    }
                                }
                            }

                            var constructorData = new ConstructorData(parameters);
                            if (documentationProvider != null)
                            {
                                constructorData.Documentation = documentationProvider.GetDocumentation(constructor);
                            }
                            var obsoleteAttribute = constructor.GetCustomAttribute<ObsoleteAttribute>();
                            if (obsoleteAttribute != null)
                            {
                                constructorData.ObsoleteMessage = obsoleteAttribute.Message?.Replace("\"", "\\\"") ?? string.Empty;
                            }
                            Constructors.Add(constructorData);
                        }
                    }

                    if (IsValueType && !Constructors.Any(c => c.Parameters.Count == 0))
                    {
                        Constructors.Add(new ConstructorData(new List<ParameterData>()));
                    }
                }
            }
        }

        protected override bool IncludeMethod(WrapperBuilder builder, MethodInfo method, HashSet<TypeData> typeDatas, out bool overrideObject)
        {
            if (method.IsVirtual)
            {
                var baseDefinition = method.GetBaseDefinition();
                overrideObject = baseDefinition.DeclaringType == typeof(object);
                return baseDefinition == method || builder.GetTypeData(baseDefinition.DeclaringType, typeDatas).BuildStatus == TypeBuildStatus.NotBuilding;
            }
            overrideObject = false;
            return true;
        }

        private async Task WriteInterfaceAsync(WrapperBuilder builder, Func<Type, string, CancellationToken, Task<TextWriter>> writerProvider, HashSet<string> interfaceUsingDirectives, CancellationToken cancellationToken)
        {
            using (var writer = await writerProvider(Type, InterfaceName.FullName, cancellationToken).ConfigureAwait(false))
            {
                var interfaceNamespace = InterfaceName.Namespace!;

                var interfaces = new List<TypeData>();
                var temp = new List<TypeData>();
                if (BaseType != null)
                {
                    temp.Add(BaseType);
                }
                temp.AddRange(ImplicitInterfaces);
                for (var i = 0; i < temp.Count; ++i)
                {
                    var interfaceToCheck = temp[i].Type;
                    var toAdd = true;
                    for (var j = 0; j < temp.Count; ++j)
                    {
                        if (i != j && temp[j].Type.GetInterfaces().Any(x => x == interfaceToCheck))
                        {
                            toAdd = false;
                            break;
                        }
                    }
                    if (toAdd)
                    {
                        interfaces.Add(temp[i]);
                    }
                }

                var anyInterfaceUsings = false;
                foreach (var ns in interfaceUsingDirectives.Where(ns => !interfaceNamespace.StartsWith(ns)).OrderBy(ns => ns != "System" && !ns.StartsWith("System.")).ThenBy(ns => ns))
                {
                    await writer.WriteLineAsync($"using {ns};").ConfigureAwait(false);
                    anyInterfaceUsings = true;
                }

                if (anyInterfaceUsings)
                {
                    await writer.WriteLineAsync().ConfigureAwait(false);
                }

                var typeFullName = Type.FullName.Replace('+', '.');
                await writer.WriteLineAsync($"namespace {interfaceNamespace}").ConfigureAwait(false);
                await writer.WriteLineAsync("{").ConfigureAwait(false);
                if (builder.DocumentationGeneration is DocumentationGeneration.GenerateWithInheritDocWithReferencesToWrapped)
                {
                    await writer.WriteLineAsync($"    /// <inheritdoc cref=\"{typeFullName}\"/>").ConfigureAwait(false);
                }
                else if (Documentation.Any())
                {
                    await writer.WriteLineAsync(string.Join(writer.NewLine, Documentation.SelectMany(d => d.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.None)).Select(d => $"    /// {(d.StartsWith("            ") ? d.Substring(12) : d)}"))).ConfigureAwait(false);
                }
                if (ObsoleteMessage != null)
                {
                    await writer.WriteLineAsync($"    [Obsolete{(ObsoleteMessage.Length > 0 ? $"(\"{ObsoleteMessage}\")" : string.Empty)}]");
                }

                await writer.WriteLineAsync($"    public partial interface {InterfaceName}{(interfaces.Count > 0 ? $" : {string.Join(", ", interfaces.Select(i => i.InterfaceName))}" : string.Empty)}").ConfigureAwait(false);
                await writer.WriteLineAsync("    {").ConfigureAwait(false);

                var addLine = false;
                foreach (var property in Properties)
                {
                    if (property.DeclaringInterfaceType == null && property.Generation != MemberGeneration.OnlyInImplementation)
                    {
                        addLine = true;
                        if (builder.DocumentationGeneration is DocumentationGeneration.GenerateWithInheritDocWithReferencesToWrapped)
                        {
                            var type = Type;
                            var bindingFlags = BindingFlags.Public | BindingFlags.DeclaredOnly | (property.IsStatic ? BindingFlags.Static : BindingFlags.Instance);
                            while (!type.GetProperties(bindingFlags).Any(p => p.Name == property.Name) && type.GetField(property.Name, bindingFlags) == null)
                            {
                                type = type.BaseType;
                            }
                            await writer.WriteLineAsync($"        /// <inheritdoc cref=\"{type.FullName.Replace('+', '.')}.{(property.Parameters.Count > 0 ? $"this[{string.Join(", ", property.Parameters.Select(p => p.GetAsActualXmlParameter()))}]" : property.Name)}\"/>").ConfigureAwait(false);
                        }
                        else if (property.Documentation.Any())
                        {
                            await writer.WriteLineAsync(string.Join(writer.NewLine, property.Documentation.SelectMany(d => d.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.None)).Select(d => $"        /// {(d.StartsWith("            ") ? d.Substring(12) : d)}"))).ConfigureAwait(false);
                        }
                        if (property.ObsoleteMessage != null)
                        {
                            await writer.WriteLineAsync($"        [Obsolete{(property.ObsoleteMessage.Length > 0 ? $"(\"{property.ObsoleteMessage}\")" : string.Empty)}]");
                        }
                        if (property.Parameters.Count > 0)
                        {
                            await writer.WriteLineAsync($"        {(BaseType?.HasProperty(p => p.Equals(property, checkReturnType: false)) == true ? "new " : string.Empty)}{property.Type.InterfaceName} this[{string.Join(", ", property.Parameters.Select(p => p.GetAsInterfaceParameter()))}] {{ {(property.HasGetter ? "get; " : string.Empty)}{(property.HasSetter ? "set; " : string.Empty)}}}").ConfigureAwait(false);
                        }
                        else
                        {
                            await writer.WriteLineAsync($"        {(BaseType?.HasProperty(p => p.Equals(property, checkReturnType: false)) == true ? "new " : string.Empty)}{property.Type.InterfaceName} {property.Name} {{ {(property.HasGetter ? "get; " : string.Empty)}{(property.HasSetter ? "set; " : string.Empty)}}}").ConfigureAwait(false);
                        }
                    }
                }

                var first = true;
                if (Events.Count > 0)
                {
                    foreach (var @event in Events)
                    {
                        if (@event.DeclaringInterfaceType == null && @event.Generation != MemberGeneration.OnlyInImplementation)
                        {
                            if (first && addLine)
                            {
                                await writer.WriteLineAsync().ConfigureAwait(false);
                            }
                            first = false;
                            addLine = true;
                            if (builder.DocumentationGeneration is DocumentationGeneration.GenerateWithInheritDocWithReferencesToWrapped)
                            {
                                var type = Type;
                                while (type.GetEvent(@event.Name, BindingFlags.Public | BindingFlags.DeclaredOnly | (@event.IsStatic ? BindingFlags.Static : BindingFlags.Instance)) == null)
                                {
                                    type = type.BaseType;
                                }
                                await writer.WriteLineAsync($"        /// <inheritdoc cref=\"{type.FullName.Replace('+', '.')}.{@event.Name}\"/>").ConfigureAwait(false);
                            }
                            else if (@event.Documentation.Any())
                            {
                                await writer.WriteLineAsync(string.Join(writer.NewLine, @event.Documentation.SelectMany(d => d.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.None)).Select(d => $"        /// {(d.StartsWith("            ") ? d.Substring(12) : d)}"))).ConfigureAwait(false);
                            }
                            if (@event.ObsoleteMessage != null)
                            {
                                await writer.WriteLineAsync($"        [Obsolete{(@event.ObsoleteMessage.Length > 0 ? $"(\"{@event.ObsoleteMessage}\")" : string.Empty)}]");
                            }
                            await writer.WriteLineAsync($"        event {@event.Type.InterfaceName} {@event.Name};").ConfigureAwait(false);
                        }
                    }
                }

                var methods = Methods.Where(m => !m.OverrideObject).ToList();
                if (methods.Count > 0)
                {
                    first = true;
                    foreach (var method in methods)
                    {
                        if (method.DeclaringInterfaceType == null && method.Generation != MemberGeneration.OnlyInImplementation)
                        {
                            if (first && addLine)
                            {
                                await writer.WriteLineAsync().ConfigureAwait(false);
                            }
                            first = false;
                            if (builder.DocumentationGeneration is DocumentationGeneration.GenerateWithInheritDocWithReferencesToWrapped)
                            {
                                var type = Type;
                                while (!type.GetMethods(BindingFlags.Public | BindingFlags.DeclaredOnly | (method.IsStatic ? BindingFlags.Static : BindingFlags.Instance)).Any(m => m.Name == method.Name))
                                {
                                    type = type.BaseType;
                                }
                                await writer.WriteLineAsync($"        /// <inheritdoc cref=\"{type.FullName.Replace('+', '.')}.{method.Name}({string.Join(", ", method.Parameters.Select(p => p.GetAsActualXmlParameter()))})\"/>").ConfigureAwait(false);
                            }
                            else if (method.Documentation.Any())
                            {
                                await writer.WriteLineAsync(string.Join(writer.NewLine, method.Documentation.SelectMany(d => d.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.None)).Select(d => $"        /// {(d.StartsWith("            ") ? d.Substring(12) : d)}"))).ConfigureAwait(false);
                            }
                            if (method.ObsoleteMessage != null)
                            {
                                await writer.WriteLineAsync($"        [Obsolete{(method.ObsoleteMessage.Length > 0 ? $"(\"{method.ObsoleteMessage}\")" : string.Empty)}]");
                            }
                            await writer.WriteLineAsync($"        {(BaseType?.HasMethod(m => m.Equals(method, checkReturnType: false)) == true ? "new " : string.Empty)}{method.ReturnType.InterfaceName} {method.Name}({string.Join(", ", method.Parameters.Select(p => p.GetAsInterfaceParameter()))});").ConfigureAwait(false);
                        }
                    }
                }

                await writer.WriteLineAsync("    }").ConfigureAwait(false);
                await writer.WriteAsync("}").ConfigureAwait(false);
                await writer.FlushAsync().ConfigureAwait(false);
            }
        }

        private async Task WriteClassAsync(WrapperBuilder builder, Func<Type, string, CancellationToken, Task<TextWriter>> writerProvider, HashSet<string> classUsingDirectives, CancellationToken cancellationToken)
        {
            using (var writer = await writerProvider(Type, ClassName.FullName, cancellationToken).ConfigureAwait(false))
            {
                var classNamespace = ClassName.Namespace!;
                var anyClassUsings = false;
                foreach (var ns in classUsingDirectives.Where(ns => !classNamespace.StartsWith(ns)).OrderBy(ns => ns != "System" && !ns.StartsWith("System.")).ThenBy(ns => ns))
                {
                    await writer.WriteLineAsync($"using {ns};").ConfigureAwait(false);
                    anyClassUsings = true;
                }

                if (anyClassUsings)
                {
                    await writer.WriteLineAsync().ConfigureAwait(false);
                }

                await writer.WriteLineAsync($"namespace {classNamespace}").ConfigureAwait(false);
                await writer.WriteLineAsync("{").ConfigureAwait(false);
                
                if (builder.DocumentationGeneration is DocumentationGeneration.GenerateWithInheritDoc or DocumentationGeneration.GenerateWithInheritDocWithReferencesToWrapped)
                {
                    await writer.WriteLineAsync($"    /// <inheritdoc cref=\"{InterfaceName}\"/>").ConfigureAwait(false);
                }
                else if (Documentation.Any())
                {
                    await writer.WriteLineAsync(string.Join(writer.NewLine, Documentation.SelectMany(d => d.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.None)).Select(d => $"    /// {(d.StartsWith("            ") ? d.Substring(12) : d)}"))).ConfigureAwait(false);
                }
                if (ObsoleteMessage != null)
                {
                    await writer.WriteLineAsync($"    [Obsolete{(ObsoleteMessage.Length > 0 ? $"(\"{ObsoleteMessage}\")" : string.Empty)}]");
                }
                var interfaces = new List<TypeData>
                {
                    this
                };
                for (var i = 0; i < ExplicitInterfaces.Count; ++i)
                {
                    var interfaceToCheck = ExplicitInterfaces[i].Type;
                    var toAdd = true;
                    for (var j = 0; j < ExplicitInterfaces.Count; ++j)
                    {
                        if (i != j && ExplicitInterfaces[j].Type.GetInterfaces().Any(x => x == interfaceToCheck))
                        {
                            toAdd = false;
                            break;
                        }
                    }
                    if (toAdd)
                    {
                        interfaces.Add(ExplicitInterfaces[i]);
                    }
                }
                await writer.WriteLineAsync($"    public {(Type.IsSealed ? "sealed " : string.Empty)}partial class {ClassName} : {(BaseType is null ? string.Empty : $"{BaseType.ClassName}, ")}{string.Join(", ", interfaces.Select(i => i.InterfaceName))}").ConfigureAwait(false);
                await writer.WriteLineAsync("    {").ConfigureAwait(false);

                var typeFullName = Type.FullName.Replace('+', '.');
                if (TypeGeneration.HasFlag(TypeGeneration.Instance))
                {
                    if (builder.DocumentationGeneration != DocumentationGeneration.None)
                    {
                        await writer.WriteLineAsync($@"        /// <summary>").ConfigureAwait(false);
                        await writer.WriteLineAsync($@"        /// The conversion operator for wrapping the <see cref=""{typeFullName}""/> object.").ConfigureAwait(false);
                        await writer.WriteLineAsync($@"        /// </summary>").ConfigureAwait(false);
                        await writer.WriteLineAsync($@"        /// <param name=""object"">The object to wrap.</param>").ConfigureAwait(false);
                    }
                    if (DirectDerivedTypes.Count == 0)
                    {
                        if (IsValueType)
                        {
                            await writer.WriteLineAsync($"        public static implicit operator {ClassName}({typeFullName} {ObjectParamName}) => new {ClassName}({ObjectParamName});").ConfigureAwait(false);
                        }
                        else
                        {
                            await writer.WriteLineAsync($"        public static implicit operator {ClassName}({typeFullName} {ObjectParamName}) => {ObjectParamName} != null ? new {ClassName}({ObjectParamName}) : null;").ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        var directDerivedTypes = DirectDerivedTypes.OrderBy(t => t.ClassName.Name);
                        if (builder.MinCSharpVersion >= 8M)
                        {
                            await writer.WriteLineAsync($"        public static implicit operator {ClassName}({typeFullName} {ObjectParamName}) => {ObjectParamName} switch").ConfigureAwait(false);
                            await writer.WriteLineAsync("        {").ConfigureAwait(false);
                            await writer.WriteLineAsync($"            null => null,").ConfigureAwait(false);
                            foreach (var derivedType in directDerivedTypes)
                            {
                                await writer.WriteLineAsync($"            {derivedType.Type.FullName} o => ({derivedType.ClassName})o,").ConfigureAwait(false);
                            }
                            await writer.WriteLineAsync($"            _ => new {ClassName}({ObjectParamName})").ConfigureAwait(false);
                            await writer.WriteLineAsync("        };").ConfigureAwait(false);
                        }
                        else if (builder.MinCSharpVersion >= 7M)
                        {
                            await writer.WriteLineAsync($"        public static implicit operator {ClassName}({typeFullName} {ObjectParamName})").ConfigureAwait(false);
                            await writer.WriteLineAsync("        {").ConfigureAwait(false);
                            await writer.WriteLineAsync($"            switch ({ObjectParamName})").ConfigureAwait(false);
                            await writer.WriteLineAsync("            {").ConfigureAwait(false);
                            await writer.WriteLineAsync("                case null:").ConfigureAwait(false);
                            await writer.WriteLineAsync("                    return null;").ConfigureAwait(false);
                            foreach (var derivedType in directDerivedTypes)
                            {
                                await writer.WriteLineAsync($"                case {derivedType.Type.FullName} o:").ConfigureAwait(false);
                                await writer.WriteLineAsync($"                    return ({derivedType.ClassName})o;").ConfigureAwait(false);
                            }
                            await writer.WriteLineAsync("                default:").ConfigureAwait(false);
                            await writer.WriteLineAsync($"                    return new {ClassName}({ObjectParamName});").ConfigureAwait(false);
                            await writer.WriteLineAsync("            }").ConfigureAwait(false);
                            await writer.WriteLineAsync("        }").ConfigureAwait(false);
                        }
                        else
                        {
                            await writer.WriteLineAsync($"        public static implicit operator {ClassName}({typeFullName} {ObjectParamName})").ConfigureAwait(false);
                            await writer.WriteLineAsync("        {").ConfigureAwait(false);
                            await writer.WriteLineAsync($"            if ({ObjectParamName} == null)").ConfigureAwait(false);
                            await writer.WriteLineAsync("            {").ConfigureAwait(false);
                            await writer.WriteLineAsync("                return null;").ConfigureAwait(false);
                            await writer.WriteLineAsync("            }").ConfigureAwait(false);
                            foreach (var derivedType in directDerivedTypes)
                            {
                                await writer.WriteLineAsync($"            if ({ObjectParamName} is {derivedType.Type.FullName})").ConfigureAwait(false);
                                await writer.WriteLineAsync("            {").ConfigureAwait(false);
                                await writer.WriteLineAsync($"                return ({derivedType.ClassName})({derivedType.Type.FullName}){ObjectParamName};").ConfigureAwait(false);
                                await writer.WriteLineAsync("            }").ConfigureAwait(false);
                            }
                            await writer.WriteLineAsync($"            return new {ClassName}({ObjectParamName});").ConfigureAwait(false);
                            await writer.WriteLineAsync("        }").ConfigureAwait(false);
                        }
                    }
                    await writer.WriteLineAsync().ConfigureAwait(false);

                    if (builder.DocumentationGeneration != DocumentationGeneration.None)
                    {
                        await writer.WriteLineAsync($@"        /// <summary>").ConfigureAwait(false);
                        await writer.WriteLineAsync($@"        /// The conversion operator for unwrapping the <see cref=""{typeFullName}""/> object.").ConfigureAwait(false);
                        await writer.WriteLineAsync($@"        /// </summary>").ConfigureAwait(false);
                        await writer.WriteLineAsync($@"        /// <param name=""object"">The object to unwrap.</param>").ConfigureAwait(false);
                    }
                    if (IsValueType)
                    {
                        await writer.WriteLineAsync($"        public static implicit operator {typeFullName}({ClassName} {ObjectParamName}) => {ObjectParamName}?.{ObjectName} ?? default{(builder.MinCSharpVersion < 7.1M ? $"({typeFullName})" : string.Empty)};").ConfigureAwait(false);
                    }
                    else
                    {
                        await writer.WriteLineAsync($"        public static implicit operator {typeFullName}({ClassName} {ObjectParamName}) => {ObjectParamName}?.{ObjectName};").ConfigureAwait(false);
                    }
                    await writer.WriteLineAsync().ConfigureAwait(false);

                    if (builder.DocumentationGeneration != DocumentationGeneration.None)
                    {
                        await writer.WriteLineAsync("        /// <summary>").ConfigureAwait(false);
                        await writer.WriteLineAsync("        /// The wrapped object.").ConfigureAwait(false);
                        await writer.WriteLineAsync("        /// </summary>").ConfigureAwait(false);
                    }
                    if (BaseType != null)
                    {
                        await writer.WriteLineAsync($"        public new {typeFullName} {ObjectName} => ({typeFullName})base.{ObjectName};").ConfigureAwait(false);
                    }
                    else
                    {
                        await writer.WriteLineAsync($"        public {typeFullName} {ObjectName} {{ get; {(IsValueType ? string.Empty : "private ")}set; }}").ConfigureAwait(false);
                    }
                    await writer.WriteLineAsync().ConfigureAwait(false);
                }

                var genericIEnumerable = ImplicitInterfaces.FirstOrDefault(i => i.Type.IsGenericType && i.Type.GetGenericTypeDefinition() == typeof(IEnumerable<>));
                var genericArg = (genericIEnumerable as CollectionData)?.ElementType;

                foreach (var property in Properties)
                {
                    if (property.Generation == MemberGeneration.WrapEventHandlerInCompilerFlag)
                    {
                        throw new NotSupportedException("WrapEventHandlerInCompilerFlag is not supported for properties.");
                    }
                    var wrapInCompilerFlag = property.Generation == MemberGeneration.WrapImplementationInCompilerFlag;
                    if (wrapInCompilerFlag || property.Generation == MemberGeneration.Full || property.Generation == MemberGeneration.FullWithSafeCaching || property.Generation == MemberGeneration.OnlyInImplementation)
                    {
                        var accessorName = property.IsStatic ? typeFullName : ObjectName;
                        if (wrapInCompilerFlag)
                        {
                            await writer.WriteLineAsync($"#if {builder.DefaultMemberGenerationCompilerFlag}").ConfigureAwait(false);
                        }
                        if (property.Parameters.Count > 0)
                        {
                            if (property.Generation == MemberGeneration.FullWithSafeCaching)
                            {
                                throw new NotSupportedException("Caching is not supported for indexers.");
                            }
                            var hasExplicitInterfaceImplementation = property.Generation != MemberGeneration.OnlyInImplementation && (property.Type.InterfaceName != property.Type.ClassName || property.Parameters.Any(p => p.Type.ClassName != p.Type.InterfaceName));
                            if (builder.DocumentationGeneration is DocumentationGeneration.GenerateWithInheritDoc or DocumentationGeneration.GenerateWithInheritDocWithReferencesToWrapped)
                            {
                                if (hasExplicitInterfaceImplementation)
                                {
                                    await writer.WriteLineAsync($"        /// <inheritdoc cref=\"{(property.DeclaringInterfaceType?.InterfaceName ?? InterfaceName).ToString(inXmlComment: true)}.this[{string.Join(", ", property.Parameters.Select(p => p.GetAsXmlParameter()))}]\"/>").ConfigureAwait(false);
                                }
                                else
                                {
                                    await writer.WriteLineAsync("        /// <inheritdoc/>").ConfigureAwait(false);
                                }
                            }
                            else if (property.Documentation.Any())
                            {
                                await writer.WriteLineAsync(string.Join(writer.NewLine, property.Documentation.SelectMany(d => d.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.None)).Select(d => $"        /// {(d.StartsWith("            ") ? d.Substring(12) : d)}"))).ConfigureAwait(false);
                            }
                            if (property.ObsoleteMessage != null)
                            {
                                await writer.WriteLineAsync($"        [Obsolete{(property.ObsoleteMessage.Length > 0 ? $"(\"{property.ObsoleteMessage}\")" : string.Empty)}]");
                            }
                            var basicArgumentList = string.Join(", ", property.Parameters.Select(p => p.GetAsArgument()));
                            await writer.WriteAsync($"        public {(BaseType?.HasProperty(p => p.Equals(property, checkReturnType: false)) == true ? "new " : string.Empty)}{property.Type.ClassName} this[{string.Join(", ", property.Parameters.Select(p => p.GetAsClassParameter()))}] ").ConfigureAwait(false);
                            if (property.HasGetter && !property.HasSetter)
                            {
                                await writer.WriteLineAsync($"=> {property.Type.GetCodeToConvertFromActualType($"{accessorName}[{basicArgumentList}]")};").ConfigureAwait(false);
                            }
                            else
                            {
                                await writer.WriteAsync("{ ").ConfigureAwait(false);
                                if (property.HasGetter)
                                {
                                    await writer.WriteAsync($"get {(builder.MinCSharpVersion >= 7M ? "=>" : "{ return")} {property.Type.GetCodeToConvertFromActualType($"{accessorName}[{basicArgumentList}]")};{(builder.MinCSharpVersion >= 7M ? string.Empty : " }")} ").ConfigureAwait(false);
                                }
                                if (property.HasSetter)
                                {
                                    if (IsValueType)
                                    {
                                        await writer.WriteAsync($"set {{ var {ObjectParamName} = {ObjectName}; {ObjectParamName}[{basicArgumentList}] = {property.Type.GetCodeToConvertToActualType("value")}; {ObjectName} = {ObjectParamName}; }} ").ConfigureAwait(false);
                                    }
                                    else
                                    {
                                        await writer.WriteAsync($"set {(builder.MinCSharpVersion >= 7M ? "=>" : "{")} {accessorName}[{basicArgumentList}] = {property.Type.GetCodeToConvertToActualType("value")};{(builder.MinCSharpVersion >= 7M ? string.Empty : " }")} ").ConfigureAwait(false);
                                    }
                                }
                                await writer.WriteLineAsync("}").ConfigureAwait(false);
                            }
                            if (hasExplicitInterfaceImplementation)
                            {
                                var advancedArgumentList = string.Join(", ", property.Parameters.Select(p => p.Type.GetCodeToConvertToClassType(p.Name)));
                                await writer.WriteLineAsync().ConfigureAwait(false);
                                await writer.WriteLineAsync($"        {property.Type.InterfaceName} {property.DeclaringInterfaceType?.InterfaceName ?? InterfaceName}.this[{string.Join(", ", property.Parameters.Select(p => p.GetAsInterfaceParameter()))}] {(property.HasGetter && !property.HasSetter ? $"=> this[{advancedArgumentList}];" : $"{{ {(property.HasGetter ? $"get {(builder.MinCSharpVersion >= 7M ? "=>" : "{ return")} this[{advancedArgumentList}];{(builder.MinCSharpVersion >= 7M ? string.Empty : " }")} " : string.Empty)}{(property.HasSetter ? $"set {(builder.MinCSharpVersion >= 7M ? "=>" : "{")} this[{advancedArgumentList}] = {property.Type.GetCodeToConvertToClassType("value")};{(builder.MinCSharpVersion >= 7M ? string.Empty : " }")} " : string.Empty)}}}")}").ConfigureAwait(false);
                            }
                        }
                        else
                        {
                            var hasExplicitInterfaceImplementation = property.Generation != MemberGeneration.OnlyInImplementation && property.Type.InterfaceName != property.Type.ClassName;
                            if (property.Generation == MemberGeneration.FullWithSafeCaching)
                            {
                                if (!property.HasGetter)
                                {
                                    throw new NotSupportedException("Caching is not supported for setter only properties.");
                                }
                                var variableName = $"{char.ToLower(property.Name[0])}{property.Name.Substring(1)}";
                                var fieldName = $"_{variableName}";
                                await writer.WriteLineAsync($"        private {property.Type.ClassName} {fieldName};").ConfigureAwait(false);
                                await writer.WriteLineAsync().ConfigureAwait(false);
                                if (builder.DocumentationGeneration is DocumentationGeneration.GenerateWithInheritDoc or DocumentationGeneration.GenerateWithInheritDocWithReferencesToWrapped)
                                {
                                    if (hasExplicitInterfaceImplementation)
                                    {
                                        await writer.WriteLineAsync($"        /// <inheritdoc cref=\"{(property.DeclaringInterfaceType?.InterfaceName ?? InterfaceName).ToString(inXmlComment: true)}.{property.Name}\"/>").ConfigureAwait(false);
                                    }
                                    else
                                    {
                                        await writer.WriteLineAsync("        /// <inheritdoc/>").ConfigureAwait(false);
                                    }
                                }
                                else if (property.Documentation.Any())
                                {
                                    await writer.WriteLineAsync(string.Join(writer.NewLine, property.Documentation.SelectMany(d => d.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.None)).Select(d => $"        /// {(d.StartsWith("            ") ? d.Substring(12) : d)}"))).ConfigureAwait(false);
                                }
                                if (property.ObsoleteMessage != null)
                                {
                                    await writer.WriteLineAsync($"        [Obsolete{(property.ObsoleteMessage.Length > 0 ? $"(\"{property.ObsoleteMessage}\")" : string.Empty)}]");
                                }
                                await writer.WriteLineAsync($"        public {(BaseType?.HasProperty(p => p.Equals(property, checkReturnType: false)) == true ? "new " : string.Empty)}{property.Type.ClassName} {property.Name} {{ get {{ var {variableName} = {fieldName}; var {ObjectParamName} = {accessorName}.{property.Name}; return ReferenceEquals({variableName}?.{ObjectName}, {ObjectParamName}) ? {variableName} : {fieldName} = {property.Type.GetCodeToConvertFromActualType(ObjectParamName)}; }} {(property.HasSetter ? $"set => {accessorName}.{property.Name} = {property.Type.GetCodeToConvertToActualType("value")}; " : string.Empty)}}}").ConfigureAwait(false);
                            }
                            else
                            {
                                if (builder.DocumentationGeneration is DocumentationGeneration.GenerateWithInheritDoc or DocumentationGeneration.GenerateWithInheritDocWithReferencesToWrapped)
                                {
                                    if (hasExplicitInterfaceImplementation)
                                    {
                                        await writer.WriteLineAsync($"        /// <inheritdoc cref=\"{property.DeclaringInterfaceType?.InterfaceName ?? InterfaceName}.{property.Name}\"/>").ConfigureAwait(false);
                                    }
                                    else
                                    {
                                        await writer.WriteLineAsync("        /// <inheritdoc/>").ConfigureAwait(false);
                                    }
                                }
                                else if(property.Documentation.Any())
                                {
                                    await writer.WriteLineAsync(string.Join(writer.NewLine, property.Documentation.SelectMany(d => d.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.None)).Select(d => $"        /// {(d.StartsWith("            ") ? d.Substring(12) : d)}"))).ConfigureAwait(false);
                                }
                                if (property.ObsoleteMessage != null)
                                {
                                    await writer.WriteLineAsync($"        [Obsolete{(property.ObsoleteMessage.Length > 0 ? $"(\"{property.ObsoleteMessage}\")" : string.Empty)}]");
                                }
                                await writer.WriteAsync($"        public {(BaseType?.HasProperty(p => p.Equals(property, checkReturnType: false)) == true ? "new " : string.Empty)}{property.Type.ClassName} {property.Name} ").ConfigureAwait(false);
                                if (property.HasGetter && !property.HasSetter)
                                {
                                    await writer.WriteLineAsync($"=> {property.Type.GetCodeToConvertFromActualType($"{accessorName}.{property.Name}")};").ConfigureAwait(false);
                                }
                                else
                                {
                                    await writer.WriteAsync("{ ").ConfigureAwait(false);
                                    if (property.HasGetter)
                                    {
                                        await writer.WriteAsync($"get {(builder.MinCSharpVersion >= 7M ? "=>" : "{ return")} {property.Type.GetCodeToConvertFromActualType($"{accessorName}.{property.Name}")};{(builder.MinCSharpVersion >= 7M ? string.Empty : " }")} ").ConfigureAwait(false);
                                    }
                                    if (property.HasSetter)
                                    {
                                        if (IsValueType)
                                        {
                                            await writer.WriteAsync($"set {{ var {ObjectParamName} = {ObjectName}; {ObjectParamName}.{property.Name} = {property.Type.GetCodeToConvertToActualType("value")}; {ObjectName} = {ObjectParamName}; }} ").ConfigureAwait(false);
                                        }
                                        else
                                        {
                                            await writer.WriteAsync($"set {(builder.MinCSharpVersion >= 7M ? "=>" : "{")} {accessorName}.{property.Name} = {property.Type.GetCodeToConvertToActualType("value")};{(builder.MinCSharpVersion >= 7M ? string.Empty : " }")} ").ConfigureAwait(false);
                                        }
                                    }
                                    await writer.WriteLineAsync("}").ConfigureAwait(false);
                                }
                            }
                            if (hasExplicitInterfaceImplementation)
                            {
                                await writer.WriteLineAsync().ConfigureAwait(false);
                                if (property.ObsoleteMessage != null)
                                {
                                    await writer.WriteLineAsync("        [Obsolete]").ConfigureAwait(false);
                                }
                                await writer.WriteLineAsync($"        {property.Type.InterfaceName} {property.DeclaringInterfaceType?.InterfaceName ?? InterfaceName}.{property.Name} {(property.HasGetter && !property.HasSetter ? $"=> {property.Name};" : $"{{ {(property.HasGetter ? $"get {(builder.MinCSharpVersion >= 7M ? "=>" : "{ return")} {property.Name};{(builder.MinCSharpVersion >= 7M ? string.Empty : " }")} " : string.Empty)}{(property.HasSetter ? $"set {(builder.MinCSharpVersion >= 7M ? "=>" : "{")} {property.Name} = {property.Type.GetCodeToConvertToClassType("value")};{(builder.MinCSharpVersion >= 7M ? string.Empty : " }")} " : string.Empty)}}}")}").ConfigureAwait(false);
                            }
                        }
                        if (wrapInCompilerFlag)
                        {
                            await writer.WriteLineAsync("#endif").ConfigureAwait(false);
                        }
                        await writer.WriteLineAsync().ConfigureAwait(false);
                    }
                }

                foreach (var @interface in Interfaces)
                {
                    var firstToImplement = BaseType?.HasInterface(i => i.Equals(@interface)) != true;
                    if (firstToImplement || @interface.Type == typeof(IList))
                    {
                        foreach (var property in @interface.Properties)
                        {
                            if (!HasProperty(p => p.Equals(property)))
                            {
                                Type genericTypeDefinition;
                                if (@interface.Type == typeof(IList))
                                {
                                    if (property.Parameters.Count > 0 && (firstToImplement || (genericArg?.BuildStatus ?? TypeBuildStatus.NotBuilding) != TypeBuildStatus.NotBuilding))
                                    {
                                        var indexer = Properties.FirstOrDefault(p => p.Name == property.Name && p.Parameters.Select((pm, i) => (pm, i)).All(t => t.pm.Equals(p.Parameters[t.i])));
                                        var getterLogic = $"((IList){ObjectName})[index]";
                                        var setterLogic = $"((IList){ObjectName})[index] = {(genericArg != null ? $"value is {genericArg.ClassName} {(builder.MinCSharpVersion >= 7M ? $"o ? o" : $"? (({genericArg.ClassName})value)")}.{ObjectName} : value" : "value")}";
                                        if (indexer?.HasGetter == true)
                                        {
                                            getterLogic = $"this[index]";
                                        }
                                        await writer.WriteLineAsync($"        object IList.this[int index] {{ get {(builder.MinCSharpVersion >= 7M ? "=>" : "{ return")} {getterLogic};{(builder.MinCSharpVersion >= 7M ? string.Empty : " }")} set {(builder.MinCSharpVersion >= 7M ? "=>" : "{")} {setterLogic};{(builder.MinCSharpVersion >= 7M ? string.Empty : " }")} }}").ConfigureAwait(false);
                                        await writer.WriteLineAsync().ConfigureAwait(false);
                                    }
                                    else if (firstToImplement)
                                    {
                                        await writer.WriteLineAsync($"        {property.Type.InterfaceName} {@interface.InterfaceName}.{property.Name} {(property.HasGetter && !property.HasSetter ? $"=> (({@interface.InterfaceName}){ObjectName}).{property.Name};" : $"{{ {(property.HasGetter ? $"get {(builder.MinCSharpVersion >= 7M ? "=>" : "{ return")} (({@interface.InterfaceName}){ObjectName}).{property.Name};{(builder.MinCSharpVersion >= 7M ? string.Empty : " }")} " : string.Empty)}{(property.HasSetter ? $"set {(builder.MinCSharpVersion >= 7M ? "=>" : "{")} (({@interface.InterfaceName}){ObjectName}).{property.Name} = {property.Type.GetCodeToConvertToClassType("value")};{(builder.MinCSharpVersion >= 7M ? string.Empty : " }")} " : string.Empty)}}}")}").ConfigureAwait(false);
                                        await writer.WriteLineAsync().ConfigureAwait(false);
                                    }
                                }
                                else if (@interface.Type.IsGenericType && ((genericTypeDefinition = @interface.Type.GetGenericTypeDefinition()) == typeof(ICollection<>) || genericTypeDefinition == typeof(IList<>) || genericTypeDefinition == typeof(IReadOnlyCollection<>) || genericTypeDefinition == typeof(IReadOnlyList<>)))
                                {
                                    if (property.Parameters.Count > 0)
                                    {
                                        var advancedArgumentList = string.Join(", ", property.Parameters.Select(p => p.Type.GetCodeToConvertToClassType(p.Name)));
                                        var indexer = Properties.FirstOrDefault(p => p.Name == property.Name && p.Parameters.Select((pm, i) => (pm, i)).All(t => t.pm.Equals(p.Parameters[t.i])));
                                        var getterLogic = $"{property.Type.GetCodeToConvertFromActualType($"({property.Type.Type.FullName.Replace('+', '.')})((IList){ObjectName})[{advancedArgumentList}]")}";
                                        var setterLogic = $"((IList){ObjectName})[{advancedArgumentList}] = ({property.Type.GetCodeToConvertToClassType("value")}).{ObjectName}";
                                        if (indexer?.HasGetter == true)
                                        {
                                            getterLogic = $"this[{advancedArgumentList}]";
                                        }
                                        if (indexer?.HasSetter == true)
                                        {
                                            setterLogic = $"this[{advancedArgumentList}] = {property.Type.GetCodeToConvertToClassType("value")}";
                                        }
                                        await writer.WriteLineAsync($"        {property.Type.InterfaceName} {@interface.InterfaceName}.this[{string.Join(", ", property.Parameters.Select(p => p.GetAsInterfaceParameter()))}] {(property.HasGetter && !property.HasSetter ? $"=> {getterLogic};" : $"{{ {(property.HasGetter ? $"get {(builder.MinCSharpVersion >= 7M ? "=>" : "{ return")} {getterLogic};{(builder.MinCSharpVersion >= 7M ? string.Empty : " }")} " : string.Empty)}{(property.HasSetter ? $"set {(builder.MinCSharpVersion >= 7M ? "=>" : "{")} {setterLogic};{(builder.MinCSharpVersion >= 7M ? string.Empty : " }")} " : string.Empty)}}}")}").ConfigureAwait(false);
                                        await writer.WriteLineAsync().ConfigureAwait(false);
                                    }
                                    else if (property.Name == "Count")
                                    {
                                        await writer.WriteLineAsync($"        int {@interface.InterfaceName}.Count => ((ICollection){ObjectName}).Count;").ConfigureAwait(false);
                                        await writer.WriteLineAsync().ConfigureAwait(false);
                                    }
                                    else if (property.Name == "IsReadOnly")
                                    {
                                        await writer.WriteLineAsync($"        bool {@interface.InterfaceName}.IsReadOnly => ((IList){ObjectName}).IsReadOnly;").ConfigureAwait(false);
                                        await writer.WriteLineAsync().ConfigureAwait(false);
                                    }
                                }
                                else if (property.Parameters.Count > 0)
                                {
                                    var advancedArgumentList = string.Join(", ", property.Parameters.Select(p => p.Type.GetCodeToConvertToClassType(p.Name)));
                                    await writer.WriteLineAsync($"        {property.Type.InterfaceName} {@interface.InterfaceName}.this[{string.Join(", ", property.Parameters.Select(p => p.GetAsInterfaceParameter()))}] {(property.HasGetter && !property.HasSetter ? $"=> (({@interface.InterfaceName}){ObjectName})[{advancedArgumentList}];" : $"{{ {(property.HasGetter ? $"get {(builder.MinCSharpVersion >= 7M ? "=>" : "{ return")} (({@interface.InterfaceName}){ObjectName})[{advancedArgumentList}];{(builder.MinCSharpVersion >= 7M ? string.Empty : " }")} " : string.Empty)}{(property.HasSetter ? $"set {(builder.MinCSharpVersion >= 7M ? "=>" : "{")} (({@interface.InterfaceName}){ObjectName})[{advancedArgumentList}] = {property.Type.GetCodeToConvertToClassType("value")};{(builder.MinCSharpVersion >= 7M ? string.Empty : " }")} " : string.Empty)}}}")}").ConfigureAwait(false);
                                    await writer.WriteLineAsync().ConfigureAwait(false);
                                }
                                else
                                {
                                    await writer.WriteLineAsync($"        {property.Type.InterfaceName} {@interface.InterfaceName}.{property.Name} {(property.HasGetter && !property.HasSetter ? $"=> (({@interface.InterfaceName}){ObjectName}).{property.Name};" : $"{{ {(property.HasGetter ? $"get {(builder.MinCSharpVersion >= 7M ? "=>" : "{ return")} (({@interface.InterfaceName}){ObjectName}).{property.Name};{(builder.MinCSharpVersion >= 7M ? string.Empty : " }")} " : string.Empty)}{(property.HasSetter ? $"set {(builder.MinCSharpVersion >= 7M ? "=>" : "{")} (({@interface.InterfaceName}){ObjectName}).{property.Name} = {property.Type.GetCodeToConvertToClassType("value")};{(builder.MinCSharpVersion >= 7M ? string.Empty : " }")} " : string.Empty)}}}")}").ConfigureAwait(false);
                                    await writer.WriteLineAsync().ConfigureAwait(false);
                                }
                            }
                        }
                    }
                }

                foreach (var @event in Events)
                {
                    if (@event.Generation == MemberGeneration.FullWithSafeCaching)
                    {
                        throw new NotSupportedException("Caching is not supported for events.");
                    }
                    var wrapInCompilerFlag = @event.Generation == MemberGeneration.WrapImplementationInCompilerFlag;
                    if (wrapInCompilerFlag || @event.Generation == MemberGeneration.Full || @event.Generation == MemberGeneration.WrapEventHandlerInCompilerFlag || @event.Generation == MemberGeneration.OnlyInImplementation)
                    {
                        var accessorName = @event.IsStatic ? typeFullName : ObjectName;
                        if (wrapInCompilerFlag)
                        {
                            await writer.WriteLineAsync($"#if {builder.DefaultMemberGenerationCompilerFlag}").ConfigureAwait(false);
                        }
                        if (builder.DocumentationGeneration is DocumentationGeneration.GenerateWithInheritDoc or DocumentationGeneration.GenerateWithInheritDocWithReferencesToWrapped)
                        {
                            await writer.WriteLineAsync("        /// <inheritdoc/>").ConfigureAwait(false);
                        }
                        else if (@event.Documentation.Any())
                        {
                            await writer.WriteLineAsync(string.Join(writer.NewLine, @event.Documentation.SelectMany(d => d.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.None)).Select(d => $"        /// {(d.StartsWith("            ") ? d.Substring(12) : d)}"))).ConfigureAwait(false);
                        }
                        if (@event.ObsoleteMessage != null)
                        {
                            await writer.WriteLineAsync($"        [Obsolete{(@event.ObsoleteMessage.Length > 0 ? $"(\"{@event.ObsoleteMessage}\")" : string.Empty)}]");
                        }
                        await writer.WriteLineAsync($"        public event {@event.Type.InterfaceName} {@event.Name}").ConfigureAwait(false);
                        await writer.WriteLineAsync("        {").ConfigureAwait(false);
                        if (@event.IsStatic && @event.Type.ClassName == @event.Type.InterfaceName)
                        {
                            await writer.WriteLineAsync($"            add {(builder.MinCSharpVersion >= 7M ? "=>" : "{")} {accessorName}.{@event.Name} += value;{(builder.MinCSharpVersion >= 7M ? string.Empty : " }")}").ConfigureAwait(false);
                            await writer.WriteLineAsync($"            remove {(builder.MinCSharpVersion >= 7M ? "=>" : "{")} {accessorName}.{@event.Name} -= value;{(builder.MinCSharpVersion >= 7M ? string.Empty : " }")}").ConfigureAwait(false);
                            await writer.WriteLineAsync("        }").ConfigureAwait(false);
                        }
                        else
                        {
                            var fieldName = $"_{char.ToLower(@event.Name[0])}{@event.Name.Substring(1)}";
                            var handlerMethod = $"{@event.Name}Handler";
                            var handlerVar1 = "handler";
                            var handlerVar2 = "handler2";
                            var combinedVar = "combined";
                            var removedVar = "removed";
                            await writer.WriteLineAsync($@"            add").ConfigureAwait(false);
                            await writer.WriteLineAsync($@"            {{").ConfigureAwait(false);
                            await writer.WriteLineAsync($@"                if (value == null)").ConfigureAwait(false);
                            await writer.WriteLineAsync($@"                {{").ConfigureAwait(false);
                            await writer.WriteLineAsync($@"                    return;").ConfigureAwait(false);
                            await writer.WriteLineAsync($@"                }}").ConfigureAwait(false);
                            await writer.WriteLineAsync($@"                {@event.Type.InterfaceName} {handlerVar1};").ConfigureAwait(false);
                            await writer.WriteLineAsync($@"                {@event.Type.InterfaceName} {handlerVar2} = {fieldName};").ConfigureAwait(false);
                            await writer.WriteLineAsync($@"                {@event.Type.InterfaceName} {combinedVar};").ConfigureAwait(false);
                            await writer.WriteLineAsync($@"                do").ConfigureAwait(false);
                            await writer.WriteLineAsync($@"                {{").ConfigureAwait(false);
                            await writer.WriteLineAsync($@"                    {handlerVar1} = {handlerVar2};").ConfigureAwait(false);
                            await writer.WriteLineAsync($@"                    {combinedVar} = ({@event.Type.InterfaceName})Delegate.Combine({handlerVar1}, value);").ConfigureAwait(false);
                            await writer.WriteLineAsync($@"                    {handlerVar2} = Interlocked.CompareExchange(ref {fieldName}, {combinedVar}, {handlerVar1});").ConfigureAwait(false);
                            await writer.WriteLineAsync($@"                }} while ({handlerVar1} != {handlerVar2});").ConfigureAwait(false);
                            await writer.WriteLineAsync($@"                if ({handlerVar1} == null)").ConfigureAwait(false);
                            await writer.WriteLineAsync($@"                {{").ConfigureAwait(false);
                            await writer.WriteLineAsync($@"                    {accessorName}.{@event.Name} += {handlerMethod};").ConfigureAwait(false);
                            await writer.WriteLineAsync($@"                }}").ConfigureAwait(false);
                            await writer.WriteLineAsync($@"            }}").ConfigureAwait(false);
                            await writer.WriteLineAsync($@"            remove").ConfigureAwait(false);
                            await writer.WriteLineAsync($@"            {{").ConfigureAwait(false);
                            await writer.WriteLineAsync($@"                if (value == null)").ConfigureAwait(false);
                            await writer.WriteLineAsync($@"                {{").ConfigureAwait(false);
                            await writer.WriteLineAsync($@"                    return;").ConfigureAwait(false);
                            await writer.WriteLineAsync($@"                }}").ConfigureAwait(false);
                            await writer.WriteLineAsync($@"                {@event.Type.InterfaceName} {handlerVar1};").ConfigureAwait(false);
                            await writer.WriteLineAsync($@"                {@event.Type.InterfaceName} {handlerVar2} = {fieldName};").ConfigureAwait(false);
                            await writer.WriteLineAsync($@"                {@event.Type.InterfaceName} {removedVar};").ConfigureAwait(false);
                            await writer.WriteLineAsync($@"                do").ConfigureAwait(false);
                            await writer.WriteLineAsync($@"                {{").ConfigureAwait(false);
                            await writer.WriteLineAsync($@"                    {handlerVar1} = {handlerVar2};").ConfigureAwait(false);
                            await writer.WriteLineAsync($@"                    {removedVar} = ({@event.Type.InterfaceName})Delegate.Remove({handlerVar1}, value);").ConfigureAwait(false);
                            await writer.WriteLineAsync($@"                    {handlerVar2} = Interlocked.CompareExchange(ref {fieldName}, {removedVar}, {handlerVar1});").ConfigureAwait(false);
                            await writer.WriteLineAsync($@"                }} while ({handlerVar1} != {handlerVar2});").ConfigureAwait(false);
                            await writer.WriteLineAsync($@"                if ({removedVar} == null)").ConfigureAwait(false);
                            await writer.WriteLineAsync($@"                {{").ConfigureAwait(false);
                            await writer.WriteLineAsync($@"                    {accessorName}.{@event.Name} -= {handlerMethod};").ConfigureAwait(false);
                            await writer.WriteLineAsync($@"                }}").ConfigureAwait(false);
                            await writer.WriteLineAsync($@"            }}").ConfigureAwait(false);
                            await writer.WriteLineAsync($@"        }}").ConfigureAwait(false);
                            await writer.WriteLineAsync().ConfigureAwait(false);
                            await writer.WriteLineAsync($@"        private {@event.Type.InterfaceName} {fieldName};").ConfigureAwait(false);
                            await writer.WriteLineAsync().ConfigureAwait(false);
                            if (@event.Generation == MemberGeneration.WrapEventHandlerInCompilerFlag)
                            {
                                await writer.WriteLineAsync($"#if {builder.DefaultMemberGenerationCompilerFlag}").ConfigureAwait(false);
                            }
                            await writer.WriteAsync($@"        private void {handlerMethod}({string.Join(", ", @event.Type.Parameters.Select(p => p.GetAsActualParameter()))})").ConfigureAwait(false);
                            var body = $"{fieldName}?.Invoke({string.Join(", ", @event.Type.Parameters.Select((p, i) => i == 0 && p.Type.Type == typeof(object) && !@event.IsStatic ? $"{p.Name} is {typeFullName} {(builder.MinCSharpVersion >= 7M ? $"o ? ({ClassName})o" : $"? ({ClassName})({typeFullName}){p.Name}")} : {p.Name}" : p.Type.GetCodeToConvertFromActualTypeToInterface(p.Name)))});";
                            if (builder.MinCSharpVersion >= 7M)
                            {
                                await writer.WriteLineAsync($" => {body}").ConfigureAwait(false);
                            }
                            else
                            {
                                await writer.WriteLineAsync().ConfigureAwait(false);
                                await writer.WriteLineAsync($@"        {{").ConfigureAwait(false);
                                await writer.WriteLineAsync($@"            {body}").ConfigureAwait(false);
                                await writer.WriteLineAsync($@"        }}").ConfigureAwait(false);
                            }
                            if (@event.Generation == MemberGeneration.WrapEventHandlerInCompilerFlag)
                            {
                                await writer.WriteLineAsync("#endif").ConfigureAwait(false);
                            }
                        }
                        if (wrapInCompilerFlag)
                        {
                            await writer.WriteLineAsync("#endif").ConfigureAwait(false);
                        }
                        await writer.WriteLineAsync().ConfigureAwait(false);
                    }
                }

                // TODO: Add explicit interface implementation support for events

                if (TypeGeneration.HasFlag(TypeGeneration.Instance))
                {
                    if (builder.DocumentationGeneration != DocumentationGeneration.None)
                    {
                        await writer.WriteLineAsync($@"        /// <summary>").ConfigureAwait(false);
                        await writer.WriteLineAsync($@"        /// The wrapper constructor.").ConfigureAwait(false);
                        await writer.WriteLineAsync($@"        /// </summary>").ConfigureAwait(false);
                        await writer.WriteLineAsync($@"        /// <param name=""object"">The object to wrap.</param>").ConfigureAwait(false);
                    }
                    await writer.WriteLineAsync($"        public {ClassName}({typeFullName} {ObjectParamName})").ConfigureAwait(false);
                    if (BaseType != null)
                    {
                        await writer.WriteLineAsync($"            : base({ObjectParamName})").ConfigureAwait(false);
                        await writer.WriteLineAsync("        {").ConfigureAwait(false);
                        await writer.WriteLineAsync("        }").ConfigureAwait(false);
                    }
                    else
                    {
                        await writer.WriteLineAsync("        {").ConfigureAwait(false);
                        if (IsValueType)
                        {
                            await writer.WriteLineAsync($"            {ObjectName} = {ObjectParamName};").ConfigureAwait(false);
                        }
                        else if (builder.MinCSharpVersion >= 7M)
                        {
                            await writer.WriteLineAsync($"            {ObjectName} = {ObjectParamName} ?? throw new ArgumentNullException(nameof({ObjectParamName}));").ConfigureAwait(false);
                        }
                        else
                        {
                            await writer.WriteLineAsync($"            if ({ObjectParamName} == null)").ConfigureAwait(false);
                            await writer.WriteLineAsync("            {").ConfigureAwait(false);
                            await writer.WriteLineAsync($"                throw new ArgumentNullException(nameof({ObjectParamName}));").ConfigureAwait(false);
                            await writer.WriteLineAsync("            }").ConfigureAwait(false);
                            await writer.WriteLineAsync($"            {ObjectName} = {ObjectParamName};").ConfigureAwait(false);
                        }
                        await writer.WriteLineAsync("        }").ConfigureAwait(false);
                    }
                }
                else
                {
                    if (builder.DocumentationGeneration != DocumentationGeneration.None)
                    {
                        await writer.WriteLineAsync($@"        /// <summary>").ConfigureAwait(false);
                        await writer.WriteLineAsync($@"        /// The wrapper constructor.").ConfigureAwait(false);
                        await writer.WriteLineAsync($@"        /// </summary>").ConfigureAwait(false);
                    }
                    await writer.WriteLineAsync($"        public {ClassName}()").ConfigureAwait(false);
                    await writer.WriteLineAsync("        {").ConfigureAwait(false);
                    await writer.WriteLineAsync("        }").ConfigureAwait(false);
                }

                foreach (var constructor in Constructors)
                {
                    if (constructor.Generation == MemberGeneration.FullWithSafeCaching)
                    {
                        throw new NotSupportedException("Caching is not supported for constructors.");
                    }
                    if (constructor.Generation == MemberGeneration.WrapEventHandlerInCompilerFlag)
                    {
                        throw new NotSupportedException("WrapEventHandlerInCompilerFlag is not supported for constructors.");
                    }
                    if (constructor.Generation == MemberGeneration.OnlyInInterface)
                    {
                        throw new NotSupportedException("OnlyInInterface is not supported for constructors.");
                    }
                    await writer.WriteLineAsync().ConfigureAwait(false);
                    var wrapInCompilerFlag = constructor.Generation == MemberGeneration.WrapImplementationInCompilerFlag;
                    if (wrapInCompilerFlag)
                    {
                        await writer.WriteLineAsync($"#if {builder.DefaultMemberGenerationCompilerFlag}").ConfigureAwait(false);
                    }
                    if (builder.DocumentationGeneration is DocumentationGeneration.GenerateWithInheritDocWithReferencesToWrapped)
                    {
                        var hasIList = Type.GetConstructors().Any(c => c.GetParameters().Any(p => p.ParameterType == typeof(IList)));
                        await writer.WriteLineAsync($"        /// <inheritdoc cref=\"{typeFullName}({string.Join(", ", constructor.Parameters.Select(p => hasIList && p.Type.Type.IsGenericType && p.Type.Type.GetGenericTypeDefinition() == typeof(List<>) ? "IList" : p.GetAsActualXmlParameter()))})\"/>").ConfigureAwait(false);
                    }
                    else if (constructor.Documentation.Any())
                    {
                        await writer.WriteLineAsync(string.Join(writer.NewLine, constructor.Documentation.SelectMany(d => d.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.None)).Select(d => $"        /// {(d.StartsWith("            ") ? d.Substring(12) : d)}"))).ConfigureAwait(false);
                    }
                    if (constructor.ObsoleteMessage != null)
                    {
                        await writer.WriteLineAsync($"        [Obsolete{(constructor.ObsoleteMessage.Length > 0 ? $"(\"{constructor.ObsoleteMessage}\")" : string.Empty)}]");
                    }
                    await writer.WriteLineAsync($"        public {ClassName}({string.Join(", ", constructor.Parameters.Select(p => p.GetAsClassParameter()))})").ConfigureAwait(false);
                    await writer.WriteLineAsync($"            : this(new {typeFullName}({string.Join(", ", constructor.Parameters.Select(p => p.GetCodeToConvertToActualType()))}))").ConfigureAwait(false);
                    await writer.WriteLineAsync("        {").ConfigureAwait(false);
                    await writer.WriteLineAsync("        }").ConfigureAwait(false);
                    if (wrapInCompilerFlag)
                    {
                        await writer.WriteLineAsync("#endif").ConfigureAwait(false);
                    }
                }

                foreach (var method in Methods)
                {
                    if (method.Generation == MemberGeneration.FullWithSafeCaching)
                    {
                        throw new NotSupportedException("Caching is not supported for methods.");
                    }
                    if (method.Generation == MemberGeneration.WrapEventHandlerInCompilerFlag)
                    {
                        throw new NotSupportedException("WrapEventHandlerInCompilerFlag is not supported for methods.");
                    }
                    var wrapInCompilerFlag = method.Generation == MemberGeneration.WrapImplementationInCompilerFlag;
                    if (wrapInCompilerFlag || method.Generation == MemberGeneration.Full || method.Generation == MemberGeneration.OnlyInImplementation)
                    {
                        await writer.WriteLineAsync().ConfigureAwait(false);
                        if (wrapInCompilerFlag)
                        {
                            await writer.WriteLineAsync($"#if {builder.DefaultMemberGenerationCompilerFlag}").ConfigureAwait(false);
                        }
                        var hasExplicitInterfaceImplementation = method.Generation != MemberGeneration.OnlyInImplementation && (method.ReturnType.ClassName != method.ReturnType.InterfaceName || method.Parameters.Any(p => p.Type.ClassName != p.Type.InterfaceName));
                        if (builder.DocumentationGeneration is DocumentationGeneration.GenerateWithInheritDoc or DocumentationGeneration.GenerateWithInheritDocWithReferencesToWrapped && method.Generation != MemberGeneration.OnlyInImplementation)
                        {
                            if (hasExplicitInterfaceImplementation)
                            {
                                await writer.WriteLineAsync($"        /// <inheritdoc cref=\"{(method.DeclaringInterfaceType?.InterfaceName ?? InterfaceName).ToString(inXmlComment: true)}.{method.Name}({string.Join(", ", method.Parameters.Select(p => p.GetAsXmlParameter()))})\"/>").ConfigureAwait(false);
                            }
                            else
                            {
                                await writer.WriteLineAsync("        /// <inheritdoc/>").ConfigureAwait(false);
                            }
                        }
                        else if (method.Documentation.Any())
                        {
                            await writer.WriteLineAsync(string.Join(writer.NewLine, method.Documentation.SelectMany(d => d.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.None)).Select(d => $"        /// {(d.StartsWith("            ") ? d.Substring(12) : d)}"))).ConfigureAwait(false);
                        }
                        if (method.ObsoleteMessage != null)
                        {
                            await writer.WriteLineAsync($"        [Obsolete{(method.ObsoleteMessage.Length > 0 ? $"(\"{method.ObsoleteMessage}\")" : string.Empty)}]");
                        }
                        var isGenericGetEnumerator = method.Name == "GetEnumerator" && method.ReturnType.Type != typeof(IEnumerator);
                        if (isGenericGetEnumerator)
                        {
                            await writer.WriteLineAsync($"        public {method.ReturnType.ClassName} GetEnumerator()").ConfigureAwait(false);
                            await writer.WriteLineAsync("        {").ConfigureAwait(false);
                            await writer.WriteLineAsync($"            foreach (var item in {ObjectName})").ConfigureAwait(false);
                            await writer.WriteLineAsync("            {").ConfigureAwait(false);
                            await writer.WriteLineAsync($"                yield return ({((GenericTypeName)genericIEnumerable.ClassName).GenericTypeArguments[0]})item;").ConfigureAwait(false);
                            await writer.WriteLineAsync("            }").ConfigureAwait(false);
                            await writer.WriteLineAsync("        }").ConfigureAwait(false);
                        }
                        else if (method.Name == "Equals" && method.OverrideObject)
                        {
                            if (builder.DocumentationGeneration is DocumentationGeneration.GenerateWithoutInheritDoc && !method.Documentation.Any())
                            {
                                await writer.WriteLineAsync(@"        /// <summary>").ConfigureAwait(false);
                                await writer.WriteLineAsync(@"        /// Determines whether the specified object is equal to the current object.").ConfigureAwait(false);
                                await writer.WriteLineAsync(@"        /// </summary>").ConfigureAwait(false);
                                await writer.WriteLineAsync(@"        /// <param name=""obj"">The object to compare with the current object.</param>").ConfigureAwait(false);
                                await writer.WriteLineAsync(@"        /// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>").ConfigureAwait(false);
                            }
                            var variableName = method.Parameters[0].Name != "o" ? "o" : "obj";
                            await writer.WriteLineAsync($"        public override bool Equals({method.Parameters[0].GetAsClassParameter()}) => {ObjectName}.Equals({method.Parameters[0].Name} is {ClassName} {(builder.MinCSharpVersion >= 7M ? $"{variableName} ? {variableName}" : $"? (({ClassName}){method.Parameters[0].Name})")}.{ObjectName} : {method.Parameters[0].Name});").ConfigureAwait(false);
                        }
                        else if (method.Name == "CompareTo" && method.DeclaringInterfaceType?.Type == typeof(IComparable))
                        {
                            var variableName = method.Parameters[0].Name != "o" ? "o" : "obj";
                            await writer.WriteLineAsync($"        public int CompareTo({method.Parameters[0].GetAsClassParameter()}) => {ObjectName}.CompareTo({method.Parameters[0].Name} is {ClassName} {(builder.MinCSharpVersion >= 7M ? $"{variableName} ? {variableName}" : $"? (({ClassName}){method.Parameters[0].Name})")}.{ObjectName} : {method.Parameters[0].Name});").ConfigureAwait(false);
                        }
                        else
                        {
                            var accessorName = method.IsStatic ? typeFullName : ObjectName;
                            if (builder.DocumentationGeneration is DocumentationGeneration.GenerateWithoutInheritDoc && method.OverrideObject && !method.Documentation.Any())
                            {
                                if (method.Name == "GetHashCode")
                                {
                                    await writer.WriteLineAsync(@"        /// <summary>").ConfigureAwait(false);
                                    await writer.WriteLineAsync(@"        /// Serves as the default hash function.").ConfigureAwait(false);
                                    await writer.WriteLineAsync(@"        /// </summary>").ConfigureAwait(false);
                                    await writer.WriteLineAsync(@"        /// <returns>A hash code for the current object.</returns>").ConfigureAwait(false);
                                }
                                else if (method.Name == "ToString")
                                {
                                    await writer.WriteLineAsync(@"        /// <summary>").ConfigureAwait(false);
                                    await writer.WriteLineAsync(@"        /// Returns a string that represents the current object.").ConfigureAwait(false);
                                    await writer.WriteLineAsync(@"        /// </summary>").ConfigureAwait(false);
                                    await writer.WriteLineAsync(@"        /// <returns>A string that represents the current object.</returns>").ConfigureAwait(false);
                                }
                            }
                            await writer.WriteLineAsync($"        public {(method.OverrideObject ? "override " : BaseType?.HasMethod(m => m.Equals(method, checkReturnType: false)) == true ? "new " : string.Empty)}{method.ReturnType.ClassName} {method.Name}({string.Join(", ", method.Parameters.Select(p => p.GetAsClassParameter()))}) => {method.ReturnType.GetCodeToConvertFromActualType($"{accessorName}.{method.Name}({string.Join(", ", method.Parameters.Select(p => p.GetCodeToConvertToActualType()))})")};").ConfigureAwait(false);
                        }
                        if (hasExplicitInterfaceImplementation)
                        {
                            if (isGenericGetEnumerator)
                            {
                                await writer.WriteLineAsync().ConfigureAwait(false);
                                await writer.WriteLineAsync($"        {method.ReturnType.InterfaceName} {method.DeclaringInterfaceType?.InterfaceName ?? InterfaceName}.GetEnumerator() => GetEnumerator();").ConfigureAwait(false);
                            }
                            else
                            {
                                await writer.WriteLineAsync().ConfigureAwait(false);
                                if (method.ObsoleteMessage != null)
                                {
                                    await writer.WriteLineAsync("        [Obsolete]").ConfigureAwait(false);
                                }
                                await writer.WriteLineAsync($"        {method.ReturnType.InterfaceName} {method.DeclaringInterfaceType?.InterfaceName ?? InterfaceName}.{method.Name}({string.Join(", ", method.Parameters.Select(p => p.GetAsInterfaceParameter()))}) => {method.Name}({string.Join(", ", method.Parameters.Select(p => p.GetCodeToConvertToClassType()))});").ConfigureAwait(false);
                            }
                        }
                        if (wrapInCompilerFlag)
                        {
                            await writer.WriteLineAsync("#endif").ConfigureAwait(false);
                        }
                    }
                }

                foreach (var @interface in Interfaces)
                {
                    var firstToImplement = BaseType?.HasInterface(i => i.Equals(@interface)) != true;
                    if (firstToImplement || (genericIEnumerable != null && (@interface.Type == typeof(IEnumerable) || @interface.Type == typeof(IList))))
                    {
                        foreach (var method in @interface.Methods)
                        {
                            if (!HasMethod(m => m.Equals(method)))
                            {
                                Type genericTypeDefinition;
                                if (@interface.Type == typeof(IList))
                                {
                                    if (firstToImplement || (genericArg?.BuildStatus ?? TypeBuildStatus.NotBuilding) != TypeBuildStatus.NotBuilding)
                                    {
                                        var argCode = genericArg != null ? $"value is {genericArg.ClassName} {(builder.MinCSharpVersion >= 7M ? $"o ? o" : $"? (({genericArg.ClassName})value)")}.{ObjectName} : value" : "value";
                                        if (method.Name == "Add")
                                        {
                                            await writer.WriteLineAsync().ConfigureAwait(false);
                                            await writer.WriteLineAsync($"        int IList.Add(object value) => ((IList){ObjectName}).Add({argCode});").ConfigureAwait(false);
                                        }
                                        else if (method.Name == "Contains")
                                        {
                                            await writer.WriteLineAsync().ConfigureAwait(false);
                                            await writer.WriteLineAsync($"        bool IList.Contains(object value) => ((IList){ObjectName}).Contains({argCode});").ConfigureAwait(false);
                                        }
                                        else if (method.Name == "IndexOf")
                                        {
                                            await writer.WriteLineAsync().ConfigureAwait(false);
                                            await writer.WriteLineAsync($"        int IList.IndexOf(object value) => ((IList){ObjectName}).IndexOf({argCode});").ConfigureAwait(false);
                                        }
                                        else if (method.Name == "Insert")
                                        {
                                            await writer.WriteLineAsync().ConfigureAwait(false);
                                            await writer.WriteLineAsync($"        void IList.Insert(int index, object value) => ((IList){ObjectName}).Insert(index, {argCode});").ConfigureAwait(false);
                                        }
                                        else if (method.Name == "Remove")
                                        {
                                            await writer.WriteLineAsync().ConfigureAwait(false);
                                            await writer.WriteLineAsync($"        void IList.Remove(object value) => ((IList){ObjectName}).Remove({argCode});").ConfigureAwait(false);
                                        }
                                        else if (firstToImplement)
                                        {
                                            await writer.WriteLineAsync().ConfigureAwait(false);
                                            await writer.WriteLineAsync($"        {method.ReturnType.InterfaceName} {@interface.InterfaceName}.{method.Name}({string.Join(", ", method.Parameters.Select(p => p.GetAsInterfaceParameter()))}) => (({@interface.InterfaceName}){ObjectName}).{method.Name}({string.Join(", ", method.Parameters.Select(p => p.GetCodeToConvertToClassType()))});").ConfigureAwait(false);
                                        }
                                    }
                                }
                                else if (@interface.Type == typeof(ICollection))
                                {
                                    await writer.WriteLineAsync().ConfigureAwait(false);
                                    await writer.WriteLineAsync($"        void ICollection.CopyTo({(classUsingDirectives.Contains("System") ? string.Empty : "System.")}Array array, int index)").ConfigureAwait(false);
                                    await writer.WriteLineAsync("        {").ConfigureAwait(false);
                                    await writer.WriteLineAsync("            if ((uint)index + Count > array.Length)").ConfigureAwait(false);
                                    await writer.WriteLineAsync("            {").ConfigureAwait(false);
                                    await writer.WriteLineAsync($@"                throw new {(classUsingDirectives.Contains("System") ? string.Empty : "System.")}ArgumentOutOfRangeException(""index + Count must be less than or equal to array.Length"");").ConfigureAwait(false);
                                    await writer.WriteLineAsync("            }").ConfigureAwait(false);
                                    await writer.WriteLineAsync().ConfigureAwait(false);
                                    await writer.WriteLineAsync("            foreach (var item in this)").ConfigureAwait(false);
                                    await writer.WriteLineAsync("            {").ConfigureAwait(false);
                                    await writer.WriteLineAsync("                array.SetValue(item, index++);").ConfigureAwait(false);
                                    await writer.WriteLineAsync("            }").ConfigureAwait(false);
                                    await writer.WriteLineAsync("        }").ConfigureAwait(false);
                                }
                                else if (genericIEnumerable != null && method.Name == "GetEnumerator")
                                {
                                    await writer.WriteLineAsync().ConfigureAwait(false);
                                    await writer.WriteLineAsync($"        {method.ReturnType.InterfaceName} {@interface.InterfaceName}.GetEnumerator() => (({genericIEnumerable.InterfaceName})this).GetEnumerator();").ConfigureAwait(false);
                                }
                                else if (method.Name == "CompareTo" && @interface.Type == typeof(IComparable))
                                {
                                    await writer.WriteLineAsync().ConfigureAwait(false);
                                    await writer.WriteLineAsync($"        int IComparable.CompareTo(object obj) => ((IComparable){ObjectName}).CompareTo(obj is {ClassName} {(builder.MinCSharpVersion >= 7M ? $"o ? o" : $"? (({ClassName})obj)")}.{ObjectName} : obj);").ConfigureAwait(false);
                                }
                                else if (@interface.Type.IsGenericType && ((genericTypeDefinition = @interface.Type.GetGenericTypeDefinition()) == typeof(ICollection<>) || genericTypeDefinition == typeof(IList<>) || genericTypeDefinition == typeof(IReadOnlyCollection<>) || genericTypeDefinition == typeof(IReadOnlyList<>)))
                                {
                                    if (method.Name == "Add")
                                    {
                                        await writer.WriteLineAsync().ConfigureAwait(false);
                                        await writer.WriteLineAsync($"        void {@interface.InterfaceName}.Add({method.Parameters[0].GetAsInterfaceParameter()}) => ((IList){ObjectName}).Add(({method.Parameters[0].GetCodeToConvertToClassType()}).{ObjectName});").ConfigureAwait(false);
                                    }
                                    else if (method.Name == "Clear")
                                    {
                                        await writer.WriteLineAsync().ConfigureAwait(false);
                                        await writer.WriteLineAsync($"        void {@interface.InterfaceName}.Clear() => ((IList){ObjectName}).Clear();").ConfigureAwait(false);
                                    }
                                    else if (method.Name == "Contains")
                                    {
                                        await writer.WriteLineAsync().ConfigureAwait(false);
                                        await writer.WriteLineAsync($"        bool {@interface.InterfaceName}.Contains({method.Parameters[0].GetAsInterfaceParameter()}) => ((IList){ObjectName}).Contains(({method.Parameters[0].GetCodeToConvertToClassType()}).{ObjectName});").ConfigureAwait(false);
                                    }
                                    else if (method.Name == "CopyTo")
                                    {
                                        var arrayInterfaceName = method.Parameters[0].Type.InterfaceName;
                                        var arrayElementType = arrayInterfaceName is GenericTypeName genericTypeName ? genericTypeName.GenericTypeArguments[0] : ((ArrayTypeName)arrayInterfaceName).ElementTypeName;
                                        await writer.WriteLineAsync().ConfigureAwait(false);
                                        await writer.WriteLineAsync($"        void {@interface.InterfaceName}.CopyTo({arrayElementType}[] array, int arrayIndex)").ConfigureAwait(false);
                                        await writer.WriteLineAsync("        {").ConfigureAwait(false);
                                        await writer.WriteLineAsync("            if ((uint)arrayIndex + Count > array.Length)").ConfigureAwait(false);
                                        await writer.WriteLineAsync("            {").ConfigureAwait(false);
                                        await writer.WriteLineAsync($@"                throw new {(classUsingDirectives.Contains("System") ? string.Empty : "System.")}ArgumentOutOfRangeException(""arrayIndex + Count must be less than or equal to array.Length"");").ConfigureAwait(false);
                                        await writer.WriteLineAsync("            }").ConfigureAwait(false);
                                        await writer.WriteLineAsync().ConfigureAwait(false);
                                        await writer.WriteLineAsync("            foreach (var item in this)").ConfigureAwait(false);
                                        await writer.WriteLineAsync("            {").ConfigureAwait(false);
                                        await writer.WriteLineAsync("                array[arrayIndex++] = item;").ConfigureAwait(false);
                                        await writer.WriteLineAsync("            }").ConfigureAwait(false);
                                        await writer.WriteLineAsync("        }").ConfigureAwait(false);
                                    }
                                    else if (method.Name == "Remove")
                                    {
                                        var item = method.Parameters[0];
                                        var codeToConvertToActual = item.Type.InterfaceName == item.Type.ClassName ? item.Name : $"({item.GetCodeToConvertToClassType()}).{ObjectName}";
                                        await writer.WriteLineAsync().ConfigureAwait(false);
                                        await writer.WriteLineAsync($"        bool {@interface.InterfaceName}.Remove({item.GetAsInterfaceParameter()})").ConfigureAwait(false);
                                        await writer.WriteLineAsync("        {").ConfigureAwait(false);
                                        var logic = $"((IList){ObjectName}).Remove({codeToConvertToActual})";
                                        if (HasMethod(m => m.Name == "Remove" && m.ReturnType.Type == typeof(void) && m.Parameters.Count == 1 && m.Parameters[0].Type == method.Parameters[0].Type))
                                        {
                                            logic = $"Remove({item.GetCodeToConvertToClassType()})";
                                        }
                                        await writer.WriteLineAsync($"            {logic};").ConfigureAwait(false);
                                        await writer.WriteLineAsync("            return true;").ConfigureAwait(false);
                                        await writer.WriteLineAsync("        }").ConfigureAwait(false);
                                    }
                                    else if (method.Name == "IndexOf")
                                    {
                                        await writer.WriteLineAsync().ConfigureAwait(false);
                                        await writer.WriteLineAsync($"        int {@interface.InterfaceName}.IndexOf({method.Parameters[0].GetAsInterfaceParameter()}) => ((IList){ObjectName}).IndexOf(({method.Parameters[0].GetCodeToConvertToClassType()}).{ObjectName});").ConfigureAwait(false);
                                    }
                                    else if (method.Name == "Insert")
                                    {
                                        await writer.WriteLineAsync().ConfigureAwait(false);
                                        await writer.WriteLineAsync($"        void {@interface.InterfaceName}.Insert(int index, {method.Parameters[1].GetAsInterfaceParameter()}) => ((IList){ObjectName}).Insert(index, ({method.Parameters[1].GetCodeToConvertToClassType()}).{ObjectName});").ConfigureAwait(false);
                                    }
                                    else if (method.Name == "RemoveAt")
                                    {
                                        await writer.WriteLineAsync().ConfigureAwait(false);
                                        await writer.WriteLineAsync($"        void {@interface.InterfaceName}.RemoveAt(int index) => ((IList){ObjectName}).RemoveAt(index);").ConfigureAwait(false);
                                    }
                                    else
                                    {
                                        await writer.WriteLineAsync().ConfigureAwait(false);
                                        await writer.WriteLineAsync($"        {method.ReturnType.InterfaceName} {@interface.InterfaceName}.{method.Name}({string.Join(", ", method.Parameters.Select(p => p.GetAsInterfaceParameter()))}) => (({@interface.InterfaceName}){ObjectName}).{method.Name}({string.Join(", ", method.Parameters.Select(p => p.GetCodeToConvertToClassType()))});").ConfigureAwait(false);
                                    }
                                }
                                else
                                {
                                    await writer.WriteLineAsync().ConfigureAwait(false);
                                    await writer.WriteLineAsync($"        {method.ReturnType.InterfaceName} {@interface.InterfaceName}.{method.Name}({string.Join(", ", method.Parameters.Select(p => p.GetAsInterfaceParameter()))}) => (({@interface.InterfaceName}){ObjectName}).{method.Name}({string.Join(", ", method.Parameters.Select(p => p.GetCodeToConvertToClassType()))});").ConfigureAwait(false);
                                }
                            }
                        }
                    }
                }

                await writer.WriteLineAsync("    }").ConfigureAwait(false);
                await writer.WriteAsync("}").ConfigureAwait(false);
                await writer.FlushAsync().ConfigureAwait(false);
            }
        }
    }
}