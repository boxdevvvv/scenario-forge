# Scenario Forge: especificación funcional y técnica

> **Nota de versión (2026-11-07):** el autor fijó Unity **6000.6.8f1** en lugar de 6.3 LTS. Las referencias a la versión se actualizaron.
>
> **Ampliación aprobada (2026-11-08):** el autor aprobó que la escena reaccione al escenario y que se registren los errores del jugador. Se agregaron `ScenarioContext.ReportMistake`, `ScenarioMistake`, `StepResult.Mistakes`, `ScenarioRunner.Mistakes`/`MistakeReported`/`SignalRaised`, los eventos C# de `ScenarioRunnerBehaviour`, el paso `SendSignalStep` y los componentes `ScenarioStepEvents` y `ScenarioSignalListener` (secciones 6.5, 6.6, 6.10, 8 y 13).
>
> **Para Claude Code:** este documento es la fuente de verdad del proyecto. Léelo completo antes de escribir código. Si algo es ambiguo o contradice lo que encuentres en la documentación de Unity, **pregunta antes de decidir**. Los nombres de código (clases, métodos, carpetas) van en inglés. Los textos de la interfaz también van en inglés.

---

## 1. Qué es y para qué existe

**Scenario Forge** es un paquete de Unity (UPM, open source, licencia MIT) para **crear procedimientos paso a paso sin programar**: entrenamientos, tutoriales, misiones lineales o checklists de seguridad. Un diseñador arma la secuencia de pasos en una ventana de editor. En runtime, un *runner* ejecuta los pasos en orden, evalúa cuándo cada uno se completa o falla, y emite eventos para la UI, el puntaje o la telemetría.

### 1.1 Objetivos del proyecto

1. **Objetivo de portafolio (el principal):** demostrar a reclutadores, leyendo el repositorio, que el autor sabe:
   - diseñar una API pública limpia y extensible;
   - construir herramientas de editor profesionales con UI Toolkit;
   - escribir código testeado, con CI funcionando;
   - documentar como un producto real.
2. **Objetivo práctico:** reutilizar el paquete como sistema de escenarios del proyecto *LiftLab VR* (simulador de grúa horquilla), sin que el paquete sepa nada de grúas.
3. **Objetivo de calidad:** que un desarrollador externo pueda instalarlo por URL de Git, abrir el sample y crear su propio tipo de paso en menos de 15 minutos siguiendo la documentación.

### 1.2 Fuera de alcance (no construir en la v0.1)

- Editor de grafo de nodos, ramificaciones o pasos en paralelo. La v0.1 es **estrictamente lineal**.
- Selección múltiple de pasos en la ventana.
- Sincronización multijugador.
- Localización de textos.
- Editor visual en runtime (dentro del juego).
- Soporte para versiones anteriores a Unity 6.6.

---

## 2. Glosario

