// UnityEngine.UI stubs — minimal for dotnet build verification only.
using System;

namespace UnityEngine.UI
{
    public class Selectable : UnityEngine.Component
    {
        public bool interactable { get; set; } = true;
    }

    public class Slider : Selectable
    {
        public float value    { get; set; }
        public float minValue { get; set; } = 0f;
        public float maxValue { get; set; } = 1f;
    }

    public class Button : Selectable
    {
        public ButtonClickedEvent onClick { get; } = new ButtonClickedEvent();

        public class ButtonClickedEvent
        {
            public void AddListener(Action call)    { }
            public void RemoveListener(Action call) { }
            public void Invoke()                    { }
        }
    }

    public class Image : UnityEngine.Component
    {
        public UnityEngine.Color color { get; set; } = UnityEngine.Color.white;
        public UnityEngine.Sprite sprite { get; set; }
        public float fillAmount { get; set; } = 1f;
    }
}
