using System.Collections.Generic;
using UnityEngine;

namespace SplineToolkit
{
    /// <summary>
    /// A curve you can ride: everything a follower, spawner or camera needs on top of
    /// <see cref="CatmullRomSpline"/>.
    /// - EvaluateAt(t) in arc-length t
    /// - Stable tangents using a *meter-based* epsilon
    /// - A consistent local frame via EvaluateFrame (yaw-only or full)
    /// - Range helpers, so a section of the curve can be addressed in its own meters
    /// </summary>
    public sealed class SplinePath
    {
        private readonly CatmullRomSpline spline;

        /// <summary>Length of the whole curve in meters.</summary>
        public float TotalLength => spline.TotalLength;

        /// <param name="points">World-space points, at least two.</param>
        /// <param name="samplesPerSegment">Spline LUT resolution (60-100 recommended for tight bends).</param>
        public SplinePath(List<Vector3> points, int samplesPerSegment = 60)
        {
            spline = new CatmullRomSpline(points, samplesPerSegment);
        }

        /// <summary>Position at normalized arc-length t in [0,1].</summary>
        public Vector3 EvaluateAt(float t) => spline.Evaluate(Mathf.Clamp01(t));

        /// <summary>Meters to normalized t (clamped).</summary>
        public float TAtDistance(float meters) => spline.TAtDistance(meters);

        /// <summary>Normalized t to meters (clamped).</summary>
        public float DistanceAtT(float t) => spline.DistanceAtT(t);

        /// <summary>Unit tangent at t.</summary>
        public Vector3 GetForwardDirection(float t) => EvaluateTangent(t);

        /// <summary>
        /// Stable tangent using a *meter-based* lookahead/behind rather than a delta in t.
        /// </summary>
        /// <remarks>
        /// The chord across +/- epsilon can collapse on a doubled control point or a hairpin tight enough
        /// to close it. Answering that with world forward hands back a direction with no relation to the
        /// curve, which orients whatever asked for it backwards, so the epsilon widens and retries first.
        /// </remarks>
        public Vector3 EvaluateTangent(float t, float epsilonMeters = 1f)
        {
            t = Mathf.Clamp01(t);
            if (spline.TotalLength <= 1e-6f) return Vector3.forward;

            float d = spline.DistanceAtT(t);

            for (int attempt = 0; attempt < 3; attempt++)
            {
                float eps = epsilonMeters * (1 + attempt * 4); // 1x, 5x, 9x
                float d0 = Mathf.Max(0f, d - eps);
                float d1 = Mathf.Min(spline.TotalLength, d + eps);

                Vector3 v = EvaluateAt(spline.TAtDistance(d1)) - EvaluateAt(spline.TAtDistance(d0));
                if (v.sqrMagnitude > 1e-10f) return v.normalized;
            }

            return Vector3.forward;
        }

        /// <summary>
        /// Local frame at t.
        /// yawOnly = true: forward is the tangent flattened onto worldUp, which keeps placed objects level.
        /// yawOnly = false: full frame from the tangent, for tunnels, loops and banking.
        /// </summary>
        public void EvaluateFrame(float t, out Vector3 pos, out Vector3 fwd, out Vector3 right, out Vector3 up,
            bool yawOnly = true, Vector3 worldUp = default)
        {
            if (worldUp == default) worldUp = Vector3.up;

            pos = EvaluateAt(Mathf.Clamp01(t));
            Vector3 tan = EvaluateTangent(t);

            if (yawOnly)
            {
                Vector3 fwdXZ = Vector3.ProjectOnPlane(tan, worldUp);
                if (fwdXZ.sqrMagnitude < 1e-8f) fwdXZ = tan; // fallback if nearly vertical
                fwd = fwdXZ.normalized;
                up = worldUp;
                right = Vector3.Cross(up, fwd).normalized;
            }
            else
            {
                fwd = tan.normalized;
                right = Vector3.Cross(worldUp, fwd).normalized;
                if (right.sqrMagnitude < 1e-6f) right = Vector3.right; // degeneracy fallback
                up = Vector3.Cross(fwd, right).normalized;
            }
        }

        /// <summary>Rotation matching the frame at t.</summary>
        public Quaternion EvaluateRotation(float t, bool yawOnly = true, Vector3 worldUp = default)
        {
            EvaluateFrame(t, out _, out var fwd, out _, out var up, yawOnly, worldUp);
            return Quaternion.LookRotation(fwd, up);
        }

        /// <summary>A point offset sideways and vertically from the curve at t, in the local frame.</summary>
        public Vector3 EvaluateOffset(float t, float lateral, float vertical = 0f, bool yawOnly = true)
        {
            EvaluateFrame(t, out var pos, out _, out var right, out var up, yawOnly);
            return pos + right * lateral + up * vertical;
        }

