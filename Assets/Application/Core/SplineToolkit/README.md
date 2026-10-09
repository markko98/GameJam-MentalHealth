# Spline Toolkit

An arc-length Catmull-Rom path, plus the glue that makes it usable in a scene. No dependencies beyond
UnityEngine, no editor scripts, one assembly definition (`SplineToolkit`) that the predefined
`Assembly-CSharp` references automatically.

## Why arc-length

A raw Catmull-Rom evaluated by its own parameter races through long segments and crawls through short
ones. Anything that rides the curve at a speed - a vehicle, a camera, pickups laid out every 10 m -
needs distance, not parameter. Every method here takes `t` as **normalized arc-length**: `t = 0.5` is
the halfway point *in meters*, and `TotalLength` is in world units.

## Files

| File | What it is |
| --- | --- |
| `CatmullRomSpline.cs` | The curve and its arc-length lookup table. Plain C#, no scene needed. |
| `SplinePath.cs` | What you actually call: positions, tangents, frames, range maths, closest-point. |
| `ObjPathLoader.cs` | Reads control points out of a Wavefront `.obj`. |
| `SplinePathSource.cs` | MonoBehaviour: names the point source, builds the path, draws it as a gizmo. |

## Quick start, in the scene

Add **Spline Toolkit > Spline Path Source** to an empty object, leave the source on
`ChildTransforms`, and parent a few empties under it. The curve draws in the scene view as you drag
them. Then, from your own code:

```csharp
[SerializeField] private SplinePathSource pathSource;

private void Update()
{
    var path = pathSource.Path;
    distance += speed * Time.deltaTime;

    float t = path.TAtDistance(distance);
    transform.position = path.EvaluateAt(t);
    transform.rotation = path.EvaluateRotation(t);
}
```

## Quick start, from code only

```csharp
var path = new SplinePath(points, samplesPerSegment: 60);
```

`points` is any world-space `List<Vector3>` with at least two entries. Raise `samplesPerSegment` for
tight bends, lower it for long gentle curves; it only costs memory at construction.

## Authoring the path in a modelling tool

Draw the route as a curve or polyline, export as `.obj`, and read the vertex lines back:

```csharp
var m = ObjPathLoader.MakeTransform(translate, rotate, scale);
var points = ObjPathLoader.ParseVertices(objTextAsset.text, m);
```

Only `v` lines are read, **in file order**. That is what every exporter tested writes for a single
open curve, but a mesh in several pieces, or an exporter that reorders vertices, comes back scrambled.
Look at the gizmo the first time you import from a new tool.

Exported paths usually arrive rotated and scaled wrong. The import transform on the component is where
you fix that; `180, 180, 0` is the rotation that has fixed it most often.

For a path that ships as data rather than as an imported asset, put the `.obj` under
`StreamingAssets`, set the source to `StreamingAssetsObj`, and let `BuildAsync` run - on Android that
file lives inside the compressed APK and only `UnityWebRequest` can reach it, which the loader handles.

## Things worth knowing

**Tangents use a meter-based epsilon, not a delta in `t`.** On a doubled control point or a hairpin,
the chord across the epsilon can collapse to nothing. Rather than return world forward - a direction
with no relation to the curve, which points whatever asked for it backwards - the epsilon widens and
retries twice.

**There are two closest-point queries and they are not interchangeable.** `GetClosestTAhead` searches
a small window ahead of where you already are: bounded cost, and progress that cannot jump. Use it
every frame. `GetClosestT` sweeps the whole curve; use it once, for placement. On a path that switches
back on itself, an unbounded per-frame search will eventually find a nearer point far down the track
and report progress the follower never made.

**`EvaluateFrame(yawOnly: true)` keeps placed objects level** by flattening the tangent onto world up.
Pass `false` for a full frame that rolls with the curve - tunnels, loops, banked turns.

**Ranges let a section own its own meters.** `RangeTAtDistance(range, 12f)` is "12 meters into this
stretch", which is how you lay content out inside a sub-section without rescaling anything by hand.
