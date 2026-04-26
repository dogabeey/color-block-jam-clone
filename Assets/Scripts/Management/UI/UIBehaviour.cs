using UnityEngine;

namespace Game
{
    public abstract class UIBehaviour : MonoBehaviour
    {
        public GameEvent[] updateTriggerEvents;

        private void OnEnable()
        {
            foreach (var updateTriggerEvent in updateTriggerEvents)
                EventManager.StartListening(updateTriggerEvent, OnUpdateUI);
        }
        private void OnDisable()
        {
            foreach (var updateTriggerEvent in updateTriggerEvents)
                EventManager.StopListening(updateTriggerEvent, OnUpdateUI);
        }
        private void OnUpdateUI(EventParam e)
        {
            UpdateUI();
        }

        public abstract void UpdateUI();
    }
}
