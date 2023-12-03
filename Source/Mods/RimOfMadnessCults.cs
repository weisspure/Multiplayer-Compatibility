using System;
using HarmonyLib;
using Multiplayer.API;
using Verse;

namespace Multiplayer.Compat
{
    /// <summary>Genetic Rim by Sarg</summary>
    /// <remarks>Tested ship, all buildings, hybrids creation, animal orders and implants</remarks>
    /// <see href="https://github.com/juanosarg/GeneticRim"/>
    /// <see href="https://steamcommunity.com/sharedfiles/filedetails/?id=1113137502"/>
    [MpCompatFor("rimofmadness.callofcthulhucults")]
    public class CultOfCthulhuCompat
    {
        public CultOfCthulhuCompat(ModContentPack mod)
        {
            // Methods
            {
                string[] methodsWithRand = {
                    // Checked

                    // Suspected
//                    "CultOfCthulhu.DamageWorker_PsionicBlast:PushEffect",
                    "CultOfCthulhu.MapComponent_LocalCultTracker:NeedSeedCountDown",
                    "CultOfCthulhu.Building_SacrificialAltar:ShouldAttendWorship",
                    // Unknown
                };

                PatchingUtilities.PatchPushPopRand(methodsWithRand);
            }
            // Constructors
            //            {
            //                var constructors = new[]
            //                {
            //                        "CultOfCthulhu.MapComponent_LocalCultTracker",
            //                };

            //                PatchingUtilities.PatchSystemRandCtor(constructors, false);
            //            }
        }
    }
}
