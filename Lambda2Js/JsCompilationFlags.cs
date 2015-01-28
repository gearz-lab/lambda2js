using System;

namespace Lambda2Js
{
    [Flags]
    public enum JsCompilationFlags
    {
        BodyOnly = 1,
        ScopeParameter = 2
    }
}