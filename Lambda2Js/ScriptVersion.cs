namespace Lambda2Js
{
    /// <summary>
    /// Versions of the ECMA Script and JavaScript.
    /// </summary>
    public enum ScriptVersion
    {
        /// <summary> ECMA Script 3 (December 1999) </summary>
        Es30 = 30,
        /// <summary> JavaScript 1.5 </summary>
        Js15 = Es30,
        /// <summary> JavaScript 1.6 </summary>
        Js16 = Es30 + 1,
        /// <summary> JavaScript 1.7 </summary>
        Js17 = Es30 + 2,
        /// <summary> JavaScript 1.8 </summary>
        Js18 = Es30 + 3,

        /// <summary> ECMA Script 5 (December 2009) </summary>
        Es50 = 50,

        /// <summary> ECMA Script 5.1 (June 2011) </summary>
        Es51 = 51,

        /// <summary> ECMA Script 6 (June 2015) </summary>
        Es60 = 60,
        /// <summary> ECMA Script 6 (June 2015) </summary>
        ECMAScript2015 = Es60,

        /// <summary> ECMA Script 7 (June 2016) </summary>
        Es70 = 70,
        /// <summary> ECMA Script 7 (June 2016) </summary>
        ECMAScript2016 = Es70,

        /// <summary> ECMA Script 8 (Draft) </summary>
        Es80 = 80,
        /// <summary> ECMA Script 8 (Draft) </summary>
        ECMAScript2017 = Es80,

        /// <summary> Non-standard </summary>
        NonStandard = 0x10000,
    }
}