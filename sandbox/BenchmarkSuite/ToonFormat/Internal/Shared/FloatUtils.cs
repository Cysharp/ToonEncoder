using System;

namespace ToonFormat.Internal.Shared
{
    internal static class FloatUtils
    {
        /// <summary>
        /// Tolerance comparison applicable to general business: Taking into account both absolute error and relative error simultaneously
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="absEps"></param>
        /// <param name="relEps"></param>
        /// <returns></returns>
        public static bool NearlyEqual(double a, double b, double absEps = 1e-12, double relEps = 1e-9)
        {
            if (double.IsNaN(a) && double.IsNaN(b)) return true;     // ﾒｵﾎﾏｳ｣ﾐ靨ｪﾊﾓ NaN == NaN
            if (double.IsInfinity(a) || double.IsInfinity(b)) return a.Equals(b);
            if (a == b) return true;                                  // ｸｲｸﾇ 0.0 == -0.0｡｢ﾍ・ｫﾏ犒ﾈ

            var diff = Math.Abs(a - b);
            var scale = Math.Max(Math.Abs(a), Math.Abs(b));
            if (scale == 0) return diff <= absEps;                    // ﾁｽﾕﾟｶｼｽﾓｽ・0
            return diff <= Math.Max(absEps, relEps * scale);
        }

        /// <summary>
        /// Explicitly change -0.0 to +0.0 to avoid exposing the sign difference in subsequent operations such as 1.0/x.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static double NormalizeSignedZero(double v) =>
            BitConverter.DoubleToInt64Bits(v) == BitConverter.DoubleToInt64Bits(-0.0) ? 0.0 : v;

        /// <summary>
        /// Explicitly change -0.0f to +0.0f for float values.
        /// </summary>
        public static float NormalizeSignedZero(float v) =>
            BitConverter.SingleToInt32Bits(v) == BitConverter.SingleToInt32Bits(-0.0f) ? 0.0f : v;
    }
}
