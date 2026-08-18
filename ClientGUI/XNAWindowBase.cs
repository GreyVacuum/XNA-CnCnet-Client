using ClientCore;
using ClientCore.I18N;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Linq;
using System.Collections.Generic;

namespace ClientGUI
{
    public class XNAWindowBase : XNAPanel
    {
        public XNAWindowBase(WindowManager windowManager) : base(windowManager)
        {
            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.TILED;
        }

        protected virtual IEnumerable<XNAControl> GetChildrenForINIProcessing()
        {
            return Children;
        }

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

                    // An INItializableWindow normally loads its configuration from a
                    // dedicated {Name}.ini file. When it is registered as an extra
                    // control, initialize it from the host window's INI file instead,
                    // so its [Name] section and $CC child controls can be declared
                    // in the same file that registers the window.
                    if (control is INItializableWindow iniWindow)
                        iniWindow.ExternalIniFile = iniFile;

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
                        case "$TextAnchor":
                            if (control is XNALabel label)
                                label.TextAnchor = (LabelTextAnchorInfo)Enum.Parse(typeof(LabelTextAnchorInfo),
                                    Translation.Instance.LookUp(control, kvp.Key, kvp.Value, false));
                            break;
                        case "$AnchorPoint":
                            if (control is XNALabel anchorLabel)
                            {
                                string[] parts = Translation.Instance.LookUp(control, kvp.Key, kvp.Value, false).Split(',');
                                if (parts.Length != 2)
                                    throw new FormatException("Invalid format for AnchorPoint: " + kvp.Value);
                                anchorLabel.AnchorPoint = new Vector2(
                                    Parser.Instance.GetExprValue(parts[0], control),
                                    Parser.Instance.GetExprValue(parts[1], control));
                            }
                            break;
                        case "$LeftClickAction":
                            string actionValue = Translation.Instance.LookUp(control, kvp.Key, kvp.Value, false);
                            if (actionValue == "Disable")
                                control.LeftClick += (s, e) => control.Disable();
                            break;
                    }
                }
            }

            IEnumerable<XNAControl> children;
            if (control is XNAOptionsPanel optionsPanel)
                children = optionsPanel.GetChildrenForINIProcessing();
            else if (control is XNAWindowBase windowBase)
                children = windowBase.GetChildrenForINIProcessing();
            else if (control is XNAScrollPanel scrollPanel)
                children = scrollPanel.GetChildrenForINIProcessing();
            else
                children = control.Children;

            foreach (var child in children)
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
