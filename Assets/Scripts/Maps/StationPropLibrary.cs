using UnityEngine;

namespace AbyssBar.Maps
{
    // Sub-assets reference regions of the original PNG; the raster image is not modified.
    public sealed class StationPropLibrary : ScriptableObject
    {
        public Sprite crates, barrels, barricade, lamp;
    }
}
