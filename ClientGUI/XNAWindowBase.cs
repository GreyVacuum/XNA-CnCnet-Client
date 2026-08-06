using ClientCore;
using ClientCore.I18N;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Linq;

namespace ClientGUI
{
    public class XNAWindowBase : XNAPanel
    {
        public XNAWindowBase(WindowManager windowManager) : base(windowManager)
        {
            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.TILED;
        }

        /// <summary>
        /// Reads extra control information from a specific section of an INI file.
        /// </summary>
        /// <param name="iniFile">The INI file.</param>
        /// <param name="sectionName">The section.</param>
        protected virtual void ParseExtraControls(IniFile iniFile, string sectionName)
        {
            var section = iniFile.GetSection(sectionName);

            if (section == null)
                return;

            foreach (var kvp in section.Keys)
            {
                string[] parts = kvp.Value.Split(':');
                if (parts.Length != 2)
                    throw new ClientConfigurationException("Invalid ExtraControl specified in " + Name + ": " + kvp.Value);

                if (!Children.Any(child => child.Name == parts[0]))
                {
                    XNAControl control = ClientGUICreator.GetXnaControl(parts[1]);
                    control.Name = parts[0];
                    control.DrawOrder = -Children.Count;
                    AddChild(control);
                }
            }
        }

        protected virtual void ReadChildControlAttributes(IniFile iniFile)
        {
            bool iniFeaturesEnabled = ClientConfiguration.Instance.AllowedAllAspectsWindowINItializable;

            if (iniFeaturesEnabled)
            {
                ProcessExpressionAttributes(iniFile, this);
            }

            foreach (XNAControl child in Children)
            {
                if (!(typeof(XNAWindowBase).IsAssignableFrom(child.GetType())))
                    child.GetAttributes(iniFile);
            }

            if (iniFeaturesEnabled)
            {
                ProcessExpressionAttributes(iniFile, this);
            }
        }

        private void ProcessExpressionAttributes(IniFile iniFile, XNAControl control)
        {
            if (Parser.Instance == null)
                _ = new Parser(WindowManager);

            Parser.Instance.SetPrimaryControl(this);

            ProcessControlExpressionAttributes(iniFile, control);
        }

        private static void ProcessControlExpressionAttributes(IniFile iniFile, XNAControl control)
        {
            var section = iniFile.GetSection(control.Name);
            if (section != null)
            {
                foreach (var kvp in section.Keys)
                {
                    switch (kvp.Key)
                    {
                        case "$X":
                            control.X = Parser.Instance.GetExprValue(
                                Translation.Instance.LookUp(control, kvp.Key, kvp.Value, false), control);
                            break;
                        case "$Y":
                            control.Y = Parser.Instance.GetExprValue(
                                Translation.Instance.LookUp(control, kvp.Key, kvp.Value, false), control);
                            break;
                        case "$Width":
                            control.Width = Parser.Instance.GetExprValue(
                                Translation.Instance.LookUp(control, kvp.Key, kvp.Value, false), control);
                            break;
                        case "$Height":
                            control.Height = Parser.Instance.GetExprValue(
                                Translation.Instance.LookUp(control, kvp.Key, kvp.Value, false), control);
                            break;
                    }
                }
            }

            foreach (var child in control.Children)
                ProcessControlExpressionAttributes(iniFile, child);
        }

        protected virtual XNAControl CreateControl(GUICreator guiCreator, string controlTypeName, string controlName)
        {
            var control = guiCreator.CreateControl(WindowManager, controlTypeName);
            control.Name = controlName;
            control.DrawOrder = -Children.Count;
            AddChild(control);
            return control;
        }
    }
}
