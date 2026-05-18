#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Recrea los materiales del pack "Resources Vol.2 - Electronics" para URP.
///
/// Los materiales originales del pack usan Standard (Specular) shader pero
/// sus texturas ya están nombradas con convención URP (_BaseMap, _Normal,
/// _AO, _Roughness). El Render Pipeline Converter no resuelve esto. Este
/// script ignora los .mat originales y crea materiales URP/Lit directamente
/// desde las carpetas de texturas.
///
/// Menú: Tools → TITA → Vol.2 Electronics → Recrear Materiales URP
/// </summary>
public static class Vol2MaterialFixer
{
    const string TEX_ROOT = "Assets/Resources Vol.2 - Electronics/Textures";
    const string MAT_ROOT = "Assets/Resources Vol.2 - Electronics/Materials";

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
        { "Antenna",           "Antenna"            },
        { "Bareboard",         "Bareboard"          },
        { "Battery 9v",        "Battery 9v"         },
        { "Battery Circle",    "Battery Circle"     },
        { "Button A",          "Button"             },
        { "Button B",          "Button"             },
        { "Button C",          "Button"             },
        { "Button D",          "Button"             },
        { "Capacitor A",       "Capacitor A"        },
        { "Circuit Board A",   "Circuit Board A"    },
        { "Coil",              "Coil"               },
        { "Controller Board",  "Controller Board"   },
        { "Fan",               "Fan"                },
        { "Led A",             "Led"                },
        { "Led B",             "Led"                },
        { "Led C",             "Led"                },
        { "Led D",             "Led"                },
        { "Led E",             "Led"                },
        { "Led Matrix A",      "Led Matrix"         },
        { "Led Matrix B",      "Led Matrix"         },
        { "Microchip A",       "Microchip A"        },
        { "Microphone",        "Microphone"         },
        { "Palette Plastic",   "Plastic Container A"},
        { "Plastic Container A","Plastic Container A"},
        { "Plastic Container B","Plastic Container B"},
        { "Plywood",           "Plywood"            },
        { "Potentiometer",     "Potentiometer"      },
        { "Relay",             "Relay"              },
        { "Segment Display",   "Segment display"    },
        { "Solderpad A",       "Solderingpad"       },
        { "Solderpad B",       "Solderpad B"        },
        { "Stepper Motor",     "Stepper Motor"      },
        { "Switch",            "Switch"             },
        { "Transistor A",      "Transistor A"       },
        { "Wire A",            "Wire A"             },
    };

    // ─────────────────────────────────────────────
    [MenuItem("Tools/TITA/Vol.2 Electronics/Recrear Materiales URP")]
    public static void FixAll()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            EditorUtility.DisplayDialog("Error",
                "No se encontró 'Universal Render Pipeline/Lit'.\n" +
                "Asegúrate de que URP esté instalado.", "OK");
            return;
        }

        int ok = 0, skipped = 0;
        EditorUtility.DisplayProgressBar("Recreando materiales Vol.2 para URP", "", 0f);

        try
        {
            var entries = new List<KeyValuePair<string, string>>(MatToFolder);
            for (int i = 0; i < entries.Count; i++)
            {
                string matName    = entries[i].Key;
                string texFolder  = entries[i].Value;
                EditorUtility.DisplayProgressBar("Recreando materiales Vol.2 para URP",
                    matName, (float)i / entries.Count);

                if (RebuildMaterial(matName, texFolder, urpLit))
                    ok++;
                else
                    skipped++;
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        Debug.Log($"[Vol2MaterialFixer] ✓ {ok} materiales reconstruidos, {skipped} omitidos.");
        EditorUtility.DisplayDialog("Materiales reconstruidos",
            $"{ok} materiales Vol.2 reconstruidos con shader URP/Lit.\n\n" +
            "Los prefabs y escenas deberían mostrar las skins ahora.\n" +
            "Si algún modelo sigue rosa, reimporta el prefab con\n" +
            "botón derecho → Reimport.", "OK");
    }

    // ─────────────────────────────────────────────
    static bool RebuildMaterial(string matName, string texFolderName, Shader urpLit)
    {
        string matPath = $"{MAT_ROOT}/{matName}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            // Crear nuevo si no existe
            mat = new Material(urpLit) { name = matName };
            AssetDatabase.CreateAsset(mat, matPath);
        }
        else
        {
            mat.shader = urpLit;
        }

        string texFolder = $"{TEX_ROOT}/{texFolderName}";

        // Albedo: para LED usar el color según letra; para el resto buscar *_BaseMap.png
        Texture2D baseMap = null;
        if (LedColorMap.TryGetValue(matName, out string ledColorFile))
        {
            baseMap = LoadTex($"{texFolder}/{ledColorFile}.png");
            // Fallback a cualquier BaseMap si el color específico no existe
            if (baseMap == null) baseMap = FindTex(texFolder, "_BaseMap");
        }
        else
        {
            baseMap = FindTex(texFolder, "_BaseMap");
        }

        Texture2D normalMap  = FindTex(texFolder, "_Normal");
        Texture2D aoMap      = FindTex(texFolder, "_AO");
        // _Roughness existe pero URP usa Smoothness (inverso). Lo ignoramos
        // y fijamos un valor razonable para electrónica (~0.25 = bastante rugoso).

        if (baseMap == null)
        {
            Debug.LogWarning($"[Vol2MaterialFixer] Sin BaseMap para '{matName}' en {texFolder}");
            skipped_count++;
            return false;
        }

        // Asignar texturas al material URP/Lit
        mat.SetTexture("_BaseMap",      baseMap);
        mat.SetColor  ("_BaseColor",    Color.white);

        if (normalMap != null)
        {
            mat.SetTexture("_BumpMap",  normalMap);
            mat.SetFloat  ("_BumpScale", 1f);
            mat.EnableKeyword("_NORMALMAP");
        }

        if (aoMap != null)
        {
            mat.SetTexture("_OcclusionMap",     aoMap);
            mat.SetFloat  ("_OcclusionStrength", 1f);
        }

        // Metallic / Smoothness
        mat.SetFloat("_WorkflowMode", 1f);   // Metallic workflow
        mat.SetFloat("_Metallic",     0f);
        mat.SetFloat("_Smoothness",   0.25f); // electrónica = superficie mate-semimate

        // Surface opaque
        mat.SetFloat("_Surface",   0f);
        mat.SetFloat("_AlphaClip", 0f);
        mat.SetInt("_ZWrite",      1);
        mat.renderQueue = (int)RenderQueue.Geometry;

        EditorUtility.SetDirty(mat);
        return true;
    }

    static int skipped_count = 0;

    // Busca archivo que contenga el sufijo en la carpeta (ej. "_BaseMap")
    static Texture2D FindTex(string folder, string suffix)
    {
        string absFolder = Path.GetFullPath(folder);
        if (!Directory.Exists(absFolder)) return null;

        foreach (var file in Directory.GetFiles(absFolder, "*.png"))
        {
            string name = Path.GetFileNameWithoutExtension(file);
            if (name.EndsWith(suffix, System.StringComparison.OrdinalIgnoreCase))
            {
                string assetPath = folder + "/" + Path.GetFileName(file);
                return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            }
        }
        return null;
    }

    static Texture2D LoadTex(string assetPath)
        => AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
}
#endif
