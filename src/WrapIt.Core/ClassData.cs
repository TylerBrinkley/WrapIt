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

        public ClassData? BaseType { get; }

        public List<ClassData> DirectDerivedTypes { get; } = new List<ClassData>();

        public TypeGeneration TypeGeneration { get; }

        public bool IsValueType { get; }

        public ClassData(Type type, TypeName className, TypeName interfaceName, TypeBuildStatus buildStatus, ClassData? baseType, TypeGeneration typeGeneration)
            : base(type, className, interfaceName, buildStatus)
        {
            BaseType = baseType;
            TypeGeneration = typeGeneration;
            IsValueType = type.IsValueType;
        }

        protected override Type[] GetInterfaces() => TypeGeneration.HasFlag(TypeGeneration.Instance) ? base.GetInterfaces() : Array.Empty<Type>();

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

            foreach (var @interface in Interfaces)
            {
                interfaceUsingDirectives.UnionWith(@interface.InterfaceName.GetNamespaces());
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

            await WriteInterfaceAsync(writerProvider, interfaceUsingDirectives, cancellationToken).ConfigureAwait(false);

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

        private async Task WriteInterfaceAsync(Func<Type, string, CancellationToken, Task<TextWriter>> writerProvider, HashSet<string> interfaceUsingDirectives, CancellationToken cancellationToken)
        {
            using (var writer = await writerProvider(Type, InterfaceName.FullName, cancellationToken).ConfigureAwait(false))
            {
                var interfaceNamespace = InterfaceName.Namespace!;
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

                await writer.WriteLineAsync($"namespace {interfaceNamespace}").ConfigureAwait(false);
                await writer.WriteLineAsync("{").ConfigureAwait(false);
                if (Documentation.Any())
                {
                    await writer.WriteLineAsync(string.Join(writer.NewLine, Documentation.SelectMany(d => d.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.None)).Select(d => $"    /// {(d.StartsWith("            ") ? d.Substring(12) : d)}"))).ConfigureAwait(false);
                }
                if (ObsoleteMessage != null)
                {
                    await writer.WriteLineAsync($"    [Obsolete{(ObsoleteMessage.Length > 0 ? $"(\"{ObsoleteMessage}\")" : string.Empty)}]");
                }
                var interfaces = (BaseType != null ? new[] { BaseType } : Enumerable.Empty<TypeData>()).Concat(Interfaces).ToList();
                await writer.WriteLineAsync($"    public partial interface {InterfaceName}{(interfaces.Count > 0 ? $" : {string.Join(", ", interfaces.Select(i => i.InterfaceName))}" : string.Empty)}").ConfigureAwait(false);
                await writer.WriteLineAsync("    {").ConfigureAwait(false);

                var addLine = false;
                foreach (var property in Properties)
                {
                    if (property.DeclaringInterfaceType == null)
                    {
                        addLine = true;
                        if (property.Documentation.Any())
                        {
                            await writer.WriteLineAsync(string.Join(writer.NewLine, property.Documentation.SelectMany(d => d.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.None)).Select(d => $"        /// {(d.StartsWith("            ") ? d.Substring(12) : d)}"))).ConfigureAwait(false);
                        }
                        if (property.ObsoleteMessage != null)
                        {
                            await writer.WriteLineAsync($"        [Obsolete{(property.ObsoleteMessage.Length > 0 ? $"(\"{property.ObsoleteMessage}\")" : string.Empty)}]");
                        }
                        if (property.Parameters.Count > 0)
                        {
                            await writer.WriteLineAsync($"        {property.Type.InterfaceName} this[{string.Join(", ", property.Parameters.Select(p => p.GetAsInterfaceParameter()))}] {{ {(property.HasGetter ? "get; " : string.Empty)}{(property.HasSetter ? "set; " : string.Empty)}}}").ConfigureAwait(false);
                        }
                        else
                        {
                            await writer.WriteLineAsync($"        {property.Type.InterfaceName} {property.Name} {{ {(property.HasGetter ? "get; " : string.Empty)}{(property.HasSetter ? "set; " : string.Empty)}}}").ConfigureAwait(false);
                        }
                    }
                }

                var first = true;
                if (Events.Count > 0)
                {
                    foreach (var @event in Events)
                    {
                        if (@event.DeclaringInterfaceType == null)
                        {
                            if (first && addLine)
                            {
                                await writer.WriteLineAsync().ConfigureAwait(false);
                            }
                            first = false;
                            addLine = true;
                            if (@event.Documentation.Any())
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
                        if (method.DeclaringInterfaceType == null)
                        {
                            if (first && addLine)
                            {
                                await writer.WriteLineAsync().ConfigureAwait(false);
                            }
                            first = false;
                            if (method.Documentation.Any())
                            {
                                await writer.WriteLineAsync(string.Join(writer.NewLine, method.Documentation.SelectMany(d => d.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.None)).Select(d => $"        /// {(d.StartsWith("            ") ? d.Substring(12) : d)}"))).ConfigureAwait(false);
                            }
                            if (method.ObsoleteMessage != null)
                            {
                                await writer.WriteLineAsync($"        [Obsolete{(method.ObsoleteMessage.Length > 0 ? $"(\"{method.ObsoleteMessage}\")" : string.Empty)}]");
                            }
                            await writer.WriteLineAsync($"        {method.ReturnType.InterfaceName} {method.Name}({string.Join(", ", method.Parameters.Select(p => p.GetAsInterfaceParameter()))});").ConfigureAwait(false);
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
                if (Documentation.Any())
                {
                    await writer.WriteLineAsync(string.Join(writer.NewLine, Documentation.SelectMany(d => d.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.None)).Select(d => $"    /// {(d.StartsWith("            ") ? d.Substring(12) : d)}"))).ConfigureAwait(false);
                }
                if (ObsoleteMessage != null)
                {
                    await writer.WriteLineAsync($"    [Obsolete{(ObsoleteMessage.Length > 0 ? $"(\"{ObsoleteMessage}\")" : string.Empty)}]");
                }
                await writer.WriteLineAsync($"    public {(Type.IsSealed ? "sealed " : string.Empty)}partial class {ClassName} : {(BaseType is null ? string.Empty : $"{BaseType.ClassName}, ")}{InterfaceName}").ConfigureAwait(false);
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
                        if (builder.MinCSharpVersion >= 8M)
                        {
                            await writer.WriteLineAsync($"        public static implicit operator {ClassName}({typeFullName} {ObjectParamName}) => {ObjectParamName} switch").ConfigureAwait(false);
                            await writer.WriteLineAsync("        {").ConfigureAwait(false);
                            await writer.WriteLineAsync($"            null => null,").ConfigureAwait(false);
                            for (var i = 0; i < DirectDerivedTypes.Count; ++i)
                            {
                                var derivedType = DirectDerivedTypes[i];
                                await writer.WriteLineAsync($"            {derivedType.Type.FullName} v{i} => ({derivedType.ClassName})v{i},").ConfigureAwait(false);
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
                            for (var i = 0; i < DirectDerivedTypes.Count; ++i)
                            {
                                var derivedType = DirectDerivedTypes[i];
                                await writer.WriteLineAsync($"                case {derivedType.Type.FullName} v{i}:").ConfigureAwait(false);
                                await writer.WriteLineAsync($"                    return ({derivedType.ClassName})v{i};").ConfigureAwait(false);
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
                            foreach (var derivedType in DirectDerivedTypes)
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

                foreach (var property in Properties)
                {
                    if (property.Generation == MemberGeneration.WrapEventHandlerInCompilerFlag)
                    {
                        throw new NotSupportedException("WrapEventHandlerInCompilerFlag is not supported for properties.");
                    }
                    var wrapInCompilerFlag = property.Generation == MemberGeneration.WrapImplementationInCompilerFlag;
                    if (wrapInCompilerFlag || property.Generation == MemberGeneration.Full || property.Generation == MemberGeneration.FullWithSafeCaching)
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
                            var hasExplicitInterfaceImplementation = property.Type.InterfaceName != property.Type.ClassName || property.Parameters.Any(p => p.Type.ClassName != p.Type.InterfaceName);
                            if (property.Documentation.Any())
                            {
                                if (builder.DocumentationGeneration == DocumentationGeneration.GenerateWithInheritDoc && !hasExplicitInterfaceImplementation)
                                {
                                    await writer.WriteLineAsync("        /// <inheritdoc/>").ConfigureAwait(false);
                                }
                                else
                                {
                                    await writer.WriteLineAsync(string.Join(writer.NewLine, property.Documentation.SelectMany(d => d.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.None)).Select(d => $"        /// {(d.StartsWith("            ") ? d.Substring(12) : d)}"))).ConfigureAwait(false);
                                }
                            }
                            if (property.ObsoleteMessage != null)
                            {
                                await writer.WriteLineAsync($"        [Obsolete{(property.ObsoleteMessage.Length > 0 ? $"(\"{property.ObsoleteMessage}\")" : string.Empty)}]");
                            }
                            var basicArgumentList = string.Join(", ", property.Parameters.Select(p => p.GetAsArgument()));
                            await writer.WriteAsync($"        public {property.Type.ClassName} this[{string.Join(", ", property.Parameters.Select(p => p.GetAsClassParameter()))}] ").ConfigureAwait(false);
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
                            var hasExplicitInterfaceImplementation = property.Type.InterfaceName != property.Type.ClassName;
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
                                if (property.Documentation.Any())
                                {
                                    if (builder.DocumentationGeneration == DocumentationGeneration.GenerateWithInheritDoc && !hasExplicitInterfaceImplementation)
                                    {
                                        await writer.WriteLineAsync("        /// <inheritdoc/>").ConfigureAwait(false);
                                    }
                                    else
                                    {
                                        await writer.WriteLineAsync(string.Join(writer.NewLine, property.Documentation.SelectMany(d => d.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.None)).Select(d => $"        /// {(d.StartsWith("            ") ? d.Substring(12) : d)}"))).ConfigureAwait(false);
                                    }
                                }
                                if (property.ObsoleteMessage != null)
                                {
                                    await writer.WriteLineAsync($"        [Obsolete{(property.ObsoleteMessage.Length > 0 ? $"(\"{property.ObsoleteMessage}\")" : string.Empty)}]");
                                }
                                await writer.WriteLineAsync($"        public {property.Type.ClassName} {property.Name} {{ get {{ var {variableName} = {fieldName}; var {ObjectParamName} = {accessorName}.{property.Name}; return ReferenceEquals({variableName}?.{ObjectName}, {ObjectParamName}) ? {variableName} : {fieldName} = {property.Type.GetCodeToConvertFromActualType(ObjectParamName)}; }} {(property.HasSetter ? $"set => {accessorName}.{property.Name} = {property.Type.GetCodeToConvertToActualType("value")}; " : string.Empty)}}}").ConfigureAwait(false);
                            }
                            else
                            {
                                if (property.Documentation.Any())
                                {
                                    if (builder.DocumentationGeneration == DocumentationGeneration.GenerateWithInheritDoc && !hasExplicitInterfaceImplementation)
                                    {
                                        await writer.WriteLineAsync("        /// <inheritdoc/>").ConfigureAwait(false);
                                    }
                                    else
                                    {
                                        await writer.WriteLineAsync(string.Join(writer.NewLine, property.Documentation.SelectMany(d => d.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.None)).Select(d => $"        /// {(d.StartsWith("            ") ? d.Substring(12) : d)}"))).ConfigureAwait(false);
                                    }
                                }
                                if (property.ObsoleteMessage != null)
                                {
                                    await writer.WriteLineAsync($"        [Obsolete{(property.ObsoleteMessage.Length > 0 ? $"(\"{property.ObsoleteMessage}\")" : string.Empty)}]");
                                }
                                await writer.WriteAsync($"        public {property.Type.ClassName} {property.Name} ").ConfigureAwait(false);
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
                    if (BaseType?.HasInterface(@interface) != true)
                    {
                        foreach (var property in @interface.Properties)
                        {
                            if (!Properties.Any(p => p.Equals(property)))
                            {
                                if (property.Parameters.Count > 0)
                                {
                                    var advancedArgumentList = string.Join(", ", property.Parameters.Select(p => p.Type.GetCodeToConvertToClassType(p.Name)));
                                    await writer.WriteLineAsync($"        {property.Type.InterfaceName} {@interface.InterfaceName}.this[{string.Join(", ", property.Parameters.Select(p => p.GetAsInterfaceParameter()))}] {(property.HasGetter && !property.HasSetter ? $"=> (({@interface.InterfaceName}){ObjectName})[{advancedArgumentList}];" : $"{{ {(property.HasGetter ? $"get {(builder.MinCSharpVersion >= 7M ? "=>" : "{ return")} (({@interface.InterfaceName}){ObjectName})[{advancedArgumentList}];{(builder.MinCSharpVersion >= 7M ? string.Empty : " }")} " : string.Empty)}{(property.HasSetter ? $"set {(builder.MinCSharpVersion >= 7M ? "=>" : "{")} (({@interface.InterfaceName}){ObjectName})[{advancedArgumentList}] = {property.Type.GetCodeToConvertToClassType("value")};{(builder.MinCSharpVersion >= 7M ? string.Empty : " }")} " : string.Empty)}}}")}").ConfigureAwait(false);
                                }
                                else
                                {
                                    await writer.WriteLineAsync($"        {property.Type.InterfaceName} {@interface.InterfaceName}.{property.Name} {(property.HasGetter && !property.HasSetter ? $"=> (({@interface.InterfaceName}){ObjectName}).{property.Name};" : $"{{ {(property.HasGetter ? $"get {(builder.MinCSharpVersion >= 7M ? "=>" : "{ return")} (({@interface.InterfaceName}){ObjectName}).{property.Name};{(builder.MinCSharpVersion >= 7M ? string.Empty : " }")} " : string.Empty)}{(property.HasSetter ? $"set {(builder.MinCSharpVersion >= 7M ? "=>" : "{")} (({@interface.InterfaceName}){ObjectName}).{property.Name} = {property.Type.GetCodeToConvertToClassType("value")};{(builder.MinCSharpVersion >= 7M ? string.Empty : " }")} " : string.Empty)}}}")}").ConfigureAwait(false);
                                }
                                await writer.WriteLineAsync().ConfigureAwait(false);
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
                    if (wrapInCompilerFlag || @event.Generation == MemberGeneration.Full || @event.Generation == MemberGeneration.WrapEventHandlerInCompilerFlag)
                    {
                        var accessorName = @event.IsStatic ? typeFullName : ObjectName;
                        if (wrapInCompilerFlag)
                        {
                            await writer.WriteLineAsync($"#if {builder.DefaultMemberGenerationCompilerFlag}").ConfigureAwait(false);
                        }
                        if (@event.Documentation.Any())
                        {
                            if (builder.DocumentationGeneration == DocumentationGeneration.GenerateWithInheritDoc)
                            {
                                await writer.WriteLineAsync("        /// <inheritdoc/>").ConfigureAwait(false);
                            }
                            else
                            {
                                await writer.WriteLineAsync(string.Join(writer.NewLine, @event.Documentation.SelectMany(d => d.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.None)).Select(d => $"        /// {(d.StartsWith("            ") ? d.Substring(12) : d)}"))).ConfigureAwait(false);
                            }
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

                if (IsValueType || !TypeGeneration.HasFlag(TypeGeneration.Instance))
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
                    if (IsValueType)
                    {
                        await writer.WriteLineAsync().ConfigureAwait(false);
                    }
                }
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
                    if (wrapInCompilerFlag || method.Generation == MemberGeneration.Full)
                    {
                        await writer.WriteLineAsync().ConfigureAwait(false);
                        if (wrapInCompilerFlag)
                        {
                            await writer.WriteLineAsync($"#if {builder.DefaultMemberGenerationCompilerFlag}").ConfigureAwait(false);
                        }
                        var hasExplicitInterfaceImplementation = method.ReturnType.ClassName != method.ReturnType.InterfaceName || method.Parameters.Any(p => p.Type.ClassName != p.Type.InterfaceName);
                        if (method.Documentation.Any())
                        {
                            if (builder.DocumentationGeneration == DocumentationGeneration.GenerateWithInheritDoc && !hasExplicitInterfaceImplementation)
                            {
                                await writer.WriteLineAsync("        /// <inheritdoc/>").ConfigureAwait(false);
                            }
                            else
                            {
                                await writer.WriteLineAsync(string.Join(writer.NewLine, method.Documentation.SelectMany(d => d.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.None)).Select(d => $"        /// {(d.StartsWith("            ") ? d.Substring(12) : d)}"))).ConfigureAwait(false);
                            }
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
                            await writer.WriteLineAsync("                yield return item;").ConfigureAwait(false);
                            await writer.WriteLineAsync("            }").ConfigureAwait(false);
                            await writer.WriteLineAsync("        }").ConfigureAwait(false);
                        }
                        else if (method.Name == "Equals" && method.OverrideObject)
                        {
                            if (builder.DocumentationGeneration != DocumentationGeneration.None && !method.Documentation.Any())
                            {
                                if (builder.DocumentationGeneration == DocumentationGeneration.GenerateWithInheritDoc)
                                {
                                    await writer.WriteLineAsync("        /// <inheritdoc/>").ConfigureAwait(false);
                                }
                                else
                                {
                                    await writer.WriteLineAsync(@"        /// <summary>").ConfigureAwait(false);
                                    await writer.WriteLineAsync(@"        /// Determines whether the specified object is equal to the current object.").ConfigureAwait(false);
                                    await writer.WriteLineAsync(@"        /// </summary>").ConfigureAwait(false);
                                    await writer.WriteLineAsync(@"        /// <param name=""obj"">The object to compare with the current object.</param>").ConfigureAwait(false);
                                    await writer.WriteLineAsync(@"        /// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>").ConfigureAwait(false);
                                }
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
                            if (builder.DocumentationGeneration != DocumentationGeneration.None && method.OverrideObject && !method.Documentation.Any())
                            {
                                if (builder.DocumentationGeneration == DocumentationGeneration.GenerateWithInheritDoc)
                                {
                                    await writer.WriteLineAsync("        /// <inheritdoc/>").ConfigureAwait(false);
                                }
                                else
                                {
                                    await writer.WriteLineAsync(@"        /// <summary>").ConfigureAwait(false);
                                    await writer.WriteLineAsync(@"        /// Serves as the default hash function.").ConfigureAwait(false);
                                    await writer.WriteLineAsync(@"        /// </summary>").ConfigureAwait(false);
                                    await writer.WriteLineAsync(@"        /// <returns>A hash code for the current object.</returns>").ConfigureAwait(false);
                                }
                            }
                            await writer.WriteLineAsync($"        public {(method.OverrideObject ? "override " : string.Empty)}{method.ReturnType.ClassName} {method.Name}({string.Join(", ", method.Parameters.Select(p => p.GetAsClassParameter()))}) => {method.ReturnType.GetCodeToConvertFromActualType($"{accessorName}.{method.Name}({string.Join(", ", method.Parameters.Select(p => p.GetCodeToConvertToActualType()))})")};").ConfigureAwait(false);
                        }
                        if (hasExplicitInterfaceImplementation)
                        {
                            await writer.WriteLineAsync().ConfigureAwait(false);
                            if (isGenericGetEnumerator)
                            {
                                await writer.WriteLineAsync($"        {method.ReturnType.InterfaceName} {method.DeclaringInterfaceType?.InterfaceName ?? InterfaceName}.GetEnumerator() => GetEnumerator();").ConfigureAwait(false);
                            }
                            else
                            {
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

                var genericIEnumerable = Interfaces.FirstOrDefault(i => i.Type.IsGenericType && i.Type.GetGenericTypeDefinition() == typeof(IEnumerable<>));
                foreach (var @interface in Interfaces)
                {
                    if (BaseType?.HasInterface(@interface) != true || (genericIEnumerable != null && @interface.Type == typeof(IEnumerable)))
                    {
                        foreach (var method in @interface.Methods)
                        {
                            if (!Methods.Any(m => m.Equals(method)))
                            {
                                await writer.WriteLineAsync().ConfigureAwait(false);
                                if (genericIEnumerable != null && method.Name == "GetEnumerator")
                                {
                                    if (method.ReturnType.Type.IsGenericType)
                                    {
                                        var hasExplicitInterfaceImplementation = method.ReturnType.ClassName != method.ReturnType.InterfaceName;
                                        if (builder.DocumentationGeneration != DocumentationGeneration.None)
                                        {
                                            if (builder.DocumentationGeneration == DocumentationGeneration.GenerateWithInheritDoc && !hasExplicitInterfaceImplementation)
                                            {
                                                await writer.WriteLineAsync("        /// <inheritdoc/>").ConfigureAwait(false);
                                            }
                                            else
                                            {
                                                await writer.WriteLineAsync(@"        /// <summary>").ConfigureAwait(false);
                                                await writer.WriteLineAsync(@"        /// Returns an enumerator that iterates through the collection.").ConfigureAwait(false);
                                                await writer.WriteLineAsync(@"        /// </summary>").ConfigureAwait(false);
                                                await writer.WriteLineAsync(@"        /// <returns>An enumerator that can be used to iterate through the collection.</returns>").ConfigureAwait(false);
                                            }
                                        }
                                        await writer.WriteLineAsync($"        public {method.ReturnType.ClassName} GetEnumerator()").ConfigureAwait(false);
                                        await writer.WriteLineAsync("        {").ConfigureAwait(false);
                                        await writer.WriteLineAsync($"            foreach (var item in {ObjectName})").ConfigureAwait(false);
                                        await writer.WriteLineAsync("            {").ConfigureAwait(false);
                                        await writer.WriteLineAsync($"                yield return ({((GenericTypeName)genericIEnumerable.ClassName).GenericTypeArguments[0]})item;").ConfigureAwait(false);
                                        await writer.WriteLineAsync("            }").ConfigureAwait(false);
                                        await writer.WriteLineAsync("        }").ConfigureAwait(false);
                                        if (hasExplicitInterfaceImplementation)
                                        {
                                            await writer.WriteLineAsync().ConfigureAwait(false);
                                            await writer.WriteLineAsync($"        {method.ReturnType.InterfaceName} {@interface.InterfaceName}.GetEnumerator() => GetEnumerator();").ConfigureAwait(false);
                                        }
                                    }
                                    else
                                    {
                                        await writer.WriteLineAsync($"        {method.ReturnType.InterfaceName} {@interface.InterfaceName}.GetEnumerator() => (({genericIEnumerable.InterfaceName})this).GetEnumerator();").ConfigureAwait(false);
                                    }
                                }
                                else if (method.Name == "CompareTo" && @interface.Type == typeof(IComparable))
                                {
                                    await writer.WriteLineAsync($"        int IComparable.CompareTo(object obj) => ((IComparable){ObjectName}).CompareTo(obj is {ClassName} {(builder.MinCSharpVersion >= 7M ? $"o ? o" : $"? (({ClassName})obj)")}.{ObjectName} : obj);").ConfigureAwait(false);
                                }
                                else
                                {
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

        private bool HasInterface(InterfaceData @interface) => Interfaces.Any(i => i.Equals(@interface)) || BaseType?.HasInterface(@interface) == true;

        private IEnumerable<MethodData> GetAllMethods() => Methods.Concat(BaseType?.GetAllMethods() ?? Enumerable.Empty<MethodData>());
    }
}