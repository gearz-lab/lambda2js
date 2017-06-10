using System;

namespace Lambda2Js
{
    public static class ScriptVersionHelper
    {
        // See: ECMAScript 5/6/7 compatibility tables
        //      https://github.com/kangax/compat-table
        //      http://kangax.github.io/compat-table
        // See: Ecma International, Technical Committee 39 - ECMAScript
        //      https://github.com/tc39

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
                    return scriptVersion.IsSupersetOf(ScriptVersion.Es60);
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
                    if (scriptVersion.IsSupersetOf(ScriptVersion.Js181))
                        return true;
                    throw new ArgumentOutOfRangeException(nameof(api));

                case JavascriptApiFeature.String_prototype_trim:
                    return scriptVersion.IsSupersetOf(ScriptVersion.Es51);

                case JavascriptApiFeature.String_prototype_startsWith:
                case JavascriptApiFeature.String_prototype_endsWith:
                case JavascriptApiFeature.String_prototype_includes:
                    return scriptVersion.IsSupersetOf(ScriptVersion.Es60);

                case JavascriptApiFeature.String_prototype_padStart:
                case JavascriptApiFeature.String_prototype_padEnd:
                    return scriptVersion.IsSupersetOf(ScriptVersion.Es80);

                default:
                    throw new ArgumentOutOfRangeException(nameof(api));
            }
        }

        /// <summary>
        /// Creates a new ScriptVersion with the given parameters.
        /// </summary>
        /// <param name="spec">The specification to use, or 0 (zero) to use ECMA standard.</param>
        /// <param name="specVersion">The specification version. Pass 0 if using ECMA standard.</param>
        /// <param name="ecmaVersion"></param>
        /// <param name="allowDeprecated"></param>
        /// <param name="allowProposals"></param>
        /// <returns></returns>
        public static ScriptVersion Create(ScriptVersion spec, int specVersion, int ecmaVersion, bool allowDeprecated, bool allowProposals)
        {
            if (spec.GetSpecification() != spec)
                throw new ArgumentException("Pure specification expected in parameter.", nameof(spec));

            if (spec == 0 && specVersion != 0)
                throw new ArgumentException("Specification version must be zero when no specification is passed.", nameof(specVersion));

            var specVerFld = specVersion * Consts.SVerFld;
            if (specVerFld >= Consts.SVerLim)
                throw new ArgumentException("Specification version overflow.", nameof(specVersion));

            var ecmaVerFld = ecmaVersion * Consts.EcmaFld;
            if (ecmaVerFld >= Consts.EcmaLim)
                throw new ArgumentException("ECMAScript version overflow.", nameof(ecmaVersion));

            var flags = (allowProposals ? 1 : 0) + (allowDeprecated ? 2 : 0);
            return spec + specVerFld + ecmaVerFld + flags * Consts.FlagFld;
        }

        /// <summary>
        /// Changes the script version to accept deprecated features.
        /// </summary>
        /// <param name="scriptVersion"></param>
        /// <returns></returns>
        public static ScriptVersion Deprecated(this ScriptVersion scriptVersion)
        {
            return Create(
                scriptVersion.GetSpecification(),
                scriptVersion.GetSpecificationVersion(),
                scriptVersion.GetStandardVersion(),
                true,
                scriptVersion.IsProposals());
        }

        /// <summary>
        /// Changes the script version to accept deprecated features.
        /// </summary>
        /// <param name="scriptVersion"></param>
        /// <returns></returns>
        public static ScriptVersion Deprecated(this ScriptVersion scriptVersion, bool value)
        {
            return Create(
                scriptVersion.GetSpecification(),
                scriptVersion.GetSpecificationVersion(),
                scriptVersion.GetStandardVersion(),
                value,
                scriptVersion.IsProposals());
        }

        /// <summary>
        /// Changes the script version to accept proposed features.
        /// </summary>
        /// <param name="scriptVersion"></param>
        /// <returns></returns>
        public static ScriptVersion Proposals(this ScriptVersion scriptVersion)
        {
            return Create(
                scriptVersion.GetSpecification(),
                scriptVersion.GetSpecificationVersion(),
                scriptVersion.GetStandardVersion(),
                scriptVersion.IsDeprecated(),
                true);
        }

        /// <summary>
        /// Changes the script version to accept proposed features.
        /// </summary>
        /// <param name="scriptVersion"></param>
        /// <returns></returns>
        public static ScriptVersion Proposals(this ScriptVersion scriptVersion, bool value)
        {
            return Create(
                scriptVersion.GetSpecification(),
                scriptVersion.GetSpecificationVersion(),
                scriptVersion.GetStandardVersion(),
                scriptVersion.IsDeprecated(),
                value);
        }

        /// <summary>
        /// Changes the script version to accept only ECMAScript standard features.
        /// Won't change deprecated nor proposals flags.
        /// </summary>
        /// <param name="scriptVersion"></param>
        /// <returns></returns>
        public static ScriptVersion ToStandard(this ScriptVersion scriptVersion)
        {
            return Create(
                0,
                0,
                scriptVersion.GetStandardVersion(),
                scriptVersion.IsDeprecated(),
                scriptVersion.IsProposals());
        }

        /// <summary>
        /// Changes the script version to accept non-standard features from any specification.
        /// </summary>
        /// <param name="scriptVersion"></param>
        /// <returns></returns>
        public static ScriptVersion NonStandard(this ScriptVersion scriptVersion)
        {
            if (scriptVersion.GetSpecification() != 0)
                throw new ArgumentException("Script version must have no assigned specification.", nameof(scriptVersion));

            return Create(
                ScriptVersion.NonStandard,
                scriptVersion.GetSpecificationVersion(),
                scriptVersion.GetStandardVersion(),
                scriptVersion.IsDeprecated(),
                scriptVersion.IsProposals());
        }

        /// <summary>
        /// Changes the script version to accept JScript features.
        /// </summary>
        /// <param name="scriptVersion"></param>
        /// <returns></returns>
        public static ScriptVersion MicrosoftJScript(this ScriptVersion scriptVersion, int specVersion)
        {
            if (scriptVersion.GetSpecification() != 0)
                throw new ArgumentException("Script version must have no assigned specification.", nameof(scriptVersion));

            return Create(
                ScriptVersion.MsJ,
                specVersion,
                scriptVersion.GetStandardVersion(),
                scriptVersion.IsDeprecated(),
                scriptVersion.IsProposals());
        }

        /// <summary>
        /// Changes the script version to accept Javascript (Mozilla) features.
        /// </summary>
        /// <param name="scriptVersion"></param>
        /// <returns></returns>
        public static ScriptVersion Javascript(this ScriptVersion scriptVersion, int specVersion)
        {
            if (scriptVersion.GetSpecification() != 0)
                throw new ArgumentException("Script version must have no assigned specification.", nameof(scriptVersion));

            return Create(
                ScriptVersion.Js,
                specVersion,
                scriptVersion.GetStandardVersion(),
                scriptVersion.IsDeprecated(),
                scriptVersion.IsProposals());
        }

        /// <summary>
        /// Whether a specific version of the script is a superset of another version.
        /// This can be used as a rather general feature indicator, but there may be exceptions, such as deprecated features.
        /// </summary>
        /// <param name="scriptVersion"></param>
        /// <param name="baseScriptVersion"></param>
        /// <returns></returns>
        public static bool IsSupersetOf(this ScriptVersion scriptVersion, ScriptVersion baseScriptVersion)
        {
            var specOther = baseScriptVersion.GetSpecification();
            if (specOther == 0)
                return scriptVersion.GetStandardVersion() >= baseScriptVersion.GetStandardVersion();

            var specThis = scriptVersion.GetSpecification();
            if (specThis == ScriptVersion.NonStandard)
                return scriptVersion.GetStandardVersion() >= baseScriptVersion.GetStandardVersion();

            if (specThis == specOther)
                return scriptVersion.GetSpecificationVersion() >= baseScriptVersion.GetSpecificationVersion();

            return false;
        }

        /// <summary>
        /// Gets the version of the non-standard specification, if one is present.
        /// </summary>
        /// <param name="scriptVersion"></param>
        /// <returns></returns>
        public static int GetSpecificationVersion(this ScriptVersion scriptVersion)
        {
            return ((int)scriptVersion % Consts.SVerLim) / Consts.SVerFld;
        }

        /// <summary>
        /// Gets the version of the ECMAScript standard.
        /// </summary>
        /// <param name="scriptVersion"></param>
        /// <returns></returns>
        public static int GetStandardVersion(this ScriptVersion scriptVersion)
        {
            return ((int)scriptVersion % Consts.EcmaLim) / Consts.EcmaFld;
        }

        /// <summary>
        /// Gets the version of the non-standard specification.
        /// </summary>
        /// <param name="scriptVersion"></param>
        /// <returns></returns>
        public static ScriptVersion GetSpecification(this ScriptVersion scriptVersion)
        {
            return (ScriptVersion)(((int)scriptVersion / Consts.SpecFld) * Consts.SpecFld);
        }

        /// <summary>
        /// Gets a value indicating whether the script version supports proposals
        /// (i.e. next ECMAScript proposed features).
        /// </summary>
        /// <param name="scriptVersion"></param>
        /// <returns></returns>
        public static bool IsProposals(this ScriptVersion scriptVersion)
        {
            return (((int)scriptVersion / (int)ScriptVersion.Proposals) & 1) == 1;
        }

        /// <summary>
        /// Gets a value indicating whether the script version supports deprecated features
        /// (i.e. features excluded from the standard, but present in one of Js or MsJ specification).
        /// </summary>
        /// <param name="scriptVersion"></param>
        /// <returns></returns>
        public static bool IsDeprecated(this ScriptVersion scriptVersion)
        {
            return (((int)scriptVersion / (int)ScriptVersion.Deprecated) & 1) == 1;
        }
    }
}