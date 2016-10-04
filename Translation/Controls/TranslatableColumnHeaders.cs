using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;

namespace Apex.Translation.Controls
{
    public class TranslatableColumnHeaders : Component
    {
        [Category("Appearance")]
        [Description("The collection of strings to use to retrieve values from the language manager.")]
        public Dictionary<object, string> TranslationStrings
        {
            get { return translationStrings; }
            set { translationStrings = value; }
        }
        private Dictionary<object, string> translationStrings = new Dictionary<object, string>();

        [Category("Appearance")]
        [Description("The collection of strings to use when the language manager doesn't have the string.")]
        public Dictionary<object, string> DefaultStrings
        {
            get { return defaultStrings; }
            set { defaultStrings = value; }
        }
        private Dictionary<object, string> defaultStrings = new Dictionary<object, string>();

        [EditorBrowsable(EditorBrowsableState.Never)]
        public LanguageManager LanguageManager
        {
            set
            {
                LM = value;
                if (LM != null)
                {
                    UpdateString(null);
                    LM.LanguageChanged += LanguageChanged;
                }
            }
        }
        protected LanguageManager LM;

        public event EventHandler<EventArgs> StringChanged;
        protected void FireStringChanged(object sender, EventArgs e)
        {
            StringChanged?.Invoke(sender, e);
        }

        /// <summary>
        /// Adds or updates an existing column's header.
        /// </summary>
        /// <param name="ColumnHeader">The column header to add / update.</param>
        /// <param name="TranslationString">The string to retrieve from the language manager.</param>
        /// <param name="DefaultString">The string to use if the language manager doesn't have a suitable string.</param>
        public void UpdateColumnHeader(ColumnHeader ColumnHeader, string TranslationString, string DefaultString = "")
        {
            UpdateColumnHeaderInternal(ColumnHeader, TranslationString, DefaultString);
        }

        /// <summary>
        /// Adds or updates an existing column's header.
        /// </summary>
        /// <param name="Column">The column to add / update.</param>
        /// <param name="TranslationString">The string to retrieve from the language manager.</param>
        /// <param name="DefaultString">The string to use if the language manager doesn't have a suitable string.</param>
        public void UpdateColumnHeader(DataGridViewColumn Column, string TranslationString, string DefaultString = "")
        {
            UpdateColumnHeaderInternal(Column, TranslationString, DefaultString);
        }

        private void UpdateColumnHeaderInternal(object Column, string TranslationString, string DefaultString)
        {
            if (translationStrings.ContainsKey(Column))
            {
                translationStrings[Column] = TranslationString;
                if (DefaultString != "")
                {
                    if (defaultStrings.ContainsKey(Column))
                    {
                        defaultStrings[Column] = DefaultString;
                    }
                    else
                    {
                        defaultStrings.Add(Column, DefaultString);
                    }
                }
            }
            else
            {
                translationStrings.Add(Column, TranslationString);
                if (DefaultString != "")
                {
                    defaultStrings.Add(Column, DefaultString);
                }
            }
            UpdateString(Column);
        }

        public virtual void LanguageChanged(object sender, EventArgs e)
        {
            UpdateString(null);
        }

        public virtual void UpdateString(object ColumnHeader)
        {
            if (!DesignMode)
            {
                if (LM == null)
                    return;
                if (ColumnHeader == null)
                {
                    foreach (var pair in translationStrings)
                    {
                        var text = "";
                        if (defaultStrings.ContainsKey(pair.Key))
                        {
                            text = LM.GetStringDefault(pair.Value, defaultStrings[pair.Key]);
                        }
                        else
                        {
                            text = LM.GetString(pair.Value);
                        }

                        if (pair.Key is ColumnHeader)
                        {
                            (pair.Key as ColumnHeader).Text = text;
                        }
                        else if (pair.Key is DataGridViewColumn)
                        {
                            (pair.Key as DataGridViewColumn).HeaderText = text;
                        }
                    }
                }
                else
                {
                    if (translationStrings.ContainsKey(ColumnHeader))
                    {
                        var text = "";
                        if (!translationStrings.ContainsKey(ColumnHeader))
                        {
                            if (!defaultStrings.ContainsKey(ColumnHeader))
                            {
                                return;
                            }
                            text = defaultStrings[ColumnHeader];
                        }
                        if (defaultStrings.ContainsKey(ColumnHeader))
                        {
                            text = LM.GetStringDefault(translationStrings[ColumnHeader], defaultStrings[ColumnHeader]);
                        }
                        else
                        {
                            text = LM.GetString(translationStrings[ColumnHeader]);
                        }

                        if (ColumnHeader is ColumnHeader)
                        {
                            (ColumnHeader as ColumnHeader).Text = text;
                        }
                        else if (ColumnHeader is DataGridViewColumn)
                        {
                            (ColumnHeader as DataGridViewColumn).HeaderText = text;
                        }
                    }
                }

                FireStringChanged(this, null);
            }
        }
    }
}
