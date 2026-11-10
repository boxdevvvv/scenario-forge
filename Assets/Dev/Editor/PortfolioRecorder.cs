using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ScenarioForge.Dev
{
    /// <summary>
    /// Development only (not part of the package): records editor windows to numbered PNG frames for the
    /// portfolio GIFs, without needing the operating system's screen recording permission. It reads each
    /// window's own pixels through the Editor's internal <c>GUIView.GrabPixels</c>, found by reflection, so it
    /// may stop working in a future Unity version. Frames from several windows are placed side by side.
    /// </summary>
    sealed class PortfolioRecorder : IDisposable
    {
        static readonly FieldInfo ParentField =
            typeof(EditorWindow).GetField("m_Parent", BindingFlags.Instance | BindingFlags.NonPublic);

        static readonly MethodInfo GrabPixels =
            typeof(EditorWindow).Assembly.GetType("UnityEditor.GUIView")?
                .GetMethod("GrabPixels", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        static readonly MethodInfo RepaintImmediately =
            typeof(EditorWindow).Assembly.GetType("UnityEditor.GUIView")?
                .GetMethod("RepaintImmediately", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        readonly EditorWindow[] windows;
        readonly string folder;
        readonly double interval;
        readonly StringBuilder durations = new();
        readonly Stopwatch clock = Stopwatch.StartNew();
        double lastFrameTime = double.NegativeInfinity;
        double lastWrittenTime;
        int frameCount;

        /// <summary>Number of frames written so far.</summary>
        public int FrameCount => frameCount;

        /// <summary>Starts recording <paramref name="windows"/> into <paramref name="folder"/> (cleared first).</summary>
        public PortfolioRecorder(string folder, float framesPerSecond, params EditorWindow[] windows)
        {
            if (ParentField == null || GrabPixels == null || RepaintImmediately == null)
                throw new NotSupportedException("GUIView.GrabPixels was not found in this Unity version, so the recorder cannot read window pixels.");

            this.windows = windows;
            this.folder = folder;
            interval = 1.0 / framesPerSecond;
            if (Directory.Exists(folder))
                Directory.Delete(folder, true);
            Directory.CreateDirectory(folder);
            EditorApplication.update += Update;
        }

        /// <summary>Captures one frame now, whatever the frame rate.</summary>
        public void CaptureNow() => Capture();

        /// <summary>Stops recording and writes <c>frames.txt</c> for ffmpeg's concat demuxer.</summary>
        public void Dispose()
        {
            EditorApplication.update -= Update;
            if (frameCount == 0)
                return;

            double now = clock.Elapsed.TotalSeconds;
            AppendDuration(Math.Max(0.5, now - lastWrittenTime));
            durations.Append(Line(frameCount - 1));
            File.WriteAllText(Path.Combine(folder, "frames.txt"), durations.ToString());
        }

        void Update()
        {
            if (clock.Elapsed.TotalSeconds - lastFrameTime >= interval)
                Capture();
        }

        void Capture()
        {
            double now = clock.Elapsed.TotalSeconds;
            var parts = new List<Texture2D>();
            try
            {
                foreach (EditorWindow window in windows)
                {
                    if (window == null)
                        continue;

                    Texture2D part = Grab(window);
                    OverlayPopups(window, part);
                    parts.Add(part);
                }

                if (parts.Count == 0)
                    return;

                Texture2D frame = Combine(parts);
                if (frameCount > 0)
                    AppendDuration(now - lastWrittenTime);
                File.WriteAllBytes(Path.Combine(folder, $"f{frameCount:D5}.png"), frame.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(frame);
                durations.Append(Line(frameCount));
                frameCount++;
                lastWrittenTime = now;
                lastFrameTime = now;
            }
            finally
            {
                foreach (Texture2D part in parts)
                {
                    if (part != null)
                        UnityEngine.Object.DestroyImmediate(part);
                }
            }
        }

        // Popups (the Add Step search, dropdown menus) are separate windows; draw them where they appear on screen.
        static void OverlayPopups(EditorWindow host, Texture2D target)
        {
            float scale = EditorGUIUtility.pixelsPerPoint;
            foreach (EditorWindow popup in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                string name = popup.GetType().Name;
                if (popup == host || popup.docked || !(name.Contains("Dropdown") || name.Contains("Popup")))
                    continue;

                Texture2D image = Grab(popup);
                try
                {
                    int left = Mathf.RoundToInt((popup.position.x - host.position.x) * scale);
                    int top = Mathf.RoundToInt((popup.position.y - host.position.y) * scale);
                    int x0 = Mathf.Max(0, left), y0 = Mathf.Max(0, top);
                    int x1 = Mathf.Min(target.width, left + image.width), y1 = Mathf.Min(target.height, top + image.height);
                    if (x1 <= x0 || y1 <= y0)
                        continue;

                    // Grabbed textures are stored top row first, so the top offset is used as is.
                    Color[] block = image.GetPixels(x0 - left, y0 - top, x1 - x0, y1 - y0);
                    target.SetPixels(x0, y0, x1 - x0, y1 - y0, block);
                    target.Apply();
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(image);
                }
            }
        }

        static string Line(int index) => $"file 'f{index:D5}.png'\n";

        void AppendDuration(double seconds) =>
            durations.Append("duration ").Append(seconds.ToString("0.###", CultureInfo.InvariantCulture)).Append('\n');

        static Texture2D Grab(EditorWindow window)
        {
            object view = ParentField.GetValue(window);
            float scale = EditorGUIUtility.pixelsPerPoint;
            int width = Mathf.RoundToInt(window.position.width * scale);
            int height = Mathf.RoundToInt(window.position.height * scale);
            var target = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
            var previous = RenderTexture.active;
            try
            {
                // Without a repaint first, a view grabbed after another one can come out scaled or black.
                RepaintImmediately.Invoke(view, null);
                GrabPixels.Invoke(view, new object[] { target, new Rect(0, 0, width, height) });
                RenderTexture.active = target;
                var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();
                return texture;
            }
            finally
            {
                RenderTexture.active = previous;
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        static Texture2D Combine(List<Texture2D> parts)
        {
            if (parts.Count == 1)
            {
                Texture2D only = parts[0];
                parts.Clear();
                return only;
            }

            const int gap = 4;
            int width = -gap, height = 0;
            foreach (Texture2D part in parts)
            {
                width += part.width + gap;
                height = Mathf.Max(height, part.height);
            }

            var frame = new Texture2D(width, height, TextureFormat.RGB24, false);
            var fill = new Color32[width * height];
            var background = new Color32(25, 25, 25, 255);
            for (int i = 0; i < fill.Length; i++)
                fill[i] = background;
            frame.SetPixels32(fill);

            int x = 0;
            foreach (Texture2D part in parts)
            {
                frame.SetPixels(x, height - part.height, part.width, part.height, part.GetPixels());
                x += part.width + gap;
            }

            frame.Apply();
            return frame;
        }

        sealed class Stopwatch
        {
            readonly double start = EditorApplication.timeSinceStartup;

            public static Stopwatch StartNew() => new();

            public TimeSpan Elapsed => TimeSpan.FromSeconds(EditorApplication.timeSinceStartup - start);
        }
    }
}

namespace ScenarioForge.Dev
{
    /// <summary>
    /// Development only: one recording at a time, started and stopped from scripts (for example the Unity CLI).
    /// The static field is acceptable here because this is dev tooling, not package code.
    /// </summary>
    static class PortfolioRecording
    {
        static PortfolioRecorder current;

        /// <summary>Starts recording the open windows whose type names are given, side by side.</summary>
        internal static string Start(string folder, float framesPerSecond, params string[] windowTypeNames)
        {
            Stop();
            var windows = new System.Collections.Generic.List<UnityEditor.EditorWindow>();
            foreach (string name in windowTypeNames)
            {
                foreach (var window in UnityEngine.Resources.FindObjectsOfTypeAll<UnityEditor.EditorWindow>())
                {
                    if (window.GetType().Name == name && !window.docked)
                    {
                        windows.Add(window);
                        break;
                    }
                }
            }

            if (windows.Count == 0)
                return "No matching windows are open.";

            current = new PortfolioRecorder(folder, framesPerSecond, windows.ToArray());
            current.CaptureNow();
            return $"Recording {windows.Count} window(s) to {folder}.";
        }

        /// <summary>Stops the current recording. Returns how many frames were written.</summary>
        internal static int Stop()
        {
            if (current == null)
                return 0;

            int frames = current.FrameCount;
            current.Dispose();
            current = null;
            return frames;
        }
    }
}
