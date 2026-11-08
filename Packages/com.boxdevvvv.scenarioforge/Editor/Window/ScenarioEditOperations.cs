using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ScenarioForge.Editor
{
    /// <summary>
    /// Edits the step list of a <see cref="ScenarioDefinition"/> through <see cref="SerializedObject"/>, so every
    /// change supports Undo and marks the asset dirty. Each operation is one named Undo group.
    /// </summary>
    static class ScenarioEditOperations
    {
        public const string AddStepUndo = "Add Step";
        public const string DeleteStepUndo = "Delete Step";
        public const string DuplicateStepUndo = "Duplicate Step";
        public const string MoveStepUndo = "Move Step";
        public const string ReorderStepsUndo = "Reorder Steps";
        public const string ChangeTypeUndo = "Change Step Type";
        public const string ResetStepUndo = "Reset Step";
        public const string PasteStepUndo = "Paste Step";
        public const string RemoveMissingUndo = "Remove Missing Step";
        public const string ImportReplaceUndo = "Import Scenario (Replace)";
        public const string ImportAppendUndo = "Import Scenario (Append)";

        /// <summary>The serialized steps array.</summary>
        public static SerializedProperty GetSteps(SerializedObject scenario) => scenario.FindProperty(ScenarioDefinition.StepsPropertyName);

        /// <summary>Number of steps, after reading the latest values from the asset.</summary>
        public static int GetStepCount(SerializedObject scenario)
        {
            scenario.Update();
            return GetSteps(scenario).arraySize;
        }

        /// <summary>The step at <paramref name="index"/>, or <c>null</c> when it is missing or out of range.</summary>
        public static ScenarioStep GetStep(SerializedObject scenario, int index)
        {
            SerializedProperty steps = GetSteps(scenario);
            if (index < 0 || index >= steps.arraySize)
                return null;

            return steps.GetArrayElementAtIndex(index).managedReferenceValue as ScenarioStep;
        }

        /// <summary>Creates a step of <paramref name="type"/> with a new id and inserts it at <paramref name="index"/>.</summary>
        /// <returns>The index of the new step.</returns>
        public static int AddStep(SerializedObject scenario, Type type, int index)
        {
            if (!StepTypeCatalog.IsCreatable(type))
                throw new ArgumentException($"'{type?.FullName}' cannot be added as a step. It must be a public, non-abstract, [Serializable] ScenarioStep with a public parameterless constructor.", nameof(type));

            var step = (ScenarioStep)Activator.CreateInstance(type);
            step.AssignNewId();
            return InsertStep(scenario, step, index, AddStepUndo);
        }

        /// <summary>Inserts <paramref name="step"/> at <paramref name="index"/> (clamped to the list).</summary>
        public static int InsertStep(SerializedObject scenario, ScenarioStep step, int index, string undoName)
        {
            BeginUndo(undoName);
            scenario.Update();
            SerializedProperty steps = GetSteps(scenario);
            index = Mathf.Clamp(index, 0, steps.arraySize);
            steps.InsertArrayElementAtIndex(index);
            steps.GetArrayElementAtIndex(index).managedReferenceValue = step;
            Apply(scenario, undoName);
            return index;
        }

        /// <summary>Deletes the step at <paramref name="index"/>.</summary>
        /// <returns>The index to select afterwards, or <c>-1</c> when the list is empty.</returns>
        public static int DeleteStep(SerializedObject scenario, int index)
        {
            scenario.Update();
            SerializedProperty steps = GetSteps(scenario);
            if (index < 0 || index >= steps.arraySize)
                return -1;

            BeginUndo(DeleteStepUndo);
            int sizeBefore = steps.arraySize;
            steps.DeleteArrayElementAtIndex(index);
            if (steps.arraySize == sizeBefore)
                steps.DeleteArrayElementAtIndex(index);

            Apply(scenario, DeleteStepUndo);
            return steps.arraySize == 0 ? -1 : Mathf.Min(index, steps.arraySize - 1);
        }

        /// <summary>
        /// Deletes a step whose type is missing and drops its stored data from the asset. This cannot be undone,
        /// because Unity cannot restore data for a class that does not exist.
        /// </summary>
        public static int RemoveMissingStep(SerializedObject scenario, int index)
        {
            scenario.Update();
            SerializedProperty steps = GetSteps(scenario);
            if (index < 0 || index >= steps.arraySize)
                return -1;

            var definition = (ScenarioDefinition)scenario.targetObject;
            bool isMissing = MissingTypes.FindInSteps(definition).TryGetValue(index, out var missing);
            int next = DeleteStep(scenario, index);
            if (isMissing)
                SerializationUtility.ClearManagedReferenceWithMissingType(definition, missing.ReferenceId);

            Undo.SetCurrentGroupName(RemoveMissingUndo);
            scenario.Update();
            return next;
        }

        /// <summary>Inserts a copy of the step at <paramref name="index"/> right after it, with a new id.</summary>
        /// <returns>The index of the copy, or <c>-1</c> when the step is missing.</returns>
        public static int DuplicateStep(SerializedObject scenario, int index)
        {
            scenario.Update();
            ScenarioStep source = GetStep(scenario, index);
            if (source == null)
                return -1;

            ScenarioStep copy = Clone(source);
            copy.AssignNewId();
            return InsertStep(scenario, copy, index + 1, DuplicateStepUndo);
        }

        /// <summary>Moves a step from <paramref name="from"/> to <paramref name="to"/>.</summary>
        /// <returns>The new index of the step, or <paramref name="from"/> when nothing moved.</returns>
        public static int MoveStep(SerializedObject scenario, int from, int to)
        {
            scenario.Update();
            SerializedProperty steps = GetSteps(scenario);
            if (from < 0 || from >= steps.arraySize || to < 0 || to >= steps.arraySize || from == to)
                return from;

            BeginUndo(MoveStepUndo);
            steps.MoveArrayElement(from, to);
            Apply(scenario, MoveStepUndo);
            return to;
        }

        /// <summary>
        /// Replaces the step at <paramref name="index"/> with a new step of <paramref name="newType"/>, keeping the
        /// id, title, instruction, timeout, failure policy and retries. Fields specific to the old type are lost.
        /// </summary>
        public static bool ChangeStepType(SerializedObject scenario, int index, Type newType)
        {
            scenario.Update();
            ScenarioStep source = GetStep(scenario, index);
            if (source == null || !StepTypeCatalog.IsCreatable(newType))
                return false;

            var replacement = (ScenarioStep)Activator.CreateInstance(newType);
            CopyCommonFields(source, replacement);
            Replace(scenario, index, replacement, ChangeTypeUndo);
            return true;
        }

        /// <summary>Replaces the step at <paramref name="index"/> with a fresh instance of its type, keeping only its id.</summary>
        public static bool ResetStep(SerializedObject scenario, int index)
        {
            scenario.Update();
            ScenarioStep source = GetStep(scenario, index);
            if (source == null)
                return false;

            var replacement = (ScenarioStep)Activator.CreateInstance(source.GetType());
            replacement.Id = source.Id;
            Replace(scenario, index, replacement, ResetStepUndo);
            return true;
        }

        /// <summary>Renames the current Undo group, for changes applied by Unity itself (drag reordering).</summary>
        /// <summary>
        /// Inserts <paramref name="newSteps"/> at <paramref name="index"/> as one Undo group. Steps whose id is
        /// empty or already used (in the scenario or earlier in <paramref name="newSteps"/>) get a new id;
        /// with <paramref name="alwaysNewIds"/> every step gets one (used by Paste).
        /// </summary>
        /// <returns>How many ids were regenerated.</returns>
        public static int InsertSteps(SerializedObject scenario, IReadOnlyList<ScenarioStep> newSteps, int index, bool alwaysNewIds, string undoName)
        {
            BeginUndo(undoName);
            scenario.Update();
            SerializedProperty steps = GetSteps(scenario);
            int regenerated = InsertInto(steps, newSteps, Mathf.Clamp(index, 0, steps.arraySize), alwaysNewIds);
            Apply(scenario, undoName);
            return regenerated;
        }

        /// <summary>
        /// Replaces the title, description and every step with imported content, as one Undo group.
        /// Empty or repeated ids get a new id.
        /// </summary>
        /// <returns>How many ids were regenerated.</returns>
        public static int ReplaceContent(SerializedObject scenario, string title, string description, IReadOnlyList<ScenarioStep> newSteps)
        {
            BeginUndo(ImportReplaceUndo);
            scenario.Update();
            scenario.FindProperty("title").stringValue = title ?? string.Empty;
            scenario.FindProperty("description").stringValue = description ?? string.Empty;
            SerializedProperty steps = GetSteps(scenario);
            steps.ClearArray();
            int regenerated = InsertInto(steps, newSteps, 0, false);
            Apply(scenario, ImportReplaceUndo);
            return regenerated;
        }

        static int InsertInto(SerializedProperty steps, IReadOnlyList<ScenarioStep> newSteps, int index, bool alwaysNewIds)
        {
            var usedIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < steps.arraySize; i++)
            {
                if (steps.GetArrayElementAtIndex(i).managedReferenceValue is ScenarioStep existing && !string.IsNullOrEmpty(existing.Id))
                    usedIds.Add(existing.Id);
            }

            int regenerated = 0;
            for (int i = 0; i < newSteps.Count; i++)
            {
                ScenarioStep step = newSteps[i];
                if (alwaysNewIds || string.IsNullOrEmpty(step.Id) || usedIds.Contains(step.Id))
                {
                    step.AssignNewId();
                    regenerated++;
                }

                usedIds.Add(step.Id);
                steps.InsertArrayElementAtIndex(index + i);
                steps.GetArrayElementAtIndex(index + i).managedReferenceValue = step;
            }

            return regenerated;
        }

        public static void NameCurrentUndoGroup(string undoName) => Undo.SetCurrentGroupName(undoName);

        /// <summary>A deep copy of <paramref name="step"/> made with the editor serializer.</summary>
        public static ScenarioStep Clone(ScenarioStep step)
        {
            var copy = (ScenarioStep)Activator.CreateInstance(step.GetType());
            EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(step), copy);
            return copy;
        }

        static void CopyCommonFields(ScenarioStep from, ScenarioStep to)
        {
            to.Id = from.Id;
            to.Title = from.Title;
            to.Instruction = from.Instruction;
            to.TimeoutSeconds = from.TimeoutSeconds;
            to.OnFailure = from.OnFailure;
            to.MaxRetries = from.MaxRetries;
        }

        static void Replace(SerializedObject scenario, int index, ScenarioStep replacement, string undoName)
        {
            BeginUndo(undoName);
            GetSteps(scenario).GetArrayElementAtIndex(index).managedReferenceValue = replacement;
            Apply(scenario, undoName);
        }

        static void BeginUndo(string undoName)
        {
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName(undoName);
        }

        static void Apply(SerializedObject scenario, string undoName)
        {
            scenario.ApplyModifiedProperties();
            Undo.SetCurrentGroupName(undoName);
        }
    }
}
