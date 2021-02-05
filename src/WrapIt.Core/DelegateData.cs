using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace WrapIt
{
    internal class DelegateData : TypeData
    {
        public TypeData ReturnType { get; }

        public List<ParameterData> Parameters { get; } = new List<ParameterData>();

        public DelegateData(Type type, TypeName className, TypeName interfaceName, TypeBuildStatus buildStatus, WrapperBuilder builder, HashSet<TypeData> typeDatas)
            : base(type, className, interfaceName, buildStatus)
        {
            var invokeMethod = type.GetMethod("Invoke");
            ReturnType = builder.GetTypeData(invokeMethod.ReturnType, typeDatas);
            DependentTypes.UnionWith(ReturnType.GetPublicTypes());
            var parameterInfos = invokeMethod.GetParameters();
            if (parameterInfos?.Length > 0)
            {
                foreach (var parameter in parameterInfos)
                {
                    var parameterType = parameter.ParameterType;
                    var parameterTypeData = builder.GetTypeData(parameterType, typeDatas);
                    DependentTypes.UnionWith(parameterTypeData.GetPublicTypes());
                    Parameters.Add(new ParameterData(parameterTypeData, parameter.Name, parameter.IsOut, parameter.GetCustomAttribute<ParamArrayAttribute>() != null));
                }
            }
        }

        public override async Task BuildAsync(WrapperBuilder builder, HashSet<TypeData> typeDatas, Func<Type, string, CancellationToken, Task<TextWriter>> writerProvider, DocumentationProvider? documentationProvider, CancellationToken cancellationToken = default)
        {
            BuildStatus = TypeBuildStatus.Building;

            var usingDirectives = new HashSet<string>();
            foreach (var dependentType in DependentTypes)
            {
                usingDirectives.UnionWith(dependentType.InterfaceName.GetNamespaces());
            }

            using (var writer = await writerProvider(Type, InterfaceName.FullName, cancellationToken).ConfigureAwait(false))
            {
                var @namespace = InterfaceName.Namespace!;
                var anyUsings = false;
                foreach (var ns in usingDirectives.Where(ns => !@namespace.StartsWith(ns)).OrderBy(ns => ns != "System" && !ns.StartsWith("System.")).ThenBy(ns => ns))
                {
                    await writer.WriteLineAsync($"using {ns};").ConfigureAwait(false);
                    anyUsings = true;
                }

                if (anyUsings)
                {
                    await writer.WriteLineAsync().ConfigureAwait(false);
                }

                await writer.WriteLineAsync($"namespace {@namespace}").ConfigureAwait(false);
                await writer.WriteLineAsync("{").ConfigureAwait(false);
                if (documentationProvider != null)
                {
                    var documentation = documentationProvider.GetDocumentation(Type);
                    if (documentation.Any())
                    {
                        await writer.WriteLineAsync(string.Join(writer.NewLine, documentation.SelectMany(d => d.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.None)).Select(d => $"    /// {(d.StartsWith("            ") ? d.Substring(12) : d)}"))).ConfigureAwait(false);
                    }
                }
                await writer.WriteLineAsync($"    public delegate {ReturnType.InterfaceName} {InterfaceName}({string.Join(", ", Parameters.Select(p => p.GetAsInterfaceParameter()))});").ConfigureAwait(false);
                await writer.WriteAsync("}").ConfigureAwait(false);
                await writer.FlushAsync().ConfigureAwait(false);
            }

            BuildStatus = TypeBuildStatus.Built;
        }

        public override string GetCodeToConvertToClassType(string parameterName) => $"({string.Join(", ", Parameters.Select(p => p.GetAsArgument()))}) => {parameterName}({string.Join(", ", Parameters.Select(p => p.Type.GetCodeToConvertToActualType(p.Name)))})";
    }
}