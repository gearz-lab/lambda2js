using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Lambda2Js
{
    /// <summary>
    /// Provides metadata about the objects that are going to be converted to JavaScript in some way.
    /// </summary>
    public class AttributeJavascriptMetadataProvider : JavascriptMetadataProvider
    {
        /// <summary>
        /// Gets metadata about a property that is going to be used in JavaScript code.
        /// </summary>
        /// <param name="memberInfo"></param>
        /// <returns></returns>
        public override IJavascriptMemberMetadata GetMemberMetadata(MemberInfo memberInfo)
        {
            var attr0 = memberInfo
                .GetCustomAttributes(typeof(JavascriptMemberAttribute), true)
                .OfType<IJavascriptMemberMetadata>()
                .SingleOrDefault();

            if (attr0 != null)
                return attr0;

            var jsonAttr = memberInfo
                .GetCustomAttributes(true)
                .Where(a => a.GetType().Name == "JsonPropertyAttribute")
                .Select(this.ConvertJsonAttribute)
                .SingleOrDefault();

            return jsonAttr;
        }

        class Accessors
        {
            public Func<object, string> PropertyNameGetter { get; set; }
        }

        private readonly Dictionary<Type, Accessors> accessors = new Dictionary<Type, Accessors>();

        private IJavascriptMemberMetadata ConvertJsonAttribute(object attr)
        {
            var type = attr.GetType();
            Accessors accessor;
            // ReSharper disable InconsistentlySynchronizedField
            if (!accessors.ContainsKey(type))
            {
                lock (accessors)
                    if (!accessors.ContainsKey(type))
                    {
                        accessors[type] = accessor = new Accessors();
                        accessor.PropertyNameGetter = type.GetProperty("PropertyName")?.MakeGetterDelegate<string>();
                    }
                    else
                        accessor = accessors[type];
            }
            else
                accessor = accessors[type];
            // ReSharper restore InconsistentlySynchronizedField

            return new JavascriptMemberAttribute
            {
                MemberName = accessor.PropertyNameGetter?.Invoke(attr),
            };
        }
    }
}