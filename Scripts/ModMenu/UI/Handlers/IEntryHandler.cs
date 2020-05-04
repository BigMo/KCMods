using UnityEngine.Events;
using Zat.ModMenu.UI.Entries;
using Zat.Shared.ModMenu.API;

namespace Zat.ModMenu.UI.Handlers
{
    /// <summary>
    /// Provides functionality to handle creating and updating of UI elements
    /// </summary>
    public interface IEntryHandler
    {
        /// <summary>
        /// Creates a matching UI element for the specified setting
        /// </summary>
        /// <param name="data">The setting to create an UI element for</param>
        /// <param name="onUpdate">Callback to be called when the UI element updated</param>
        /// <returns></returns>
        BaseEntry CreateEntry(SettingsEntry data, UnityAction onUpdate);
        /// <summary>
        /// Updates an UI element using the specified setting
        /// </summary>
        /// <param name="data">Setting holding data to update the UI element with</param>
        /// <param name="control">The UI element to update</param>
        void UpdateEntry(SettingsEntry data, BaseEntry control);
    }
}
