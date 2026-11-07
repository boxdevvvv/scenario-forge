# CLAUDE.md: reglas del proyecto Scenario Forge

Este archivo va en la raíz del repositorio. Claude Code lo lee automáticamente en cada sesión.

## Fuente de verdad
- `docs/SPEC.md` define qué construir. Si algo no está en la especificación o la contradice, **pregunta antes de decidir**.
- Trabaja **un hito a la vez** (sección 16 de la especificación). Al terminar cada hito, detente y entrega: qué hiciste, qué tests agregaste, qué debe probar el autor a mano en Unity y qué quedó pendiente.

## Entorno

- Unity **6.6 (6000.6.8f1)** (el autor cambió la versión desde 6.3 LTS el 2026-11-07). No uses APIs obsoletas ni APIs que no existan en esta versión. Si no estás seguro de que una API existe, búscala en la documentación de Unity 6.6 antes de usarla y dilo.
- Proyecto Unity con el paquete embebido en `Packages/com.yourname.scenarioforge/`.
- No puedes hacer clic en el editor de Unity. Cuando algo requiera el editor (crear escenas, asignar referencias en el Inspector, revisar visualmente), dale al autor instrucciones paso a paso, o escribe un script de editor que lo haga.

## Arquitectura
- La lógica va en clases C# puras. Los `MonoBehaviour` son envoltorios delgados.
- Nada de singletons ni de estado estático mutable. El estado de ejecución vive en `ScenarioContext`.
- El runtime (`ScenarioForge.Runtime`) **nunca** referencia `UnityEditor`. El código de editor va solo en el assembly `ScenarioForge.Editor`.
- Las modificaciones de assets desde el editor pasan siempre por `SerializedObject`, con un nombre de Undo descriptivo.
- Los pasos nunca modifican el asset en runtime: el runner usa una copia (`Object.Instantiate`) y el estado de ejecución va en campos `[NonSerialized]`.

## Estilo de código
- C# con las convenciones de Microsoft: PascalCase para tipos y miembros públicos, camelCase para campos privados (sin prefijo `m_` ni `_`).
- Usa `sealed` por defecto en las clases que no están pensadas para herencia.
- Documentación XML (`///`) en toda la API pública.
- Sin `Debug.Log` olvidados. Los errores se reportan con mensajes que digan qué pasó y cómo arreglarlo.
- Todo en inglés: código, comentarios, UI, commits y documentación del paquete.

## Tests
- Toda regla del runner, todo paso incluido y toda regla de validación tiene un test.
- Agrega o actualiza los tests **en el mismo cambio** que el código que prueban.
- Corre los tests antes de dar un hito por terminado, con el CLI de Unity:
  - con el proyecto cerrado: `unity test . --mode EditMode --output ./TestResults/editmode.xml` (y `--mode PlayMode`);
  - con el editor abierto (paquete de desarrollo `com.unity.pipeline`): `unity command test run --mode editor` / `--mode playmode`.
  Editor instalado en `/Applications/Unity/Hub/Editor/6000.6.8f1/Unity.app`.
- Si un test falla, arréglalo. No lo desactives ni lo borres sin avisar.

## Git
- Commits pequeños y descriptivos en inglés, con el formato `feat:`, `fix:`, `test:`, `docs:`, `chore:`.
- No hagas commit de `Library/`, `Temp/`, `Logs/`, `UserSettings/` ni `TestResults/`.

## Lo que nunca debes hacer
- Inventar APIs de Unity, versiones de paquetes o pasos de configuración que no verificaste.
- Agregar dependencias que no estén en la sección 4 de la especificación.
- Cambiar la API pública definida en la especificación sin preguntar.
