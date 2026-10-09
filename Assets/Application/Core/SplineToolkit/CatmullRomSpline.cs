using System;
using System.Collections.Generic;
using UnityEngine;

namespace SplineToolkit
{
    /// <summary>
    /// Catmull-Rom spline evaluated in *arc-length* parameter space (t in [0,1]).
    /// - Pre-samples each segment, builds a cumulative arc-length LUT.
    /// - Evaluate(t) interpolates along normalized arc-length, so motion is uniform.
    /// - TotalLength is in meters (Unity world units).
    /// </summary>
    /// <remarks>
    /// Evaluating a raw Catmull-Rom by its natural parameter moves fast through long segments and slow
    /// through short ones. Anything that rides the curve at a speed - a vehicle, a camera, a spawned
    /// pickup laid out every N meters - needs distance, not parameter, which is what the LUT buys.
    /// </remarks>
    public sealed class CatmullRomSpline
    {
        // Input
        private readonly List<Vector3> controlPoints;   // padded ends: p0 + original + pN

        // LUT
        private readonly List<Vector3> samples = new List<Vector3>();      // sampled positions along the whole curve
        private readonly List<float> cumulative01 = new List<float>();     // normalized cumulative length per sample [0..1]
        private float totalLength;

        /// <summary>Length of the whole curve in meters.</summary>
        public float TotalLength => totalLength;

        /// <param name="originalPoints">At least two points in world space.</param>
        /// <param name="samplesPerSegment">Higher = smoother arc-length/tangent. 60-100 recommended for tight bends.</param>
        public CatmullRomSpline(List<Vector3> originalPoints, int samplesPerSegment = 60)
        {
            if (originalPoints == null || originalPoints.Count < 2)
                throw new ArgumentException("CatmullRomSpline needs at least 2 points.");

            // Pad ends so the first and last segment have the neighbours the formula needs.
            controlPoints = new List<Vector3>(originalPoints.Count + 2);
            controlPoints.Add(originalPoints[0]);                          // p0
            controlPoints.AddRange(originalPoints);                        // p1..pN
            controlPoints.Add(originalPoints[originalPoints.Count - 1]);   // pN+1

            BuildLUT(Mathf.Max(1, samplesPerSegment));
        }

        private void BuildLUT(int samplesPerSegment)
        {
            samples.Clear();
            cumulative01.Clear();
            totalLength = 0f;

            for (int i = 0; i < controlPoints.Count - 3; i++)
            {
                for (int j = 0; j <= samplesPerSegment; j++)
                {
                    float u = j / (float)samplesPerSegment; // parameter inside this segment
                    Vector3 pt = Catmull(controlPoints[i], controlPoints[i + 1], controlPoints[i + 2], controlPoints[i + 3], u);

                    if (samples.Count > 0)
                    {
                        totalLength += Vector3.Distance(samples[samples.Count - 1], pt);
                        samples.Add(pt);
                        cumulative01.Add(totalLength); // unnormalized for now
                    }
                    else
                    {
                        samples.Add(pt);
                        cumulative01.Add(0f);
                    }
                }
            }

            if (totalLength <= 1e-6f)
            {
                // Degenerate: every point in the same place.
                totalLength = 0f;
                for (int k = 0; k < cumulative01.Count; k++) cumulative01[k] = 0f;
                return;
            }

            for (int k = 0; k < cumulative01.Count; k++)
                cumulative01[k] /= totalLength;
        }

        /// <summary>Evaluate at normalized arc-length t in [0,1].</summary>
        public Vector3 Evaluate(float t)
        {
            if (samples.Count == 0) return Vector3.zero;
            if (t <= 0f) return samples[0];
            if (t >= 1f) return samples[samples.Count - 1];

            // Binary search for the LUT interval [lo, hi] with cumulative01[lo] <= t <= cumulative01[hi].
            int lo = 0, hi = cumulative01.Count - 1;
            while (lo + 1 < hi)
            {
                int mid = (lo + hi) >> 1;
                if (cumulative01[mid] < t) lo = mid; else hi = mid;
            }

            float t0 = cumulative01[lo];
            float t1 = cumulative01[hi];
            float segT = (Mathf.Abs(t1 - t0) > 1e-6f) ? Mathf.InverseLerp(t0, t1, t) : 0f;
            return Vector3.Lerp(samples[lo], samples[hi], segT);
        }

        /// <summary>Absolute meters -> normalized t (clamped).</summary>
        public float TAtDistance(float distanceMeters)
        {
            if (totalLength <= 1e-6f) return 0f;
            return Mathf.Clamp01(distanceMeters / totalLength);
        }

        /// <summary>Normalized t -> meters along the curve.</summary>
        public float DistanceAtT(float t) => Mathf.Clamp01(t) * totalLength;

        // Catmull-Rom, uniform, tension 0.5.
        private static Vector3 Catmull(in Vector3 p0, in Vector3 p1, in Vector3 p2, in Vector3 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;

            return 0.5f * (
                (2f * p1) +
                (-p0 + p2) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                (-p0 + 3f * p1 - 3f * p2 + p3) * t3
            );
        }
    }
}
