#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Recrea los materiales del pack "Resources Vol.2 - Electronics" para URP.
///
/// Escribe los archivos .mat directamente al disco en lugar de usar
/// AssetDatabase.SaveAssets() que no persiste los cambios en este proyecto.
/// Los GUIDs de las texturas se obtienen del AssetDatabase.
///
/// Menú: Tools → TITA → Vol.2 Electronics → Recrear Materiales URP
/// </summary>
public static class Vol2MaterialFixer
{
    const string TEX_ROOT    = "Assets/Resources Vol.2 - Electronics/Textures";
    const string MAT_ROOT    = "Assets/Resources Vol.2 - Electronics/Materials";
    const string URP_LIT_GUID = "933532a4fcc9baf4fa0491de14d08ed7";

    // Colores por letra de LED (A=Verde, B=Azul, C=Cyan, D=Amarillo, E=Rojo)
    static readonly Dictionary<string, string> LedColorMap = new()
    {
        { "Led A", "Led Green_BaseMap" },
        { "Led B", "Led Blue_BaseMap"  },
        { "Led C", "Led Cyan_BaseMap"  },
        { "Led D", "Led Yellow_BaseMap"},
        { "Led E", "Led Red_BaseMap"   },
    };

    // Mapa: nombre de material → subcarpeta de texturas
    static readonly Dictionary<string, string> MatToFolder = new()
    {
        { "Antenna",            "Antenna"             },
        { "Bareboard",          "Bareboard"           },
        { "Battery 9v",         "Battery 9v"          },
        { "Battery Circle",     "Battery Circle"      },
        { "Button A",           "Button"              },
        { "Button B",           "Button"              },
        { "Button C",           "Button"              },
        { "Button D",           "Button"              },
        { "Capacitor A",        "Capacitor A"         },
        { "Circuit Board A",    "Circuit Board A"     },
        { "Coil",               "Coil"                },
        { "Controller Board",   "Controller Board"    },
        { "Fan",                "Fan"                 },
        { "Led A",              "Led"                 },
        { "Led B",              "Led"                 },
        { "Led C",              "Led"                 },
        { "Led D",              "Led"                 },
        { "Led E",              "Led"                 },
        { "Led Matrix A",       "Led Matrix"          },
        { "Led Matrix B",       "Led Matrix"          },
        { "Microchip A",        "Microchip A"         },
        { "Microphone",         "Microphone"          },
        { "Palette Plastic",    "Plastic Container A" },
        { "Plastic Container A","Plastic Container A" },
        { "Plastic Container B","Plastic Container B" },
        { "Plywood",            "Plywood"             },
        { "Potentiometer",      "Potentiometer"       },
        { "Relay",              "Relay"               },
        { "Segment Display",    "Segment display"     },
        { "Solderpad A",        "Solderingpad"        },
        { "Solderpad B",        "Solderpad B"         },
        { "Stepper Motor",      "Stepper Motor"       },
        { "Switch",             "Switch"              },
        { "Transistor A",       "Transistor A"        },
        { "Wire A",             "Wire A"              },
    };

    // ─────────────────────────────────────────────
    [MenuItem("Tools/TITA/Vol.2 Electronics/Recrear Materiales URP")]
    public static void FixAll()
    {
        FixAllSilent(out int ok, out int skipped);
        Debug.Log($"[Vol2MaterialFixer] ✓ {ok} materiales reconstruidos, {skipped} omitidos.");
        EditorUtility.DisplayDialog("Materiales reconstruidos",
            $"{ok} materiales Vol.2 reconstruidos con shader URP/Lit.\n\n" +
            "Los prefabs y escenas deberían mostrar las skins ahora.\n" +
            "Si algún modelo sigue rosa, reimporta el prefab con\n" +
            "botón derecho → Reimport.", "OK");
    }

