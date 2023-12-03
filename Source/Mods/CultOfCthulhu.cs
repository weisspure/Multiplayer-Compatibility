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
            // RNG patching
            {

                //{
                //    string[] constructorsToPatch = {
                //        "GeneticRim.CompHatcherRandomizer",
                //        "GeneticRim.CompIncubator",
                //        "GeneticRim.CompRecombinator",
                //        "GeneticRim.CompRecombinatorSerum",
                //        "GeneticRim.DeathActionWorker_Eggxplosion",
                //        "GeneticRim.CompExploder",
                //    };
                //    PatchingUtilities.PatchSystemRandCtor(constructorsToPatch, false);
                //S


                string[] methodsWithRand = {
                    // Checked
                    "CultOfCthulhu.Building_WombBetweenWorlds:Activate", // 					untested In batch C 29/11/2023
                    // Suspected
                    "CultOfCthulhu.DamageWorker_PsionicBlast:PushEffect",
                    "CultOfCthulhu.Building_SacrificialAltar:ShouldAttendWorship", // Maybe, itself is not dangerous, where it's called could be.
                    // Unknown
                    "CultOfCthulhu.MapComponent_LocalCultTracker:InquisitionCheck",
                    "CultOfCthulhu.MapComponent_LocalCultTracker:TryInquisition",
                    "CultOfCthulhu.MapComponent_LocalCultTracker:ResolveTerribleCultFounder",
                    "CultOfCthulhu.MapComponent_LocalCultTracker:NeedSeedCountDown",
                    "CultOfCthulhu.MapComponent_SacrificeTracker:GenerateFailureString",
                    "CultOfCthulhu.SpellWorker_BlackIchor:TryExecuteWorker",
                    "CultOfCthulhu.SpellWorker_EcstaticFrenzy:TryExecuteWorker",
                    "CultOfCthulhu.SpellWorker_FoodSpoilage:TryExecuteWorker",
                    "CultOfCthulhu.SpellWorker_MotherOfGoats:TryExecuteWorker",
                    "CultOfCthulhu.SpellWorker_NoLongerDomesticated:TryExecuteWorker",
                    "CultOfCthulhu.SpellWorker_OrbitalInsanityWave:TryExecuteWorker",
                    "CultOfCthulhu.SpellWorker_PsionicGrowth:TryExecuteWorker",
                    "CultOfCthulhu.SpellWorker_RatsInTheWalls:TryExecuteWorker",
                    "CultOfCthulhu.SpellWorker_StarVampireVisit:TryExecuteWorker",
                    "CultOfCthulhu.SpellWorker_TerrestrialInsanityWave:TryExecuteWorker",
                    "CultOfCthulhu.SpellWorker_TreasuresOfTheDeep:TryExecuteWorker",
                };

                PatchingUtilities.PatchPushPopRand(methodsWithRand);
            }
            // RNG
            {
                var constructors = new[]
                {
                        "CultOfCthulhu.MapComponent_LocalCultTracker",
                };

                PatchingUtilities.PatchSystemRandCtor(constructors, false);
            }
        }
    }
}
