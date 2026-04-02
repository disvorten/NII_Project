using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public sealed class HalfCylinderScreenMesh : MonoBehaviour
{
    [Header("Shape")]
    [Min(0.01f)] public float radius = 2f;
    [Min(0.01f)] public float height = 2f;
    [Range(10f, 360f)] public float arcDegrees = 180f;

    [Header("Rounding (Top/Bottom)")]
    [Tooltip("Enable to curve the surface near top/bottom (reduces radius toward edges).")]
    public bool roundTop = false;
    public bool roundBottom = false;

    [Tooltip("Vertical size of the rounded zone (world units).")]
    [Min(0f)] public float roundHeight = 0.35f;

    [Tooltip("How much radius is reduced at the very top/bottom (world units).")]
    [Min(0f)] public float roundInset = 0.35f;

    [Header("Tessellation")]
    [Min(3)] public int arcSegments = 64;
    [Min(1)] public int heightSegments = 1;

    [Header("Caps")]
    public bool capTop = false;
    public bool capBottom = false;
    public bool capLeft = false;
    public bool capRight = false;

    [Header("Orientation")]
    [Tooltip("If enabled, normals face inward (good for looking from inside).")]
    public bool normalsInward = true;

    [Tooltip("Rotate the mesh around Y (degrees). Useful to align the 180° opening.")]
    [Range(-180f, 180f)] public float yawDegrees = 0f;

    [Header("UV")]
    public bool flipU = false;
    public bool flipV = false;

    MeshFilter _mf;
    Mesh _mesh;

    public void RebuildNow()
    {
        EnsureMesh();
        Rebuild();
    }

    void OnEnable()
    {
        EnsureMesh();
        Rebuild();
    }

    void OnDisable()
    {
        // Keep mesh asset-less and tied to the component.
        if (_mesh != null)
        {
            if (Application.isPlaying) Destroy(_mesh);
            else DestroyImmediate(_mesh);
        }
    }

    void OnValidate()
    {
        radius = Mathf.Max(0.01f, radius);
        height = Mathf.Max(0.01f, height);
        arcDegrees = Mathf.Clamp(arcDegrees, 10f, 360f);
        arcSegments = Mathf.Max(3, arcSegments);
        heightSegments = Mathf.Max(1, heightSegments);
        roundHeight = Mathf.Max(0f, roundHeight);
        roundInset = Mathf.Max(0f, roundInset);

        EnsureMesh();
        Rebuild();
    }

    void EnsureMesh()
    {
        if (_mf == null) _mf = GetComponent<MeshFilter>();
        if (_mesh == null)
        {
            _mesh = new Mesh { name = "HalfCylinderScreenMesh (Generated)" };
            _mesh.MarkDynamic();
        }
        if (_mf.sharedMesh != _mesh) _mf.sharedMesh = _mesh;
    }

    void Rebuild()
    {
        if (_mesh == null) return;

        var verts = new List<Vector3>(8192);
        var norms = new List<Vector3>(8192);
        var uvs = new List<Vector2>(8192);
        var trisSurface = new List<int>(16384);
        var trisCaps = new List<int>(8192);

        float arcRad = Mathf.Deg2Rad * arcDegrees;
        float halfArc = arcRad * 0.5f;
        float yaw = Mathf.Deg2Rad * yawDegrees;

        float yMin = -height * 0.5f;
        float yMax = height * 0.5f;

        float RadiusAtY(float y)
        {
            float r = radius;
            float inset = Mathf.Min(roundInset, radius - 0.0001f);
            float rh = Mathf.Min(roundHeight, height * 0.5f);

            if (rh > 0f && roundTop)
            {
                float t = Mathf.InverseLerp(yMax - rh, yMax, y);
                float s = t <= 0f ? 0f : (t >= 1f ? 1f : t * t * (3f - 2f * t)); // smoothstep
                r -= inset * s;
            }

            if (rh > 0f && roundBottom)
            {
                float t = Mathf.InverseLerp(yMin + rh, yMin, y);
                float s = t <= 0f ? 0f : (t >= 1f ? 1f : t * t * (3f - 2f * t)); // smoothstep
                r -= inset * s;
            }

            return Mathf.Max(0.0001f, r);
        }

        int V(int a, int h) => (h * (arcSegments + 1)) + a;

        // Side surface (cylindrical wall)
        for (int h = 0; h <= heightSegments; h++)
        {
            float tV = heightSegments == 0 ? 0f : (float)h / heightSegments;
            float y = Mathf.Lerp(yMin, yMax, tV);
            float rY = RadiusAtY(y);

            for (int a = 0; a <= arcSegments; a++)
            {
                float tU = (float)a / arcSegments;
                float ang = Mathf.Lerp(-halfArc, halfArc, tU) + yaw;

                float x = Mathf.Sin(ang) * rY;
                float z = Mathf.Cos(ang) * rY;

                verts.Add(new Vector3(x, y, z));

                Vector3 outward = new Vector3(x, 0f, z).normalized;
                norms.Add(normalsInward ? -outward : outward);

                float u = flipU ? (1f - tU) : tU;
                float v = flipV ? (1f - tV) : tV;
                uvs.Add(new Vector2(u, v));
            }
        }

        for (int h = 0; h < heightSegments; h++)
        {
            for (int a = 0; a < arcSegments; a++)
            {
                int i0 = V(a, h);
                int i1 = V(a + 1, h);
                int i2 = V(a, h + 1);
                int i3 = V(a + 1, h + 1);

                if (normalsInward)
                {
                    // Flip winding for inward-facing normals.
                    trisSurface.Add(i0); trisSurface.Add(i2); trisSurface.Add(i1);
                    trisSurface.Add(i1); trisSurface.Add(i2); trisSurface.Add(i3);
                }
                else
                {
                    trisSurface.Add(i0); trisSurface.Add(i1); trisSurface.Add(i2);
                    trisSurface.Add(i1); trisSurface.Add(i3); trisSurface.Add(i2);
                }
            }
        }

        // Caps (top/bottom) as half-disks, UV mapped to [0..1] with center at (0.5,0.5).
        void AddCap(bool top)
        {
            float y = top ? yMax : yMin;
            Vector3 normal = top ? Vector3.up : Vector3.down;
            if (normalsInward) normal = -normal;

            int centerIndex = verts.Count;
            verts.Add(new Vector3(0f, y, 0f));
            norms.Add(normal);
            uvs.Add(new Vector2(0.5f, 0.5f));

            int ringStart = verts.Count;
            for (int a = 0; a <= arcSegments; a++)
            {
                float tU = (float)a / arcSegments;
                float ang = Mathf.Lerp(-halfArc, halfArc, tU) + yaw;
                float rY = RadiusAtY(y);
                float x = Mathf.Sin(ang) * rY;
                float z = Mathf.Cos(ang) * rY;

                verts.Add(new Vector3(x, y, z));
                norms.Add(normal);

                // Project x/z into UV circle.
                float u = 0.5f + (x / (2f * Mathf.Max(radius, 0.0001f)));
                float v = 0.5f + (z / (2f * Mathf.Max(radius, 0.0001f)));
                if (flipU) u = 1f - u;
                if (flipV) v = 1f - v;
                uvs.Add(new Vector2(u, v));
            }

            for (int a = 0; a < arcSegments; a++)
            {
                int i0 = centerIndex;
                int i1 = ringStart + a;
                int i2 = ringStart + a + 1;

                // Winding depends on which cap and inward/outward.
                bool flip = normalsInward;
                if (!top) flip = !flip; // bottom cap reverses relative to top

                if (flip)
                {
                    trisCaps.Add(i0); trisCaps.Add(i2); trisCaps.Add(i1);
                }
                else
                {
                    trisCaps.Add(i0); trisCaps.Add(i1); trisCaps.Add(i2);
                }
            }
        }

        if (capTop) AddCap(top: true);
        if (capBottom) AddCap(top: false);

        // Side caps (left/right) to close the 180° cut.
        // Each side cap is a rectangle from the center line (x=z=0) to the cylinder edge line.
        void AddSideCap(bool left)
        {
            float ang = (left ? -halfArc : halfArc) + yaw;

            // Tangent along increasing angle.
            Vector3 tangent = new Vector3(Mathf.Cos(ang), 0f, -Mathf.Sin(ang)).normalized;
            // For an inward-facing screen, the "inside" normal points toward the interior of the arc.
            Vector3 normal = left ? tangent : -tangent;
            if (!normalsInward) normal = -normal;

            int start = verts.Count;
            for (int h = 0; h <= heightSegments; h++)
            {
                float tV = heightSegments == 0 ? 0f : (float)h / heightSegments;
                float y = Mathf.Lerp(yMin, yMax, tV);
                float rY = RadiusAtY(y);
                float xEdge = Mathf.Sin(ang) * rY;
                float zEdge = Mathf.Cos(ang) * rY;

                // Center line vertex
                verts.Add(new Vector3(0f, y, 0f));
                norms.Add(normal);
                float v = flipV ? (1f - tV) : tV;
                uvs.Add(new Vector2(flipU ? 1f : 0f, v));

                // Edge vertex
                verts.Add(new Vector3(xEdge, y, zEdge));
                norms.Add(normal);
                uvs.Add(new Vector2(flipU ? 0f : 1f, v));
            }

            for (int h = 0; h < heightSegments; h++)
            {
                int i0 = start + (h * 2);
                int i1 = i0 + 1;
                int i2 = start + ((h + 1) * 2);
                int i3 = i2 + 1;

                // Winding based on normalsInward.
                if (normalsInward)
                {
                    trisSurface.Add(i0); trisSurface.Add(i3); trisSurface.Add(i1);
                    trisSurface.Add(i0); trisSurface.Add(i2); trisSurface.Add(i3);
                }
                else
                {
                    trisSurface.Add(i0); trisSurface.Add(i1); trisSurface.Add(i3);
                    trisSurface.Add(i0); trisSurface.Add(i3); trisSurface.Add(i2);
                }
            }
        }

        if (capLeft) AddSideCap(left: true);
        if (capRight) AddSideCap(left: false);

        _mesh.Clear(keepVertexLayout: false);
        _mesh.SetVertices(verts);
        _mesh.SetNormals(norms);
        _mesh.SetUVs(0, uvs);
        _mesh.subMeshCount = 2;
        _mesh.SetTriangles(trisSurface, 0, calculateBounds: false);
        _mesh.SetTriangles(trisCaps, 1, calculateBounds: true);
        _mesh.RecalculateBounds();
    }
}

