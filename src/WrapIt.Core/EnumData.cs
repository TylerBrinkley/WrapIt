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

        public override async Task BuildAsync(WrapperBuilder builder, HashSet<TypeData> typeDatas, Func<Type, string, CancellationToken, Task<TextWriter>> writerProvider, CancellationToken cancellationToken = default)
        {
            BuildStatus = TypeBuildStatus.Building;
            using (var writer = await writerProvider(Type, ClassName.FullName, cancellationToken).ConfigureAwait(false))
            {
                var underlyingType = Enum.GetUnderlyingType(Type);
                var typeFullName = Type.FullName;
                await writer.WriteLineAsync(@$"namespace {ClassName.Namespace}
{{
    public enum {ClassName.Name}{(underlyingType != typeof(int) ? $" : {builder.GetTypeData(underlyingType, typeDatas).ClassName}" : string.Empty)}
    {{
{string.Join($",{Environment.NewLine}", Enum.GetNames(Type).Select(n => $"        {n} = {typeFullName}.{n}"))}
    }}
}}").ConfigureAwait(false);
                await writer.FlushAsync().ConfigureAwait(false);
            }
            BuildStatus = TypeBuildStatus.Built;
        }

        public override string GetCodeToConvertToActualType(string parameterName) => $"({Type.FullName}){parameterName}";
    }
}