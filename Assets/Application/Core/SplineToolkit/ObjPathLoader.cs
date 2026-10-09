using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace SplineToolkit
{
    /// <summary>
    /// Reads control points out of a Wavefront .obj file: the vertex lines, in file order, transformed
    /// into world space.
    /// </summary>
    /// <remarks>
    /// <para>Drawing a route as a curve or a polyline in a modelling tool and exporting it as .obj is a
    /// faster way to author a long path than dragging transforms in the Unity scene, and it survives the
    /// level geometry being re-exported alongside it.</para>
    ///
    /// <para>Only the <c>v</c> lines are read, and their file order is the path order. Every exporter the
    /// loader has met writes the vertices of a single open curve in order; a mesh with several separate
    /// pieces, or a curve the exporter chose to reorder, will come back scrambled. Check the gizmo after
    /// the first import from any new tool.</para>
    ///
    /// <para>Parsing is culture-invariant on purpose. A machine set to a comma decimal separator silently
    /// reads every coordinate as zero otherwise.</para>
    /// </remarks>
    public static class ObjPathLoader
    {
        /// <summary>Build the matrix applied to every parsed vertex.</summary>
        public static Matrix4x4 MakeTransform(Vector3 translate, Vector3 eulerRotate, Vector3 scale)
            => Matrix4x4.TRS(translate, Quaternion.Euler(eulerRotate), scale);

        /// <summary>Parse the whole text of an .obj file.</summary>
        public static List<Vector3> ParseVertices(string objText, Matrix4x4 transform)
        {
            if (string.IsNullOrEmpty(objText)) return new List<Vector3>();
            return ParseVertices(objText.Split('\n'), transform);
        }

        /// <summary>Parse .obj lines that have already been read.</summary>
        public static List<Vector3> ParseVertices(string[] lines, Matrix4x4 transform)
        {
            var points = new List<Vector3>();
            if (lines == null) return points;

            foreach (string line in lines)
            {
                if (!line.StartsWith("v ")) continue;

                var tokens = line.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length >= 4 &&
                    float.TryParse(tokens[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) &&
                    float.TryParse(tokens[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float y) &&
                    float.TryParse(tokens[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float z))
                {
                    points.Add(transform.MultiplyPoint(new Vector3(x, y, z)));
                }
            }

            return points;
        }

        /// <summary>Read an .obj from an absolute path on disk. Not valid on Android or WebGL.</summary>
        public static List<Vector3> LoadFromFile(string absolutePath, Matrix4x4 transform)
        {
            if (!File.Exists(absolutePath))
            {
                Debug.LogError("[SplineToolkit] OBJ file not found at: " + absolutePath);
                return new List<Vector3>();
            }

            return ParseVertices(File.ReadAllLines(absolutePath), transform);
        }

        /// <summary>
        /// Read an .obj from StreamingAssets. Start it as a coroutine.
        /// </summary>
        /// <param name="relativePath">Path under StreamingAssets, e.g. "Paths/TrackA.obj".</param>
        /// <remarks>
        /// On Android StreamingAssets lives inside the compressed APK, where no file path exists and only
        /// UnityWebRequest can reach it. Every other platform reads the file directly.
        /// </remarks>
        public static IEnumerator LoadFromStreamingAssets(string relativePath, Matrix4x4 transform,
            Action<List<Vector3>> onComplete)
        {
            if (string.IsNullOrEmpty(relativePath))
            {
                Debug.LogError("[SplineToolkit] No StreamingAssets path given.");
                onComplete?.Invoke(new List<Vector3>());
                yield break;
            }

            var path = Path.Combine(Application.streamingAssetsPath, relativePath);

#if UNITY_ANDROID && !UNITY_EDITOR
            using (UnityWebRequest www = UnityWebRequest.Get(path))
            {
                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("[SplineToolkit] Failed to load OBJ file: " + www.error);
                    onComplete?.Invoke(new List<Vector3>());
                    yield break;
                }

                onComplete?.Invoke(ParseVertices(www.downloadHandler.text, transform));
            }
#else
            onComplete?.Invoke(LoadFromFile(path, transform));
            yield break;
#endif
        }
    }
}
