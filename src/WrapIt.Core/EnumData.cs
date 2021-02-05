using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WrapIt
{
    internal class EnumData : TypeData
    {
        public EnumData(Type type, TypeName name)
            : base(type, name, TypeBuildStatus.NotYetBuilt)
        {
        }

        public override async Task BuildAsync(WrapperBuilder builder, HashSet<TypeData> typeDatas, Func<Type, string, CancellationToken, Task<TextWriter>> writerProvider, DocumentationProvider? documentationProvider, CancellationToken cancellationToken = default)
        {
            BuildStatus = TypeBuildStatus.Building;
            using (var writer = await writerProvider(Type, ClassName.FullName, cancellationToken).ConfigureAwait(false))
            {
                var underlyingType = Enum.GetUnderlyingType(Type);
                var typeFullName = Type.FullName;
                await writer.WriteLineAsync($@"namespace {ClassName.Namespace}").ConfigureAwait(false);
                await writer.WriteLineAsync($@"{{").ConfigureAwait(false);
                if (documentationProvider != null)
                {
                    var documentation = documentationProvider.GetDocumentation(Type);
                    if (documentation.Any())
                    {
                        await writer.WriteLineAsync(string.Join(writer.NewLine, documentation.SelectMany(d => d.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.None)).Select(d => $"    /// {(d.StartsWith("            ") ? d.Substring(12) : d)}"))).ConfigureAwait(false);
                    }
                }
                await writer.WriteLineAsync($@"    public enum {ClassName.Name}{(underlyingType != typeof(int) ? $" : {builder.GetTypeData(underlyingType, typeDatas).ClassName}" : string.Empty)}").ConfigureAwait(false);
                await writer.WriteLineAsync($@"    {{").ConfigureAwait(false);
                var names = Enum.GetNames(Type);
                for (var i = 0; i < names.Length; ++i)
                {
                    var name = names[i];
                    if (documentationProvider != null)
                    {
                        var documentation = documentationProvider.GetDocumentation(Type.GetField(name));
                        if (documentation.Any())
                        {
                            await writer.WriteLineAsync(string.Join(writer.NewLine, documentation.SelectMany(d => d.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.None)).Select(d => $"        /// {(d.StartsWith("            ") ? d.Substring(12) : d)}"))).ConfigureAwait(false);
                        }
                    }
                    await writer.WriteLineAsync($"        {name} = {typeFullName}.{name}{(i < names.Length - 1 ? "," : string.Empty)}").ConfigureAwait(false);
                }
                string.Join($",{writer.NewLine}", Enum.GetNames(Type).Select(n => $"        {n} = {typeFullName}.{n}"));
                await writer.WriteLineAsync($@"    }}").ConfigureAwait(false);
                await writer.WriteLineAsync($@"}}").ConfigureAwait(false);
                await writer.FlushAsync().ConfigureAwait(false);
            }
            BuildStatus = TypeBuildStatus.Built;
        }

        public override string GetCodeToConvertToActualType(string parameterName) => $"({Type.FullName}){parameterName}";
    }
}