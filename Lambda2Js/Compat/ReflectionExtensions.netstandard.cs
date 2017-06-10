namespace System.Reflection
{
    static class ReflectionExtensions
    {
        public static MethodInfo GetGetMethod(this PropertyInfo pi, bool nonPublic)
        {
            return pi.GetMethod;
        }
    }
}
