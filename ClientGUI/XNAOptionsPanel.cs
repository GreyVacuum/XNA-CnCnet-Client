using ClientCore;
using ClientGUI.Settings;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;

namespace ClientGUI
{
    /// <summary>
    /// A base class for all option panels.
    /// Handles custom game-specific panel options
    /// defined in INI files.
    /// </summary>
    public abstract class XNAOptionsPanel : XNAWindowBase
    {
        public XNAOptionsPanel(WindowManager windowManager, 
            UserINISettings iniSettings) : base(windowManager)
        {
            IniSettings = iniSettings;
        }

        private readonly List<IUserSetting> userSettings = new List<IUserSetting>();

        private XNAScrollPanel scrollPanel;

        /// <summary>
        /// Set to true once the panel has finished initializing, so that
        /// <see cref="RefreshPanelLayout"/> is only invoked after children exist.
        /// </summary>
        private bool layoutInitialized;

        /// <summary>
        /// Gets or sets whether the panel should use a scrollable container.
        /// Can be set via INI: EnableScrolling=Yes
        /// </summary>
        public bool EnableScrolling { get; set; } = true;

        public override void Initialize()
        {
            ClientRectangle = new Rectangle(12, 47,
                Parent.Width - 24,
                Parent.Height - 94);
            BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 2, 2);
            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;

            base.Initialize();

            if (EnableScrolling)
            {
                scrollPanel = new XNAScrollPanel(WindowManager);
                scrollPanel.Name = Name + "_ScrollPanel";
                scrollPanel.AllowScroll = (false, true);
                scrollPanel.DrawBorders = false;
                scrollPanel.ClientRectangle = new Rectangle(0, 0, Width, Height);
                AddChild(scrollPanel);
            }

            layoutInitialized = true;

            GameProcessLogic.GameProcessExited += GameProcessExited_Callback;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (scrollPanel != null)
            {
                bool hasOpenDropDown = false;
                CheckDropDownStates(this, ref hasOpenDropDown);
                scrollPanel.CanHandleScrollWheel = !hasOpenDropDown;
            }
        }

        private static void CheckDropDownStates(XNAControl control, ref bool hasOpenDropDown)
        {
            if (hasOpenDropDown)
                return;

            if (control is XNADropDown dd && dd.DropDownState != DropDownState.CLOSED)
            {
                hasOpenDropDown = true;
                return;
            }

            foreach (var child in control.Children)
                CheckDropDownStates(child, ref hasOpenDropDown);
        }

        protected override void OnClientRectangleUpdated()
        {
            base.OnClientRectangleUpdated();

            if (scrollPanel != null)
            {
                scrollPanel.ClientRectangle = new Rectangle(0, 0, Width, Height);
            }

            if (layoutInitialized)
                RefreshPanelLayout();
        }

        /// <summary>
        /// The bottom-most Y coordinate of the panel's scrollable content.
        /// When scrolling is disabled, falls back to the panel's own bottom.
        /// </summary>
        public int ContentBottom => (scrollPanel != null && scrollPanel.Visible) ? scrollPanel.ContentBottom : Bottom;

        /// <summary>
        /// The right-most X coordinate of the panel's scrollable content.
        /// When scrolling is disabled, falls back to the panel's own right.
        /// </summary>
        public int ContentRight => (scrollPanel != null && scrollPanel.Visible) ? scrollPanel.ContentRight : Right;

        protected override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
        {
            switch (key)
            {
                case "EnableScrolling":
                    EnableScrolling = Conversions.BooleanFromString(value, true);
                    if (!EnableScrolling && scrollPanel != null)
                    {
                        var children = scrollPanel.ExtractContentChildren();
                        foreach (var child in children)
                            base.AddChildWithoutInitialize(child);
                        scrollPanel.Visible = false;
                    }
                    return;
            }

            base.ParseControlINIAttribute(iniFile, key, value);
        }

        private void GameProcessExited_Callback()
        {
            foreach (IUserSetting setting in userSettings)
            {
                if (!setting.ResetToDefaultOnGameExit)
                    continue;

                if (setting is SettingCheckBoxBase cb)
                    cb.Checked = cb.DefaultValue;
                else if (setting is SettingDropDownBase dd)
                    dd.SelectedIndex = dd.DefaultValue;

                setting.Save();
            }
        }

        /// <summary>
        /// Parses user-defined game options from an INI file.
        /// </summary>
        /// <param name="iniFile">The INI file.</param>
        public void ParseUserOptions(IniFile iniFile)
        {
            GetAttributes(iniFile);
            ParseExtraControls(iniFile, Name + "ExtraControls");
            ReadChildControlAttributes(iniFile);

            if (scrollPanel != null)
                scrollPanel.RefreshScrollbars();

            RefreshPanelLayout();
        }

        /// <summary>
        /// Re-derives the layout of children that are positioned relative to the
        /// panel's own <see cref="Width"/>, <see cref="Height"/>, <see cref="Bottom"/>
        /// or <see cref="Right"/>. Called after the panel's size has been resolved
        /// from INI and whenever the panel is resized, so panel-relative children
        /// stay correct even when the panel size is overridden via INI (or when
        /// scrolling is disabled and the panel's own bounds are used directly).
        /// Override in subclasses to re-anchor their panel-dependent controls.
        /// </summary>
        protected virtual void RefreshPanelLayout()
        {
        }

        public override void AddChild(XNAControl child)
        {
            if (EnableScrolling && scrollPanel != null && child != scrollPanel)
            {
                scrollPanel.AddContentChild(child);
            }
            else
            {
                base.AddChild(child);
            }

            if (child is IUserSetting setting)
                userSettings.Add(setting);
        }

        protected override IEnumerable<XNAControl> GetChildrenForINIProcessing()
        {
            if (EnableScrolling && scrollPanel != null && scrollPanel.Visible)
                return scrollPanel.GetChildrenForINIProcessing();

            return Children;
        }

        protected UserINISettings IniSettings { get; private set; }

        /// <summary>
        /// Saves the options of this panel.
        /// <returns>A bool that determines whether the 
        /// client needs to restart for changes to apply.</returns>
        /// </summary>
        public virtual bool Save()
        {
            bool restartRequired = false;
            foreach (var setting in userSettings)
            {
                try
                {
                    restartRequired = setting.Save() || restartRequired;
                }
                catch (Exception ex)
                {
                    Logger.Log($"Saving setting {setting.SettingSection}/{setting.SettingKey} failed: {ex.Message}");
                }
            }

            return restartRequired;
        }

        /// <summary>
        /// Refreshes the panel's settings to account for possible
        /// changes that could affect the functionality.
        /// </summary>
        /// <returns>A bool that determines whether the 
        /// setting's value was changed.</returns>
        public virtual bool RefreshPanel()
        {
            bool valuesChanged = false;
            foreach (var setting in userSettings)
            {
                if (setting is IFileSetting fileSetting)
                    valuesChanged = fileSetting.RefreshSetting() || valuesChanged;
            }

            return valuesChanged;
        }

        /// <summary>
        /// Loads the options of this panel.
        /// </summary>
        public virtual void Load()
        {
            foreach (var setting in userSettings)
                setting.Load();
        }

        /// <summary>
        /// Enables or disables any options that should only be available when
        /// options window was opened in main menu.
        /// </summary>
        /// <param name="enable">If true enables options, disables if false.</param>
        public virtual void ToggleMainMenuOnlyOptions(bool enable)
        {
        }
    }
}