    /// <summary>
    /// Escribe los materiales directamente al disco sin diálogos.
    /// Llamar desde otros Editor tools antes de construir la escena.
    /// </summary>
    public static void FixAllSilent(out int ok, out int skipped)
    {
        ok = 0; skipped = 0;

        EditorUtility.DisplayProgressBar("Recreando materiales Vol.2 para URP", "", 0f);
        try
        {
            var entries = new List<KeyValuePair<string, string>>(MatToFolder);
            for (int i = 0; i < entries.Count; i++)
            {
                string matName   = entries[i].Key;
                string texFolder = entries[i].Value;
                EditorUtility.DisplayProgressBar("Recreando materiales Vol.2 para URP",
                    matName, (float)i / entries.Count);

                if (WriteURPMaterial(matName, texFolder)) ok++;
                else                                      skipped++;
            }
            AssetDatabase.Refresh();
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    // ─────────────────────────────────────────────
    //  Core — escribe el .mat YAML directamente al disco
    // ─────────────────────────────────────────────

    static bool WriteURPMaterial(string matName, string texFolderName)
    {
        string texFolder  = $"{TEX_ROOT}/{texFolderName}";
        string matPath    = $"{MAT_ROOT}/{matName}.mat";
        string absMatPath = Path.GetFullPath(matPath);

        // Asegurar que el archivo .mat exista (para preservar su .meta / GUID)
        if (!File.Exists(absMatPath))
        {
            Debug.LogWarning($"[Vol2MaterialFixer] No existe: {matPath} — omitido.");
            return false;
        }

        // BaseMap: LEDs tienen color específico, el resto busca *_BaseMap.png
        string baseGuid = null;
        if (LedColorMap.TryGetValue(matName, out string ledColorFile))
        {
            baseGuid = GUIDFor($"{texFolder}/{ledColorFile}.png");
            if (baseGuid == null) baseGuid = FindTexGUID(texFolder, "_BaseMap");
        }
        else
        {
            baseGuid = FindTexGUID(texFolder, "_BaseMap");
        }

        if (baseGuid == null)
        {
            Debug.LogWarning($"[Vol2MaterialFixer] Sin BaseMap para '{matName}' en {texFolder}");
            return false;
        }

        string normalGuid = FindTexGUID(texFolder, "_Normal");
        string aoGuid     = FindTexGUID(texFolder, "_AO");
        bool   hasNormal  = normalGuid != null;
        bool   hasAO      = aoGuid     != null;

        string keywords  = hasNormal ? "_NORMALMAP" : "";
        string baseRef   = TexRef(baseGuid);
        string normalRef = hasNormal ? TexRef(normalGuid) : NullRef();
        string aoRef     = hasAO     ? TexRef(aoGuid)     : NullRef();

        string yaml = BuildYAML(matName, keywords, baseRef, normalRef, aoRef);
        File.WriteAllText(absMatPath, yaml, new UTF8Encoding(false)); // sin BOM
        AssetDatabase.ImportAsset(matPath, ImportAssetOptions.ForceUpdate);
        return true;
    }

    // ─────────────────────────────────────────────
    //  YAML builder
    // ─────────────────────────────────────────────

    static string BuildYAML(string matName, string keywords,
                            string baseRef, string normalRef, string aoRef)
    {
        // Nota: {{ y }} producen { y } en strings interpolados de C#.
        // Las variables baseRef/normalRef/aoRef ya contienen el texto {fileID:...} correcto.
        return
$@"%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!21 &2100000
Material:
  serializedVersion: 6
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: {matName}
  m_Shader: {{fileID: 4800000, guid: {URP_LIT_GUID}, type: 3}}
  m_ShaderKeywords: {keywords}
  m_LightmapFlags: 4
  m_EnableInstancingVariants: 0
  m_DoubleSidedGI: 0
  m_CustomRenderQueue: -1
  stringTagMap:
    RenderType: Opaque
  disabledShaderPasses: []
  m_SavedProperties:
    serializedVersion: 3
    m_TexEnvs:
    - _BaseMap:
        m_Texture: {baseRef}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _BumpMap:
        m_Texture: {normalRef}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _EmissionMap:
        m_Texture: {{fileID: 0}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _MetallicGlossMap:
        m_Texture: {{fileID: 0}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _OcclusionMap:
        m_Texture: {aoRef}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _ParallaxMap:
        m_Texture: {{fileID: 0}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    m_Floats:
    - _AlphaClip: 0
    - _BumpScale: 1
    - _Metallic: 0
    - _OcclusionStrength: 1
    - _Smoothness: 0.25
    - _Surface: 0
    - _WorkflowMode: 1
    - _ZWrite: 1
    m_Colors:
    - _BaseColor: {{r: 1, g: 1, b: 1, a: 1}}
    - _EmissionColor: {{r: 0, g: 0, b: 0, a: 1}}
  m_BuildTextureStacks: []
";
    }

    // ─────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────

    static string TexRef(string guid)  => $"{{fileID: 2800000, guid: {guid}, type: 3}}";
    static string NullRef()            => "{fileID: 0}";

    /// <summary>Devuelve el GUID de la primera textura cuyo nombre termine con suffix.</summary>
    static string FindTexGUID(string folder, string suffix)
    {
        string absFolder = Path.GetFullPath(folder);
        if (!Directory.Exists(absFolder)) return null;

        foreach (var file in Directory.GetFiles(absFolder, "*.png"))
        {
            string name = Path.GetFileNameWithoutExtension(file);
            if (name.EndsWith(suffix, System.StringComparison.OrdinalIgnoreCase))
            {
                string assetPath = folder + "/" + Path.GetFileName(file);
                return GUIDFor(assetPath);
            }
        }
        return null;
    }

    static string GUIDFor(string assetPath)
    {
        string guid = AssetDatabase.AssetPathToGUID(assetPath);
        return string.IsNullOrEmpty(guid) ? null : guid;
    }
}
#endif
