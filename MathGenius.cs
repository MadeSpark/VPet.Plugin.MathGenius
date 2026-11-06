using System;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using VPet_Simulator.Windows.Interface;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using LinePutScript;
using LinePutScript.Localization.WPF;
using Panuon.WPF.UI;

namespace VPet.Plugin.MathGenius
{
    public class MathGeniusPlugin : MainPlugin
    {
        private LowLevelKeyboardHook keyboardHook;
        private KeyboardHookDependencyBridge keyboardBridge;
        private Dispatcher dispatcher;
        private bool hookInstalled = false;
        private bool hookInitializing = false;
        public Setting Set { get; private set; } = new Setting();
        private winSetting SetWindow;


        private void Log(string message) { }

        public MathGeniusPlugin(IMainWindow mainwin) : base(mainwin)
        {
            dispatcher = MW.Dispatcher;
        }



        public override void LoadPlugin()
        {
            try
            {
                Set = new Setting(MW.Set["MathGenius"]);
            }
            catch
            {
                Set = new Setting();
            }
            if (!Set.Contains("HookEnabled")) Set.HookEnabled = true;
            Set.AutoTypeResult = true;
            if (!Set.Contains("TypeByChar")) Set.TypeByChar = true;
            try { MW.Set["MathGenius"] = Set; } catch { }
            Task.Run(async () =>
            {
                await Task.Delay(5000);
                InitializeHookAsync();
            });
        }

        private void InitializeHookAsync()
        {
            if (!Set.HookEnabled)
                return;

            if (hookInitializing || hookInstalled)
                return;

            hookInitializing = true;
            dispatcher.Invoke(() =>
            {
                try
                {
                    if (keyboardHook == null)
                    {
                        keyboardHook = new LowLevelKeyboardHook(dispatcher, this);
                    }
                    if (keyboardBridge == null)
                    {
                        keyboardBridge = new KeyboardHookDependencyBridge(keyboardHook);
                    }
                    bool subscribed = keyboardBridge.TrySubscribe();
                    if (subscribed)
                    {
                        hookInstalled = true;
                    }
                    else
                    {
                        hookInstalled = false;
                        Set.HookEnabled = false;
                        try { MW.Set["MathGenius"] = Set; } catch { }
                        try
                        {
                            var text = "数学天才：功能启用失败，请检查是否已安装并启用依赖。";
                            if (!string.IsNullOrWhiteSpace(text))
                                MW.Main.LabelDisplayShow(text, 5000);
                        }
                        catch
                        {
                        }
                    }
                }
                catch { }
                finally
                {
                    hookInitializing = false;
                }
            });
        }

        ~MathGeniusPlugin()
        {
            try { keyboardBridge?.Unsubscribe(); } catch { }
        }

        public override string PluginName => "MathGenius";

        public override void Setting()
        {
            if (SetWindow == null)
            {
                SetWindow = new winSetting(this);
                SetWindow.Closed += (s, e) => { SetWindow = null; };
                SetWindow.Show();
            }
            else
            {
                SetWindow.Close();
                SetWindow = new winSetting(this);
                SetWindow.Closed += (s, e) => { SetWindow = null; };
                SetWindow.Show();
            }
        }

