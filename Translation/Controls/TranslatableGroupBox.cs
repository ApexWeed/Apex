﻿using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace Apex.Translation.Controls
{
    public class TranslatableGroupBox : GroupBox
    {
        [Category("Appearance")]
        [Description("The string to retrieve from the language manager.")]
        public string TranslationString
        {
            get { return translationString; }
            set
            {
                translationString = value;
                UpdateString(null, null);
            }
        }
        protected string translationString;

        [Category("Appearance")]
        [Description("The string to use when the language manager doesn't have a suitable string.")]
        [DefaultValue("")]
        public string DefaultString
        {
            get { return defaultString; }
            set { defaultString = value; }
        }
        protected string defaultString;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public LanguageManager LanguageManager
        {
            set
            {
                LM = value;
                if (LM != null)
                {
                    UpdateString(LM, null);
                    LM.LanguageChanged += UpdateString;
                }
            }
        }
        protected LanguageManager LM;

        public event EventHandler<EventArgs> StringChanged;
        protected void FireStringChanged(object sender, EventArgs e)
        {
            StringChanged?.Invoke(sender, e);
        }

        public virtual void UpdateString(object sender, EventArgs e)
        {
            if (DesignMode)
            {
                Text = translationString;
            }
            else
            {
                if (LM == null)
                    return;
                if (string.IsNullOrWhiteSpace(translationString))
                {
                    if (string.IsNullOrWhiteSpace(defaultString))
                    {
                        return;
                    }
                    Text = defaultString;
                }
                if (string.IsNullOrWhiteSpace(defaultString))
                {
                    Text = LM.GetString(translationString);
                }
                else
                {
                    Text = LM.GetStringDefault(translationString, defaultString);
                }

                FireStringChanged(this, e);
            }
        }
    }
}
