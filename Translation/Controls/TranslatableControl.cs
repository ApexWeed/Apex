using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace Apex.Translation.Controls
{
    public class TranslatableControl : Component
    {
        [Category("Data")]
        [Description("Sets the Control object to translate.")]
        public Control Control
        {
            get { return control; }
            set { control = value; }
        }
        protected Control control;

        [Category("Data")]
        [Description("The field to set as the translation.")]
        public string FieldName
        {
            get { return fieldName; }
            set { fieldName = value; }
        }
        private string fieldName = "Text";

        [Category("Appearance")]
        [Description("The collection of strings to use to retrieve values from the language manager.")]
        public string TranslationString
        {
            get { return translationString; }
            set { translationString = value; }
        }
        private string translationString;

        [Category("Appearance")]
        [Description("The collection of strings to use when the language manager doesn't have the string.")]
        public string DefaultString
        {
            get { return defaultString; }
            set { defaultString = value; }
        }
        private string defaultString;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public LanguageManager LanguageManager
        {
            set
            {
                LM = value;
                if (LM != null)
                {
                    UpdateString();
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

        public virtual void LanguageChanged(object sender, EventArgs e)
        {
            UpdateString();
        }

        public virtual void UpdateString()
        {
            if (control != null)
            {
                if (!DesignMode)
                {
                    if (LM == null)
                        return;
                    if (string.IsNullOrWhiteSpace(translationString))
                    {
                        if (string.IsNullOrWhiteSpace(defaultString))
                        {
                            return;
                        }
                        SetText(defaultString);
                    }
                    if (string.IsNullOrWhiteSpace(defaultString))
                    {
                        SetText(LM.GetString(translationString));
                    }
                    else
                    {
                        SetText(LM.GetStringDefault(translationString, defaultString));
                    }

                    FireStringChanged(this, null);
                }
            }
        }

        private void SetText(string Text)
        {
            if (FieldName == "Text")
            {
                Control.Text = Text;
            }
            else
            {
                var Fields = Control.GetType().GetFields();
                foreach (var Field in Fields)
                {
                    if (Field.Name == FieldName)
                    {
                        Field.SetValue(Control, Text);
                    }
                }
            }
        }
    }
}
