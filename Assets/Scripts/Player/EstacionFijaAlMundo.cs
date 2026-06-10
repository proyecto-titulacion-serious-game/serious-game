using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hace que la estación de trabajo del Explorador quede FIJA en el mundo en lugar de seguir
/// al jugador. Va en el rig <c>Explorer_Player</c>.
///
/// PROBLEMA QUE RESUELVE:
///   La bandeja, los slots, la estación de validación y el clipboard son hijos del rig del
///   jugador, así que se mueven/teletransportan con él. Esto hace que la "mesa de trabajo"
///   persiga al jugador, cuando debería quedarse quieta.
///
/// CÓMO FUNCIONA:
///   Al iniciar (o al spawnear el player por red), desemparenta cada objeto de la lista,
///   conservando su pose mundial. Quedan quietos en el mundo —en la posición que tenían
///   delante del jugador— y el jugador ya puede moverse sin arrastrarlos.
///
///   Además, si <c>autoDescubrirPorNombre</c> está activo, busca por nombre las piezas
///   conocidas de la estación entre los hijos del rig y también las fija. Esto cubre el caso
///   de que la lista 'objetosFijos' esté incompleta o se haya roto en la escena (referencias
///   nulas), garantizando que la mesa de trabajo NO siga al jugador.
///
/// USO:
///   - Añade este componente a la raíz del prefab Explorer_Player.
///   - Arrastra a 'objetosFijos' la bandeja, la estación, el clipboard, etc. (opcional si
///     usas auto-descubrimiento por nombre).
/// </summary>
public class EstacionFijaAlMundo : MonoBehaviour
{
    [Tooltip("Objetos de la estación que deben quedar fijos en el mundo (no seguir al jugador). " +
             "Ej.: Bandeja_Recepcion, ValidationStation_VR, Clipboard_VR, Explorer_StatusPanel.")]
    public Transform[] objetosFijos;

    [Tooltip("Opcional, por índice: si se asigna, el objeto se coloca en la pose de este ancla " +
             "fija de la escena. Vacío = se queda donde aparece al spawnear.")]
    public Transform[] anclasFijas;

    [Tooltip("Si está activo, fija la estación en Start (cubre spawn normal y spawn por red).")]
    public bool fijarEnStart = true;

    [Tooltip("Busca por nombre las piezas de la estación entre los hijos del rig y también las fija.\n" +
             "Cubre el caso de que 'objetosFijos' esté incompleta o con referencias nulas.")]
    public bool autoDescubrirPorNombre = true;

    [Tooltip("Nombres (o fragmentos) de las piezas de la estación a fijar por nombre.")]
    public string[] nombresEstacion =
    {
        "Bandeja_Recepcion",
        "ValidationStation_VR",
        "Clipboard_VR",
        "Explorer_StatusPanel",
    };

    void Start()
    {
        if (fijarEnStart) FijarAlMundo();
    }

    /// <summary>Desliga los objetos de la estación del jugador y los deja fijos en el mundo.</summary>
    public void FijarAlMundo()
    {
        var yaFijados = new HashSet<Transform>();

        // 1) Lista explícita asignada en el Inspector.
        if (objetosFijos != null)
        {
            for (int i = 0; i < objetosFijos.Length; i++)
            {
                var t = objetosFijos[i];
                if (t == null || yaFijados.Contains(t)) continue;

                Desligar(t);
                yaFijados.Add(t);

                // Colocación exacta opcional en un ancla fija de la escena.
                if (anclasFijas != null && i < anclasFijas.Length && anclasFijas[i] != null)
                    t.SetPositionAndRotation(anclasFijas[i].position, anclasFijas[i].rotation);
            }
        }

        // 2) Auto-descubrimiento por nombre (robusto frente a listas rotas/incompletas).
        //    Busca en TODOS los descendientes (no solo hijos directos): si la bandeja está
        //    anidada más profundo, el detach por lista directa podía no encontrarla.
        if (autoDescubrirPorNombre && nombresEstacion != null)
        {
            // Recolectar primero para no modificar la jerarquía mientras se itera.
            var candidatos = new List<Transform>();
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                if (t == transform || t == null || yaFijados.Contains(t)) continue;
                if (CoincideConEstacion(t.name))
                    candidatos.Add(t);
            }

            foreach (var t in candidatos)
            {
                if (yaFijados.Contains(t)) continue;
                Desligar(t);
                yaFijados.Add(t);
            }
        }

        if (yaFijados.Count > 0)
            Debug.Log($"[EstacionFijaAlMundo] {yaFijados.Count} pieza(s) de la estación fijadas al mundo " +
                      "(ya no siguen al jugador).");
        else
            Debug.LogWarning("[EstacionFijaAlMundo] No se fijó ninguna pieza. ¿Lista vacía y sin coincidencias por nombre?");
    }

    // Saca el objeto de la jerarquía del jugador conservando su pose mundial → deja de seguirlo.
    static void Desligar(Transform t)
    {
        if (t.parent != null) t.SetParent(null, worldPositionStays: true);
    }

    bool CoincideConEstacion(string nombre)
    {
        foreach (var n in nombresEstacion)
        {
            if (!string.IsNullOrEmpty(n) &&
                nombre.IndexOf(n, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }
        return false;
    }
}
