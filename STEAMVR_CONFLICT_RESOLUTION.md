# Resolución de Conflictos SteamVR + Meta Quest - TITA

## 🎯 **Diagnóstico Completado**

### **✅ Estado Actual del Sistema**
- **OpenXR Runtime**: `C:\Program Files\Meta Horizon\Support\oculus-runtime\oculus_openxr_64.json` ✅
- **Meta Horizon Link**: Ejecutándose (OVRServer_x64.exe activo) ✅
- **SteamVR**: No ejecutándose actualmente ✅
- **VR Device**: Habilitado en Unity ✅

**Conclusión**: La configuración OpenXR está correcta, pero pueden existir conflictos intermitentes.

## 🔍 **Problemas Identificados con SteamVR**

### **1. Conflictos de Runtime OpenXR**
SteamVR puede sobrescribir el runtime OpenXR activo, causando que Unity use SteamVR en lugar de Meta OpenXR.

### **2. Interferencia en Input System**
SteamVR Input puede interceptar comandos del Meta Quest antes que lleguen a Unity.

### **3. Drivers Conflictivos**
Múltiples drivers VR (SteamVR + Meta) pueden causar errores de inicialización.

## 🛠️ **Soluciones Paso a Paso**

### **Paso 1: Verificar Runtime Activo**
```bash
# Ejecutar en PowerShell (como Admin)
Get-ItemProperty -Path 'HKLM:\SOFTWARE\Khronos\OpenXR\1' -Name ActiveRuntime
```
**Debe mostrar**: `C:\Program Files\Meta Horizon\Support\oculus-runtime\oculus_openxr_64.json`

### **Paso 2: Configurar Meta como Runtime Predeterminado**

#### **Opción A: Via Meta Horizon Link**
1. Abrir **Meta Horizon Link**
2. **Settings → General → OpenXR Runtime**
3. Seleccionar **"Set Meta as OpenXR Runtime"**
4. Reiniciar Unity

#### **Opción B: Via Registry (Comando Admin)**
```cmd
reg add "HKEY_LOCAL_MACHINE\SOFTWARE\Khronos\OpenXR\1" /v ActiveRuntime /t REG_SZ /d "C:\Program Files\Meta Horizon\Support\oculus-runtime\oculus_openxr_64.json" /f
```

### **Paso 3: Deshabilitar SteamVR Autostart**

#### **En SteamVR Settings**
1. Abrir **SteamVR**
2. **Settings → Startup/Shutdown**
3. **Desmarcar**: "Start SteamVR when computer starts"
4. **Desmarcar**: "Start SteamVR when app starts"

#### **En Steam Client**
1. **Steam → Settings → VR**
2. **Deshabilitar**: "Start SteamVR automatically"

### **Paso 4: Configurar Unity XR Management**

#### **Project Settings → XR Plug-in Management**
1. **Provider Order** (importante):
   - ✅ **OpenXR** (primera prioridad)
   - ✅ **Oculus** (fallback)
   - ❌ **Remove SteamVR** si aparece

2. **OpenXR Settings**:
   - **Render Mode**: Single Pass Instanced
   - **Feature Sets**: Meta Quest Support

### **Paso 5: Script de Verificación Automática**

Usar las herramientas creadas:
```
Tools → TITA → Diagnosticar OpenXR Runtime
```

## ⚠️ **Señales de Conflicto SteamVR/Meta**

### **Errores Comunes**:
```
XR Device not active
Failed to initialize XR subsystem 
Screen position out of view frustum
OpenXR instance create failed
```

### **En Console Unity**:
```
[XRBootManager] XR Device active: False
[PlayerController] No hay dispositivo XR activo
[OpenXR] Failed to initialize
```

## 🔧 **Quick Fixes Durante Desarrollo**

### **Fix Inmediato (Temporal)**
1. **Cerrar Steam** completamente
2. **Reiniciar Meta Horizon Link**
3. **Conectar Quest 3** via Link
4. **Play en Unity**

### **Fix Permanente**
1. **Configurar Meta como runtime predeterminado** (Paso 2)
2. **Deshabilitar SteamVR autostart** (Paso 3)
3. **Configurar Unity XR providers** (Paso 4)

## 🎮 **Testing Workflow Recomendado**

### **Antes de cada sesión VR**:
1. Verificar `Tools → TITA → Diagnosticar OpenXR Runtime`
2. Confirmar que OVRServer_x64.exe está ejecutándose
3. Quest 3 conectado y detectado en Meta Horizon Link
4. Unity Play Mode → verificar "XR Device active: True"

### **Si hay problemas**:
1. `Tools → TITA → Diagnosticar VR`
2. Verificar Console Unity por errores XR
3. Reiniciar Meta Horizon Link
4. Verificar runtime OpenXR (Paso 1)

## 📋 **Checklist de Configuración**

- [ ] ✅ **Meta Horizon Link instalado y ejecutándose**
- [ ] ✅ **Quest 3 conectado (USB o WiFi)**
- [ ] ✅ **Developer Mode habilitado en Quest**
- [ ] ✅ **Meta OpenXR como runtime activo**
- [ ] ✅ **Unity XR providers: OpenXR + Oculus**
- [ ] ✅ **SteamVR autostart deshabilitado**
- [ ] ✅ **Unity VR Device habilitado**
- [ ] ⚠️ **Input Actions configuradas** (usar Tools → TITA)

## 🚨 **Troubleshooting Avanzado**

### **Si persisten errores después de seguir todos los pasos**:

#### **1. Reinstalar OpenXR Runtime**
```bash
# Desinstalar y reinstalar Meta Horizon Link
# Esto reinstala el OpenXR runtime
```

#### **2. Limpiar Registry OpenXR**
```cmd
# Como Administrator
reg delete "HKEY_LOCAL_MACHINE\SOFTWARE\Khronos\OpenXR\1" /f
# Luego reinstalar Meta Horizon Link
```

#### **3. Verificar Drivers USB**
- Quest 3 debe aparecer como "Oculus Composite ADB Interface"
- Si aparece como "Unknown Device", reinstalar drivers

#### **4. Log Detallado Unity**
```
Edit → Project Settings → Player → Configuration
- Logging: Full
- Development Build: True
```

## 🎯 **Próximos Pasos**

1. **Aplicar configuraciones** de este documento
2. **Ejecutar herramientas TITA** en Unity
3. **Probar conexión Quest 3** con Meta Horizon Link
4. **Test VR gameplay** en Unity Play Mode
5. **Build y deploy** para Quest standalone si Link falla

---

**Nota**: Esta configuración prioriza Meta OpenXR sobre SteamVR para máxima compatibilidad con Quest 3 + KAT VR setup del proyecto TITA.