        public override void LoadDIY()
        {
            try
            {
                var menu = new MenuItem()
                {
                    Header = "数学天才".Translate(),
                    HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center,
                };
                var menuEnable = new MenuItem()
                {
                    Header = Set.HookEnabled ? "关闭".Translate() : "启用".Translate(),
                    HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center,
                };
                menuEnable.Click += (s, e) =>
                {
                    if (Set.HookEnabled)
                    {
                        try { keyboardBridge?.Unsubscribe(); } catch { }
                        hookInstalled = false;
                        Set.HookEnabled = false;
                        MW.Main.SayRnd("阿巴巴巴，什么东西从窝大脑里跑掉了。（智慧的眼神）".Translate(), true);
                    }
                    else
                    {
                        if (keyboardHook == null)
                        {
                            keyboardHook = new LowLevelKeyboardHook(dispatcher, this);
                        }
                        if (keyboardBridge == null)
                        {
                            keyboardBridge = new KeyboardHookDependencyBridge(keyboardHook);
                        }
                        bool subscribed = keyboardBridge.TrySubscribe();
                        hookInstalled = subscribed;
                        Set.HookEnabled = subscribed;
                        if (subscribed)
                        {
                            MW.Main.SayRnd("知识正在涌入大脑......泥的数学天才女鹅又回来啦！".Translate(), true);
                        }
                        else
                        {
                            MessageBoxX.Show("功能无法启用，请检查是否已安装并启用依赖", "提示");
                        }
                    }
                    try { MW.Set["MathGenius"] = Set; } catch { }
                    if (s.GetType() == typeof(MenuItem))
                    {
                        var mi = s as MenuItem;
                        mi.Header = Set.HookEnabled ? "关闭".Translate() : "启用".Translate();
                    }
                };
                menu.Items.Add(menuEnable);
                var menuAutoType = new MenuItem()
                {
                    Header = Set.AutoTypeResult ? "自动输入√".Translate() : "自动输入".Translate(),
                    HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center,
                };
                menuAutoType.Click += (s, e) =>
                {
                    Set.AutoTypeResult = !Set.AutoTypeResult;
                    if (s.GetType() == typeof(MenuItem))
                    {
                        var mi = s as MenuItem;
                        mi.Header = Set.AutoTypeResult ? "自动输入√".Translate() : "自动输入".Translate();
                    }
                };
                menu.Items.Add(menuAutoType);
                var menuTypeMethod = new MenuItem()
                {
                    Header = "输入方式".Translate(),
                    HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center,
                };
                var menuTypeChar = new MenuItem()
                {
                    Header = Set.TypeByChar ? "逐字输入√".Translate() : "逐字输入".Translate(),
                    HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center,
                };
                var menuTypePaste = new MenuItem()
                {
                    Header = !Set.TypeByChar ? "复制粘贴√".Translate() : "复制粘贴".Translate(),
                    HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center,
                };
                menuTypeChar.Click += (s, e) =>
                {
                    Set.TypeByChar = true;
                    menuTypeChar.Header = "逐字输入√".Translate();
                    menuTypePaste.Header = "复制粘贴".Translate();
                    try { MW.Set["MathGenius"] = Set; } catch { }
                };
                menuTypePaste.Click += (s, e) =>
                {
                    Set.TypeByChar = false;
                    menuTypeChar.Header = "逐字输入".Translate();
                    menuTypePaste.Header = "复制粘贴√".Translate();
                    try { MW.Set["MathGenius"] = Set; } catch { }
                };
                menuTypeMethod.Items.Add(menuTypeChar);
                menuTypeMethod.Items.Add(menuTypePaste);
                menu.Items.Add(menuTypeMethod);
                MW.Main.ToolBar.MenuDIY.Items.Add(menu);
            }
            catch (Exception ex)
            {
                try
                {
                    var detail = ex.InnerException?.Message ?? ex.Message;
                    var text = string.IsNullOrWhiteSpace(detail) ? "自定加载失败" : $"自定加载失败\n{detail}";
                    if (!string.IsNullOrWhiteSpace(text))
                        MW.Main.LabelDisplayShow(text, 5000);
                }
                catch
                {
                }
            }
        }


    }

    public class LowLevelKeyboardHook
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;
        private const int WM_PASTE = 0x0302;
        private const int EM_REPLACESEL = 0x00C2;
        private const int VK_EQUAL = 0xBB;
        private const int VK_OEM_MINUS = 0xBD;
        private const int VK_OEM_PERIOD = 0xBE;
        private const int VK_ADD = 0x6B;
        private const int VK_SUBTRACT = 0x6D;
        private const int VK_DECIMAL = 0x6E;
        private const int VK_SHIFT = 0x10;
        private const int VK_LSHIFT = 0xA0;
        private const int VK_RSHIFT = 0xA1;
        private const int VK_CONTROL = 0x11;
        private const int VK_LCONTROL = 0xA2;
        private const int VK_RCONTROL = 0xA3;
        private const int VK_MENU = 0x12;
        private const int VK_LMENU = 0xA4;
        private const int VK_RMENU = 0xA5;
        private const int VK_LWIN = 0x5B;
        private const int VK_RWIN = 0x5C;

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private IntPtr hookHandle = IntPtr.Zero;
        private LowLevelKeyboardProc hookDelegate = null;
        private Dispatcher dispatcher;
        private MathGeniusPlugin plugin;
        private int keyPressCount = 0;
        private bool hasDigitTyped = false;
        private bool equalKeyDownWithModifier = false;
        private volatile bool pendingEqualTrigger = false;
        private const int equalConfirmDelayMilliseconds = 250;

