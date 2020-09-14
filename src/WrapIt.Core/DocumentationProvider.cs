using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace WrapIt
{
    internal sealed class DocumentationProvider
    {
        private readonly Dictionary<Assembly, XDocument> _assemblyDocumentation = new Dictionary<Assembly, XDocument>();

        public IEnumerable<XElement> GetDocumentation(Type type)
        {
            var members = GetMembersElement(type.Assembly);
            var name = $"T:{(type.IsGenericType ? type.GetGenericTypeDefinition() : type).FullName}";
            var member = members.FirstOrDefault(m => m.Attribute("name").Value == name);
            return member?.Elements() ?? Enumerable.Empty<XElement>();
        }

        public IEnumerable<XElement> GetDocumentation(MethodInfo method)
        {
            var methodName = method.Name;
            var type = method.DeclaringType;
            var members = GetMembersElement(type.Assembly);
            var parameters = method.GetParameters();
            var name = $"M:{(type.IsGenericType ? type.GetGenericTypeDefinition() : type).FullName}.{method.Name}{(parameters.Length > 0 ? $"({string.Join(",", parameters.Select(p => p.ParameterType.IsGenericType ? $"{p.ParameterType.FullName.Substring(0, p.ParameterType.FullName.IndexOf('`'))}{{{string.Join(",", p.ParameterType.GetGenericArguments().Select(a => a.FullName))}}}" : (p.ParameterType.IsByRef ? $"{p.ParameterType.GetElementType().FullName}@" : p.ParameterType.FullName)))})" : string.Empty)}";
            var member = members.FirstOrDefault(m => m.Attribute("name").Value == name);
            return member?.Elements() ?? Enumerable.Empty<XElement>();
        }

        public IEnumerable<XElement> GetDocumentation(PropertyInfo property)
        {
            var type = property.DeclaringType;
            var members = GetMembersElement(type.Assembly);
            var parameters = property.GetIndexParameters();
            var name = $"P:{(type.IsGenericType ? type.GetGenericTypeDefinition() : type).FullName}.{property.Name}{(parameters.Length > 0 ? $"({string.Join(",", parameters.Select(p => p.ParameterType.FullName))})" : string.Empty)}";
            var member = members.FirstOrDefault(m => m.Attribute("name").Value == name);
            return member?.Elements() ?? Enumerable.Empty<XElement>();
        }

        public IEnumerable<XElement> GetDocumentation(EventInfo @event)
        {
            var type = @event.DeclaringType;
            var members = GetMembersElement(type.Assembly);
            var name = $"E:{(type.IsGenericType ? type.GetGenericTypeDefinition() : type).FullName}.{@event.Name}";
            var member = members.FirstOrDefault(m => m.Attribute("name").Value == name);
            return member?.Elements() ?? Enumerable.Empty<XElement>();
        }

        public IEnumerable<XElement> GetDocumentation(FieldInfo field)
        {
            var type = field.DeclaringType;
            var members = GetMembersElement(type.Assembly);
            var name = $"F:{(type.IsGenericType ? type.GetGenericTypeDefinition() : type).FullName}.{field.Name}";
            var member = members.FirstOrDefault(m => m.Attribute("name").Value == name);
            return member?.Elements() ?? Enumerable.Empty<XElement>();
        }

        private IEnumerable<XElement> GetMembersElement(Assembly assembly)
        {
            if (!_assemblyDocumentation.TryGetValue(assembly, out var doc))
            {
                var xmlPath = Path.ChangeExtension(assembly.Location, "xml");
                if (File.Exists(xmlPath))
                {
                    try
                    {
                        doc = XDocument.Load(xmlPath);
                    }
                    catch
                    {
                    }
                }
                _assemblyDocumentation.Add(assembly, doc);
            }
            return doc?.Element("doc")?.Element("members")?.Elements() ?? Enumerable.Empty<XElement>();
        }
    }
}