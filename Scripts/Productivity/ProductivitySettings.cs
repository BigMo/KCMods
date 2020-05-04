using Zat.Shared.ModMenu.Interactive;

namespace Zat.Productivity
{
    /* Using interactive mod settings:
     * 1) Define a class that has a Mod attribute and no constructors (=> ProductivitySettings)
     * 2) To add settings, define properties that
     *  - are of a type that inherits from InteractiveSettings
     *  - have a Setting attribute
     *  - have a specific setting type (e.g. Toggle, Hotkey, ...) that supply default values (also used for resets)
     * 3) To add categories, define properties that
     *  - are of a reference-type with no constructors (=> BuildingSettings)
     *  - have a Category attribute
     * 
     * You can put categories into categories and settings into categories as you like.
     */

    [Mod("Productivity", "v0.1","Zat")]
    public class ProductivitySettings
    {

        //Specific Building Options
        [Category("Bakery")]
        public BuildingSettings Baker { get; private set; }

        [Category("Charcoal Maker")]
        public BuildingSettings CharcoalMaker { get; private set; }

        [Category("Field")]
        public BuildingSettings Field { get; private set; }

        [Category("Iron Mine")]
        public BuildingSettings IronMine { get; private set; }

        [Category("Orchard")]
        public BuildingSettings Orchard { get; private set; }

        [Category("Quarry")]
        public BuildingSettings Quarry { get; private set; }
    }


    public class BuildingSettings
    {
        [Setting("Enabled")]
        [Toggle(true, "Enabled")]
        public InteractiveToggleSetting Enabled { get; private set; }

        [Setting("Factor", "The yield is multiplied by this value")]
        [Slider(0f, 10f, 1f, "Factor: x1.00")]
        public InteractiveSliderSetting Factor { get; private set; }

        [Setting("Mode", "Whether to multiply the yield or set it to a fixed value")]
        [Select(0, "Multiply", "Fixed")]
        public InteractiveSelectSetting Mode { get; private set; }

        public ResourceManipulation.ModificationMode ModificationMode { get { return (ResourceManipulation.ModificationMode)(Mode.Value); } }
    }
}
