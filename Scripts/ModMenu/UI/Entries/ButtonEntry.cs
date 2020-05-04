using TMPro;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using Zat.Shared.ModMenu.API;

namespace Zat.ModMenu.UI.Entries
{
    public class ButtonEntry : BaseEntry
    {
        public class StateChangedEvent : UnityEvent<ButtonState> { }
        private UnityEngine.UI.Button button;
        private TextMeshProUGUI label;
        private ButtonState state, previousState;
        private StateChangedEvent stateChanged;

        public string Label
        {
            get { return label?.text; }
            set { if (label) label.text = value; }
        }
        public ButtonState State
        {
            get { return state; }
            private set
            {
                if (state != value)
                {
                    previousState = state;
                    state = value;
                    stateChanged?.Invoke(state);
                }
            }
        }
        public StateChangedEvent OnStateChanged
        {
            get { return stateChanged; }
        }

        protected override void RetrieveControls()
        {
            base.RetrieveControls();
            stateChanged = new StateChangedEvent();

            button = transform.Find("Button")?.GetComponent<UnityEngine.UI.Button>();
            label = transform.Find("Button/Text")?.GetComponent<TextMeshProUGUI>();
            state = previousState = ButtonState.Normal;
            var events = button.gameObject.AddComponent<EventTrigger>();
            var enter = new EventTrigger.Entry();
            enter.eventID = EventTriggerType.PointerEnter;
            enter.callback.AddListener((d) => State = ButtonState.Highlighted);
            var leave = new EventTrigger.Entry();
            leave.eventID = EventTriggerType.PointerExit;
            leave.callback.AddListener((d) => State = ButtonState.Normal);
            var down = new EventTrigger.Entry();
            down.eventID = EventTriggerType.PointerDown;
            down.callback.AddListener((d) => State = ButtonState.Pressed);
            var up = new EventTrigger.Entry();
            up.eventID = EventTriggerType.PointerUp;
            up.callback.AddListener((d) => State = previousState);

            events.triggers.Add(enter);
            events.triggers.Add(leave);
            events.triggers.Add(down);
            events.triggers.Add(up);
        }

        protected override void SetupControls()
        {
            base.SetupControls();
            label.alignment = TextAlignmentOptions.Midline;
        }
    }
}
