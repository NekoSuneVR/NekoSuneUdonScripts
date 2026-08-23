using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace NekoSune.Worlds.Editor
{
    [Serializable]
    internal sealed class NekoLocEntry
    {
        public string k;
        public string v;
    }

    [Serializable]
    internal sealed class NekoLocFile
    {
        public string code;
        public string name;
        public string nativeName;
        public List<NekoLocEntry> entries = new List<NekoLocEntry>();
    }

    internal struct NekoLanguageInfo
    {
        public string Code;
        public string Name;
        public string NativeName;
        public string FilePath;

        public string Display
        {
            get
            {
                if (!string.IsNullOrEmpty(NativeName) && NativeName != Name)
                    return NativeName + " (" + Name + ")";
                return string.IsNullOrEmpty(NativeName) ? Name : NativeName;
            }
        }
    }

    internal static class NekoLoc
    {
        const string PrefKey = "NekoSune.Worlds.Language";
        const string FallbackCode = "en";

        static Dictionary<string, string> _active;
        static Dictionary<string, string> _fallback;
        static List<NekoLanguageInfo> _languages;
        static string _activeCode;

        public static event Action LanguageChanged;

        public static string ActiveCode
        {
            get
            {
                EnsureLoaded();
                return _activeCode;
            }
        }

        public static List<NekoLanguageInfo> Languages
        {
            get
            {
                EnsureLoaded();
                return _languages;
            }
        }

        public static string T(string key, params object[] args)
        {
            EnsureLoaded();

            string value;
            if (_active == null || !_active.TryGetValue(key, out value) || string.IsNullOrEmpty(value))
            {
                if (_fallback == null || !_fallback.TryGetValue(key, out value) || string.IsNullOrEmpty(value))
                    value = key;
            }

            if (args != null && args.Length > 0)
            {
                try
                {
                    value = string.Format(value, args);
                }
                catch (FormatException)
                {
                    // Keep the untranslated value rather than throwing from an editor window.
                }
            }

            return value;
        }

        public static void SetLanguage(string code)
        {
            if (string.IsNullOrEmpty(code) || !HasLanguage(code)) return;

            EditorPrefs.SetString(PrefKey, code);
            _activeCode = code;
            _active = LoadTable(code);

            if (LanguageChanged != null)
                LanguageChanged();

            RepaintAll();
        }

        public static void Reload()
        {
            _languages = null;
            _fallback = null;
            _active = null;
            EnsureLoaded();

            if (LanguageChanged != null)
                LanguageChanged();

            RepaintAll();
        }

        static void EnsureLoaded()
        {
            if (_languages == null)
                ScanLanguages();

            if (_fallback == null)
                _fallback = LoadTable(FallbackCode);

            if (_active == null)
            {
                _activeCode = EditorPrefs.GetString(PrefKey, FallbackCode);
                if (!HasLanguage(_activeCode))
                    _activeCode = FallbackCode;

                _active = LoadTable(_activeCode);
            }
        }

        static bool HasLanguage(string code)
        {
            if (_languages == null) return false;
            for (int i = 0; i < _languages.Count; i++)
                if (_languages[i].Code == code) return true;
            return false;
        }

        static void ScanLanguages()
        {
            _languages = new List<NekoLanguageInfo>();
            string directory = NekoPaths.ToAbsolute(NekoPaths.LanguagesDir);
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
                return;

            string[] files = Directory.GetFiles(directory, "*.json", SearchOption.TopDirectoryOnly);
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < files.Length; i++)
            {
                NekoLocFile file = ReadFile(files[i]);
                if (file == null) continue;

                NekoLanguageInfo info;
                info.Code = string.IsNullOrEmpty(file.code) ? Path.GetFileNameWithoutExtension(files[i]) : file.code;
                info.Name = string.IsNullOrEmpty(file.name) ? info.Code : file.name;
                info.NativeName = string.IsNullOrEmpty(file.nativeName) ? info.Name : file.nativeName;
                info.FilePath = files[i];
                _languages.Add(info);
            }

            _languages.Sort((a, b) =>
            {
                if (a.Code == FallbackCode) return -1;
                if (b.Code == FallbackCode) return 1;
                return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
            });
        }

        static NekoLocFile ReadFile(string path)
        {
            try
            {
                NekoLocFile file = JsonUtility.FromJson<NekoLocFile>(File.ReadAllText(path));
                if (file != null && file.entries == null)
                    file.entries = new List<NekoLocEntry>();
                return file;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[NekoSune Worlds] Failed to load localization file " + path + ": " + e.Message);
                return null;
            }
        }

        static Dictionary<string, string> LoadTable(string code)
        {
            var table = new Dictionary<string, string>(StringComparer.Ordinal);
            if (_languages == null)
                ScanLanguages();

            for (int i = 0; i < _languages.Count; i++)
            {
                if (_languages[i].Code != code) continue;

                NekoLocFile file = ReadFile(_languages[i].FilePath);
                if (file == null) break;

                for (int e = 0; e < file.entries.Count; e++)
                {
                    NekoLocEntry entry = file.entries[e];
                    if (entry == null || string.IsNullOrEmpty(entry.k)) continue;
                    table[entry.k] = entry.v;
                }

                break;
            }

            return table;
        }

        static void RepaintAll()
        {
            EditorWindow[] windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
            for (int i = 0; i < windows.Length; i++)
                if (windows[i] != null) windows[i].Repaint();
        }
    }
}
