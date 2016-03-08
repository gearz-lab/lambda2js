using System;

namespace Lambda2Js
{
    [Flags]
    public enum JsCompilationFlags
    {
        BodyOnly = 1,

        /// <summary>
        /// Flag that indicates whether the single argument of the lambda
        /// represents the arguments passed to the JavaScript.
        /// <para>The lambda:</para>
        /// <para>(obj) => obj.X + obj.Y</para>
        /// <para>results in this kind of JavaScript:</para>
        /// <para>function(x,y){return x+y;}</para>
        /// </summary>
        ScopeParameter = 2,
    }
}