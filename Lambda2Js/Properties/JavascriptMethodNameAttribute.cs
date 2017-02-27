using System;

namespace Lambda2Js.Properties
{
    public class JavascriptMethodNameAttribute : Attribute
    {
        public string Name { get; }

        public object[] PositionalArguments { get; set; }

        public JavascriptMethodNameAttribute(string name)
        {
            Name = name;
        }
    }
}