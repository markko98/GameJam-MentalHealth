using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SplineToolkit
{
    /// <summary>
    /// The scene-facing half of the toolkit: says where the control points come from, builds the
    /// <see cref="SplinePath"/> from them, and draws the result in the scene view.
    /// </summary>
    /// <remarks>
    /// <see cref="SplinePath"/> and <see cref="CatmullRomSpline"/> are plain C# on purpose - they can be
    /// built from any list of points, tested without a scene, and used from a system that has no
    /// GameObject. This component exists so a designer can author and see one without writing code.
    /// </remarks>
    [ExecuteAlways]
    [AddComponentMenu("Spline Toolkit/Spline Path Source")]
    public sealed class SplinePathSource : MonoBehaviour
    {
        public enum PointSource
        {
            /// <summary>Every child transform, in sibling order. Drag them around to shape the curve.</summary>
            ChildTransforms,

            /// <summary>The list below, in this object's local space.</summary>
            InlinePoints,

            /// <summary>A .obj file imported into the project as a text asset.</summary>
            ObjTextAsset,

            /// <summary>A .obj file under StreamingAssets, loaded at runtime.</summary>
            StreamingAssetsObj,
        }

        [Header("Source")]
        [SerializeField] private PointSource source = PointSource.ChildTransforms;

        [Tooltip("Where the child points hang. Defaults to this object.")]
        [SerializeField] private Transform childRoot;

        [Tooltip("Local-space points, used when Source is InlinePoints.")]
        [SerializeField] private List<Vector3> inlinePoints = new List<Vector3>();

        [Tooltip("An .obj whose extension was changed to .txt, or any TextAsset holding .obj text.")]
        [SerializeField] private TextAsset objTextAsset;

        [Tooltip("Path under StreamingAssets, e.g. Paths/TrackA.obj")]
        [SerializeField] private string streamingAssetsPath;

        [Header("OBJ import transform")]
        [Tooltip("Applied to every vertex read out of the .obj, before this object's transform.")]
        [SerializeField] private Vector3 objTranslate = Vector3.zero;

        [Tooltip("Most exporters hand back a path facing the wrong way. 180,180,0 is the usual fix.")]
        [SerializeField] private Vector3 objRotate = Vector3.zero;

        [SerializeField] private Vector3 objScale = Vector3.one;

        [Header("Build")]
        [Tooltip("Whether points are placed relative to this object, or taken as world space as-is.")]
        [SerializeField] private bool applyObjectTransform = true;

        [Tooltip("LUT resolution per segment. 60-100 for tight bends, lower for long gentle curves.")]
        [Range(4, 200)]
        [SerializeField] private int samplesPerSegment = 60;

        [SerializeField] private bool buildOnAwake = true;

        [Header("Gizmos")]
        [SerializeField] private bool drawCurve = true;
        [Range(0f, 1f)] [SerializeField] private float tStart = 0f;
        [Range(0f, 1f)] [SerializeField] private float tEnd = 1f;
        [Range(8, 1000)] [SerializeField] private int gizmoSamples = 300;
        [SerializeField] private Color curveColor = Color.yellow;
        [Range(0f, 1f)] [SerializeField] private float lineAlpha = 0.9f;
        [SerializeField] private bool drawTangents = false;
        [SerializeField] private float tangentLength = 3f;
        [SerializeField] private Color tangentColor = Color.red;
        [SerializeField] private bool drawRight = false;
        [SerializeField] private bool drawUp = false;
        [SerializeField] private bool drawControlPoints = true;
        [SerializeField] private float controlPointRadius = 0.5f;

        private SplinePath path;

        /// <summary>
        /// Raised every time the path is (re)built. Subscribe before Awake, or check
        /// <see cref="IsReady"/> first - a late subscriber is not called for a build that already happened.
        /// </summary>
        public event Action<SplinePath> Built;

        /// <summary>The built path, or null while an async source is still loading.</summary>
        public SplinePath Path
        {
            get
            {
                if (path == null && source != PointSource.StreamingAssetsObj) Rebuild();
                return path;
            }
        }

        public bool IsReady => path != null;

        /// <summary>Total length in meters, or zero if the path is not built yet.</summary>
        public float TotalLength => path != null ? path.TotalLength : 0f;

        private void Awake()
        {
            if (!buildOnAwake) return;

            if (source == PointSource.StreamingAssetsObj)
            {
                if (Application.isPlaying) StartCoroutine(BuildAsync());
                else Rebuild(); // the editor can read StreamingAssets straight off disk
            }
            else
            {
                Rebuild();
            }
        }

        /// <summary>Rebuild from the current source. Synchronous, so not for StreamingAssets on Android.</summary>
        public void Rebuild()
        {
            var points = CollectPoints();
            path = BuildFrom(points);
            if (path != null) Built?.Invoke(path);
        }

        /// <summary>
        /// Build from any source, including the ones that have to go through UnityWebRequest.
        /// Start it as a coroutine.
        /// </summary>
        public IEnumerator BuildAsync(Action<SplinePath> onReady = null)
        {
            if (source != PointSource.StreamingAssetsObj)
            {
                Rebuild();
                onReady?.Invoke(path);
                yield break;
            }

            List<Vector3> loaded = null;
            yield return ObjPathLoader.LoadFromStreamingAssets(streamingAssetsPath, ObjMatrix(), p => loaded = p);

            path = BuildFrom(ApplyObjectTransform(loaded));
            if (path != null) Built?.Invoke(path);
            onReady?.Invoke(path);
        }

        /// <summary>Build straight from points supplied by something else, ignoring the serialized source.</summary>
        public SplinePath BuildFromPoints(List<Vector3> worldPoints)
        {
            path = BuildFrom(worldPoints);
            if (path != null) Built?.Invoke(path);
            return path;
        }

        // ------------------------------------------------------------------ internals

        private SplinePath BuildFrom(List<Vector3> points)
        {
            if (points == null || points.Count < 2)
            {
                if (Application.isPlaying)
                    Debug.LogWarning($"[SplineToolkit] {name}: need at least 2 points, got {points?.Count ?? 0}.", this);
                return null;
            }

            return new SplinePath(points, samplesPerSegment);
        }

        private List<Vector3> CollectPoints()
        {
            switch (source)
            {
                case PointSource.ChildTransforms:
                {
                    var root = childRoot != null ? childRoot : transform;
                    var list = new List<Vector3>(root.childCount);
                    for (int i = 0; i < root.childCount; i++) list.Add(root.GetChild(i).position);
                    return list; // already world space
                }

                case PointSource.InlinePoints:
                    return ApplyObjectTransform(new List<Vector3>(inlinePoints));

                case PointSource.ObjTextAsset:
                {
                    if (objTextAsset == null) return null;
                    return ApplyObjectTransform(ObjPathLoader.ParseVertices(objTextAsset.text, ObjMatrix()));
                }

                case PointSource.StreamingAssetsObj:
                {
                    if (string.IsNullOrEmpty(streamingAssetsPath)) return null;
                    var full = System.IO.Path.Combine(Application.streamingAssetsPath, streamingAssetsPath);
                    return ApplyObjectTransform(ObjPathLoader.LoadFromFile(full, ObjMatrix()));
                }

                default:
                    return null;
            }
        }

        private Matrix4x4 ObjMatrix() => ObjPathLoader.MakeTransform(objTranslate, objRotate, objScale);

        private List<Vector3> ApplyObjectTransform(List<Vector3> points)
        {
            if (points == null || !applyObjectTransform) return points;

            var m = transform.localToWorldMatrix;
            for (int i = 0; i < points.Count; i++) points[i] = m.MultiplyPoint3x4(points[i]);
            return points;
        }

        // ------------------------------------------------------------------ gizmos

        private void OnDrawGizmos()
        {
            var p = path;

            // In edit mode nothing has called Rebuild, so build a throwaway to draw. Cheap enough for a
            // scene-view frame, and it means the curve updates as points are dragged.
            if (p == null && !Application.isPlaying) p = BuildFrom(CollectPoints());
            if (p == null) return;

            float a = Mathf.Clamp01(Mathf.Min(tStart, tEnd));
            float b = Mathf.Clamp01(Mathf.Max(tStart, tEnd));
            if (b <= a) return;

            if (drawCurve)
            {
                Gizmos.color = new Color(curveColor.r, curveColor.g, curveColor.b, lineAlpha);
                Vector3 prev = p.EvaluateAt(a);
                for (int i = 1; i <= gizmoSamples; i++)
                {
                    float t = Mathf.Lerp(a, b, i / (float)gizmoSamples);
                    Vector3 pt = p.EvaluateAt(t);
                    Gizmos.DrawLine(prev, pt);
                    prev = pt;
                }
            }

            if (drawTangents || drawRight || drawUp)
            {
                int frames = Mathf.Max(1, gizmoSamples / 10);
                for (int i = 0; i <= frames; i++)
                {
                    float t = Mathf.Lerp(a, b, i / (float)frames);
                    p.EvaluateFrame(t, out var pos, out var fwd, out var right, out var up, yawOnly: true);

                    if (drawTangents)
                    {
                        Gizmos.color = tangentColor;
                        Gizmos.DrawLine(pos, pos + fwd * tangentLength);
                    }
                    if (drawRight)
                    {
                        Gizmos.color = Color.blue;
                        Gizmos.DrawLine(pos, pos + right * (tangentLength * 0.6f));
                    }
                    if (drawUp)
                    {
                        Gizmos.color = Color.green;
                        Gizmos.DrawLine(pos, pos + up * (tangentLength * 0.6f));
                    }
                }
            }

            if (drawControlPoints && source == PointSource.ChildTransforms)
            {
                var root = childRoot != null ? childRoot : transform;
                Gizmos.color = Color.white;
                for (int i = 0; i < root.childCount; i++)
                    Gizmos.DrawWireSphere(root.GetChild(i).position, controlPointRadius);
            }
        }
    }
}
