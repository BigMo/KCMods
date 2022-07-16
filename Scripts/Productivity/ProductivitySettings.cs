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

    [Mod("Productivity", "v1.0", "Zat")]
    public class ProductivitySettings
    {
        [Category("AI")]
        public AISettings AI { get; private set; }

        [Category("Food")]
        public FoodSettings Food { get; private set; }

        [Category("Goods")]
        public GoodsSettings Goods { get; private set; }

        [Category("Resources")]
        public ResourceSettings Resources { get; private set; }
    }

    public class AISettings
    {
        [Setting("Allies", "Enables Productivity for allied AIs")]
        [Toggle(true, "Enabled")]
        public InteractiveToggleSetting Allies { get; private set; }

        [Setting("Enemies", "Enables Productivity for hostile AIs")]
        [Toggle(true, "Enabled")]
        public InteractiveToggleSetting Enemies { get; private set; }

        [Setting("Neutral", "Enables Productivity for neutral AIs")]
        [Toggle(true, "Enabled")]
        public InteractiveToggleSetting Neutral { get; private set; }

        public bool EnabledForAI(int teamId)
        {
            if (teamId == 0) return true;
            switch (World.inst.RelationBetween(teamId, 0))
            {
                case World.Relations.Allies:
                    return Allies.Value;
                case World.Relations.Enemy:
                    return Enemies.Value;
                case World.Relations.Neutral:
                    return Neutral.Value;
            }
            return Neutral.Value;
        }
    }

    public class FoodSettings
    {
        [Category("Bakery")]
        public BuildingSettings Baker { get; private set; }

        [Category("Field")]
        public BuildingSettings Field { get; private set; }

        [Category("Orchard")]
        public BuildingSettings Orchard { get; private set; }

        [Category("Fishing Hut")]
        public BuildingSettings FishingHut { get; private set; }
    }

    public class GoodsSettings
    {
        [Category("Charcoal Maker")]
        public BuildingSettings CharcoalMaker { get; private set; }

        [Category("Blacksmith")]
        public BlacksmithSettings Blacksmith { get; private set; }
    }

    public class BlacksmithSettings
    {
        [Setting("Enabled")]
        [Toggle(true, "Enabled")]
        public InteractiveToggleSetting Enabled { get; private set; }

        [Category("Tools")]
        public ProduceSettings Tools { get; private set; }

        [Category("Armament")]
        public ProduceSettings Armament { get; private set; }
    }

    public class ResourceSettings
    {

        [Category("Forester")]
        public BuildingSettings Forester { get; private set; }

        [Category("Iron Mine")]
        public BuildingSettings IronMine { get; private set; }

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

    public class ProduceSettings
    {
        [Setting("Factor", "The yield is multiplied by this value")]
        [Slider(0f, 10f, 1f, "Factor: x1.00")]
        public InteractiveSliderSetting Factor { get; private set; }

        [Setting("Mode", "Whether to multiply the yield or set it to a fixed value")]
        [Select(0, "Multiply", "Fixed")]
        public InteractiveSelectSetting Mode { get; private set; }

        public ResourceManipulation.ModificationMode ModificationMode { get { return (ResourceManipulation.ModificationMode)(Mode.Value); } }
    }
}
