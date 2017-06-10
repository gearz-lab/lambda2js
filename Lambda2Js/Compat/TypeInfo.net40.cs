using System.Collections.Generic;
using System.Linq;

namespace System.Reflection
{
    class TypeInfo
    {
        private readonly Type type;

        public TypeInfo(Type type)
        {
            this.type = type;
        }

        public bool IsGenericType => this.type.IsGenericType;

        public bool IsEnum => this.type.IsEnum;

        public bool IsAssignableFrom(TypeInfo typeInfo)
        {
            return this.type.IsAssignableFrom(typeInfo.type);
        }

        public IEnumerable<Attribute> GetCustomAttributes(Type type, bool inherit)
        {
            return this.type.GetCustomAttributes(type, inherit).OfType<Attribute>();
        }

        public PropertyInfo GetDeclaredProperty(string name)
        {
            return this.type.GetProperty(name,
                BindingFlags.NonPublic |
                BindingFlags.Public |
                BindingFlags.Static |
                BindingFlags.Instance |
                BindingFlags.DeclaredOnly);
        }

        public bool IsDefined(Type type, bool inherit)
        {
            return this.type.IsDefined(type, inherit);
        }
    }
}
