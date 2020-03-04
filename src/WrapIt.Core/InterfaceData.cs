using System;
using System.Collections.Generic;
using System.Reflection;

namespace WrapIt
{
    internal class InterfaceData : TypeMembersData
    {
        public InterfaceData(Type type, TypeName className, TypeName interfaceName, TypeBuildStatus buildStatus)
            : base(type, className, interfaceName, buildStatus)
        {
        }

        protected override bool IncludeMethod(WrapperBuilder builder, MethodInfo method, HashSet<TypeData> typeDatas, out bool overrideObject)
        {
            overrideObject = false;
            return true;
        }
    }
}