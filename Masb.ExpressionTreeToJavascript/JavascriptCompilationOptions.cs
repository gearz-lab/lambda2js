using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Masb.ExpressionTreeToJavascript
{
    public class JavascriptCompilationOptions
    {
        public static readonly JavascriptCompilationOptions DefaultOptions = new JavascriptCompilationOptions();

        private JavascriptCompilationOptions()
        {
            this.BodyOnly = true;
            this.ScopeParameter = true;
            this.Extensions = Enumerable.Empty<JavascriptConversionExtension>();
        }

        public JavascriptCompilationOptions(JsCompilationFlags flags, IEnumerable<JavascriptConversionExtension> extensions = null)
        {
            this.BodyOnly = (flags & JsCompilationFlags.BodyOnly) != 0;
            this.ScopeParameter = (flags & JsCompilationFlags.ScopeParameter) != 0;
            this.Extensions = extensions == null
                ? Enumerable.Empty<JavascriptConversionExtension>()
                : new ReadOnlyCollection<JavascriptConversionExtension>(extensions.ToArray());
        }

        public bool BodyOnly { get; private set; }

        public bool ScopeParameter { get; private set; }

        public IEnumerable<JavascriptConversionExtension> Extensions { get; private set; }
    }
}