        /// <summary>Meters between tRange.x and tRange.y (t is arc-length normalized).</summary>
        public float GetRangeLengthMeters(Vector2 tRange)
        {
            float a = Mathf.Clamp01(Mathf.Min(tRange.x, tRange.y));
            float b = Mathf.Clamp01(Mathf.Max(tRange.x, tRange.y));
            return (b - a) * spline.TotalLength;
        }

        /// <summary>Meters at the start/end of a range, measured along the whole curve.</summary>
        public void GetRangeStartEndMeters(Vector2 tRange, out float dStart, out float dEnd)
        {
            float a = Mathf.Clamp01(Mathf.Min(tRange.x, tRange.y));
            float b = Mathf.Clamp01(Mathf.Max(tRange.x, tRange.y));
            float total = spline.TotalLength;
            dStart = a * total;
            dEnd = b * total;
        }

        /// <summary>Meters travelled between two t values, never negative.</summary>
        public float MetersBetween(float fromT, float toT)
        {
            float dStart = DistanceAtT(Mathf.Clamp01(fromT));
            float dNow = DistanceAtT(Mathf.Clamp01(toT));
            return Mathf.Clamp(dNow - dStart, 0f, TotalLength - dStart);
        }

        /// <summary>
        /// Meters from the start of a range to a global normalized t inside [a..b], clamped to the range.
        /// </summary>
        public float RangeTAtDistance(Vector2 tRange, float metersFromRangeStart)
        {
            float a = Mathf.Clamp01(Mathf.Min(tRange.x, tRange.y));
            float b = Mathf.Clamp01(Mathf.Max(tRange.x, tRange.y));
            float len = Mathf.Max(1e-6f, (b - a) * spline.TotalLength);
            float u = Mathf.Clamp01(metersFromRangeStart / len);
            return Mathf.Lerp(a, b, u);
        }

        /// <summary>Normalized u in [0..1] inside a range to meters along the whole curve.</summary>
        public float RangeDistanceAtU(Vector2 tRange, float u01)
        {
            float a = Mathf.Clamp01(Mathf.Min(tRange.x, tRange.y));
            float b = Mathf.Clamp01(Mathf.Max(tRange.x, tRange.y));
            float total = spline.TotalLength;
            return Mathf.Lerp(a * total, b * total, Mathf.Clamp01(u01));
        }

        /// <summary>
        /// Nearest t to <paramref name="pos"/>, searched only inside [fromT, fromT + windowT].
        /// </summary>
        /// <remarks>
        /// Bounding the window matters twice over. It caps the cost at windowT/step evaluations instead of
        /// scanning the rest of the curve every frame, and it keeps progress monotonic. A path that
        /// switches back on itself lets an unbounded search find a nearer point far ahead and report
        /// progress the follower has not made. Keep the window wider than the furthest anything can travel
        /// in one frame.
        /// </remarks>
        public float GetClosestTAhead(Vector3 pos, float fromT, float windowT = 0.02f, float step = 0.001f)
        {
            fromT = Mathf.Clamp01(fromT);
            step = Mathf.Max(1e-5f, step);

            int steps = Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(0f, windowT) / step));

            float closestT = fromT;
            float minDistSq = float.MaxValue;

            for (int i = 0; i <= steps; i++)
            {
                // Integer stepping: accumulating tt += step in a float loop drifts over a long curve.
                float tt = Mathf.Min(1f, fromT + i * step);

                float d2 = (pos - EvaluateAt(tt)).sqrMagnitude;
                if (d2 < minDistSq)
                {
                    minDistSq = d2;
                    closestT = tt;
                }

                if (tt >= 1f) break;
            }

            return closestT;
        }

        /// <summary>
        /// Nearest t anywhere on the curve: coarse sweep, then a local refine.
        /// Use it for a one-off placement query, never per frame on a path that doubles back.
        /// </summary>
        public float GetClosestT(Vector3 pos, int coarseSamples = 256)
        {
            coarseSamples = Mathf.Max(8, coarseSamples);

            float bestT = 0f;
            float best = float.MaxValue;

            for (int i = 0; i <= coarseSamples; i++)
            {
                float t = i / (float)coarseSamples;
                float d2 = (pos - EvaluateAt(t)).sqrMagnitude;
                if (d2 < best) { best = d2; bestT = t; }
            }

            float window = 1f / coarseSamples;
            for (int refine = 0; refine < 4; refine++)
            {
                float lo = Mathf.Clamp01(bestT - window);
                float hi = Mathf.Clamp01(bestT + window);
                for (int i = 0; i <= 8; i++)
                {
                    float t = Mathf.Lerp(lo, hi, i / 8f);
                    float d2 = (pos - EvaluateAt(t)).sqrMagnitude;
                    if (d2 < best) { best = d2; bestT = t; }
                }
                window *= 0.25f;
            }

            return bestT;
        }
    }
}
