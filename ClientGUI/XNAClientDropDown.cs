using System;

using ClientCore;
using ClientCore.Extensions;
using ClientCore.I18N;

using Microsoft.Xna.Framework;

using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace ClientGUI
{
    public class XNAClientDropDown : XNADropDown, IToolTipContainer
    {
        public ToolTip ToolTip { get; private set; }

        private string _initialToolTipText;
        public string ToolTipText
        {
            get => Initialized ? ToolTip?.Text : _initialToolTipText;
            set
            {
                if (Initialized)
                    ToolTip.Text = value;
                else
                    _initialToolTipText = value;
            }
        }

        private string _overrideText;
        public string OverrideText
        {
            get => _overrideText;
            set => _overrideText = value;
        }

        public XNAClientDropDown(WindowManager windowManager) : base(windowManager) { }

        public override void Initialize()
        {
            ClickSoundEffect = new EnhancedSoundEffect("dropdown.wav");

            base.Initialize();

            ToolTip = new ToolTip(WindowManager, this) { Text = _initialToolTipText };
        }

        protected override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
        {
            if (key == "ToolTip")
            {
                ToolTipText = value.FromIniString();
                return;
            }

            base.ParseControlINIAttribute(iniFile, key, value);
        }

        public override void OnMouseLeftDown(InputEventArgs inputEventArgs)
        {
            base.OnMouseLeftDown(inputEventArgs);
            UpdateToolTipBlock();
        }

        protected override void CloseDropDown()
        {
            base.CloseDropDown();
            UpdateToolTipBlock();
        }

        protected void UpdateToolTipBlock()
        {
            if (DropDownState == DropDownState.CLOSED)
                ToolTip.Blocked = false;
            else
                ToolTip.Blocked = true;
        }

        protected override string LocalizeDropDownItemText(string text, string key)
            => Translation.Instance.LookUp(this, key, text.FromIniString());

        public override void Draw(GameTime gameTime)
        {
            if (string.IsNullOrEmpty(_overrideText) || SelectedIndex < 0 || SelectedIndex >= Items.Count)
            {
                base.Draw(gameTime);
                return;
            }

            XNADropDownItem originalItem = Items[SelectedIndex];
            string originalText = originalItem.Text;
            originalItem.Text = _overrideText;

            try
            {
                base.Draw(gameTime);
            }
            finally
            {
                originalItem.Text = originalText;
            }
        }
    }
}
