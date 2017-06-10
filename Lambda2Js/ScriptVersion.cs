using System;

namespace Lambda2Js
{
    /// <summary>
    /// Versions of the ECMAScript and JavaScript.
    /// </summary>
    public enum ScriptVersion
    {
        ///// <summary> ECMAScript 1 (June 1997) </summary>
        //Es10 = 10 * Consts.EcmaFld,
        ///// <summary> JavaScript 1.0 </summary>
        //Js13 = Es10 + Js + 130 * Consts.ImpvFld,
        ///// <summary> Microsoft JScript 3.0 </summary>
        //MsJ30 = Es10 + MsJ + 30 * Consts.ImpvFld,

        ///// <summary> ECMAScript 2 (review) </summary>
        //Es20 = 20 * Consts.EcmaFld,
        ///// <summary> Microsoft JScript 5.0 </summary>
        //MsJ50 = Es20 + MsJ + 50 * Consts.ImpvFld,

        /// <summary> ECMAScript 3 (December 1999) </summary>
        Es30 = 30 * Consts.EcmaFld,
        /// <summary> JavaScript 1.5 </summary>
        Js15 = Es30 + Js + 150 * Consts.SVerFld,
        /// <summary> Microsoft JScript 5.5 </summary>
        MsJ55 = Es30 + MsJ + 55 * Consts.SVerFld,

        /// <summary> ECMAScript 5 (December 2009) </summary>
        Es50 = 50 * Consts.EcmaFld,
        /// <summary> JavaScript 1.8.1 </summary>
        Js181 = Es50 + Js + 181 * Consts.SVerFld,
        /// <summary> Microsoft JScript 9.0 </summary>
        MsJ90 = Es50 + MsJ + 90 * Consts.SVerFld,

        /// <summary> ECMAScript 5.1 (June 2011) </summary>
        Es51 = 51 * Consts.EcmaFld,

        /// <summary> ECMAScript 6 (June 2015) </summary>
        Es60 = 60 * Consts.EcmaFld,
        /// <summary> ECMAScript 6 (June 2015) </summary>
        ECMAScript2015 = Es60,

        /// <summary> ECMAScript 7 (June 2016) </summary>
        Es70 = 70 * Consts.EcmaFld,
        /// <summary> ECMAScript 7 (June 2016) </summary>
        ECMAScript2016 = Es70,

        /// <summary> ECMAScript 8 (Draft) (may break compatibility) </summary>
        Es80 = 80 * Consts.EcmaFld,
        /// <summary> ECMAScript 8 (Draft) (may break compatibility) </summary>
        ECMAScript2017 = Es80,

        /// <summary> ECMAScript latest stable version (may break compatibility) </summary>
        EsLatestStable = Consts.EcmaLim - Consts.EcmaFld,

        /// <summary> ECMAScript Next (may break compatibility) </summary>
        EsNext = EsLatestStable + Proposals,

        /// <summary> Allow ECMAScript proposed features (may break compatibility) </summary>
        Proposals = 1 * Consts.FlagFld,

        /// <summary> Allow deprecated features (may break compatibility) </summary>
        Deprecated = 2 * Consts.FlagFld,

        /// <summary> Allow any non-standard specification features (may break compatibility) </summary>
        NonStandard = 1 * Consts.SpecFld,
        /// <summary> Allow JavaScript specification features (Netscape, Mozilla, Firefox) (may break compatibility) </summary>
        Js = 2 * Consts.SpecFld,
        /// <summary> Allow Microsoft JScript specification features (IE) (may break compatibility) </summary>
        MsJ = 3 * Consts.SpecFld,
    }

    class Consts
    {
        internal const int FlagFld = 0x1;

        internal const int EcmaFld = 100;
        internal const int EcmaLim = 100000;

        internal const int SVerFld = 100000;
        internal const int SVerLim = 100000000;

        internal const int SpecFld = 100000000;
    }
}