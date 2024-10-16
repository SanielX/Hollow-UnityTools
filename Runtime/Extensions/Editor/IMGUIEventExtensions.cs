#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HollowEditor.Extensions
{
    public static class UnityUIEventExtensions
    {
        public const int LMB = 0;
        public const int RMB = 1;
        public const int MMB = 2;

        public static bool IsMouseButtonDown(this Event ev, int button) => IsMouseButton(ev, button, EventType.MouseDown);
        public static bool IsMouseButtonUp  (this Event ev, int button) => IsMouseButton(ev, button, EventType.MouseUp);

        public static bool IsMouseButton(this Event ev, int button, EventType type = EventType.MouseDown)
        {
            return ev.type == type && ev.isMouse && ev.button == button;
        }
    }
}
#endif 