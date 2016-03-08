using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Lambda2Js
{
    /// <summary>
    /// Options that change how Lambda2Js converts to the resulting JavaScript.
    /// </summary>
    public class JavascriptCompilationOptions
    {
        /// <summary>
        /// Gets the default options used by Lambda2Js converts to JavaScript.
        /// </summary>
        public static readonly JavascriptCompilationOptions DefaultOptions = new JavascriptCompilationOptions();

        private JavascriptCompilationOptions()
        {
            this.BodyOnly = true;
            this.ScopeParameter = true;
            this.Extensions = Enumerable.Empty<JavascriptConversionExtension>();
        }

        /// <summary>
        /// Creates an instance of the <see cref="JavascriptCompilationOptions"/> object.
        /// </summary>
        /// <param name="flags">JavaScript compilation flags.</param>
        /// <param name="extensions">Extensions to the compilation.</param>
        public JavascriptCompilationOptions(
            JsCompilationFlags flags,
            params JavascriptConversionExtension[] extensions)
            : this(flags, (IEnumerable<JavascriptConversionExtension>)extensions)
        {
        }

        public JavascriptCompilationOptions(JsCompilationFlags flags, IEnumerable<JavascriptConversionExtension> extensions = null)
        {
            this.BodyOnly = (flags & JsCompilationFlags.BodyOnly) != 0;
            this.ScopeParameter = (flags & JsCompilationFlags.ScopeParameter) != 0;
            this.Extensions = extensions == null
                ? Enumerable.Empty<JavascriptConversionExtension>()
                : new ReadOnlyCollection<JavascriptConversionExtension>(extensions.ToArray());
        }

        /// <summary>
        /// Gets a value indicating whether only the body of the lambda expression will be rendered.
        /// </summary>
        public bool BodyOnly { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the single argument of the lambda represents the arguments passed to the JavaScript.
        /// <para>The lambda:</para>
        /// <para>(obj) => obj.X + obj.Y</para>
        /// <para>results in this kind of JavaScript:</para>
        /// <para>function(x,y){return x+y;}</para>
        /// </summary>
        public bool ScopeParameter { get; private set; }

        public IEnumerable<JavascriptConversionExtension> Extensions { get; private set; }
    }
}