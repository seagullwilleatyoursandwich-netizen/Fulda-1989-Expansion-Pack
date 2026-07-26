using UnityEngine;
using GHPC.Equipment;

namespace Fulda1989
{
    public class M1ERA
    {
        public static EraSetup schema = new EraSetup()
        {
            era_so = ScriptableObject.CreateInstance<ArmorCodexScriptable>(),
            era_armour = new ArmorType(),
            name = "M1 ERA",
            heat_rha = 375f,
            ke_rha = 20f,
        };

        public static void Setup(Transform era_armour_parent, Transform visual_parent,
            bool hide_on_detonate = true, Material destroyed_mat = null, string destroyed_target = "")
        {
            FuldaEra.Setup(M1ERA.schema, era_armour_parent.transform, visual_parent.transform, hide_on_detonate, destroyed_mat, destroyed_target);
        }
    }
}
