using System;
using System.Reflection;

namespace VPet.Plugin.MathGenius
{
    internal sealed class KeyboardHookDependencyBridge
    {
        private const string ServiceTypeName = "VPet.Plugin.KeyboardHookDependency.KeyboardHookService";
        private const string ArgsTypeName = "VPet.Plugin.KeyboardHookDependency.KeyboardHookEventArgs";

        private readonly LowLevelKeyboardHook processor;

        private Type serviceType;
        private EventInfo eventInfo;
        private Delegate handler;

        public KeyboardHookDependencyBridge(LowLevelKeyboardHook processor)
        {
            this.processor = processor;
        }

        public bool IsSubscribed => eventInfo != null && handler != null;

        public bool TrySubscribe()
        {
            if (IsSubscribed)
                return true;

            serviceType = FindLoadedType(ServiceTypeName);
            if (serviceType == null)
                return false;

            eventInfo = serviceType.GetEvent("KeyboardEvent", BindingFlags.Public | BindingFlags.Static);
            if (eventInfo == null)
                return false;

            var eventHandlerType = eventInfo.EventHandlerType;
            if (eventHandlerType == null)
                return false;

            var mi = GetType().GetMethod(nameof(OnKeyboardEvent), BindingFlags.NonPublic | BindingFlags.Instance);
            if (mi == null)
                return false;

            handler = Delegate.CreateDelegate(eventHandlerType, this, mi);
            eventInfo.AddEventHandler(null, handler);
            return true;
        }

        public void Unsubscribe()
        {
            if (!IsSubscribed)
                return;

            try
            {
                eventInfo.RemoveEventHandler(null, handler);
            }
            catch
            {
            }
            finally
            {
                handler = null;
                eventInfo = null;
                serviceType = null;
            }
        }

        private void OnKeyboardEvent(object sender, object e)
        {
            try
            {
                if (e == null)
                    return;

                var t = e.GetType();
                if (!string.Equals(t.FullName, ArgsTypeName, StringComparison.Ordinal))
                    return;

                var vkObj = t.GetProperty("VirtualKey", BindingFlags.Public | BindingFlags.Instance)?.GetValue(e);
                if (vkObj == null)
                    return;

                var msgObj = t.GetProperty("Message", BindingFlags.Public | BindingFlags.Instance)?.GetValue(e);
                if (msgObj == null)
                    return;

                var vk = Convert.ToInt32(vkObj);
                var msgName = msgObj.ToString();
                var wMsg = msgName switch
                {
                    "KeyDown" => 0x0100,
                    "KeyUp" => 0x0101,
                    "SysKeyDown" => 0x0104,
                    "SysKeyUp" => 0x0105,
                    _ => 0
                };

                if (wMsg == 0)
                    return;

                processor.HandleVkMessage(vk, wMsg);
            }
            catch
            {
            }
        }

        private static Type FindLoadedType(string fullName)
        {
            try
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    Type t;
                    try
                    {
                        t = asm.GetType(fullName, false, false);
                    }
                    catch
                    {
                        continue;
                    }

                    if (t != null)
                        return t;
                }
            }
            catch
            {
            }

            return null;
        }
    }
}
