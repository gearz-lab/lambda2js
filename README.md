# Lambda2Js

This is an ExpressionTree-to-Javascript converter.

Converts a C# expression tree (from Linq namespace) to a syntatically correct javascript code.

It can be extended to customize the mapping of expressions:

 - support custom static methods, instead of emiting code that would otherwise depend on external javascript
 - support custom types, converting method calls and properties accordingly

It is well tesded, and won't break.

This project will use Semantic versioning, and soon will have a NuGet package.
