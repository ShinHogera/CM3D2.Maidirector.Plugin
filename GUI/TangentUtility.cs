using System;
using System.Collections.Generic;
using UnityEngine;

namespace CM3D2.HandmaidsTale.Plugin
{
    internal static class TangentUtility
    {
        public enum TangentMode
        {
            Free,
            Auto,
            Linear,
            Constant,
            // ClampedAuto
        }

        private static int kBrokenMask = 1;

        private static int kLeftTangentMask = 30;

        private static int kRightTangentMask = 480;

        private static float Internal_CalculateLinearTangent(AnimationCurve curve, int index, int toIndex)
        {
            float num = curve[index].time - curve[toIndex].time;
            float result;
            if (Mathf.Approximately(num, 0f))
            {
                result = 0f;
            }
            else
            {
                result = (curve[index].value - curve[toIndex].value) / num;
            }
            return result;
        }

        private static void Internal_UpdateTangents(MovieCurve movieCurve, int index)
        {
            AnimationCurve curve = movieCurve.curve;
            if (index >= 0 && index < curve.length)
            {
                Keyframe key = curve[index];

                var leftTang = TangentUtility.GetKeyLeftTangentMode(movieCurve, index);
                var rightTang = TangentUtility.GetKeyRightTangentMode(movieCurve, index);

                if (leftTang == TangentUtility.TangentMode.Linear && index >= 1)
                {
                    key.inTangent = TangentUtility.Internal_CalculateLinearTangent(curve, index, index - 1);
                    curve.MoveKey(index, key);
                }
                if (rightTang == TangentUtility.TangentMode.Linear && index + 1 < curve.length)
                {
                    key.outTangent = TangentUtility.Internal_CalculateLinearTangent(curve, index, index + 1);
                    curve.MoveKey(index, key);
                }
                // if (TangentUtility.GetKeyLeftTangentMode(key) == TangentUtility.TangentMode.ClampedAuto || TangentUtility.GetKeyRightTangentMode(key) == TangentUtility.TangentMode.ClampedAuto)
                // {
                //     TangentUtility.Internal_CalculateAutoTangent(curve, index);
                // }
                if (leftTang == TangentUtility.TangentMode.Auto || rightTang == TangentUtility.TangentMode.Auto)
                {
                    curve.SmoothTangents(index, 0f);
                }
                if (leftTang == TangentUtility.TangentMode.Free && rightTang == TangentUtility.TangentMode.Free && !TangentUtility.GetKeyBroken(movieCurve, index))
                {
                    key.outTangent = key.inTangent;
                    curve.MoveKey(index, key);
                }
                if (leftTang == TangentUtility.TangentMode.Constant)
                {
                    key.inTangent = float.PositiveInfinity;
                    curve.MoveKey(index, key);
                }
                if (rightTang == TangentUtility.TangentMode.Constant)
                {
                    key.outTangent = float.PositiveInfinity;
                    curve.MoveKey(index, key);
                }
            }
        }

        internal static void UpdateTangentsFromModeSurrounding(MovieCurve curve, int index)
        {
            TangentUtility.Internal_UpdateTangents(curve, index - 2);
            TangentUtility.Internal_UpdateTangents(curve, index - 1);
            TangentUtility.Internal_UpdateTangents(curve, index);
            TangentUtility.Internal_UpdateTangents(curve, index + 1);
            TangentUtility.Internal_UpdateTangents(curve, index + 2);
        }

        internal static void UpdateTangentsFromMode(MovieCurve curve)
        {
            for (int i = 0; i < curve.curve.length; i++)
            {
                TangentUtility.Internal_UpdateTangents(curve, i);
            }
        }

        internal static TangentUtility.TangentMode GetKeyLeftTangentMode(MovieCurve curve, int index)
        {
            return (TangentUtility.TangentMode)((curve.GetTangentMode(index) & TangentUtility.kLeftTangentMask) >> 1);
        }

        internal static TangentUtility.TangentMode GetKeyRightTangentMode(MovieCurve curve, int index)
        {
            return (TangentUtility.TangentMode)((curve.GetTangentMode(index) & TangentUtility.kRightTangentMask) >> 5);
        }

        internal static bool GetKeyBroken(MovieCurve curve, int index)
        {
            return (curve.GetTangentMode(index) & TangentUtility.kBrokenMask) != 0;
        }

        internal static void SetKeyLeftTangentMode(ref MovieCurve curve, int index, TangentUtility.TangentMode tangentMode)
        {
            int curTangentMode = curve.GetTangentMode(index);
            curTangentMode &= ~TangentUtility.kLeftTangentMask;
            curTangentMode |= (int)((int)tangentMode << 1);
            curve.SetTangentMode(index, curTangentMode);
        }

        internal static void SetKeyRightTangentMode(ref MovieCurve curve, int index, TangentUtility.TangentMode tangentMode)
        {
            int curTangentMode = curve.GetTangentMode(index);
            curTangentMode &= ~TangentUtility.kRightTangentMask;
            curTangentMode |= (int)((int)tangentMode << 5);
            curve.SetTangentMode(index, curTangentMode);
        }

        internal static void SetKeyBroken(ref MovieCurve curve, int index, bool broken)
        {
            int curTangentMode = curve.GetTangentMode(index);
            if (broken)
            {
                curTangentMode |= TangentUtility.kBrokenMask;
            }
            else
            {
                curTangentMode &= ~TangentUtility.kBrokenMask;
            }
            curve.SetTangentMode(index, curTangentMode);
        }

        public static void SetKeyBroken(MovieCurve curve, int index, bool broken)
        {
            if (curve == null)
            {
                throw new ArgumentNullException("curve");
            }
            if (index < 0 || index >= curve.curve.length)
            {
                throw new ArgumentException("Index out of bounds.");
            }
            TangentUtility.SetKeyBroken(ref curve, index, broken);
            // curve.curve.MoveKey(index, key);
            TangentUtility.UpdateTangentsFromModeSurrounding(curve, index);
        }

        public static void SetKeyLeftTangentMode(MovieCurve curve, int index, TangentUtility.TangentMode tangentMode)
        {
            if (curve == null)
            {
                throw new ArgumentNullException("curve");
            }
            if (index < 0 || index >= curve.curve.length)
            {
                throw new ArgumentException("Index out of bounds.");
            }
            if (tangentMode != TangentUtility.TangentMode.Free)
            {
                TangentUtility.SetKeyBroken(ref curve, index, true);
            }
            TangentUtility.SetKeyLeftTangentMode(ref curve, index, tangentMode);
            // curve.MoveKey(index, key);
            TangentUtility.UpdateTangentsFromModeSurrounding(curve, index);
        }

        public static void SetKeyRightTangentMode(MovieCurve curve, int index, TangentUtility.TangentMode tangentMode)
        {
            if (curve == null)
            {
                throw new ArgumentNullException("curve");
            }
            if (index < 0 || index >= curve.curve.length)
            {
                throw new ArgumentException("Index out of bounds.");
            }
            if (tangentMode != TangentUtility.TangentMode.Free)
            {
                TangentUtility.SetKeyBroken(curve, index, true);
            }
            TangentUtility.SetKeyRightTangentMode(ref curve, index, tangentMode);
            // curve.MoveKey(index, key);
            TangentUtility.UpdateTangentsFromModeSurrounding(curve, index);
        }
    }
}
