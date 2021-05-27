using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Company;
using NUnit.Framework;

namespace WrapIt.Tests
{
    public class GenerationTests
    {
        private readonly Dictionary<string, MemoryStream> _files = new Dictionary<string, MemoryStream>();

        [Test]
        public async Task Generation()
        {
            var builder = new WrapperBuilder();
            builder.RootTypes.Add(typeof(Derived));
            builder.PropertyResolver += (type, propertyInfo) =>
                type == typeof(Derived) && propertyInfo.Name == nameof(Derived.CachedProperty)
                ? MemberGeneration.FullWithSafeCaching
                : MemberGeneration.Full;
            await builder.BuildAsync(GetWriter);

            foreach (var directory in new DirectoryInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)).Parent.Parent.Parent.Parent.EnumerateDirectories("WrapIt.Generated").First().EnumerateDirectories("Generated").First().EnumerateDirectories())
            {
                foreach (var file in directory.EnumerateFiles())
                {
                    var classFullName = $"{directory.Name}.{Path.GetFileNameWithoutExtension(file.Name)}";
                    var stream = _files[classFullName];
                    stream.Position = 0;
                    var code = new StreamReader(stream).ReadToEnd();
                    using (var sr = file.OpenText())
                    {
                        Assert.AreEqual(sr.ReadToEnd(), code, classFullName);
                    }
                }
            }
        }

        private Task<TextWriter> GetWriter(Type type, string classFullName, CancellationToken cancellationToken)
        {
            var stream = new MemoryStream();
            TextWriter writer = new StreamWriter(stream, Encoding.UTF8, 1024, leaveOpen: true);
            _files.Add(classFullName, stream);
            return Task.FromResult(writer);
        }
    }
}