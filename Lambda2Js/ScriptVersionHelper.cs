using System;

namespace Lambda2Js
{
    public static class ScriptVersionHelper
    {
        /// <summary>
        /// Indicates whether the specified version of JavaScript supports the given syntax.
        /// </summary>
        /// <param name="scriptVersion"></param>
        /// <param name="syntax"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static bool Supports(this ScriptVersion scriptVersion, JavascriptSyntaxFeature syntax)
        {
            switch (syntax)
            {
                case JavascriptSyntaxFeature.ArrowFunction:
                case JavascriptSyntaxFeature.ArraySpread:
                    return ((int)scriptVersion & 0xFFFF) >= 60;
                default:
                    throw new ArgumentOutOfRangeException(nameof(syntax));
            }
        }

        /// <summary>
        /// Indicates whether the specified version of JavaScript supports the given syntax.
        /// </summary>
        /// <param name="scriptVersion"></param>
        /// <param name="api"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static bool Supports(this ScriptVersion scriptVersion, JavascriptApiFeature api)
        {
            switch (api)
            {
                case JavascriptApiFeature.String_prototype_substring:
                case JavascriptApiFeature.String_prototype_toUpperCase:
                case JavascriptApiFeature.String_prototype_toLowerCase:
                case JavascriptApiFeature.String_prototype_indexOf:
                case JavascriptApiFeature.String_prototype_lastIndexOf:
                    return true;

                case JavascriptApiFeature.String_prototype_trimLeft:
                case JavascriptApiFeature.String_prototype_trimRight:
                    if ((scriptVersion & ScriptVersion.NonStandard) != 0)
                        return (scriptVersion & (ScriptVersion)0xFFFF) >= ScriptVersion.Js18;
                    throw new ArgumentOutOfRangeException(nameof(api));

                case JavascriptApiFeature.String_prototype_trim:
                    return ((int)scriptVersion & 0xFFFF) >= 51;

                case JavascriptApiFeature.String_prototype_startsWith:
                case JavascriptApiFeature.String_prototype_endsWith:
                case JavascriptApiFeature.String_prototype_includes:
                    return ((int)scriptVersion & 0xFFFF) >= 60;

                case JavascriptApiFeature.String_prototype_padStart:
                case JavascriptApiFeature.String_prototype_padEnd:
                    return ((int)scriptVersion & 0xFFFF) >= 80;

                default:
                    throw new ArgumentOutOfRangeException(nameof(api));
            }
        }
    }
}