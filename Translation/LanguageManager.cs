using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Linq;
using static System.Windows.Forms.Control;

namespace Apex.Translation
{
    public class LanguageManager
    {
        public string MainLanguage
        {
            get { return mainLanguage; }
            set
            {
                mainLanguage = value;
                OnLanguageChanged();
            }
        }
        private string mainLanguage;
        public string FallbackLanguage
        {
            get { return fallbackLanguage; }
            set
            {
                fallbackLanguage = value;
                OnLanguageChanged();
            }
        }
        private string fallbackLanguage;

        public event EventHandler<EventArgs> LanguageChanged;

        readonly Dictionary<string, Language> Languages;

        protected void OnLanguageChanged()
        {
            var handler = LanguageChanged;
            LanguageChanged?.Invoke(this, null);
        }

        /// <summary>
        /// Initialises the language manager and loads languages.
        /// </summary>
        public LanguageManager(string Path, bool IgnoreFileNotFound = false)
        {
            Languages = new Dictionary<string, Language>();

            if (!Directory.Exists(Path))
            {
                if (IgnoreFileNotFound)
                    return;
                else
                    throw new FileNotFoundException("Language folder not found, the program will still work but the interface will use identifier names instead.");
            }

            foreach (var File in Directory.GetFiles(Path, "*.lang"))
            {
                AddLanguage(File);
            }
        }

        /// <summary>
        /// Gets all the language without the translation strings.
        /// </summary>
        /// <returns>List of languages.</returns>
        public List<Language> GetLanguages()
        {
            var languages = new List<Language>();

            foreach (var Pair in Languages)
            {
                languages.Add(new Language(Pair.Value.Code, Pair.Value.Name, Pair.Value.Description, Pair.Value.Author));
            }

            return languages;
        }

        /// <summary>
        /// Gets the requested string in the current language or the key if it doesn't exist.
        /// </summary>
        /// <param name="Key">String to retrieve.</param>
        /// <returns>The language specific string.</returns>
        public string GetString(string Key)
        {
            var Value = GetString(Key, mainLanguage);
            if (Value != Key)
                return Value;

            Value = GetString(Key, fallbackLanguage);
            return Value;
        }

        /// <summary>
        /// Gets the requested string in the specified language or the key if it doesn't exist.
        /// </summary>
        /// <param name="Key">String to retrieve.</param>
        /// <param name="Language">The language code to use.</param>
        /// <returns>The language specific string.</returns>
        public string GetString(string Key, string Language)
        {
            if (Languages.ContainsKey(Language))
                return Languages[Language].GetString(Key);

            return Key;
        }

        /// <summary>
        /// Gets the requested string in the current language or returns the default if it doesn't exist.
        /// </summary>
        /// <param name="Key">The string to retrieve.</param>
        /// <param name="Default">What to return if the string does not exist.</param>
        /// <returns>The language specific string.</returns>
        public string GetStringDefault(string Key, string Default)
        {
            var Value = GetString(Key);
            if (Value == Key)
                return Default;

            return Value;
        }

        /// <summary>
        /// Adds a language from a file.
        /// </summary>
        /// <param name="Filename">The path to the file to load.</param>
        private void AddLanguage(string Filename)
        {
            using (FileStream fs = File.Open(Filename, FileMode.Open))
            {
                using (StreamReader r = new StreamReader(fs))
                {
                    var Name = r.ReadLine();
                    var Description = r.ReadLine();
                    var Author = r.ReadLine();
                    var language = new Language(Path.GetFileNameWithoutExtension(Filename), Name, Description, Author);
                    while (!r.EndOfStream)
                    {
                        var Line = r.ReadLine();
                        // Ignore blank lines and comments.
                        if (string.IsNullOrWhiteSpace(Line) || Line.Trim().StartsWith("#"))
                            continue;

                        // STRING_NAME=Output string
                        // STRING_NAME = Output string
                        var Parts = Line.Split('=');
                        if (Parts.Length != 2)
                            continue;

                        language.AddString(Parts[0].Trim(), Parts[1].Trim());
                    }
                    Languages.Add(Path.GetFileNameWithoutExtension(Filename), language);
                }
            }
        }

        /// <summary>
        /// Attempts to find any compatible components in the specified component container and
        /// set the language manager instance to this one.
        /// </summary>
        /// <param name="Components">The component collection to search.</param>
        public void AddComponents(IEnumerable<Component> Components)
        {
            var bindFlags = BindingFlags.Instance | BindingFlags.Public;
            var searchQueue = new Queue<Component>();
            foreach (Component component in Components)
            {
                var props = component.GetType().GetProperties(bindFlags);
                foreach (var prop in props)
                {
                    if (prop.Name == nameof(LanguageManager))
                    {
                        prop.SetValue(component, this);
                    }
                }
            }
        }

        /// <summary>
        /// Attempts to find any compatible components in the specified control container and
        /// set the language manager instance to this one.
        /// </summary>
        /// <param name="Controls">The control collection to search.</param>
        public void AddControls(IEnumerable<Control> Controls)
        {
            var bindFlags = BindingFlags.Instance | BindingFlags.Public;
            var searchQueue = new Queue<Control>();
            foreach (Control control in Controls)
            {
                searchQueue.Enqueue(control);
            }

            while (searchQueue.Count > 0)
            {
                var component = searchQueue.Dequeue();
                var props = component.GetType().GetProperties(bindFlags);
                foreach (var prop in props)
                {
                    if (prop.Name == nameof(LanguageManager))
                    {
                        prop.SetValue(component, this);
                    }
                    else if (prop.Name == "Controls")
                    {
                        var children = (Control.ControlCollection)prop.GetValue(component);
                        foreach (Control child in children)
                        {
                            searchQueue.Enqueue(child);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Attempts to find any compatible components in the specified control container and
        /// set the language manager instance to this one.
        /// </summary>
        /// <param name="Controls">The control collection to search.</param>
        public void AddControls(ControlCollection Controls)
        {
            var bindFlags = BindingFlags.Instance | BindingFlags.Public;
            var searchQueue = new Queue<Control>();
            foreach (Control control in Controls)
            {
                searchQueue.Enqueue(control);
            }

            while (searchQueue.Count > 0)
            {
                var component = searchQueue.Dequeue();
                var props = component.GetType().GetProperties(bindFlags);
                foreach (var prop in props)
                {
                    if (prop.Name == nameof(LanguageManager))
                    {
                        prop.SetValue(component, this);
                    }
                    else if (prop.Name == "Controls")
                    {
                        var children = (Control.ControlCollection)prop.GetValue(component);
                        foreach (Control child in children)
                        {
                            searchQueue.Enqueue(child);
                        }
                    }
                }
            }
        }

        public bool UseOSLanguage(string Default)
        {
            var currentCulture = CultureInfo.InstalledUICulture;
            var langs = GetLanguages();
            if (langs.Count((x) => x.Code == currentCulture.Name) == 1)
            {
                // Found the OS language.
                MainLanguage = currentCulture.Name;
                FallbackLanguage = Default;
                return true;
            }
            else
            {
                MainLanguage = Default;
                return false;
            }
        }
    }
}