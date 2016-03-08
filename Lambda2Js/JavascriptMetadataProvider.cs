using System.Reflection;
using JetBrains.Annotations;

namespace Lambda2Js
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class JavascriptMetadataProvider
    {
        /// <summary>
        /// Gets or sets the default metadata provider.
        /// The default is the <see cref="AttributeJavascriptMetadataProvider"/> class, but it can be changed.
        /// </summary>
        public static JavascriptMetadataProvider Default = new AttributeJavascriptMetadataProvider();

        /// <summary>
        /// Gets metadata about a property that is going to be used in JavaScript code.
        /// </summary>
        /// <param name="memberInfo"></param>
        /// <returns></returns>
        [CanBeNull]
        public abstract IJavascriptMemberMetadata GetMemberMetadata(MemberInfo memberInfo);
    }
}