# Lambda2Js

This is an ExpressionTree (lambda) to Javascript converter.

It is portable, so that you can use it is most environments.

It's purpose is to convert a C# expression tree (from Linq namespace) to a syntatically correct javascript code.

It can be extended to customize the mapping of expressions:

 - support custom static methods, instead of emiting code that would otherwise depend on external javascript
 - support custom types, converting method calls and properties accordingly

It is well tesded, and won't break.

This project will use Semantic versioning.
Installing [NuGet package](https://www.nuget.org/packages/Lambda2Js):

    PM> Install-Package Lambda2Js

Samples
-------

    Expression<Func<MyClass, object>> expr = x => x.PhonesByName["Miguel"].DDD == 32 || x.Phones.Length != 1;
    var js = expr.CompileToJavascript();
    Assert.AreEqual("PhonesByName[\"Miguel\"].DDD==32||Phones.length!=1", js);


    
    Expression<Func<MyClass, object>> expr = x => x.Phones.FirstOrDefault(p => p.DDD > 10);
    var js = expr.CompileToJavascript();
    Assert.AreEqual("System.Linq.Enumerable.FirstOrDefault(Phones,function(p){return p.DDD>10;})", js);
