using System;

namespace Lambda2Js
{
    /// <summary>
    /// Attribute containing metadata for JavaScript conversion.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public class JavascriptMemberAttribute : Attribute,
        IJavascriptMemberMetadata
    {
        /// <summary>
        /// Gets or sets the name of the property when converted to JavaScript.
        /// </summary>
        public string MemberName { get; set; }
    }
}