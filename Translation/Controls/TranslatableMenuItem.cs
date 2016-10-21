using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Apex.Translation.Controls
{
    [ProvideProperty("Translation String", typeof(ToolStripMenuItem))]
    [ProvideProperty("Default String", typeof(ToolStripMenuItem))]
    class TranslatableMenuItem : Component, IExtenderProvider
    {
        private Dictionary<ToolStripMenuItem, string> translationStrings;
        private Dictionary<ToolStripMenuItem, string> defaultStrings;

        public bool CanExtend(object Extendee)
        {
            return Extendee is ToolStripMenuItem;
        }

        public void SetTranslationString(ToolStripMenuItem Extendee, string Value)
        {
            if (string.IsNullOrWhiteSpace(Value))
            {
                translationStrings.Remove(Extendee);
            }
            else
            {
                translationStrings[Extendee] = Value;
            }
        }

        [DisplayName("Translation String")]
        [ExtenderProvidedProperty()]
        [Category("Appearance")]
        [Description("The string to retrieve from the language manager.")]
        public string GetTranslationString(ToolStripMenuItem Extendee)
        {
            if (translationStrings.ContainsKey(Extendee))
            {
                return translationStrings[Extendee];
            }
            else
            {
                return String.Empty;
            }
        }

        public void SetDefaultString(ToolStripMenuItem Extendee, string Value)
        {
            if (string.IsNullOrWhiteSpace(Value))
            {
                defaultStrings.Remove(Extendee);
            }
            else
            {
                defaultStrings[Extendee] = Value;
            }
        }

        [DisplayName("Default String")]
        [ExtenderProvidedProperty()]
        [Category("Appearance")]
        [Description("The string to use when the language manager doesn't have one.")]
        public string GetDefaultString(ToolStripMenuItem Extendee)
        {
            if (defaultStrings.ContainsKey(Extendee))
            {
                return defaultStrings[Extendee];
            }
            else
            {
                return String.Empty;
            }
        }

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
            if (!DesignMode)
            {
                if (LM == null)
                    return;
                foreach (var pair in translationStrings)
                {
                    if (defaultStrings.ContainsKey(pair.Key))
                    {
                        pair.Key.Text = LM.GetStringDefault(translationStrings[pair.Key], defaultStrings[pair.Key]);
                    }
                    else
                    {
                        pair.Key.Text = LM.GetString(translationStrings[pair.Key]);
                    }
                }

                FireStringChanged(this, e);
            }
        }
    }
}
