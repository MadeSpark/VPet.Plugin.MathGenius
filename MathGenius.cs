using System;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using VPet_Simulator.Windows.Interface;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using LinePutScript;
using LinePutScript.Localization.WPF;

namespace VPet.Plugin.MathGenius
{
    public class MathGeniusPlugin : MainPlugin
    {
        private LowLevelKeyboardHook keyboardHook;
        private Dispatcher dispatcher;
        private bool hookInstalled = false;
        private bool hookInitializing = false;
        public Setting Set { get; private set; } = new Setting();
        private Window winSetting;


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
            try { MW.Set["MathGenius"] = Set; } catch { }

            try
            {
                var modset = MW.Main.ToolBar.MenuMODConfig;
                modset.Visibility = System.Windows.Visibility.Visible;
                var menuset = new System.Windows.Controls.MenuItem()
                {
                    Header = "数学天才".Translate(),
                    HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center,
                };
                menuset.Click += (s, e) => { Setting(); };
                modset.Items.Add(menuset);
            }
            catch { }

            System.Threading.Tasks.Task.Delay(5000).ContinueWith(_ =>
            {
                InitializeHookAsync();
            });
        }

        private void InitializeHookAsync()
        {
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
                    bool installed = keyboardHook.InstallHook();
                    if (installed)
                    {
                        hookInstalled = true;
                        MW.Main.SayRnd("知识正在涌入大脑......泥的数学天才女鹅加载成功！", true);
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
            if (keyboardHook != null)
            {
                keyboardHook.UninstallHook();
            }
        }

        public override string PluginName => "MathGenius";

        public override void Setting()
        {
            if (winSetting == null)
            {
                var w = new winSetting(this);
                winSetting = w;
                w.Closed += (s, e) => { winSetting = null; };
                w.Show();
            }
            else
            {
                winSetting.Close();
                var w = new winSetting(this);
                winSetting = w;
                w.Closed += (s, e) => { winSetting = null; };
                w.Show();
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
        private const int VK_EQUAL = 0xBB;
        private const int VK_SHIFT = 0x10;
        private const int VK_LSHIFT = 0xA0;
        private const int VK_RSHIFT = 0xA1;

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

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                keyPressCount++;
                if (nCode >= 0)
                {
                    int vkCode = Marshal.ReadInt32(lParam);
                    int wMsg = (int)wParam;

                    if (vkCode == VK_EQUAL && (wMsg == WM_KEYUP || wMsg == WM_SYSKEYUP))
                    {
                        bool shiftDown = IsShiftDown();
                        if (!shiftDown && hasDigitTyped)
                        {
                            hasDigitTyped = false;
                            dispatcher.BeginInvoke(new Action(async () =>
                            {
                                try
                                {
                                    bool backupHasText = false;
                                    string backupText = null;
                                    try
                                    {
                                        backupHasText = System.Windows.Clipboard.ContainsText();
                                        if (backupHasText)
                                        {
                                            backupText = System.Windows.Clipboard.GetText();
                                        }
                                    }
                                    catch { }
                                    plugin.MW.Main.SayRnd("让我看看...", true);
                                    string formula = await ExtractFormula();
                                    if (!string.IsNullOrEmpty(formula))
                                    {
                                        var result = EvaluateExpression(formula);
                                        if (result.HasValue)
                                        {
                                            double r = result.Value;
                                            string resultStr = Math.Abs(r - Math.Round(r)) < 1e-10 ? Math.Round(r).ToString() : r.ToString();
                                            await Task.Delay(2000);
                                            if (plugin.Set.AutoTypeResult)
                                            {
                                                TypeTextToFocusedWindow(resultStr);
                                                plugin.MW.Main.SayRnd($"笨蛋杂鱼，{formula}等于{resultStr}哦~已经帮主人把答案写上去啦！", true);
                                                if (backupHasText) SetClipboardTextAsync(backupText);
                                            }
                                            else
                                            {
                                                SetClipboardTextAsync(resultStr);
                                                plugin.MW.Main.SayRnd($"笨蛋杂鱼，{formula}等于{resultStr}哦~人家已经勉为其难的帮主人把答案复制到剪切板上啦！", true);
                                            }
                                        }
                                    }
                                }
                                catch { }
                            }), DispatcherPriority.Normal);
                        }
                    }
                    else if (wMsg == WM_KEYDOWN || wMsg == WM_SYSKEYDOWN)
                    {
                        if ((vkCode >= 0x30 && vkCode <= 0x39) || (vkCode >= 0x60 && vkCode <= 0x69))
                        {
                            hasDigitTyped = true;
                        }
                    }
                }
            }
            catch { }

            return CallNextHookEx(hookHandle, nCode, wParam, lParam);
        }

        private async Task<string> ExtractFormula()
        {
            string prevFormula = null;
            string finalFormula = null;
            for (int i = 1; i <= 100 && finalFormula == null; i++)
            {
                try
                {
                    SimulateKeyDown(0xA0);
                    await Task.Delay(10);
                    for (int j = 0; j < i * 10; j++)
                    {
                        SendKeyToFocusedWindow(0x25);
                        await Task.Delay(10);
                    }
                    SimulateKeyUp(0xA0);
                    await Task.Delay(10);
                    SimulateKeyCombo(0x43, 0xA2);
                    await Task.Delay(100);
                    SendKeyToFocusedWindow(0x27);
                    await Task.Delay(10);
                    string text = GetClipboardText();
                    text = text.Replace("（", "(").Replace("）", ")").Replace("＋", "+").Replace("－", "-").Replace("×", "*").Replace("÷", "/");
                    if (text == "=") continue;
                    Regex regex = new Regex(@"[\d\(\)\+\-\*\/\^\%]+=$");
                    var matches = regex.Matches(text);
                    if (matches.Count == 0) break;
                    if (!string.IsNullOrEmpty(prevFormula))
                    {
                    }
                    string lastMatch = matches[matches.Count - 1].Value;
                    if (!string.IsNullOrEmpty(prevFormula) && prevFormula == lastMatch)
                    {
                        finalFormula = lastMatch;
                        break;
                    }
                    else
                    {
                        prevFormula = matches[0].Value;
                    }
                }
                catch { }
            }
            if (!string.IsNullOrEmpty(finalFormula))
            {
                if (finalFormula.Length > 0)
                {
                    finalFormula = finalFormula.Substring(0, finalFormula.Length - 1);
                }
                return finalFormula;
            }
            if (!string.IsNullOrEmpty(prevFormula))
            {
                if (prevFormula.Length > 0)
                {
                    prevFormula = prevFormula.Substring(0, prevFormula.Length - 1);
                }
                return prevFormula;
            }
            return null;
        }

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);
        private bool IsShiftDown()
        {
            return ((GetAsyncKeyState(VK_SHIFT) & 0x8000) != 0)
                || ((GetAsyncKeyState(VK_LSHIFT) & 0x8000) != 0)
                || ((GetAsyncKeyState(VK_RSHIFT) & 0x8000) != 0);
        }

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, int dwFlags, IntPtr dwExtraInfo);
        private void SimulateKeyDown(int vkCode) { keybd_event((byte)vkCode, 0, 0, IntPtr.Zero); }
        private void SimulateKeyUp(int vkCode) { keybd_event((byte)vkCode, 0, 2, IntPtr.Zero); }

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
                if (System.Windows.Clipboard.ContainsText())
                {
                    return System.Windows.Clipboard.GetText();
                }
            }
            catch { }
            return "";
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
