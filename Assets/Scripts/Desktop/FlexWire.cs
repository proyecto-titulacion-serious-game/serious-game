using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cable flexible editable en Scene View.
/// Genera un tubo Catmull-Rom a partir de una lista de puntos de control locales.
/// Mueve los puntos con los handles en FlexWireEditor (azul en Scene View).
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class FlexWire : MonoBehaviour
{
    [Header("Puntos de control (espacio local)")]
    [SerializeField] List<Vector3> controlPoints = new()
    {
        new Vector3(-0.10f, 0f,  0f),
        new Vector3(-0.03f, 0.04f, 0f),
        new Vector3( 0.03f, 0.04f, 0f),
        new Vector3( 0.10f, 0f,  0f),
    };

    [Header("Apariencia")]
    [SerializeField] Color  wireColor        = Color.red;
    [SerializeField] float  radius           = 0.006f;
    [SerializeField, Range(4, 16)] int sides           = 8;
    [SerializeField, Range(1, 20)] int stepsPerSegment = 5;

    // ── Internos ─────────────────────────────────────────────────────
    MeshFilter   _mf;
    MeshRenderer _mr;
    Material     _mat;
    Mesh         _mesh;
    Color        _builtColor;

    int          _lastHash;          // hash del estado previo para evitar rebuilds inutiles
    bool         _needsRebuild = true;

    public List<Vector3> Points => controlPoints;

    /// <summary>Marca el cable para reconstruir en el proximo Rebuild() aunque el hash no cambie.</summary>
    public void SetDirty() => _needsRebuild = true;

    // ── Lifecycle ────────────────────────────────────────────────────

    void OnEnable()
    {
        _mf = GetComponent<MeshFilter>();
        _mr = GetComponent<MeshRenderer>();
        Rebuild();
    }

    // OnValidate se llama desde el Inspector — NO se puede usar DestroyImmediate aquí.
    // Reusamos los objetos existentes en vez de destruirlos.
    void OnValidate() => Rebuild();

    void OnDestroy()
    {
        // OnDestroy sí permite DestroyImmediate
        if (_mat  != null) DestroyImmediate(_mat);
        if (_mesh != null) DestroyImmediate(_mesh);
    }

    // ── API pública ───────────────────────────────────────────────────

    public void Rebuild()
    {
        if (controlPoints == null || controlPoints.Count < 2) return;

        // Evitar reconstruccion si nada cambio (importante cuando el editor llama Rebuild() cada frame al arrastrar)
        int currentHash = ComputeHash();
        if (!_needsRebuild && currentHash == _lastHash) return;
        _lastHash     = currentHash;
        _needsRebuild = false;

        _mf = _mf ? _mf : GetComponent<MeshFilter>();
        _mr = _mr ? _mr : GetComponent<MeshRenderer>();

        // ── Material: reusar, actualizar color si cambió ──────────────
        if (_mat == null)
        {
            var sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            _mat            = new Material(sh) { hideFlags = HideFlags.HideAndDontSave };
            _builtColor     = wireColor - Color.white; // forzar primera actualización
        }
        if (_builtColor != wireColor)
        {
            _mat.SetColor("_BaseColor", wireColor);
            _mat.color  = wireColor;
            _builtColor = wireColor;
        }
        _mr.sharedMaterial = _mat;

        // ── Mesh: reusar, llamar Clear() y reasignar datos ────────────
        if (_mesh == null)
            _mesh = new Mesh { name = "FlexWireMesh", hideFlags = HideFlags.HideAndDontSave };

        FillTube(_mesh, BuildSpine());
        _mf.sharedMesh = _mesh;
    }

    // ── Hash de estado ────────────────────────────────────────────────

    int ComputeHash()
    {
        var h = new System.HashCode();
        h.Add(radius);
        h.Add(sides);
        h.Add(stepsPerSegment);
        h.Add(wireColor.GetHashCode());
        foreach (var p in controlPoints) h.Add(p.GetHashCode());
        return h.ToHashCode();
    }

    // ── Spline Catmull-Rom ────────────────────────────────────────────

    List<Vector3> BuildSpine()
    {
        var pts = new List<Vector3>();
        int n = controlPoints.Count;
        for (int i = 0; i < n - 1; i++)
        {
            var p0 = controlPoints[Mathf.Max(i - 1, 0)];
            var p1 = controlPoints[i];
            var p2 = controlPoints[i + 1];
            var p3 = controlPoints[Mathf.Min(i + 2, n - 1)];
            for (int s = 0; s < stepsPerSegment; s++)
                pts.Add(CatmullRom(p0, p1, p2, p3, s / (float)stepsPerSegment));
        }
        pts.Add(controlPoints[n - 1]);
        return pts;
    }

    static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t, t3 = t2 * t;
        return 0.5f * (2f * p1
            + (-p0 + p2) * t
            + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2
            + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
    }

    // ── Generación del tubo (reutiliza el Mesh existente) ─────────────

    void FillTube(Mesh mesh, List<Vector3> spine)
    {
        mesh.Clear();

        int rings  = spine.Count;
        int vpRing = sides + 1;

        var verts = new Vector3[rings * vpRing];
        var norms = new Vector3[rings * vpRing];
        var uvs   = new Vector2[rings * vpRing];

        for (int r = 0; r < rings; r++)
        {
            Vector3 fwd = r < rings - 1
                ? (spine[r + 1] - spine[r]).normalized
                : (spine[r]     - spine[r - 1]).normalized;

            Quaternion frame = Mathf.Abs(Vector3.Dot(fwd, Vector3.up)) > 0.99f
                ? Quaternion.LookRotation(fwd, Vector3.right)
                : Quaternion.LookRotation(fwd, Vector3.up);

            for (int s = 0; s <= sides; s++)
            {
                float a = s / (float)sides * Mathf.PI * 2f;
                var   c = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                int   i = r * vpRing + s;
                verts[i] = spine[r] + frame * (c * radius);
                norms[i] = frame * c;
                uvs[i]   = new Vector2(s / (float)sides, r / (float)(rings - 1));
            }
        }

        var tris = new int[(rings - 1) * sides * 6];
        int ti = 0;
        for (int r = 0; r < rings - 1; r++)
            for (int s = 0; s < sides; s++)
            {
                int a = r * vpRing + s, b = a + 1;
                int c2 = a + vpRing,   d = c2 + 1;
                tris[ti++] = a;  tris[ti++] = c2; tris[ti++] = b;
                tris[ti++] = b;  tris[ti++] = c2; tris[ti++] = d;
            }

        mesh.vertices  = verts;
        mesh.normals   = norms;
        mesh.uv        = uvs;
        mesh.triangles = tris;
        mesh.RecalculateBounds();
    }
}
