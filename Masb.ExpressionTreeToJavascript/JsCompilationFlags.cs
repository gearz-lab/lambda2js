using System;

namespace Masb.ExpressionTreeToJavascript
{
    [Flags]
    public enum JsCompilationFlags
    {
        BodyOnly = 1,
        ScopeParameter = 2
    }
}