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
            var builder = new WrapperBuilder
            {
                ClassFullNameFormat = (ns, nm) => $"Wrappers{ns.Substring("Company".Length)}.{nm}Wrapper",
                DelegateFullNameFormat = (ns, nm) => $"Wrappers{ns.Substring("Company".Length)}.{nm}Wrapper",
                InterfaceFullNameFormat = (ns, nm) => $"Wrappers{ns.Substring("Company".Length)}.I{nm}"
            };
            builder.RootTypes.Add(typeof(Derived));
            builder.PropertyResolver += (type, propertyInfo) =>
                type == typeof(Derived) && propertyInfo.Name == nameof(Derived.CachedProperty)
                ? MemberGeneration.FullWithSafeCaching
                : MemberGeneration.Full;
            await builder.BuildAsync(GetWriter);

            var root = new DirectoryInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)).Parent.Parent.Parent.Parent.EnumerateDirectories("WrapIt.Generated").First();
            var wrappersDirectory = root.EnumerateDirectories("Wrappers").First();
            Validate(wrappersDirectory, root.FullName.Length + 1);
            Assert.AreEqual(0, _files.Count, $"Missing the following wrappers '{string.Join("', '", _files.Keys)}'");
        }

        private void Validate(DirectoryInfo directory, int rootLength)
        {
            foreach (var file in directory.EnumerateFiles())
            {
                var classFullName = $"{directory.FullName.Substring(rootLength).Replace('\\', '.')}.{Path.GetFileNameWithoutExtension(file.Name)}";
                if (!_files.TryGetValue(classFullName, out var stream))
                {
                    Assert.Fail($"Could not find generated '{classFullName}'");
                }
                _files.Remove(classFullName);
                stream.Position = 0;
                var code = new StreamReader(stream).ReadToEnd();
                using (var sr = file.OpenText())
                {
                    Assert.AreEqual(sr.ReadToEnd(), code, classFullName);
                }
            }

            foreach (var subDirectory in directory.EnumerateDirectories())
            {
                Validate(subDirectory, rootLength);
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