        private void Log(string message) { }

        public LowLevelKeyboardHook(Dispatcher disp, MathGeniusPlugin p)
        {
            dispatcher = disp;
            plugin = p;
            hookDelegate = HookCallback;
        }

        public bool InstallHook()
        {
            try
            {
                IntPtr moduleHandle = GetModuleHandle(null);
                hookHandle = SetWindowsHookEx(WH_KEYBOARD_LL, hookDelegate, moduleHandle, 0);
                return hookHandle != IntPtr.Zero;
            }
            catch
            {
                return false;
            }
        }

        public void UninstallHook()
        {
            if (hookHandle != IntPtr.Zero)
            {
                UnhookWindowsHookEx(hookHandle);
                hookHandle = IntPtr.Zero;
            }
        }

        private volatile bool isProcessing = false;
        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                keyPressCount++;
                if (isProcessing) return CallNextHookEx(hookHandle, nCode, wParam, lParam);
                if (nCode >= 0)
                {
                    int vkCode = Marshal.ReadInt32(lParam);
                    int wMsg = (int)wParam;
                    HandleVkMessage(vkCode, wMsg);
                }
            }
            catch { }

            return CallNextHookEx(hookHandle, nCode, wParam, lParam);
        }

        public void HandleVkMessage(int vkCode, int wMsg)
        {
            try
            {
                if (isProcessing)
                    return;

                bool isKeyDownMessage = wMsg == WM_KEYDOWN || wMsg == WM_SYSKEYDOWN;
                bool isKeyUpMessage = wMsg == WM_KEYUP || wMsg == WM_SYSKEYUP;

                if (vkCode == VK_EQUAL)
                {
                    if (isKeyDownMessage)
                    {
                        if (pendingEqualTrigger)
                        {
                            // Code such as "1==x" types two '=' back to back: the second
                            // one proves the first was not a formula terminator, so cancel
                            // the pending trigger and keep this '=' harmless too.
                            pendingEqualTrigger = false;
                            hasDigitTyped = false;
                            equalKeyDownWithModifier = true;
                            return;
                        }

                        // Remember how '=' went down: Shift+'=' types '+', and such a
                        // combination must stay harmless even when the modifier is released
                        // before the '=' key itself is released.
                        equalKeyDownWithModifier = IsAnyModifierDown();
                        return;
                    }

                    if (!isKeyUpMessage)
                        return;

                    // Ignore every '=' that belongs to a key combination.
                    bool pressedAsCombination = equalKeyDownWithModifier || IsAnyModifierDown();
                    equalKeyDownWithModifier = false;
                    if (!pressedAsCombination && hasDigitTyped)
                    {
                        hasDigitTyped = false;
                        pendingEqualTrigger = true;
                        Task.Run(async () =>
                        {
                            // Give the user a moment to type a second '=' (code like
                            // "1==x" or "0==err"): that second press cancels the pending
                            // trigger instead of hijacking the selection.
                            await Task.Delay(equalConfirmDelayMilliseconds);
                            if (!pendingEqualTrigger)
                                return;
                            pendingEqualTrigger = false;
                            isProcessing = true;
                            try
                            {
                                _ = dispatcher.BeginInvoke(new Action(() =>
                                {
                                    plugin.MW.Main.SayRnd("让我看看...".Translate(), false);
                                }), DispatcherPriority.Background);
                                string formula = await ExtractFormula();
                                if (!string.IsNullOrEmpty(formula))
                                {
                                    var result = EvaluateExpression(formula);
                                    if (result.HasValue)
                                    {
                                        double r = result.Value;
                                        string resultStr = Math.Abs(r - Math.Round(r)) < 1e-10 ? Math.Round(r).ToString() : r.ToString();
                                        if (plugin.Set.AutoTypeResult)
                                        {
                                            TypeTextToFocusedWindow(resultStr);
                                            _ = dispatcher.BeginInvoke(new Action(() =>
                                            {
                                                plugin.MW.Main.SayRnd("笨蛋杂鱼，{0}等于{1}哦~已经帮主人把答案写上去啦！".Translate(formula, resultStr), false);
                                            }), DispatcherPriority.Background);
                                        }
                                        else
                                        {
                                            SetClipboardTextAsync(resultStr);
                                            _ = dispatcher.BeginInvoke(new Action(() =>
                                            {
                                                plugin.MW.Main.SayRnd("笨蛋杂鱼，{0}等于{1}哦~人家已经勉为其难地帮主人把答案复制到剪切板上啦！".Translate(formula, resultStr), false);
                                            }), DispatcherPriority.Background);
                                        }
                                    }
                                }
                            }
                            catch { }
                            finally
                            {
                                isProcessing = false;
                            }
                        });
                    }
                    return;
                }

                if (!isKeyDownMessage)
                    return;

                // Still typing after the '=' means it was not a formula terminator
                // ("1=x", "a=1" ...): drop the pending trigger before it presses any key.
                if (pendingEqualTrigger && (IsLetterKey(vkCode) || IsDigitKey(vkCode) || vkCode == 0x20))
                {
                    pendingEqualTrigger = false;
                }

                // Ignore keys typed as part of a Ctrl/Alt/Win combination (Ctrl+C, Ctrl+V,
                // Alt+Tab, Win+...). They never type a bare number, so they must neither
                // arm nor fire formula recognition.
                if (IsCtrlDown() || IsAltDown() || IsWinDown())
                {
                    hasDigitTyped = false;
                    return;
                }

                // A bare modifier does not type anything, so it must not clear a valid
                // arming (tapping Shift after "12+3" still allows "=" to fire).
                if (IsModifierKey(vkCode))
                {
                    return;
                }

                if (vkCode >= 0x30 && vkCode <= 0x39)
                {
                    // A digit typed while Shift is held is a symbol, not a number:
                    // '!' = Shift+1, '@' = Shift+2, '(' = Shift+9 ...
                    // Typing code like "!=0" must therefore never arm the trigger.
                    // Shift+0 is the one exception: it types ')' and can legitimately
                    // end a formula such as (1+2)=
                    hasDigitTyped = !IsShiftDown() || vkCode == 0x30;
                }
                else if (vkCode >= 0x60 && vkCode <= 0x69)
                {
                    hasDigitTyped = true;
                }
                else
                {
                    hasDigitTyped = false;
                }
            }
            catch
            {
            }
        }

        private async Task<string> ExtractFormula()
        {
            string previousFormula = null;
            string finalFormula = null;
            bool selectionStarted = false;
            bool shiftKeyDown = false;
            bool leftKeyDown = false;
            int consecutiveFailures = 0;
            const int maxIterations = 100;
            const int maxConsecutiveFailures = 5;
            const int holdStepMilliseconds = 50;
            const int initialHoldMultiplier = 1;
            const int holdMultiplierIncrement = 1;
            const int maxHoldMultiplier = 10;
            const int leftKeyDownMilliseconds = 5;
            const int leftKeyGapMilliseconds = 10;

            try
            {
                for (int i = 0; i < maxIterations && finalFormula == null; i++)
                {
                    try
                    {
                        SimulateKeyDown(VK_LSHIFT);
                        shiftKeyDown = true;
                        await Task.Delay(10);

                        // keybd_event does not reliably synthesize typematic repeat from one
                        // KeyDown, so keep Shift held and emit explicit left-key press pairs.
                        // The hold window is 50ms x multiplier, where the multiplier starts at
                        // 1 and grows by 1 per iteration, capped at 10 (i.e. 500ms).
                        int holdMultiplier = initialHoldMultiplier + i * holdMultiplierIncrement;
                        if (holdMultiplier > maxHoldMultiplier)
                        {
                            holdMultiplier = maxHoldMultiplier;
                        }
                        int holdMilliseconds = holdStepMilliseconds * holdMultiplier;
                        int repeatCount = holdMilliseconds / (leftKeyDownMilliseconds + leftKeyGapMilliseconds);
                        if (repeatCount < 1)
                        {
                            repeatCount = 1;
                        }
                        for (int repeat = 0; repeat < repeatCount; repeat++)
                        {
                            SimulateKeyDown(0x25);
                            leftKeyDown = true;
                            selectionStarted = true;
                            await Task.Delay(leftKeyDownMilliseconds);
                            SimulateKeyUp(0x25);
                            leftKeyDown = false;

                            if (repeat + 1 < repeatCount)
                            {
                                await Task.Delay(leftKeyGapMilliseconds);
                            }
                        }

                        int elapsedMilliseconds = (repeatCount * leftKeyDownMilliseconds)
                            + ((repeatCount - 1) * leftKeyGapMilliseconds);
                        int remainingMilliseconds = holdMilliseconds - elapsedMilliseconds;
                        if (remainingMilliseconds > 0)
                        {
                            await Task.Delay(remainingMilliseconds);
                        }
                    }
                    finally
                    {
                        if (leftKeyDown)
                        {
                            SimulateKeyUp(0x25);
                            leftKeyDown = false;
                        }
                        if (shiftKeyDown)
                        {
                            SimulateKeyUp(VK_LSHIFT);
                            shiftKeyDown = false;
                        }
                    }

                    await Task.Delay(10);
                    SimulateKeyCombo(0x43, 0xA2);
                    await Task.Delay(100);

                    string text = GetClipboardText();
                    text = text.Replace("（", "(").Replace("）", ")").Replace("＋", "+").Replace("－", "-").Replace("×", "*").Replace("÷", "/");
                    // Input methods may produce full-width characters; normalize them too,
                    // otherwise "12＋3＝" typed with a Chinese IME would never be recognized.
                    text = text.Replace("＝", "=").Replace("＊", "*").Replace("／", "/").Replace("＾", "^").Replace("％", "%").Replace("．", ".");
                    for (int fullWidthDigit = 0; fullWidthDigit < 10; fullWidthDigit++)
                    {
                        text = text.Replace((char)('０' + fullWidthDigit), (char)('0' + fullWidthDigit));
                    }

                    string candidate = null;
                    if (text != "=")
                    {
                        candidate = ExtractTrailingFormula(text);

                        // Validate each copied candidate while the selection grows. This makes
                        // the loop do the parsing work before the single final right-arrow.
                        if (candidate != null &&
                            (string.IsNullOrWhiteSpace(candidate) || !ContainsOperator(candidate) || !EvaluateExpression(candidate).HasValue))
                        {
                            candidate = null;
                        }
                    }

                    if (candidate == null)
                    {
                        // The first couple of rounds can legitimately come back empty while the
                        // selection is still too short to reach the trailing '='. Stop as soon as
                        // a run of consecutive rounds yields nothing usable, so a formula that is
                        // not there never turns into an endless retry loop.
                        consecutiveFailures++;
                        if (consecutiveFailures >= maxConsecutiveFailures)
                        {
                            break;
                        }
                        continue;
                    }

                    consecutiveFailures = 0;

                    if (string.Equals(previousFormula, candidate, StringComparison.Ordinal))
                    {
                        finalFormula = candidate;
                        break;
                    }

                    previousFormula = candidate;
                }
            }
            catch
            {
            }
            finally
            {
                // Always release injected keys before restoring the caret, including errors.
                try
                {
                    if (leftKeyDown)
                    {
                        SimulateKeyUp(0x25);
                        leftKeyDown = false;
                    }
                    if (shiftKeyDown)
                    {
                        SimulateKeyUp(VK_LSHIFT);
                        shiftKeyDown = false;
                    }
                }
                catch { }

                // Collapse the accumulated selection only once, after all copies were processed.
                if (selectionStarted)
                {
                    try
                    {
                        SendKeyToFocusedWindow(0x27);
                        await Task.Delay(10);
                    }
                    catch { }
                }
            }

            return finalFormula ?? previousFormula;
        }

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);
        private bool IsShiftDown()
        {
            return ((GetAsyncKeyState(VK_SHIFT) & 0x8000) != 0)
                || ((GetAsyncKeyState(VK_LSHIFT) & 0x8000) != 0)
                || ((GetAsyncKeyState(VK_RSHIFT) & 0x8000) != 0);
        }

        /// <summary>
        /// Returns the trailing "...=" fragment when the copied text really ends a formula,
        /// or null when the text is just code glued to an identifier ("x1=", "v2=").
        /// </summary>
        private static string ExtractTrailingFormula(string text)
        {
            if (string.IsNullOrEmpty(text) || text[text.Length - 1] != '=')
                return null;

            int start = text.Length - 1;
            while (start > 0 && IsFormulaCharacter(text[start - 1]))
                start--;

            if (start == text.Length - 1)
                return null;                     // nothing in front of the '='

            if (start > 0)
            {
                char before = text[start - 1];
                if (char.IsLetterOrDigit(before) || before == '_')
                    return null;                 // "x1=" is code, not a formula
            }

            return text.Substring(start, text.Length - 1 - start);
        }

        private static bool IsFormulaCharacter(char c)
        {
            return (c >= '0' && c <= '9')
                || c == '(' || c == ')' || c == '+' || c == '-' || c == '*' || c == '/'
                || c == '^' || c == '%' || c == '.';
        }

        private static bool IsLetterKey(int vkCode)
        {
            return vkCode >= 0x41 && vkCode <= 0x5A;
        }

        private static bool IsDigitKey(int vkCode)
        {
            return (vkCode >= 0x30 && vkCode <= 0x39) || (vkCode >= 0x60 && vkCode <= 0x69);
        }

        private static bool ContainsOperator(string expression)
        {
            foreach (char c in expression)
            {
                if (c == '+' || c == '-' || c == '*' || c == '/' || c == '^' || c == '%')
                    return true;
            }
            return false;
        }

        private static bool IsModifierKey(int vkCode)
        {
            return vkCode == VK_SHIFT || vkCode == VK_LSHIFT || vkCode == VK_RSHIFT
                || vkCode == VK_CONTROL || vkCode == VK_LCONTROL || vkCode == VK_RCONTROL
                || vkCode == VK_MENU || vkCode == VK_LMENU || vkCode == VK_RMENU
                || vkCode == VK_LWIN || vkCode == VK_RWIN;
        }

        private static bool IsKeyHeld(int vkCode)
        {
            return (GetAsyncKeyState(vkCode) & 0x8000) != 0;
        }

        private static bool IsCtrlDown()
        {
            return IsKeyHeld(VK_CONTROL) || IsKeyHeld(VK_LCONTROL) || IsKeyHeld(VK_RCONTROL);
        }

        private static bool IsAltDown()
        {
            return IsKeyHeld(VK_MENU) || IsKeyHeld(VK_LMENU) || IsKeyHeld(VK_RMENU);
        }

        private static bool IsWinDown()
        {
            return IsKeyHeld(VK_LWIN) || IsKeyHeld(VK_RWIN);
        }

        private static bool IsAnyModifierDown()
        {
            return IsKeyHeld(VK_SHIFT) || IsKeyHeld(VK_LSHIFT) || IsKeyHeld(VK_RSHIFT)
                || IsCtrlDown() || IsAltDown() || IsWinDown();
        }

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, int dwFlags, IntPtr dwExtraInfo);
        private const int KEYEVENTF_EXTENDEDKEY = 0x1;
        private const int KEYEVENTF_KEYUP = 0x2;
        private void SimulateKeyDown(int vkCode)
        {
            int flags = IsExtendedKey(vkCode) ? KEYEVENTF_EXTENDEDKEY : 0;
            keybd_event((byte)vkCode, 0, flags, IntPtr.Zero);
        }
        private void SimulateKeyUp(int vkCode)
        {
            int flags = (IsExtendedKey(vkCode) ? KEYEVENTF_EXTENDEDKEY : 0) | KEYEVENTF_KEYUP;
            keybd_event((byte)vkCode, 0, flags, IntPtr.Zero);
        }
        private void SimulateKeyPress(int vkCode)
        {
            SimulateKeyDown(vkCode);
            System.Threading.Thread.Sleep(5);
            SimulateKeyUp(vkCode);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr GetFocus();
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern uint GetCurrentThreadId();
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);

        private IntPtr GetFocusedControlHandle()
        {
            IntPtr hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return IntPtr.Zero;
            uint thisTid = GetCurrentThreadId();
            uint pid;
            uint targetTid = GetWindowThreadProcessId(hwnd, out pid);
            if (targetTid == 0) return hwnd;
            bool attached = AttachThreadInput(thisTid, targetTid, true);
            IntPtr focus = GetFocus();
            if (attached) AttachThreadInput(thisTid, targetTid, false);
            if (focus == IntPtr.Zero) return hwnd;
            return focus;
        }

        private bool IsExtendedKey(int vk)
        {
            return vk == 0x25 || vk == 0x26 || vk == 0x27 || vk == 0x28 || vk == 0x21 || vk == 0x22 || vk == 0x23 || vk == 0x24 || vk == 0x2D || vk == 0x2E;
        }

        private int MakeKeyLParam(int vk, bool keyUp, bool extended, int repeat)
        {
            uint scan = MapVirtualKey((uint)vk, 0);
            int lp = (repeat & 0xFFFF) | ((int)(scan & 0xFF) << 16);
            if (extended) lp |= 1 << 24;
            if (keyUp) lp |= (1 << 30) | (1 << 31);
            return lp;
        }

        private void SendKeyToFocusedWindow(int vkCode)
        {
            IntPtr hwnd = GetFocusedControlHandle();
            if (hwnd == IntPtr.Zero) hwnd = GetForegroundWindow();
            bool extended = IsExtendedKey(vkCode);
            IntPtr wParam = (IntPtr)vkCode;
            IntPtr lParamDown = (IntPtr)MakeKeyLParam(vkCode, false, extended, 1);
            IntPtr lParamUp = (IntPtr)MakeKeyLParam(vkCode, true, extended, 1);
            SendMessage(hwnd, (uint)WM_KEYDOWN, wParam, lParamDown);
            System.Threading.Thread.Sleep(5);
            SendMessage(hwnd, (uint)WM_KEYUP, wParam, lParamUp);
        }

        private void TypeTextToFocusedWindow(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (plugin.Set.TypeByChar)
            {
                for (int i = 0; i < text.Length; i++)
                {
                    char ch = text[i];
                    if (ch >= '0' && ch <= '9')
                    {
                        int vk = 0x30 + (ch - '0');
                        SimulateKeyPress(vk);
                        System.Threading.Thread.Sleep(10);
                        continue;
                    }
                    if (ch == '.')
                    {
                        try { SimulateKeyPress(VK_OEM_PERIOD); }
                        catch { SimulateKeyPress(VK_DECIMAL); }
                        System.Threading.Thread.Sleep(10);
                        continue;
                    }
                    if (ch == '-')
                    {
                        try { SimulateKeyPress(VK_OEM_MINUS); }
                        catch { SimulateKeyPress(VK_SUBTRACT); }
                        System.Threading.Thread.Sleep(10);
                        continue;
                    }
                    if (ch == '+')
                    {
                        try
                        {
                            SimulateKeyDown(VK_SHIFT);
                            System.Threading.Thread.Sleep(5);
                            SimulateKeyPress(VK_EQUAL);
                            System.Threading.Thread.Sleep(5);
                        }
                        finally
                        {
                            SimulateKeyUp(VK_SHIFT);
                        }
                        System.Threading.Thread.Sleep(10);
                        continue;
                    }
                }
            }
            else
            {
                var done = new System.Threading.ManualResetEvent(false);
                try
                {
                    var t = new System.Threading.Thread(() =>
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            try
                            {
                                System.Windows.Clipboard.SetText(text);
                                break;
                            }
                            catch
                            {
                                System.Threading.Thread.Sleep(100);
                            }
                        }
                        done.Set();
                    });
                    t.SetApartmentState(System.Threading.ApartmentState.STA);
                    t.IsBackground = true;
                    t.Start();
                }
                catch
                {
                    done.Set();
                }
                done.WaitOne(500);
                SimulateKeyCombo(0x56, 0xA2);
            }
        }

        private void SimulateKeyCombo(int key1, int key2)
        {
            keybd_event((byte)key2, 0, 0, IntPtr.Zero);
            System.Threading.Thread.Sleep(5);
            keybd_event((byte)key1, 0, 0, IntPtr.Zero);
            System.Threading.Thread.Sleep(5);
            keybd_event((byte)key1, 0, 2, IntPtr.Zero);
            System.Threading.Thread.Sleep(5);
            keybd_event((byte)key2, 0, 2, IntPtr.Zero);
        }

        private string GetClipboardText()
        {
            try
            {
                return dispatcher.Invoke(() =>
                {
                    try
                    {
                        if (System.Windows.Clipboard.ContainsText())
                        {
                            return System.Windows.Clipboard.GetText();
                        }
                    }
                    catch { }
                    return "";
                }, DispatcherPriority.Background);
            }
            catch
            {
                try
                {
                    string result = "";
                    var done = new System.Threading.ManualResetEvent(false);
                    var t = new System.Threading.Thread(() =>
                    {
                        try
                        {
                            if (System.Windows.Clipboard.ContainsText())
                            {
                                result = System.Windows.Clipboard.GetText();
                            }
                        }
                        catch { }
                        finally { done.Set(); }
                    });
                    t.SetApartmentState(System.Threading.ApartmentState.STA);
                    t.IsBackground = true;
                    t.Start();
                    done.WaitOne(500);
                    return result ?? "";
                }
                catch { return ""; }
            }
        }

        private void SetClipboardTextAsync(string text)
        {
            try
            {
                var t = new System.Threading.Thread(() =>
                {
                    for (int i = 0; i < 5; i++)
                    {
                        try
                        {
                            System.Windows.Clipboard.SetText(text);
                            break;
                        }
                        catch
                        {
                            System.Threading.Thread.Sleep(100);
                        }
                    }
                });
                t.SetApartmentState(System.Threading.ApartmentState.STA);
                t.IsBackground = true;
                t.Start();
            }
            catch { }
        }

        private double? EvaluateExpression(string expr)
        {
            if (string.IsNullOrWhiteSpace(expr)) return null;
            int i = 0;
            var output = new System.Collections.Generic.List<string>();
            var ops = new System.Collections.Generic.Stack<char>();
            System.Func<char, int> prec = c => c == '^' ? 4 : c == '*' || c == '/' || c == '%' ? 3 : c == '+' || c == '-' ? 2 : 0;
            System.Func<char, bool> rightAssoc = c => c == '^';
            while (i < expr.Length)
            {
                char ch = expr[i];
                if (char.IsWhiteSpace(ch)) { i++; continue; }
                if (char.IsDigit(ch) || ch == '.')
                {
                    int start = i;
                    while (i < expr.Length && (char.IsDigit(expr[i]) || expr[i] == '.')) i++;
                    output.Add(expr.Substring(start, i - start));
                    continue;
                }
                if (ch == '(')
                {
                    ops.Push(ch);
                    i++;
                    continue;
                }
                if (ch == ')')
                {
                    while (ops.Count > 0 && ops.Peek() != '(') output.Add(ops.Pop().ToString());
                    if (ops.Count == 0 || ops.Pop() != '(') return null;
                    i++;
                    continue;
                }
                if ("+-*/%^".IndexOf(ch) >= 0)
                {
                    while (ops.Count > 0 && ops.Peek() != '(' &&
                           (prec(ops.Peek()) > prec(ch) || (prec(ops.Peek()) == prec(ch) && !rightAssoc(ch))))
                    {
                        output.Add(ops.Pop().ToString());
                    }
                    ops.Push(ch);
                    i++;
                    continue;
                }
                return null;
            }
            while (ops.Count > 0)
            {
                char op = ops.Pop();
                if (op == '(') return null;
                output.Add(op.ToString());
            }
            var st = new System.Collections.Generic.Stack<double>();
            foreach (var t in output)
            {
                if (t.Length > 1 || char.IsDigit(t[0]) || t[0] == '.')
                {
                    if (!double.TryParse(t, out var val)) return null;
                    st.Push(val);
                }
                else
                {
                    if (st.Count < 2) return null;
                    double b = st.Pop();
                    double a = st.Pop();
                    switch (t[0])
                    {
                        case '+': st.Push(a + b); break;
                        case '-': st.Push(a - b); break;
                        case '*': st.Push(a * b); break;
                        case '/': st.Push(b == 0 ? double.NaN : a / b); break;
                        case '%': st.Push(a % b); break;
                        case '^': st.Push(Math.Pow(a, b)); break;
                        default: return null;
                    }
                }
            }
            if (st.Count != 1) return null;
            var res = st.Pop();
            if (double.IsNaN(res) || double.IsInfinity(res)) return null;
            return res;
        }
    }
}
