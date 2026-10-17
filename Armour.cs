using GHPC.Equipment;
using UnityEngine;

namespace Fulda1989
{
    public class Armour
    {
        internal static bool done = false;
        public static ArmorCodexScriptable eber;
        public static ArmorCodexScriptable vorschlaghammer;
        public static ArmorCodexScriptable ctech;
        public static void Init()
        {
            if (done) return;

            eber = ScriptableObject.CreateInstance<ArmorCodexScriptable>();
            eber.name = "Eber composite";
            ArmorType ebercomposite= new ArmorType();
            ebercomposite.Name = "Eber composite";
            ebercomposite.CanRicochet = true;
            ebercomposite.CanShatterLongRods = true;
            ebercomposite.NormalizesHits = true;
            ebercomposite.ThicknessSource = ArmorType.RhaSource.Multipliers;
            ebercomposite.SpallAngleMultiplier = 0.9f;
            ebercomposite.SpallPowerMultiplier = 0.85f;
            ebercomposite.RhaeMultiplierCe = 2.4f;
            ebercomposite.RhaeMultiplierKe = 1f;
            ebercomposite.CrushThicknessModifier = 1f;
            eber.ArmorType = ebercomposite;

            vorschlaghammer = ScriptableObject.CreateInstance<ArmorCodexScriptable>();
            vorschlaghammer.name = "Vorschlaghammer composite";
            ArmorType vorschlag = new ArmorType();
            vorschlag.Name = "Vorschlaghammer composite";
            vorschlag.CanRicochet = true;
            vorschlag.CanShatterLongRods = true;
            vorschlag.NormalizesHits = true;
            vorschlag.ThicknessSource = ArmorType.RhaSource.Multipliers;
            vorschlag.SpallAngleMultiplier = 0.8f;
            vorschlag.SpallPowerMultiplier = 0.6f;
            vorschlag.RhaeMultiplierCe = 2.25f;
            vorschlag.RhaeMultiplierKe = 1f;
            vorschlag.CrushThicknessModifier = 1f;
            vorschlaghammer.ArmorType = vorschlag;

            ctech = ScriptableObject.CreateInstance<ArmorCodexScriptable>();
            ctech.name = "C-Tech composite";
            ArmorType ctec = new ArmorType();
            ctec.Name = "C-Tech composite";
            ctec.CanRicochet = true;
            ctec.CanShatterLongRods = true;
            ctec.NormalizesHits = true;
            ctec.ThicknessSource = ArmorType.RhaSource.Multipliers;
            ctec.SpallAngleMultiplier = 0.9f;
            ctec.SpallPowerMultiplier = 0.8f;
            ctec.RhaeMultiplierCe = 2f;
            ctec.RhaeMultiplierKe = 1f;
            ctec.CrushThicknessModifier = 1f;
            ctech.ArmorType = ctec;

            done = true;
        }
    }
}