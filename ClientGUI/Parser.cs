/*********************************************************************
* Dawn of the Tiberium Age MonoGame/XNA CnCNet Client
* Expression Parser
* Copyright (C) Rampastring 2022
* 
* The CnCNet Client is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 3 of the License, or
* (at your option) any later version.
* 
* The CnCNet Client is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
* 
* You should have received a copy of the GNU General Public License
* along with this program.If not, see<https://www.gnu.org/licenses/>.
* 
*********************************************************************/

using ClientCore;

using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

using System;
using System.Collections.Generic;

namespace ClientGUI
{
    /// <summary>
    /// Parses arithmetic expressions.
    /// </summary>
    class Parser
    {
        private const int CHAR_VALUE_ZERO = 48;

        public Parser(WindowManager windowManager)
        {
            if (_instance != null)
                throw new InvalidOperationException("Only one instance of Parser can exist at a time.");

            globalConstants = new Dictionary<string, int>();
            globalConstants.Add("RESOLUTION_WIDTH", windowManager.RenderResolutionX);
            globalConstants.Add("RESOLUTION_HEIGHT", windowManager.RenderResolutionY);

            IniSection parserConstantsSection = ClientConfiguration.Instance.GetParserConstants();
            if (parserConstantsSection != null)
            {
                foreach (var kvp in parserConstantsSection.Keys)
                    globalConstants.Add(kvp.Key, Conversions.IntFromString(kvp.Value, 0));
            }

            IniSection sameNameSection = ClientConfiguration.Instance.GetSameNameConstants();
            if (sameNameSection != null)
            {
                foreach (var kvp in sameNameSection.Keys)
                    constantAliases.Add(kvp.Key, kvp.Value);
            }

            _instance = this;
        }

        private static Parser _instance;
        public static Parser Instance => _instance;

        private static Dictionary<string, int> globalConstants;
        private static Dictionary<string, string> constantAliases = new Dictionary<string, string>();

        public string Input { get; private set; }

        private int tokenPlace;
        private XNAControl primaryControl;
        private XNAControl parsingControl;

        private XNAControl GetControl(string controlName)
        {
            // 1. The primary control itself.
            if (controlName == primaryControl.Name)
                return primaryControl;

            // 2. Descendants of the primary control (its own subtree).
            var control = Find(primaryControl.Children, controlName);
            if (control != null)
                return control;

            // 3. Walk up the ancestor chain: each ancestor itself and its direct
            //    children (i.e. sibling controls of the primary control, such as
            //    the hosting window's tab control or Save/Cancel buttons).
            //    Only direct children are matched at each level so that a deeply
            //    nested control of a sibling subtree can never shadow a window
            //    level control with the same name.
            XNAControl ancestor = primaryControl.Parent;
            while (ancestor != null)
            {
                if (controlName == ancestor.Name)
                    return ancestor;

                foreach (XNAControl child in ancestor.Children)
                {
                    if (child.Name == controlName)
                        return child;
                }

                ancestor = ancestor.Parent;
            }

            throw new KeyNotFoundException($"Control '{controlName}' not found while parsing input '{Input}'");
        }

        private XNAControl Find(IEnumerable<XNAControl> list, string controlName)
        {
            foreach (XNAControl child in list)
            {
                if (child.Name == controlName)
                    return child;

                XNAControl childOfChild = Find(child.Children, controlName);
                if (childOfChild != null)
                    return childOfChild;
            }

            return null;
        }

        /// <summary>
        /// Walks up from the given control's parent, skipping the internal
        /// scroll container (<see cref="XNAScrollPanel"/>) and its content panel,
        /// so that expressions such as <c>$ParentControl</c> resolve to the actual
        /// logical parent (the <see cref="XNAOptionsPanel"/>) rather than the
        /// auto-sized scrollable content.
        /// </summary>
        private static XNAControl GetLogicalParent(XNAControl control)
        {
            XNAControl parent = control.Parent;

            while (parent != null)
            {
                // Skip the scroll panel itself.
                if (parent is XNAScrollPanel scrollPanel)
                {
                    parent = scrollPanel.Parent;
                    continue;
                }

                // Skip the scroll panel's content panel, which sits directly
                // inside the scroll panel.
                if (parent.Parent is XNAScrollPanel)
                {
                    parent = parent.Parent.Parent;
                    continue;
                }

                break;
            }

            return parent;
        }

        private int GetConstant(string constantName)
        {
            // 1. Canonical [ParserConstants] lookup takes precedence.
            if (globalConstants.TryGetValue(constantName, out int value))
                return value;

            // 2. Not a canonical constant — resolve through the [SameNameConstants] alias table
            //    (supports chained aliases A=B, B=C and detects cyclic definitions).
            string resolvedName = ResolveConstantName(constantName);

            if (!globalConstants.TryGetValue(resolvedName, out value))
            {
                if (resolvedName == constantName)
                {
                    throw new KeyNotFoundException($"Constant '{constantName}' not found. " +
                        $"Please check [ParserConstants] section in either {ClientConfiguration.CLIENT_SETTINGS} file, " +
                        $"or any possible files that {ClientConfiguration.CLIENT_SETTINGS} depends on, e.g., GlobalThemeSettings.ini.");
                }

                throw new KeyNotFoundException($"Constant '{resolvedName}' (referenced by alias '{constantName}') not found. " +
                    $"Please check [ParserConstants] section in either {ClientConfiguration.CLIENT_SETTINGS} file, " +
                    $"or any possible files that {ClientConfiguration.CLIENT_SETTINGS} depends on, e.g., GlobalThemeSettings.ini.");
            }

            return value;
        }

