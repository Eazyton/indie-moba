using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace IndieMoba.EditorTools
{
    public static class GameViewResolutionPresets
    {
        private static readonly Assembly EditorAssembly = typeof(EditorWindow).Assembly;
        private const int DefaultReferenceHeight = 540;
        private const int PreviewZoom = 2;
        private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags AnyStatic = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        [MenuItem("IndieMoba/Game View/Desktop 16:9")]
        public static void SelectDesktop() => Select(16f / 9f, "IndieMoba 16:9");

        [MenuItem("IndieMoba/Game View/Mobile 19.5:9")]
        public static void SelectMobile() => Select(19.5f / 9f, "IndieMoba 19.5:9");

        private static void Select(float aspect, string label)
        {
            int height = ReferenceHeight() * PreviewZoom;
            int width = Mathf.RoundToInt(height * aspect / 2f) * 2;
            try
            {
                int index = FindOrAddFixedSize(width, height, label);
                ApplyToGameView(index);
                Debug.Log($"[GameViewResolutionPresets] Game View set to {label} ({width}x{height})");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[GameViewResolutionPresets] Could not set Game View automatically ({e.Message}). " +
                    $"Add a Fixed Resolution {width}x{height} in the Game View size dropdown instead.");
            }
        }

        private static int ReferenceHeight()
        {
            var ppc = UnityEngine.Object.FindFirstObjectByType<PixelPerfectCamera>();
            return ppc != null && ppc.refResolutionY > 0 ? ppc.refResolutionY : DefaultReferenceHeight;
        }

        private static int FindOrAddFixedSize(int width, int height, string label)
        {
            var sizesType = EditorAssembly.GetType("UnityEditor.GameViewSizes");
            var sizeType = EditorAssembly.GetType("UnityEditor.GameViewSize");
            var sizeKindType = EditorAssembly.GetType("UnityEditor.GameViewSizeType");
            var singletonType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var sizes = singletonType.GetProperty("instance", AnyStatic).GetValue(null);
            var groupType = sizesType.GetProperty("currentGroupType", AnyInstance).GetValue(sizes);
            var group = sizesType.GetMethod("GetGroup", AnyInstance).Invoke(sizes, new[] { groupType });

            var groupClass = group.GetType();
            int total = (int)groupClass.GetMethod("GetTotalCount", AnyInstance).Invoke(group, null);
            var getSize = groupClass.GetMethod("GetGameViewSize", AnyInstance);
            var widthProp = sizeType.GetProperty("width", AnyInstance);
            var heightProp = sizeType.GetProperty("height", AnyInstance);
            var kindProp = sizeType.GetProperty("sizeType", AnyInstance);
            var fixedKind = Enum.Parse(sizeKindType, "FixedResolution");

            for (int i = 0; i < total; i++)
            {
                var size = getSize.Invoke(group, new object[] { i });
                if ((int)widthProp.GetValue(size) == width && (int)heightProp.GetValue(size) == height &&
                    kindProp.GetValue(size).Equals(fixedKind))
                {
                    return i;
                }
            }

            var ctor = sizeType.GetConstructor(new[] { sizeKindType, typeof(int), typeof(int), typeof(string) });
            var newSize = ctor.Invoke(new[] { fixedKind, width, height, label });
            groupClass.GetMethod("AddCustomSize", AnyInstance).Invoke(group, new[] { newSize });
            sizesType.GetMethod("SaveToHDD", AnyInstance)?.Invoke(sizes, null);
            return total;
        }

        private static void ApplyToGameView(int index)
        {
            var gameViewType = EditorAssembly.GetType("UnityEditor.GameView");
            var window = EditorWindow.GetWindow(gameViewType);
            var callback = gameViewType.GetMethod("SizeSelectionCallback", AnyInstance);
            if (callback != null)
            {
                callback.Invoke(window, new object[] { index, null });
            }
            else
            {
                gameViewType.GetProperty("selectedSizeIndex", AnyInstance).SetValue(window, index);
            }
            window.Repaint();
        }
    }
}
