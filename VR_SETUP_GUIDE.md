# Guía de Configuración VR - Proyecto TITA

## Problemas Identificados y Soluciones

### ✅ **1. VR Device Habilitado**
- **Problema**: VR estaba deshabilitado en ProjectSettings
- **Solución**: Corregido automáticamente en `XRSettings.asset`

### ⚠️ **2. Input Actions Configuration**
- **Problema**: `moveAction` no asignado en PlayerController
- **Solución**: Usar herramientas automáticas creadas

### 🔧 **3. Herramientas de Configuración Creadas**

#### Scripts de Ayuda:
- `Assets/Scripts/Editor/VRSetupTool.cs` - Configuración completa
- `Assets/Scripts/Editor/PlayerControllerFixer.cs` - Fix Input Actions

#### Menús Disponibles:
- **Tools → TITA → Setup Completo VR Explorador**
- **Tools → TITA → Fix PlayerController Input Actions**
- **Tools → TITA → Diagnosticar VR**

## Pasos para Completar la Configuración

### **Paso 1: Abrir Unity Editor**
```bash
# Navegar al proyecto
cd "C:\Users\holaq\Proyecto-TITA"
# Abrir con Unity Hub o directamente
```

### **Paso 2: Ejecutar Setup Automático**
1. En Unity Editor: **Tools → TITA → Setup Completo VR Explorador**
2. Verificar en Console que no hay errores
3. Ejecutar: **Tools → TITA → Fix PlayerController Input Actions**

### **Paso 3: Configuración Manual Restante**

#### **A) XR Plug-in Management**
1. **Edit → Project Settings → XR Plug-in Management**
2. Verificar providers activos:
   - ✅ **OpenXR** (principal)
   - ✅ **Oculus** (backup para Quest)

#### **B) PlayerController Inspector**
1. Seleccionar objeto con **PlayerController** en la escena
2. En Inspector, verificar:
   - **moveAction**: debe mostrar "XRI Left Locomotion/Move"
   - **xrRig**: asignado al XR Origin
   - **headCamera**: asignada a Main Camera VR

#### **C) Input Action Asset**
1. Verificar que existe: `Assets/Samples/XR Interaction Toolkit/3.4.1/Starter Assets/XRI Default Input Actions.inputactions`
2. Alternativamente usar: `Assets/InputSystem_Actions.inputactions`

### **Paso 4: Verificación**
1. **Tools → TITA → Diagnosticar VR**
2. **Play Mode**: verificar que no hay errores en Console
3. **Build Settings**: Android para Quest, PC para Link

## Configuración Quest Link (PC)

### **Prerrequisitos**
1. **Meta Horizon Link** instalado y configurado
2. Quest 3 conectado via USB o WiFi
3. OpenXR runtime activo

### **Pasos**
1. Abrir **Meta Horizon Link**
2. Conectar Quest 3
3. Configurar OpenXR como runtime activo
4. En Unity: **Play** para probar

## Resolución de Problemas Comunes

### ❌ **"UnassignedReferenceException: _cc of PlayerController not assigned"**
**🚑 Solución Rápida**: 
1. **Tools → TITA → Fix PlayerController Components**
2. **Tools → TITA → Safe Diagnostic PlayerController** (verificar)
3. **Guía detallada**: `QUICK_FIX_CHARACTER_CONTROLLER_ERROR.md`

**Causa**: GameObject PlayerController no tiene CharacterController component

### ❌ **"No hay dispositivo XR activo"**
**Solución**: 
- Verificar Meta Horizon Link está ejecutándose
- Quest 3 conectado y detectado
- OpenXR configurado como runtime activo

### ❌ **"moveAction NO está asignado"**
**Solución**: 
- Ejecutar: **Tools → TITA → Fix PlayerController Input Actions**
- Verificar manualmente en Inspector de PlayerController

### ❌ **"Screen position out of view frustum"**
**Solución**: 
- XRBootManager lo corrige automáticamente
- Verificar que XRBootManager.cs está en la escena

### ❌ **Errores de Compilación (exit code 1)**
**Causas Posibles**:
- VR deshabilitado (✅ ya corregido)
- CharacterController faltante (✅ ya corregido con tools)
- Input Actions mal configuradas
- Dependencias XR faltantes
- SteamVR conflictos (usar Resolver Conflictos SteamVR)

**Solución**:
1. **Tools → TITA → Setup Completo VR Explorador**
2. **Tools → TITA → Resolver Conflictos SteamVR**
3. Verificar Package Manager: XR Interaction Toolkit instalado
4. Rebuild All

## Arquitectura VR del Proyecto

### **Dual Mode Support**
- **Hardware Real**: Meta Quest 3 + KAT VR treadmill
- **Fallback**: Joystick input (modo desarrollador)

### **Componentes Clave**
- **XRBootManager**: Auto-configura VR en runtime
- **PlayerController**: Maneja input y movimiento
- **XRDiagnostics**: Monitoreo y debug

### **Input System**
- **Primary**: OpenXR via XR Interaction Toolkit
- **Fallback**: Direct Input System para desarrollo

## Estado Actual del Proyecto

✅ **Completados**:
- Challenge 1: Exploración básica VR
- Challenge 2: Interacción con objetos

🔄 **En Progreso**:
- Challenge 3: Haptic feedback (capacitors)

⏳ **Pendientes**:
- Challenge 4: Arduino integration

## Próximos Pasos

1. ✅ **Aplicar fixes de VR** (este documento)
2. 🔧 **Probar en Meta Quest 3**
3. 🎯 **Completar Challenge 3** (haptic feedback)
4. 🔌 **Desarrollar Challenge 4** (Arduino integration)

---

## 🔍 **ACTUALIZACIÓN: Diagnóstico SteamVR Completado**

### **✅ Causa Raíz Identificada: Conflictos SteamVR + Meta Quest**

**Estado Actual Verificado:**
- ✅ **OpenXR Runtime**: `C:\Program Files\Meta Horizon\Support\oculus-runtime\oculus_openxr_64.json`
- ✅ **Meta Horizon Link**: Ejecutándose (OVRServer_x64.exe, OVRServiceLauncher.exe activos)
- ✅ **SteamVR**: No ejecutándose actualmente 
- ✅ **Unity VR Device**: Habilitado

### **🛠️ Nuevas Herramientas de Diagnóstico**

#### **Menús Adicionales Creados:**
- **Tools → TITA → Diagnosticar OpenXR Runtime**
- **Tools → TITA → Resolver Conflictos SteamVR**

#### **Scripts Avanzados:**
- `XRRuntimeDiagnostic.cs` - Diagnóstico completo OpenXR
- `SteamVRConflictResolver.cs` - Resolución automática conflictos

### **📝 Documentación Adicional**
- `STEAMVR_CONFLICT_RESOLUTION.md` - Guía específica conflictos SteamVR

### **🚑 Solución Rápida si Hay Problemas**

1. **Unity Editor**: **Tools → TITA → Resolver Conflictos SteamVR**
2. **Verificar**: Quest 3 conectado en Meta Horizon Link
3. **Test**: Play Mode en Unity

**Problema Principal**: SteamVR puede sobrescribir OpenXR runtime o interferir con input. Las herramientas creadas detectan y corrigen estos conflictos automáticamente.

---

**¿Problemas?** Ejecutar: **Tools → TITA → Resolver Conflictos SteamVR** y luego **Tools → TITA → Diagnosticar VR**