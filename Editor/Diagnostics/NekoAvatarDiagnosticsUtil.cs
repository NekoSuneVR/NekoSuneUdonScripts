using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Profiling;

namespace NekoSune.Avatars.Editor
{
    internal sealed class NekoExpressionParameterInfo
    {
        public object Raw;
        public string Name;
        public string TypeName;
        public float DefaultValue;
        public bool Saved;
        public bool Synced;
        public int NetworkBits;
    }

    internal sealed class NekoTextureInfo
    {
        public Texture Texture;
        public string AssetPath;
        public int Width;
        public int Height;
        public long RuntimeBytes;
        public string ImportCompression;
        public int MaxTextureSize;
        public bool Mipmaps;
        public int UseCount;
    }

    internal static class NekoAvatarDiagnosticsUtil
    {
        internal const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy;

        public static Type FindType(params string[] fullNames)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int n = 0; n < fullNames.Length; n++)
            {
                string wanted = fullNames[n];
                if (string.IsNullOrEmpty(wanted)) continue;
                for (int a = 0; a < assemblies.Length; a++)
                {
                    try
                    {
                        Type t = assemblies[a].GetType(wanted, false);
                        if (t != null) return t;
                    }
                    catch { }
                }
            }
            return null;
        }

        public static object GetMember(object target, params string[] names)
        {
            if (target == null) return null;
            Type t = target.GetType();
            for (int i = 0; i < names.Length; i++)
            {
                string name = names[i];
                FieldInfo f = t.GetField(name, Members);
                if (f != null)
                {
                    try { return f.GetValue(target); } catch { }
                }
                PropertyInfo p = t.GetProperty(name, Members);
                if (p != null && p.GetIndexParameters().Length == 0)
                {
                    try { return p.GetValue(target, null); } catch { }
                }
            }
            return null;
        }

        public static bool SetMember(object target, object value, params string[] names)
        {
            if (target == null) return false;
            Type t = target.GetType();
            for (int i = 0; i < names.Length; i++)
            {
                string name = names[i];
                FieldInfo f = t.GetField(name, Members);
                if (f != null)
                {
                    try
                    {
                        object converted = ConvertFor(value, f.FieldType);
                        f.SetValue(target, converted);
                        return true;
                    }
                    catch { }
                }
                PropertyInfo p = t.GetProperty(name, Members);
                if (p != null && p.CanWrite && p.GetIndexParameters().Length == 0)
                {
                    try
                    {
                        object converted = ConvertFor(value, p.PropertyType);
                        p.SetValue(target, converted, null);
                        return true;
                    }
                    catch { }
                }
            }
            return false;
        }

        static object ConvertFor(object value, Type destination)
        {
            if (value == null) return null;
            Type source = value.GetType();
            if (destination.IsAssignableFrom(source)) return value;
            if (destination.IsEnum)
            {
                if (value is string) return Enum.Parse(destination, (string)value, true);
                return Enum.ToObject(destination, Convert.ToInt32(value));
            }
            return Convert.ChangeType(value, destination);
        }

        public static Component FindAvatarDescriptor(GameObject avatar)
        {
            if (avatar == null) return null;
            Component[] components = avatar.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                Component c = components[i];
                if (c == null) continue;
                string name = c.GetType().Name;
                if (name == "VRCAvatarDescriptor" || name == "VRC_AvatarDescriptor") return c;
            }
            return null;
        }

        public static List<Component> FindComponentsByTypeName(GameObject avatar, params string[] names)
        {
            var result = new List<Component>();
            if (avatar == null) return result;
            Component[] all = avatar.GetComponentsInChildren<Component>(true);
            for (int i = 0; i < all.Length; i++)
            {
                Component c = all[i];
                if (c == null) continue;
                Type t = c.GetType();
                string simple = t.Name;
                string full = t.FullName ?? simple;
                for (int n = 0; n < names.Length; n++)
                {
                    if (simple == names[n] || full == names[n] || full.EndsWith("." + names[n], StringComparison.Ordinal))
                    {
                        if (!result.Contains(c)) result.Add(c);
                        break;
                    }
                }
            }
            return result;
        }

        public static UnityEngine.Object ExpressionParameters(Component descriptor)
        {
            return GetMember(descriptor, "expressionParameters", "ExpressionParameters") as UnityEngine.Object;
        }

        public static UnityEngine.Object ExpressionsMenu(Component descriptor)
        {
            return GetMember(descriptor, "expressionsMenu", "ExpressionsMenu") as UnityEngine.Object;
        }

        public static List<NekoExpressionParameterInfo> ReadExpressionParameters(Component descriptor)
        {
            var list = new List<NekoExpressionParameterInfo>();
            object asset = ExpressionParameters(descriptor);
            if (asset == null) return list;
            IEnumerable parameters = GetMember(asset, "parameters", "Parameters") as IEnumerable;
            if (parameters == null) return list;

            foreach (object p in parameters)
            {
                if (p == null) continue;
                string name = Convert.ToString(GetMember(p, "name", "Name"));
                if (string.IsNullOrEmpty(name)) continue;
                object type = GetMember(p, "valueType", "ValueType", "type", "Type");
                string typeName = type == null ? "Unknown" : type.ToString();
                object defaultValue = GetMember(p, "defaultValue", "DefaultValue");
                object saved = GetMember(p, "saved", "Saved");
                object synced = GetMember(p, "networkSynced", "NetworkSynced", "synced", "Synced");

                bool isSynced = synced == null || Convert.ToBoolean(synced);
                bool isBool = typeName.IndexOf("Bool", StringComparison.OrdinalIgnoreCase) >= 0;
                list.Add(new NekoExpressionParameterInfo
                {
                    Raw = p,
                    Name = name,
                    TypeName = typeName,
                    DefaultValue = defaultValue == null ? 0f : Convert.ToSingle(defaultValue),
                    Saved = saved != null && Convert.ToBoolean(saved),
                    Synced = isSynced,
                    NetworkBits = isSynced ? (isBool ? 1 : 8) : 0
                });
            }
            return list;
        }

        public static int ParameterBits(List<NekoExpressionParameterInfo> parameters)
        {
            int bits = 0;
            for (int i = 0; i < parameters.Count; i++) bits += parameters[i].NetworkBits;
            return bits;
        }

        public static Dictionary<string, NekoExpressionParameterInfo> ParameterMap(List<NekoExpressionParameterInfo> parameters)
        {
            var map = new Dictionary<string, NekoExpressionParameterInfo>(StringComparer.Ordinal);
            for (int i = 0; i < parameters.Count; i++)
                if (!map.ContainsKey(parameters[i].Name)) map.Add(parameters[i].Name, parameters[i]);
            return map;
        }

        public static List<RuntimeAnimatorController> FindControllers(GameObject avatar, Component descriptor)
        {
            var result = new List<RuntimeAnimatorController>();
            if (avatar != null)
            {
                Animator[] animators = avatar.GetComponentsInChildren<Animator>(true);
                for (int i = 0; i < animators.Length; i++) AddController(result, animators[i] == null ? null : animators[i].runtimeAnimatorController);
            }
            AddControllersFromLayerArray(result, GetMember(descriptor, "baseAnimationLayers", "BaseAnimationLayers"));
            AddControllersFromLayerArray(result, GetMember(descriptor, "specialAnimationLayers", "SpecialAnimationLayers"));
            return result;
        }

        static void AddControllersFromLayerArray(List<RuntimeAnimatorController> result, object layers)
        {
            IEnumerable sequence = layers as IEnumerable;
            if (sequence == null) return;
            foreach (object layer in sequence)
            {
                RuntimeAnimatorController controller = GetMember(layer, "animatorController", "AnimatorController") as RuntimeAnimatorController;
                AddController(result, controller);
            }
        }

        static void AddController(List<RuntimeAnimatorController> list, RuntimeAnimatorController controller)
        {
            if (controller == null) return;
            AnimatorOverrideController over = controller as AnimatorOverrideController;
            RuntimeAnimatorController baseController = over == null ? controller : over.runtimeAnimatorController;
            if (baseController != null && !list.Contains(baseController)) list.Add(baseController);
        }

        public static Dictionary<string, AnimatorControllerParameterType> AnimatorParameterMap(List<RuntimeAnimatorController> controllers)
        {
            var map = new Dictionary<string, AnimatorControllerParameterType>(StringComparer.Ordinal);
            for (int i = 0; i < controllers.Count; i++)
            {
                AnimatorController ac = controllers[i] as AnimatorController;
                if (ac == null) continue;
                AnimatorControllerParameter[] parameters = ac.parameters;
                for (int p = 0; p < parameters.Length; p++)
                    if (!map.ContainsKey(parameters[p].name)) map.Add(parameters[p].name, parameters[p].type);
            }
            return map;
        }

        public static List<NekoTextureInfo> CollectTextures(GameObject avatar)
        {
            var result = new List<NekoTextureInfo>();
            if (avatar == null) return result;
            var byTexture = new Dictionary<Texture, NekoTextureInfo>();
            var seenMaterials = new HashSet<Material>();
            Renderer[] renderers = avatar.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Material[] materials = renderers[i] == null ? null : renderers[i].sharedMaterials;
                if (materials == null) continue;
                for (int m = 0; m < materials.Length; m++)
                {
                    Material material = materials[m];
                    if (material == null) continue;
                    bool firstMaterialUse = seenMaterials.Add(material);
                    Shader shader = material.shader;
                    if (shader == null) continue;
                    int count;
                    try { count = ShaderUtil.GetPropertyCount(shader); } catch { continue; }
                    for (int p = 0; p < count; p++)
                    {
                        try
                        {
                            if (ShaderUtil.GetPropertyType(shader, p) != ShaderUtil.ShaderPropertyType.TexEnv) continue;
                            Texture texture = material.GetTexture(ShaderUtil.GetPropertyName(shader, p));
                            if (texture == null) continue;
                            NekoTextureInfo info;
                            if (!byTexture.TryGetValue(texture, out info))
                            {
                                info = CreateTextureInfo(texture);
                                byTexture.Add(texture, info);
                                result.Add(info);
                            }
                            if (firstMaterialUse) info.UseCount++;
                        }
                        catch { }
                    }
                }
            }
            result.Sort(delegate (NekoTextureInfo a, NekoTextureInfo b) { return b.RuntimeBytes.CompareTo(a.RuntimeBytes); });
            return result;
        }

        static NekoTextureInfo CreateTextureInfo(Texture texture)
        {
            var info = new NekoTextureInfo
            {
                Texture = texture,
                AssetPath = AssetDatabase.GetAssetPath(texture),
                Width = texture.width,
                Height = texture.height,
                UseCount = 0
            };
            try { info.RuntimeBytes = Profiler.GetRuntimeMemorySizeLong(texture); } catch { }
            TextureImporter importer = string.IsNullOrEmpty(info.AssetPath) ? null : AssetImporter.GetAtPath(info.AssetPath) as TextureImporter;
            if (importer != null)
            {
                info.MaxTextureSize = importer.maxTextureSize;
                info.Mipmaps = importer.mipmapEnabled;
                info.ImportCompression = importer.textureCompression.ToString();
            }
            return info;
        }

        public static HashSet<Material> CollectMaterials(GameObject avatar)
        {
            var result = new HashSet<Material>();
            if (avatar == null) return result;
            Renderer[] renderers = avatar.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                Material[] materials = renderers[i].sharedMaterials;
                if (materials == null) continue;
                for (int m = 0; m < materials.Length; m++) if (materials[m] != null) result.Add(materials[m]);
            }
            return result;
        }

        public static bool IsQuestAvatarShader(Material material)
        {
            if (material == null || material.shader == null) return false;
            string name = material.shader.name ?? string.Empty;
            return name.StartsWith("VRChat/Mobile/", StringComparison.OrdinalIgnoreCase);
        }

        public static string ExpressionTypeToAnimatorType(string expressionType)
        {
            if (expressionType.IndexOf("Bool", StringComparison.OrdinalIgnoreCase) >= 0) return "Bool";
            if (expressionType.IndexOf("Int", StringComparison.OrdinalIgnoreCase) >= 0) return "Int";
            if (expressionType.IndexOf("Float", StringComparison.OrdinalIgnoreCase) >= 0) return "Float";
            return expressionType;
        }

        public static string AnimatorTypeName(AnimatorControllerParameterType type)
        {
            if (type == AnimatorControllerParameterType.Bool) return "Bool";
            if (type == AnimatorControllerParameterType.Int) return "Int";
            if (type == AnimatorControllerParameterType.Float) return "Float";
            return "Trigger";
        }

        public static string ObjectPath(Transform root, Transform target)
        {
            if (target == null) return "";
            if (root == null || target == root) return target.name;
            var parts = new List<string>();
            Transform cur = target;
            while (cur != null && cur != root)
            {
                parts.Add(cur.name);
                cur = cur.parent;
            }
            parts.Reverse();
            return string.Join("/", parts.ToArray());
        }

        public static int CountAffectedTransforms(Component physBone)
        {
            if (physBone == null) return 0;
            Transform root = GetMember(physBone, "rootTransform", "RootTransform") as Transform;
            if (root == null) root = physBone.transform;
            var ignored = new HashSet<Transform>();
            IEnumerable ignore = GetMember(physBone, "ignoreTransforms", "IgnoreTransforms") as IEnumerable;
            if (ignore != null) foreach (object o in ignore) if (o is Transform) ignored.Add((Transform)o);
            int count = 0;
            var stack = new Stack<Transform>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                Transform current = stack.Pop();
                for (int i = 0; i < current.childCount; i++)
                {
                    Transform child = current.GetChild(i);
                    if (child == null || ignored.Contains(child)) continue;
                    count++;
                    stack.Push(child);
                }
            }
            return count;
        }

        public static int CountCollection(object value)
        {
            ICollection collection = value as ICollection;
            if (collection != null) return collection.Count;
            IEnumerable enumerable = value as IEnumerable;
            if (enumerable == null) return 0;
            int count = 0;
            foreach (object o in enumerable) if (o != null) count++;
            return count;
        }

        public static bool TrySetAndroidTextureOverride(Texture texture, int maxSize)
        {
            if (texture == null) return false;
            string path = AssetDatabase.GetAssetPath(texture);
            TextureImporter importer = string.IsNullOrEmpty(path) ? null : AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return false;
            TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings("Android");
            settings.name = "Android";
            settings.overridden = true;
            settings.maxTextureSize = maxSize;
            importer.SetPlatformTextureSettings(settings);
            importer.SaveAndReimport();
            return true;
        }

        public static GameObject SuggestedAvatarFromSelection()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null) return null;
            Transform current = selected.transform;
            while (current != null)
            {
                if (FindAvatarDescriptor(current.gameObject) != null) return current.gameObject;
                current = current.parent;
            }
            return selected;
        }
    }
}