| Término | Significado |
| --- | --- |
| **Scenario** | Un procedimiento completo. Se guarda como asset `ScenarioDefinition` (ScriptableObject). |
| **Step** | Un paso del procedimiento. Clase C# serializable que hereda de `ScenarioStep`. |
| **Runner** | El objeto que ejecuta un escenario en runtime (`ScenarioRunner`, C# puro). |
| **Context** | Lo que un paso necesita para evaluarse: blackboard, señales, targets y servicios (`ScenarioContext`). |
| **Blackboard** | Diccionario tipado clave → valor que comparten los pasos durante una ejecución. |
| **Signal** | Un evento con nombre (`"door_opened"`) que el juego emite y los pasos escuchan. |
| **Target** | Un objeto de la escena identificado por un ID de texto (`ScenarioTarget`). Los pasos lo referencian por ID porque un ScriptableObject no puede referenciar objetos de escena. |

---

## 3. Usuarios y casos de uso

1. **Diseñador:** abre la ventana, crea un escenario, agrega pasos desde un menú con búsqueda, los reordena arrastrando, completa campos, corrige los errores que marca la validación y prueba en Play Mode viendo el paso activo en vivo.
2. **Programador:** crea un nuevo tipo de paso heredando de `ScenarioStep` y agregando un atributo. Aparece automáticamente en el menú de la ventana, sin registrar nada más.
3. **QA:** en Play Mode puede pausar, saltar un paso o reiniciar, y ver el log de eventos y el blackboard.
4. **Pipeline/CI:** valida todos los escenarios del proyecto en modo batch y falla el build si hay errores.

---

## 4. Requisitos técnicos

| Área | Requisito |
| --- | --- |
| Versión de Unity | **Unity 6.6 (6000.6.x)**. En `package.json`: `"unity": "6000.6"`. |
| UI del editor | UI Toolkit (C# + USS). Nada de IMGUI salvo `AdvancedDropdown` para el selector de tipos. |
| Serialización de pasos | `[SerializeReference]` para la lista polimórfica de pasos. |
| JSON | `com.unity.nuget.newtonsoft-json` (paquete oficial de Unity). Usa la versión que el Package Manager instale y copia ese número exacto a `package.json`. |
| Tests | Unity Test Framework (EditMode y PlayMode). |
| Dependencias | Ninguna fuera de las anteriores. Sin Input System, sin XR, sin Addressables. |
| Licencia | MIT. |

---

## 5. Estructura del repositorio

El repositorio **es un proyecto de Unity completo** (para desarrollar y testear), con el paquete **embebido** dentro de `Packages/`.

```
scenario-forge/                          ← raíz del repo = proyecto Unity
├── .github/workflows/tests.yml
├── .gitignore                           ← plantilla oficial de Unity
├── .gitattributes                       ← Git LFS para .png, .gif, .psd, .fbx
├── README.md                            ← copia del README del paquete + badges
├── Assets/
│   ├── Samples/ScenarioForge/AssemblyProcedure/   ← copia de desarrollo del sample
│   └── Dev/                             ← escenas de prueba internas (no se publican)
├── Packages/
│   ├── manifest.json                    ← debe incluir "testables" (ver 5.2)
│   └── com.boxdevvvv.scenarioforge/      ← EL PAQUETE
│       ├── package.json
│       ├── README.md
│       ├── CHANGELOG.md
│       ├── LICENSE.md
│       ├── Runtime/
│       │   ├── ScenarioForge.Runtime.asmdef
│       │   ├── Core/        (ScenarioDefinition, ScenarioStep, ScenarioRunner, ScenarioContext…)
│       │   ├── Blackboard/
│       │   ├── Signals/
│       │   ├── Targets/
│       │   ├── Steps/       (pasos incluidos, ver sección 8)
│       │   ├── Validation/
│       │   ├── Serialization/ (JSON)
│       │   └── Components/  (ScenarioRunnerBehaviour, ScenarioTarget, ScenarioSignalEmitter)
│       ├── Editor/
│       │   ├── ScenarioForge.Editor.asmdef   ← solo plataforma Editor
│       │   ├── Window/      (ScenarioForgeWindow y sus paneles)
│       │   ├── Inspectors/
│       │   ├── TypePicker/
│       │   ├── Validation/
│       │   ├── Debugger/
│       │   └── Styles/      (archivos .uss)
│       ├── Tests/
│       │   ├── Runtime/ScenarioForge.Runtime.Tests.asmdef
│       │   └── Editor/ScenarioForge.Editor.Tests.asmdef
│       ├── Documentation~/
│       └── Samples~/AssemblyProcedure/
└── ProjectSettings/
```

### 5.1 Nombres

- **Paquete:** `com.boxdevvvv.scenarioforge` (el usuario de GitHub del autor es `boxdevvvv`, confirmado el 2026-11-08). Todo en minúsculas.
- **Namespaces:** `ScenarioForge` (runtime), `ScenarioForge.Steps` (pasos incluidos), `ScenarioForge.Editor`, `ScenarioForge.Tests`, `ScenarioForge.Samples`.

### 5.2 Problemas conocidos de esta estructura (resolverlos desde el inicio)

1. **Los tests de un paquete no aparecen en el Test Runner** a menos que el paquete esté listado en `Packages/manifest.json` bajo `"testables": ["com.boxdevvvv.scenarioforge"]`.
2. **Unity ignora las carpetas que terminan en `~`**, así que el sample dentro de `Samples~` no se puede abrir en el proyecto de desarrollo. Solución: el sample vive en `Assets/Samples/ScenarioForge/AssemblyProcedure/` durante el desarrollo, y un menú de editor **interno** `Tools/Scenario Forge/Dev/Sync Sample To Package` lo copia a `Samples~/AssemblyProcedure/`. Ese menú va en un asmdef de desarrollo fuera del paquete (por ejemplo `Assets/Dev/Editor/`), **nunca dentro del paquete publicado**.
3. **Instalación por URL de Git:** como el paquete no está en la raíz del repo, se instala con el parámetro `?path=`, y el `path` va **antes** del ancla de versión:
   `https://github.com/boxdevvvv/scenario-forge.git?path=/Packages/com.boxdevvvv.scenarioforge#v0.1.0`
4. **Referencia a Newtonsoft desde el asmdef:** si el asmdef del runtime no ve `Newtonsoft.Json`, activa `overrideReferences` y agrega `Newtonsoft.Json.dll` a `precompiledReferences`.

---

## 6. Arquitectura del runtime

### 6.1 Diagrama de dependencias

```
ScenarioRunnerBehaviour (MonoBehaviour, opcional)
        │ crea y hace Tick de
        ▼
ScenarioRunner (C# puro) ──usa──► ScenarioContext ──contiene──► Blackboard
        │                                         ├──────────► SignalBus
        │ ejecuta en orden                        ├──────────► ITargetResolver
        ▼                                         └──────────► IScenarioServices (opcional, del juego)
ScenarioDefinition (ScriptableObject)
        └── [SerializeReference] List<ScenarioStep>
```

**Regla de oro:** toda la lógica vive en clases C# puras (`ScenarioRunner`, `Blackboard`, `SignalBus`, los pasos). Los `MonoBehaviour` son envoltorios delgados. Esto permite testear casi todo con EditMode tests rápidos.

### 6.2 `ScenarioDefinition` (ScriptableObject)

```csharp
[CreateAssetMenu(menuName = "Scenario Forge/Scenario", fileName = "NewScenario")]
public sealed class ScenarioDefinition : ScriptableObject
{
    [SerializeField] string title;
    [SerializeField, TextArea] string description;
    [SerializeReference] List<ScenarioStep> steps = new();

    public string Title => title;
    public string Description => description;
    public IReadOnlyList<ScenarioStep> Steps => steps;
}
```

- Solo datos. Nada de lógica de ejecución.
- Para modificar la lista desde código de editor se usa `SerializedObject` (para que funcionen Undo y el guardado), nunca acceso directo al campo.

### 6.3 `ScenarioStep` (clase base de todos los pasos)

```csharp
[Serializable]
public abstract class ScenarioStep
{
    [SerializeField, HideInInspector] string id;          // GUID en formato "N", único dentro del escenario
    [SerializeField] string title;                         // nombre visible en la ventana y en el log
    [SerializeField, TextArea] string instruction;         // texto que la UI del juego muestra al jugador
    [SerializeField, Min(0)] float timeoutSeconds;         // 0 = sin límite de tiempo
    [SerializeField] FailurePolicy onFailure = FailurePolicy.FailScenario;
    [SerializeField, Min(0)] int maxRetries;               // solo aplica si onFailure == Retry

    public string Id => id;
    public string Title => title;
    public string Instruction => instruction;
    // … resto de propiedades de solo lectura

    protected internal virtual void OnEnter(ScenarioContext context) { }
    protected internal abstract StepStatus OnTick(ScenarioContext context, float deltaTime);
    protected internal virtual void OnExit(ScenarioContext context, StepOutcome outcome) { }

    /// Validación propia del paso. Agrega problemas al reporte; nunca lanza excepciones.
    public virtual void Validate(ValidationReport report) { }

    internal void AssignNewId() => id = Guid.NewGuid().ToString("N");
}

public enum StepStatus { Running, Succeeded, Failed }
public enum StepOutcome { Succeeded, Failed, Skipped, TimedOut, Aborted }
public enum FailurePolicy { FailScenario, SkipStep, Retry }
```

- **Atributo de menú:** cada paso concreto lleva `[ScenarioStepMenu("Categoría/Nombre", Tooltip = "...")]`. La ventana arma el menú de "Add Step" con ese atributo. Un paso sin atributo aparece en la categoría `Other` usando el nombre de la clase.
- **Requisitos para que un tipo aparezca en el menú:** no abstracto, `[Serializable]`, constructor público sin parámetros, hereda de `ScenarioStep`.
- **Convención obligatoria:** dos clases de paso **no deben** tener campos con el mismo nombre y distinto tipo. El `PropertyField` de UI Toolkit con managed references tiene errores conocidos en ese caso. Documenta esta regla para quienes extiendan el paquete.

### 6.4 Estado en runtime: el problema del asset compartido

**Problema:** los pasos viven dentro de un asset. Si un paso guarda estado durante la ejecución (por ejemplo, un contador) en un campo serializado, en el editor **ese cambio modificaría el asset en disco**.

**Solución obligatoria:**

1. Al iniciar, el runner trabaja sobre una **copia**: `Object.Instantiate(definition)`. La copia clona también las managed references. El runner destruye la copia al terminar o abortar.
2. Además, el estado de ejecución dentro de un paso va en campos `[NonSerialized]`, y se reinicia en `OnEnter`.
3. Un test debe verificar que, después de ejecutar un escenario completo, el asset original no cambió (comparando su JSON antes y después).

### 6.5 `ScenarioRunner` (C# puro)

```csharp
public sealed class ScenarioRunner : IDisposable
{
    public ScenarioRunner(ScenarioDefinition definition, ScenarioContext context);

    public RunnerState State { get; }                 // NotStarted, Running, Paused, Completed, Failed, Aborted
    public int CurrentStepIndex { get; }              // -1 si no ha iniciado
    public ScenarioStep CurrentStep { get; }          // null si no hay paso activo
    public float CurrentStepElapsed { get; }
    public IReadOnlyList<StepResult> Results { get; }
    public IReadOnlyList<ScenarioMistake> Mistakes { get; }  // errores del jugador de esta ejecución

    public event Action<RunnerState> StateChanged;
    public event Action<StepInfo> StepStarted;
    public event Action<StepResult> StepEnded;
    public event Action<string> InstructionChanged;   // texto de la instrucción del paso activo
    public event Action<string> SignalRaised;         // toda señal de la ejecución actual (también tras Restart)
    public event Action<ScenarioMistake> MistakeReported;

    public void Start();
    public void Tick(float deltaTime);                // no hace nada si State != Running
    public void Pause();
    public void Resume();
    public void Abort();
    public void SkipCurrentStep();                    // herramienta de depuración: termina el paso con outcome Skipped
    public void Restart();                            // aborta y vuelve a empezar con un contexto nuevo
}

public readonly struct StepResult
{
    public readonly string StepId;
    public readonly int Index;
    public readonly StepOutcome Outcome;
    public readonly float Duration;
    public readonly int Attempts;
    public readonly string FailureReason;             // null si no falló
    public readonly int Mistakes;                     // errores reportados durante el paso (sumando reintentos)
}

public readonly struct ScenarioMistake
{
    public readonly string StepId;                    // null si no había paso activo
    public readonly int StepIndex;                    // -1 si no había paso activo
    public readonly string Message;
    public readonly float Time;                       // TotalElapsed al reportarlo
}
```

**Reglas de ejecución (cada una debe tener test):**

1. `Start()` desde `NotStarted`: pasa a `Running`, llama `OnEnter` del primer paso y emite `StepStarted` y `InstructionChanged`. Si el escenario no tiene pasos, pasa directo a `Completed`.
2. `Tick(dt)`: suma `dt` al tiempo del paso y llama `OnTick`.
   - `Succeeded` → `OnExit(Succeeded)`, registra el resultado, emite `StepEnded` y avanza al siguiente paso **en el mismo tick** (llama su `OnEnter`, pero su primer `OnTick` ocurre en el tick siguiente).
   - `Failed` → aplica `FailurePolicy`.
   - Si `timeoutSeconds > 0` y se superó el tiempo → outcome `TimedOut`, que se trata como una falla y aplica `FailurePolicy`.
3. **FailurePolicy:**
   - `FailScenario`: termina el paso y el escenario pasa a `Failed`.
   - `SkipStep`: registra el paso como fallido y continúa con el siguiente.
   - `Retry`: llama `OnExit(Failed)` y luego `OnEnter` otra vez, y cuenta el intento. Al agotar `maxRetries`, se comporta como `FailScenario`.
4. Al terminar el último paso con éxito → `Completed`.
5. `Pause()` solo funciona desde `Running`; `Resume()` solo desde `Paused`. Durante la pausa, `Tick` no avanza el tiempo.
6. `Abort()` llama `OnExit(Aborted)` en el paso activo y pasa a `Aborted`. No hace nada si el escenario ya terminó.
7. Una excepción dentro de un paso **no debe romper el juego**: el runner la captura, la registra con `Debug.LogException`, y trata el paso como `Failed` con `FailureReason` igual al mensaje.
8. **Orden de eventos garantizado:** `StepEnded(n)` → `StepStarted(n+1)` → `InstructionChanged`. Un test lo verifica.
9. **Errores del jugador:** `ScenarioContext.ReportMistake(message)` registra un error en el paso activo mientras el escenario está en `Running` o `Paused` (fuera de eso se ignora). Un error **no** hace fallar el paso; se acumula en `Mistakes` y en `StepResult.Mistakes`, y `Restart` los borra.
10. **Rendimiento:** en estado estable, `Tick` no debe generar basura (GC alloc = 0). Hay un test que lo mide con `GC.GetAllocatedBytesForCurrentThread()` usando un paso de prueba que no asigna memoria.

### 6.6 `ScenarioContext`

```csharp
public sealed class ScenarioContext
{
    public Blackboard Blackboard { get; }
    public SignalBus Signals { get; }
    public ITargetResolver Targets { get; }
    public IScenarioServices Services { get; }    // opcional; el juego lo implementa para exponer sus sistemas
    public float TotalElapsed { get; }
    public void UpdateInstruction(string text);   // el paso activo cambia el texto mostrado
    public void ReportMistake(string message);    // registra un error del jugador (ver regla 9)
}
```

- Se crea uno nuevo por ejecución. **Nada estático y global:** así no hay estado que sobreviva entre sesiones de Play Mode cuando el *domain reload* está desactivado.

### 6.7 `Blackboard`

- `Set<T>(string key, T value)`, `bool TryGet<T>(string key, out T value)`, `bool Has(string key)`, `void Remove(string key)`, `void Clear()`.
- Evento `ValueChanged(string key)`.
- Tipos soportados en la v0.1: `bool`, `int`, `float`, `string`. Usar otro tipo lanza `ArgumentException` con un mensaje claro.
- Una lectura con el tipo equivocado devuelve `false` en `TryGet`; no lanza excepción.

### 6.8 `SignalBus`

- `Raise(string signal)`, `int GetCount(string signal)`, `bool WasRaised(string signal)`, evento `Raised(string signal)`.
- Los nombres de señales distinguen mayúsculas, sin espacios al inicio ni al final (se recortan al emitir).
- `ResetCounts()` lo llama el runner en el `OnEnter` de los pasos que lo necesiten (por ejemplo, `WaitForSignalStep` solo cuenta las señales emitidas **después** de empezar el paso).

### 6.9 Targets (referencias a objetos de escena)

- `ScenarioTarget` (MonoBehaviour): campo `string targetId`. Opcionalmente dibuja un gizmo con su ID en la vista de escena.
- `ITargetResolver`: `bool TryGetTransform(string targetId, out Transform transform)`.
- `SceneTargetResolver`: implementación que el `ScenarioRunnerBehaviour` arma al iniciar, buscando con `FindObjectsByType<ScenarioTarget>(FindObjectsInactive.Include, FindObjectsSortMode.None)`. Si hay IDs duplicados, registra una advertencia con ambos objetos.

### 6.10 Componentes de escena

1. **`ScenarioRunnerBehaviour`**
   - Campos: `ScenarioDefinition definition`, `bool autoStart = true`, y `UnityEvent`s para diseñadores: `onScenarioStarted`, `onScenarioCompleted`, `onScenarioFailed`, `onInstructionChanged (string)`.
   - Crea el contexto y el runner en `Start()` (si `autoStart`), llama `Tick(Time.deltaTime)` en `Update()` y libera todo en `OnDestroy()`.
   - Expone `Runner` y `Context` como propiedades públicas, que usa el depurador.
   - Reenvía, para todas sus ejecuciones, los eventos C# `StepStarted`, `StepEnded`, `SignalRaised` y `MistakeReported`, para que otros componentes se suscriban una sola vez.
2. **`ScenarioSignalEmitter`**: un método público `Raise()` con un campo `signalName`, para conectarlo a botones o triggers desde el Inspector con UnityEvents. También `RaiseSignal(string)` para pasar el nombre dinámicamente. Busca el runner con un campo asignado o, si está vacío, el primero de la escena.
3. **`ScenarioTarget`**: ver 6.9.
4. **`ScenarioStepEvents`**: lista de `StepEventBinding` (`stepId`, `UnityEvent onStarted`, `UnityEvent onEnded`). Invoca `onStarted` cuando empieza el paso elegido (y en cada reintento) y `onEnded` cuando termina, sea cual sea el resultado. Sirve para que la escena reaccione: habilitar una interacción solo durante su paso, mostrar un resaltado, encender un fuego. En el Inspector el paso se elige de una lista con los pasos del escenario del runner (`N. Título`), no escribiendo el ID.
5. **`ScenarioSignalListener`**: campo `signalName` y `UnityEvent onSignal`, que se invoca cada vez que se emite esa señal (por un `SendSignalStep`, un emitter o el código). Es la contraparte de `ScenarioSignalEmitter`.
- Los tres componentes que buscan runner (emitter, step events, listener) usan el campo asignado o, si está vacío, el primero de la escena.

---

## 7. Validación

### 7.1 API

```csharp
public sealed class ValidationReport
{
    public IReadOnlyList<ValidationIssue> Issues { get; }
    public bool HasErrors { get; }
    public void Add(ValidationSeverity severity, string message, string stepId = null);
}
public enum ValidationSeverity { Info, Warning, Error }

public static class ScenarioValidator
{
    public static ValidationReport Validate(ScenarioDefinition definition);
}
```

### 7.2 Reglas incluidas

| Regla | Severidad |
| --- | --- |
| El escenario no tiene pasos | Warning |
| Un elemento de la lista es `null` (tipo borrado o renombrado) | Error |
| Un paso no tiene `title` | Warning |
| Dos pasos tienen el mismo `id` | Error |
| Un paso tiene `id` vacío | Error |
| `onFailure == Retry` y `maxRetries == 0` | Warning |
| `timeoutSeconds` en un paso que nunca puede fallar por sí solo (por ejemplo, `WaitStep`) | Info |
| Reglas propias de cada paso (`Validate` de cada uno, ver sección 8) | Según el paso |
| (Solo editor) El asset tiene managed references con tipos faltantes | Error |

- **Tipos faltantes:** en el editor, usa la API de `SerializationUtility` para detectar managed references con tipos faltantes (verifica el nombre exacto del método en la documentación de Unity 6.6 antes de usarlo). En la ventana, esos elementos se muestran como una fila roja *"Missing type: {nombre del tipo}"* con una opción para eliminarlos.
- **Targets:** si hay una escena abierta, la validación del editor puede verificar que los `targetId` usados existan en las escenas abiertas. Si no existen, es una **Warning** (no Error), porque el escenario podría usarse en otra escena.

### 7.3 Validación por línea de comandos (CI)

- Método estático `ScenarioForge.Editor.BatchValidation.ValidateAllScenarios()`, invocable con `-executeMethod`.
- Busca todos los `ScenarioDefinition` del proyecto (`AssetDatabase.FindAssets("t:ScenarioDefinition")`), imprime cada problema y termina con `EditorApplication.Exit(1)` si hay algún Error, o `Exit(0)` si no.
- Menú equivalente: `Tools/Scenario Forge/Validate All Scenarios`.

---

## 8. Pasos incluidos en el paquete

Todos viven en `ScenarioForge.Steps`, llevan `[ScenarioStepMenu]` y tienen tests propios.

| Paso | Menú | Campos | Se completa cuando | Validación propia |
| --- | --- | --- | --- | --- |
| `WaitStep` | Flow/Wait | `float duration` | pasan `duration` segundos | `duration <= 0` → Warning |
| `InstructionStep` | Flow/Instruction | `bool requireConfirmation`, `string confirmSignal = "confirm"`, `float autoAdvanceSeconds` | se emite `confirmSignal` (si `requireConfirmation`) o pasan `autoAdvanceSeconds` | instrucción vacía → Warning; ambas opciones desactivadas → Error |
| `WaitForSignalStep` | Signals/Wait For Signal | `string signal`, `int requiredCount = 1` | `signal` se emite `requiredCount` veces después de iniciar el paso | señal vacía → Error; `requiredCount < 1` → Error |
| `ChecklistStep` | Signals/Checklist | `List<ChecklistItem>` (cada item: `string label`, `string signal`), `bool ordered` | todas las señales se emitieron (en orden, si `ordered`; si se emite la señal de un item posterior antes de tiempo, se reporta un error del jugador) | lista vacía → Error; item sin señal → Error; señales repetidas → Warning |
| `SendSignalStep` | Signals/Send Signal | `string signal` | inmediatamente, después de emitir `signal` (en el primer tick) | señal vacía → Error |
| `ReachTargetStep` | Spatial/Reach Target | `string actorTargetId`, `string destinationTargetId`, `float radius = 1` | la distancia entre el actor y el destino es ≤ `radius` | IDs vacíos → Error; `radius <= 0` → Error |
| `SetBlackboardValueStep` | Blackboard/Set Value | `string key`, `BlackboardValue value` | inmediatamente (en el primer tick) | clave vacía → Error |
| `BlackboardConditionStep` | Blackboard/Wait For Condition | `string key`, `CompareOperator op`, `BlackboardValue value`, `bool failIfMissing` | la condición se cumple | clave vacía → Error; operador incompatible con el tipo (por ejemplo, `>` con string) → Error |

- `BlackboardValue` es un struct serializable con un enum de tipo (`Bool`, `Int`, `Float`, `String`) y un campo por tipo. Necesita un **PropertyDrawer en UI Toolkit** que muestre solo el campo del tipo elegido.
- `CompareOperator`: `Equal`, `NotEqual`, `Greater`, `GreaterOrEqual`, `Less`, `LessOrEqual`.
- `ChecklistStep` emite el evento `InstructionChanged` con el progreso actualizado (por ejemplo: *"Check the mirrors (2/4)"*) cada vez que se marca un item. Agrega al runner una forma de que un paso pida actualizar la instrucción durante su ejecución.
- `ReachTargetStep` falla con un mensaje claro si no encuentra un target (no lanza excepción).

### 8.1 Cómo crear un paso propio (debe quedar así de simple, y así se documenta)

```csharp
using System;
using ScenarioForge;

[Serializable]
[ScenarioStepMenu("Custom/Press Lever", Tooltip = "Waits until the lever reaches the target angle.")]
public sealed class PressLeverStep : ScenarioStep
{
    [SerializeField] string leverTargetId;
    [SerializeField] float targetAngle = 45f;

    protected override StepStatus OnTick(ScenarioContext context, float deltaTime)   // desde otro assembly es "protected override"
    {
        // lógica del juego…
        return StepStatus.Running;
    }

    public override void Validate(ValidationReport report)
    {
        if (string.IsNullOrWhiteSpace(leverTargetId))
            report.Add(ValidationSeverity.Error, "Lever target is empty.", Id);
    }
}
```

**Renombrar un paso sin romper los assets:** si una clase de paso se renombra o se mueve de namespace, los assets que la usan pierden la referencia. La documentación debe explicar el uso de `[MovedFrom]` (`UnityEngine.Scripting.APIUpdating`) para evitarlo, y debe haber un test que lo demuestre.

---

## 9. Ventana de editor: Scenario Forge

### 9.1 Cómo se abre

- Menú `Window/Scenario Forge`.
- Doble clic en un asset `ScenarioDefinition` (usando `[OnOpenAsset]`).
- Botón **Open in Scenario Forge** en el Inspector del asset.
- La ventana recuerda el último escenario abierto (guarda su GUID en `EditorPrefs`) y lo reabre al volver a abrirse.

### 9.2 Diseño de la ventana

```
┌──────────────────────────────────────────────────────────────────────────────┐
│ [Scenario: ▼ ObjectField] [+ Add Step ▼] [Validate] [Import] [Export] [⌕ Search] │  ← toolbar
├───────────────────────────────┬──────────────────────────────────────────────┤
│ STEPS (7)                     │ STEP DETAILS                                 │
│ ≡ 1 ◷ Wait 2 seconds          │ Type: Wait (Flow)               [⋮ menu]     │
│ ≡ 2 ✉ Wait for "door_opened"  │ Title:        [ Wait 2 seconds          ]    │
│ ≡ 3 ☑ Pre-check   ⚠           │ Instruction:  [ …                       ]    │
│ ≡ 4 ⌖ Drive to dock  ⛔        │ Timeout (s):  [ 0 ]   On failure: [Fail ▼]   │
│ …                             │ Duration:     [ 2.0 ]                        │
│                               │                                              │
├───────────────────────────────┴──────────────────────────────────────────────┤
│ VALIDATION  ⛔ 1 error  ⚠ 2 warnings  ℹ 0                    [Errors|Warn|Info] │
│ ⛔ Step 4 "Drive to dock": Destination target is empty.                       │
│ ⚠ Step 3 "Pre-check": Duplicate signal "mirror_ok".                           │
└──────────────────────────────────────────────────────────────────────────────┘
```

- Panel izquierdo y derecho divididos con `TwoPaneSplitView` (ancho guardado entre sesiones). Panel de validación abajo, colapsable.
- **Estado vacío:** si no hay escenario seleccionado, un mensaje centrado con dos botones: **Create New Scenario** (pide ruta con `EditorUtility.SaveFilePanelInProject`) y **Open Existing…**.
- **Estilos en USS** usando las variables de color del editor de Unity, para que se vea bien con tema claro y oscuro. Nada de colores fijos en código C#.

### 9.3 Lista de pasos (panel izquierdo)

- `ListView` enlazado al `SerializedProperty` `steps` con `BindProperty`, con:
  - `reorderable = true`, `reorderMode = ListViewReorderMode.Animated`;
  - `showBoundCollectionSize = false` (sin esto, la ListView muestra un elemento extra cuando está enlazada a una lista con `[SerializeReference]`);
  - `showAddRemoveFooter = false` (agregar y quitar se hace con los controles propios).
- Cada fila muestra: manija de arrastre, número (1-based), ícono por categoría, `title` (o el nombre del tipo en gris si está vacío), el tipo en texto secundario, e íconos de error o advertencia de la validación.
- **Búsqueda:** filtra por título o por nombre de tipo. **Mientras haya un filtro activo, el reordenamiento se desactiva** (reordenar una lista filtrada produce índices incorrectos) y se muestra un aviso pequeño.
- **Menú contextual (clic derecho)** y **atajos de teclado** (con la ventana enfocada):

| Acción | Atajo |
| --- | --- |
| Duplicate | Ctrl/Cmd + D |
| Delete | Delete / Backspace |
| Copy | Ctrl/Cmd + C |
| Paste (después del seleccionado) | Ctrl/Cmd + V |
| Move Up / Move Down | Alt + ↑ / Alt + ↓ |
| Insert Step Above / Below | (solo menú) |

- **Duplicar y pegar siempre generan un `id` nuevo.**
- **Copiar** serializa el paso a JSON (con el mismo serializador de la sección 10) y lo pone en `EditorGUIUtility.systemCopyBuffer`. **Pegar** valida que el JSON sea un paso válido y, si no, muestra un error en la barra de estado de la ventana, sin lanzar excepción.

### 9.4 Agregar pasos: selector de tipo

- El botón **+ Add Step** abre un `AdvancedDropdown` con búsqueda, agrupado por las categorías de `[ScenarioStepMenu]`. Cada opción muestra el tooltip del atributo.
- Los tipos se obtienen con `TypeCache.GetTypesDerivedFrom<ScenarioStep>()`, filtrando los que cumplen los requisitos de la sección 6.3.
- El paso nuevo se inserta **después del seleccionado** (o al final si no hay selección), con `InsertArrayElementAtIndex` + `managedReferenceValue = Activator.CreateInstance(type)`. Se le asigna un `id` nuevo y queda seleccionado.
- Todo bajo un solo grupo de Undo llamado *"Add Step"*.

### 9.5 Panel de detalles (panel derecho)

- Encabezado: nombre del tipo, categoría y un menú `⋮` con *Change Type…*, *Copy*, *Duplicate*, *Delete* y *Reset to Defaults*.
- Cuerpo: un `PropertyField` enlazado a `steps.Array.data[i]`, sin el `id` (que está oculto).
- **Reconstruir el panel completo** (crear un `PropertyField` nuevo y volver a enlazarlo) cuando: cambia la selección, se hace Undo o Redo (`Undo.undoRedoPerformed`), cambia el tipo del paso, o se reordena la lista. **No reutilizar el `PropertyField` anterior**, para evitar enlaces obsoletos con managed references.
- **Change Type…:** reemplaza el paso por uno del tipo nuevo, conservando `id`, `title`, `instruction`, `timeoutSeconds`, `onFailure` y `maxRetries`. Pide confirmación porque los campos propios del tipo anterior se pierden.

### 9.6 Undo, guardado y estado sucio

- Toda modificación pasa por `SerializedObject` + `ApplyModifiedProperties()`, con un nombre de Undo descriptivo (*"Delete Step"*, *"Reorder Steps"*, *"Import Scenario"*).
- El asset queda marcado como modificado y se guarda con Ctrl/Cmd + S como cualquier asset (`AssetDatabase.SaveAssetIfDirty`).
- Si el asset se borra o se renombra mientras la ventana está abierta, la ventana vuelve al estado vacío sin errores en consola.

### 9.7 Inspector personalizado de `ScenarioDefinition`

- Muestra título, descripción, la cantidad de pasos, un resumen de la validación (por ejemplo, *"1 error, 2 warnings"*) y el botón **Open in Scenario Forge**.
- La lista de pasos **no** se edita desde el Inspector: se edita solo desde la ventana.

---

## 10. Importar y exportar JSON

### 10.1 Formato

```json
{
  "formatVersion": 1,
  "scenario": {
    "title": "Forklift pre-operation check",
    "description": "…",
    "steps": [
      {
        "$type": "ScenarioForge.Steps.WaitStep, ScenarioForge.Runtime",
        "id": "3f2a9c1e5b7d4e8a9c217d4e5f6a8b90",
        "title": "Wait 2 seconds",
        "instruction": "",
        "timeoutSeconds": 0,
        "onFailure": "FailScenario",
        "maxRetries": 0,
        "duration": 2.0
      }
    ]
  }
}
```

### 10.2 Reglas

1. API en el runtime (para poder cargar escenarios en runtime, por ejemplo desde un LMS):
   `ScenarioJson.Export(ScenarioDefinition) → string` y `ScenarioJson.TryImport(string json, out ScenarioData data, out IReadOnlyList<string> errors) → bool`.
   `ScenarioData` expone `Title`, `Description`, `Steps` y `CreateDefinition()`, que arma un `ScenarioDefinition` en memoria listo para ejecutar. `ScenarioJson.FormatVersion` es la versión actual (1).
2. **Serializa lo mismo que serializa Unity:** campos públicos y campos privados con `[SerializeField]`, e ignora `[NonSerialized]`, los campos `readonly` y las referencias a `UnityEngine.Object` (un asset no puede viajar en JSON). Implementa un `ContractResolver` propio para lograrlo. Los enums se escriben como texto.
3. **Seguridad (obligatorio):** `TypeNameHandling.Auto` con un `ISerializationBinder` propio que **solo** acepte tipos que hereden de `ScenarioStep`. Cualquier otro tipo en `$type` genera un error de importación. Así un JSON malicioso no puede instanciar tipos arbitrarios. Hay un test que lo verifica.
4. **Tipos de Unity:** si un paso usa `Vector3`, `Color` u otros structs de Unity, agrega convertidores explícitos. Sin ellos, Newtonsoft entra en bucles con propiedades como `Vector3.normalized`. La v0.1 incluye convertidores para `Vector2`, `Vector3`, `Quaternion` y `Color`.
5. `formatVersion` desconocido o mayor al soportado → error claro, sin importar nada.
6. **Importar en la ventana:** pregunta *Replace* o *Append*. Los IDs que choquen con IDs existentes se regeneran (y se informa cuántos). Todo es un solo grupo de Undo.
7. **Exportar en la ventana:** guarda con `EditorUtility.SaveFilePanel`, con el nombre del asset como nombre sugerido.
8. **Ida y vuelta sin pérdida:** exportar → importar → exportar debe producir el mismo JSON. Hay un test con todos los pasos incluidos.

---

## 11. Depurador en Play Mode

Cuando el editor está en Play Mode, la ventana muestra una barra de depuración extra:

1. **Selector de runner:** lista los `ScenarioRunnerBehaviour` activos de las escenas cargadas, con el nombre del GameObject y del escenario. Selecciona por defecto el que usa el escenario abierto en la ventana.
2. **En la lista de pasos**, cada fila muestra su estado de ejecución: pendiente (gris), activo (resaltado y con el tiempo transcurrido), completado (✓), fallido (✕), saltado (↷) o con tiempo agotado (⏱). La lista hace scroll automático al paso activo.
3. **Controles:** Pause / Resume, Skip Step, Restart y Abort, que llaman a los métodos del runner.
4. **Pestaña Blackboard:** tabla clave / tipo / valor, actualizada en vivo.
5. **Pestaña Event Log:** los últimos 200 eventos (inicio y fin de pasos, señales, cambios de estado), con marca de tiempo. Botón para limpiar y botón para copiar el log al portapapeles.
6. **Aviso visible:** *"Editing the asset does not affect the running instance"*, porque el runner trabaja sobre una copia (sección 6.4).
7. La actualización de la UI usa `schedule.Execute(...).Every(100)`, no cada frame. Al salir de Play Mode, la barra de depuración desaparece sin errores.
8. *(Agregado al implementar M5.)* El log también registra las señales y los errores del jugador (`MistakeReported`). Si el runner elegido ejecuta otro escenario, las filas no muestran estados y aparece un aviso con un botón para abrir ese escenario. Sin runners en la escena, el selector lo indica y los controles quedan desactivados.

---

## 12. Sample incluido: Assembly Procedure

**Objetivo:** demostrar el paquete en menos de un minuto, sin ninguna dependencia extra (sin Input System, sin XR).

- Escena con 3 piezas (primitivas de Unity con colores) y una base. Un "carrito" (cubo) y una zona de destino marcada con un `ScenarioTarget`.
- UI con uGUI: panel con la instrucción actual, la barra de progreso del escenario y botones: *Pick Part A*, *Pick Part B*, *Attach Part*, *Confirm* y *Move Cart* (mientras se mantiene presionado, el carrito avanza hacia la zona).
- Escenario de ejemplo (`AssemblyProcedure.asset`) con los 7 tipos de pasos incluidos: instrucción inicial con confirmación → checklist de piezas → esperar la señal de ensamblaje → asignar un valor en el blackboard → condición del blackboard → mover el carrito a la zona → espera final.
- Pantalla final con el resultado (completado o fallido) y el tiempo total.
- Se registra en `package.json` en `"samples"` con `displayName`, `description` y `path: "Samples~/AssemblyProcedure"`.

---

## 13. Tests requeridos

### 13.1 EditMode (runtime)

- Runner: cada regla de la sección 6.5 (transiciones de estado, avance, timeout, las tres `FailurePolicy`, reintentos, pausa, abortar, escenario vacío, excepción dentro de un paso, orden de eventos).
- **El asset original no cambia** después de una ejecución completa.
- **GC = 0** en `Tick` en estado estable.
- Blackboard: tipos válidos e inválidos, `TryGet` con tipo equivocado, evento de cambio.
- SignalBus: conteo, recorte de espacios, reinicio.
- Cada paso incluido: se completa en el caso correcto, no se completa antes, su validación detecta cada error de su tabla.
- JSON: ida y vuelta con todos los pasos, `formatVersion` inválido, JSON corrupto, `$type` no permitido (rechazado), convertidores de `Vector3` y `Color`.
- Validador: cada regla de la sección 7.2.
- `[MovedFrom]`: un asset de prueba con un tipo renombrado sigue cargando.

### 13.2 EditMode (editor)

- La ventana abre sin errores con y sin escenario asignado.
- Agregar, borrar, duplicar y reordenar pasos modifica el asset, y Undo lo revierte.
- Duplicar y pegar generan IDs nuevos.
- El selector de tipos encuentra todos los pasos incluidos y excluye los abstractos.

### 13.3 PlayMode

- Una escena mínima con `ScenarioRunnerBehaviour` ejecuta un escenario de 3 pasos de principio a fin, emitiendo señales desde un `ScenarioSignalEmitter`.
- `ReachTargetStep` se completa al mover un objeto con `ScenarioTarget` a la zona.
- `ScenarioStepEvents` invoca sus eventos solo para el paso elegido, también cuando el paso se salta.
- `ScenarioSignalListener` reacciona a `SendSignalStep` y a emitters, y sigue funcionando al iniciar el escenario de nuevo.

**Meta de cobertura:** 80% o más en el assembly del runtime (medir con el paquete Code Coverage si es posible; no es requisito de CI).

---

## 14. CI con GitHub Actions

- Workflow `.github/workflows/tests.yml` que corre en cada push y pull request a `main`.
- Usa `game-ci/unity-test-runner@v4`, con `testMode: all`, la versión exacta de Unity del proyecto en `unityVersion`, `githubToken` para publicar los resultados como check, y `actions/upload-artifact@v4` con `if: always()` para subir los resultados.
- Cache de la carpeta `Library` con `actions/cache`, usando un hash de `Assets/**`, `Packages/**` y `ProjectSettings/**` como clave.
- **Licencia:** con licencia Personal, GameCI pide los secrets `UNITY_LICENSE`, `UNITY_EMAIL` y `UNITY_PASSWORD`. El proceso de activación de licencias Personal ha cambiado en el pasado, así que **sigue la guía actual de activación en game.ci** y explícale al autor, paso a paso, qué debe hacer él (crear los secrets en GitHub). No inventes pasos.
- Badge de estado del workflow en el README.

---

## 15. Documentación

1. **README.md** (en inglés):
   - GIF principal arriba (placeholder hasta grabarlo).
   - Qué es y para qué sirve (2–3 líneas).
   - Instalación por URL de Git (con `?path=`).
   - Quick start: crear escenario → agregar pasos → poner `ScenarioRunnerBehaviour` en la escena → Play.
   - Pasos incluidos (tabla).
   - Cómo crear un paso propio (el ejemplo de la sección 8.1).
   - Importar y exportar JSON.
   - Limitaciones conocidas y roadmap (grafo de nodos, pasos en paralelo).
   - Créditos y licencia. Mención transparente: *"Built with AI-assisted development (Claude Code); architecture and every change reviewed by the author."*
2. **Documentation~/**: `index.md`, `getting-started.md`, `creating-custom-steps.md`, `json-format.md`, `debugging.md`, `api-overview.md`.
3. **XML docs** (`///`) en toda la API pública.
4. **CHANGELOG.md** con formato *Keep a Changelog* y versionado semántico. Primera versión: `0.1.0`.

---

## 16. Hitos y criterios de aceptación

Claude Code trabaja **un hito a la vez** y se detiene al final de cada uno para que el autor revise y pruebe en Unity.

### M0: Esqueleto del proyecto
- [ ] Proyecto Unity 6.6 con el paquete embebido y la estructura de la sección 5.
- [ ] `package.json` válido; asmdefs de Runtime, Editor y Tests con referencias correctas.
- [ ] `testables` configurado; un test de prueba pasa en EditMode y PlayMode.
- [ ] `.gitignore` de Unity, `.gitattributes` con LFS, licencia MIT.
- [ ] Workflow de CI creado (puede fallar hasta que el autor configure los secrets).

### M1: Núcleo del runtime
- [ ] `ScenarioDefinition`, `ScenarioStep`, `ScenarioRunner`, `ScenarioContext`, `Blackboard`, `SignalBus`, targets y los 3 componentes.
- [ ] Los 7 pasos incluidos con sus validaciones.
- [ ] Todos los tests de runtime de la sección 13.1 (salvo JSON) pasan.

### M2: Ventana de editor básica
- [ ] Apertura por menú, doble clic e Inspector. Estado vacío funcional.
- [ ] Lista enlazada, reordenable, con búsqueda, menú contextual y atajos.
- [ ] Selector de tipos con categorías y búsqueda.
- [ ] Panel de detalles que se reconstruye correctamente (sección 9.5), incluido *Change Type…*.
- [ ] Undo y Redo funcionan en todas las acciones.
- [ ] Drawer de `BlackboardValue`.

### M3: Validación
- [ ] `ScenarioValidator` con todas las reglas, el panel de validación con filtros, y clic en un problema para seleccionar el paso.
- [ ] Íconos de validación en cada fila de la lista.
- [ ] Manejo de tipos faltantes.
- [ ] Validación por línea de comandos y menú *Validate All Scenarios*.

### M4: JSON y portapapeles
- [x] Exportar e importar (*Replace* y *Append*) desde la ventana.
- [x] Copiar y pegar pasos.
- [x] Binder de seguridad y convertidores.
- [x] Tests de JSON pasan.

### M5: Depurador en Play Mode
- [x] Todo lo de la sección 11.

### M6: Sample, documentación y pulido
- [x] Sample completo y registrado en `package.json`; menú de sincronización de desarrollo.
- [x] README, `Documentation~` y XML docs completos.
- [ ] Revisión visual con tema claro y oscuro.
- [ ] Cero errores y cero advertencias en consola al abrir el proyecto, entrar a Play Mode y salir.
- [ ] CI en verde. Tag `v0.1.0` y release en GitHub.

---

## 17. Material para el portafolio (al terminar)

Grabar estos GIFs o videos cortos (de 5 a 10 segundos cada uno):

1. Crear un escenario y agregar pasos desde el selector con búsqueda.
2. Reordenar pasos arrastrando.
3. Un error de validación: clic en el error → el paso se selecciona → se corrige → el error desaparece.
4. Play Mode con el depurador: el paso activo avanzando en vivo y el log de eventos.
5. Crear un paso propio en código y verlo aparecer en el menú.

Y para el caso de estudio, documentar 2 o 3 decisiones en formato **problema → decisión → resultado**. Candidatas: el asset compartido y la copia en runtime (6.4); el binder de seguridad del JSON (10.2); la reconstrucción del `PropertyField` con managed references (9.5).