        /// <summary>
        /// Resolves a constant name through the [SameNameConstants] alias table.
        /// Supports chained aliases and detects cyclic definitions.
        /// </summary>
        private string ResolveConstantName(string name)
        {
            string current = name;
            int guard = 0;

            while (constantAliases.TryGetValue(current, out string target))
            {
                if (guard++ > 100)
                    throw new INIConfigException($"Cyclic alias definition detected for constant '{name}'.");

                current = target;
            }

            return current;
        }

        public void SetPrimaryControl(XNAControl primaryControl)
        {
            this.primaryControl = primaryControl;
        }

        public int GetExprValue(string input, XNAControl parsingControl)
        {
            this.parsingControl = parsingControl;
            Input = input;
            tokenPlace = 0;
            return GetExprValue();
        }

        private int GetExprValue()
        {
            int value = 0;

            while (true)
            {
                SkipWhitespace();

                if (IsEndOfInput())
                    return value;

                char c = Input[tokenPlace];

                if (char.IsDigit(c))
                {
                    value = GetInt();
                }
                else if (c == '+')
                {
                    tokenPlace++;
                    value += GetNumericalValue();
                }
                else if (c == '-')
                {
                    tokenPlace++;
                    value -= GetNumericalValue();
                }
                else if (c == '/')
                {
                    tokenPlace++;
                    value /= GetExprValue();
                }
                else if (c == '*')
                {
                    tokenPlace++;
                    value *= GetExprValue();
                }
                else if (c == '(')
                {
                    tokenPlace++;
                    value = GetExprValue();
                }
                else if (c == ')')
                {
                    tokenPlace++;
                    return value;
                }
                else if (char.IsUpper(c))
                {
                    value = GetConstantValue();
                }
                else if (char.IsLower(c))
                {
                    value = GetFunctionValue();
                }
                else if (char.IsLetter(c))
                {
                    value = GetConstantValue();
                }
            }
        }

        private int GetNumericalValue()
        {
            SkipWhitespace();

            if (IsEndOfInput())
                return 0;

            char c = Input[tokenPlace];

            if (char.IsDigit(c))
            {
                return GetInt();
            }
            else if (char.IsUpper(c))
            {
                return GetConstantValue();
            }
            else if (char.IsLower(c))
            {
                return GetFunctionValue();
            }
            else if (char.IsLetter(c))
            {
                return GetConstantValue();
            }
            else if (c == '(')
            {
                tokenPlace++;
                return GetExprValue();
            }
            else
            {
                throw new INIConfigException("Unexpected character " + c + " when parsing input: " + Input);
            }
        }

        private void SkipWhitespace()
        {
            while (true)
            {
                if (IsEndOfInput())
                    return;

                char c = Input[tokenPlace];
                if (c == ' ' || c == '\r' || c == '\n')
                    tokenPlace++;
                else
                    break;
            }
        }

        private string GetIdentifier()
        {
            string identifierName = "";

            while (true)
            {
                if (IsEndOfInput())
                    break;

                char c = Input[tokenPlace];
                if (char.IsWhiteSpace(c))
                    break;

                if (!char.IsLetterOrDigit(c) && c != '_' && c != '$' && c != '.')
                    break;

                identifierName += c.ToString();
                tokenPlace++;
            }

            return identifierName;
        }

        private int GetConstantValue()
        {
            string constantName = GetIdentifier();
            return GetConstant(constantName);
        }

        private int GetFunctionValue()
        {
            string functionName = GetIdentifier();
            SkipWhitespace();
            ConsumeChar('(');
            string paramName = GetIdentifier();
            SkipWhitespace();
            ConsumeChar(')');

            if (paramName == "$ParentControl")
            {
                XNAControl logicalParent = GetLogicalParent(parsingControl);
                if (logicalParent == null)
                    throw new INIConfigException("$ParentControl used for control that has no parent: " + parsingControl.Name);

                paramName = logicalParent.Name;
            }
            else if (paramName == "$Self")
            {
                paramName = parsingControl.Name;
            }

            switch (functionName)
            {
                case "getX":
                    return GetControl(paramName).X;
                case "getY":
                    return GetControl(paramName).Y;
                case "getWidth":
                    return GetControl(paramName).Width;
                case "getHeight":
                    return GetControl(paramName).Height;
                case "getBottom":
                {
                    var control = GetControl(paramName);
                    if (control is XNAOptionsPanel optionsPanel)
                        return optionsPanel.Bottom;
                    return control.Bottom;
                }
                case "getRight":
                {
                    var control = GetControl(paramName);
                    if (control is XNAOptionsPanel optionsPanel)
                        return optionsPanel.Right;
                    return control.Right;
                }
                case "horizontalCenterOnParent":
                    parsingControl.CenterOnParentHorizontally();
                    return parsingControl.X;
                default:
                    throw new INIConfigException("Unknown function " + functionName + " in expression " + Input);
            }
        }

        private void ConsumeChar(char token)
        {
            if (Input[tokenPlace] != token)
                throw new INIConfigException($"Parse error: expected '{token}' in expression {Input}. Instead encountered '{Input[tokenPlace]}'.");

            tokenPlace++;
        }

        private int GetInt()
        {
            int value = 0;
            while (true)
            {
                if (IsEndOfInput())
                    return value;

                char c = Input[tokenPlace];
                if (!char.IsDigit(c))
                    return value;

                value = (value * 10) + Input[tokenPlace] - CHAR_VALUE_ZERO;
                tokenPlace++;
            }
        }

        private bool IsEndOfInput() => tokenPlace >= Input.Length;
    }
}
