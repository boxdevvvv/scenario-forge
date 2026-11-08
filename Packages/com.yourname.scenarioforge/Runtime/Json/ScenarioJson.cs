using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using ScenarioForge.Json;

namespace ScenarioForge
{
    /// <summary>
    /// Exports scenarios to JSON and imports them back, in the editor or at runtime (for example a
    /// scenario downloaded from a learning platform). Only <see cref="ScenarioStep"/> types can be
    /// created from a <c>$type</c>; anything else is rejected.
    /// </summary>
    public static class ScenarioJson
    {
        /// <summary>The format version written by <see cref="Export"/> and the newest one <see cref="TryImport"/> reads.</summary>
        public const int FormatVersion = 1;

        const string FormatVersionKey = "formatVersion";
        const string ScenarioKey = "scenario";
        const string StepsKey = "steps";
        const string TypeKey = "$type";

        /// <summary>Serializes <paramref name="definition"/> to indented JSON.</summary>
        /// <exception cref="ArgumentNullException">When <paramref name="definition"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">When a step has a missing type (its class was deleted or renamed).</exception>
        public static string Export(ScenarioDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition), "Pass the ScenarioDefinition to export.");

            return ExportSteps(definition.Title, definition.Description, definition.Steps);
        }

        /// <summary>
        /// Parses a JSON scenario. Nothing is created unless the whole file is valid.
        /// </summary>
        /// <param name="json">The JSON text.</param>
        /// <param name="data">The scenario when the import succeeds; otherwise <c>null</c>.</param>
        /// <param name="errors">What is wrong with the JSON and how to fix it; empty when the import succeeds.</param>
        /// <returns><c>true</c> when the JSON is a valid scenario.</returns>
        public static bool TryImport(string json, out ScenarioData data, out IReadOnlyList<string> errors)
        {
            var problems = new List<string>();
            errors = problems;
            data = null;

            JObject scenario = ReadEnvelope(json, problems);
            if (scenario == null)
                return false;

            CheckSteps(scenario[StepsKey], problems);
            if (problems.Count > 0)
                return false;

            ScenarioContent content;
            try
            {
                content = scenario.ToObject<ScenarioContent>(JsonSerializer.Create(CreateSettings()));
            }
            catch (JsonException exception)
            {
                // Newtonsoft wraps binder errors ("Error resolving type…"); show the cause, which says how to fix it.
                string reason = exception.InnerException is JsonException inner ? inner.Message : exception.Message;
                problems.Add($"The scenario could not be read: {reason}");
                return false;
            }

            if (content == null)
            {
                problems.Add("The \"scenario\" object is empty.");
                return false;
            }

            data = new ScenarioData(content.title, content.description, content.steps ?? new List<ScenarioStep>());
            return true;
        }

        /// <summary>Serializes a list of steps with a title and description, for export and the clipboard.</summary>
        internal static string ExportSteps(string title, string description, IEnumerable<ScenarioStep> steps)
        {
            var list = new List<ScenarioStep>();
            int index = 0;
            foreach (ScenarioStep step in steps)
            {
                index++;
                if (step == null)
                    throw new InvalidOperationException($"Step {index} has a missing type (its class was deleted or renamed), so it cannot be exported. Fix or remove it in the Scenario Forge window first.");
                list.Add(step);
            }

            var file = new ScenarioFile
            {
                formatVersion = FormatVersion,
                scenario = new ScenarioContent { title = title ?? string.Empty, description = description ?? string.Empty, steps = list }
            };
            return JsonConvert.SerializeObject(file, Formatting.Indented, CreateSettings());
        }

        static JObject ReadEnvelope(string json, List<string> problems)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                problems.Add("The JSON is empty. Choose a file exported by Scenario Forge.");
                return null;
            }

            JObject root;
            try
            {
                root = JObject.Parse(json, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
            }
            catch (JsonException exception)
            {
                problems.Add($"The text is not valid JSON: {exception.Message}");
                return null;
            }

            JToken version = root[FormatVersionKey];
            if (version == null)
            {
                problems.Add("\"formatVersion\" is missing, so this does not look like a Scenario Forge file.");
                return null;
            }

            if (version.Type != JTokenType.Integer)
            {
                problems.Add($"\"formatVersion\" must be a whole number, but it is {version.Type}.");
                return null;
            }

            long number = version.Value<long>();
            if (number > FormatVersion)
            {
                problems.Add($"\"formatVersion\" {number} is newer than this version of Scenario Forge supports ({FormatVersion}). Update the package to import it.");
                return null;
            }

            if (number < 1)
            {
                problems.Add($"\"formatVersion\" {number} is not valid. The first version is 1.");
                return null;
            }

            if (root[ScenarioKey] is not JObject scenario)
            {
                problems.Add("The \"scenario\" object is missing.");
                return null;
            }

            return scenario;
        }

        static void CheckSteps(JToken steps, List<string> problems)
        {
            if (steps == null || steps.Type == JTokenType.Null)
                return;

            if (steps is not JArray array)
            {
                problems.Add("\"steps\" must be an array.");
                return;
            }

            for (int i = 0; i < array.Count; i++)
            {
                if (array[i] is not JObject step)
                {
                    problems.Add($"Step {i + 1} is not an object.");
                    continue;
                }

                if (step[TypeKey] is not JValue { Type: JTokenType.String } typeToken)
                {
                    problems.Add($"Step {i + 1} has no \"$type\", so its step type is unknown. Add the type name, for example \"ScenarioForge.Steps.WaitStep, ScenarioForge.Runtime\".");
                    continue;
                }

                ScenarioStepBinder.Split((string)typeToken, out string typeName, out string assemblyName);
                if (!ScenarioStepBinder.TryResolve(assemblyName, typeName, out _, out string error))
                    problems.Add($"Step {i + 1}: {error}");
            }
        }

        static JsonSerializerSettings CreateSettings()
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new UnitySerializationContractResolver(),
                SerializationBinder = new ScenarioStepBinder(),
                TypeNameHandling = TypeNameHandling.Auto,
                MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead,
                ObjectCreationHandling = ObjectCreationHandling.Replace,
                ReferenceLoopHandling = ReferenceLoopHandling.Error,
                MaxDepth = 64,
                Culture = System.Globalization.CultureInfo.InvariantCulture
            };
            settings.Converters.Add(new StringEnumConverter());
            foreach (JsonConverter converter in UnityStructConverters.Create())
                settings.Converters.Add(converter);
            return settings;
        }

        // Field names are the JSON keys; the contract resolver serializes public fields.
#pragma warning disable IDE1006
        sealed class ScenarioFile
        {
            public int formatVersion;
            public ScenarioContent scenario;
        }

        sealed class ScenarioContent
        {
            public string title;
            public string description;
            public List<ScenarioStep> steps;
        }
#pragma warning restore IDE1006
    }
}
