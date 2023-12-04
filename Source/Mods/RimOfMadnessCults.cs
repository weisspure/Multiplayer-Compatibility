using HarmonyLib; 
using Multiplayer.API;
using System;
using Verse;
using Verse.AI;


namespace Multiplayer.Compat
{ 
    /// <summary>Call of Cthulu Cults by Rim of Madness Team</summary>
    /// <see href="https://github.com/Rim-Of-Madness-Team/Call-of-Cthulhu---Cults"/>
    /// <see href="https://steamcommunity.com/sharedfiles/filedetails/?id=815039373"/>
    [MpCompatFor("rimofmadness.CallofCthulhuCults")]
    public class RimOfMadnessCallOfCthulhuCults
    {

        public RimOfMadnessCallOfCthulhuCults(ModContentPack mod)
        {
            PatchCultOfCthulhu();

            //// CallOfCthulhuCult
            //{
            //    var type = AccessTools.TypeByName("CallOfCthulhu.Dialog_CosmicEntityInfoBox");
            //    MP.RegisterSyncMethod(type, "DoWindowContents"); // Draw Menu
            //    MP.RegisterSyncMethod(type, "OnCancelKeyPressed"); // Menu -> Cancel Button
            //    MP.RegisterSyncMethod(type, "OnAcceptKeyPressed"); // Menu -> Accept Button
            //    MP.RegisterSyncMethod(type, "CreateConfirmation"); // Seems to be empty implementation?
            //}
        }

        private static void PatchCultOfCthulhu()
        {
            // Building_Monolith
            {
                var type = AccessTools.TypeByName("CultOfCthulhu.Building_Monolith");
                MP.RegisterSyncMethod(type, "MuteToggle"); // Turn sound on or off
            }

            // Building_SacrificialAltar
            {
                var type = AccessTools.TypeByName("CultOfCthulhu.Building_SacrificialAltar");

                var methodNames = new[]
                {
                    "TryUpgrade", // Gizmo -> TryUpgrade
                    "TrySacrifice", // Gizmo -> TrySacrifice
                    "CancelSacrifice", // Gizmo -> CancelSacrifice
                    "TryWorshipForced", // Gizmo -> TryWorshipForced
                    "CancelWorship", // Gizmo ->  CancelWorship
                    "TryOffering", // Gizmo -> TryOffering
                    "CancelOffering", // Gizmo -> CancelOffering
                    "GiveReport", // Gizmo -> GiveReport
                    "PruneAndRepairToggle", // Gizmo -> PruneAndRepairToggle
                    "NightmareEvent" // Gizmo -> NightmareEvent
                };

                foreach (var method in methodNames) {                
                    MP.RegisterSyncMethod(type, method);
                }

                // Lambda Delegates (Dev mode Gizmos)
                var lambdaOrdinals = new[]
                {
                    1, // Debug: Discover All Deities
                    2, // Debug: All Favor to 0
                    5, // Debug: Unlock All Spells
                };

                MpCompat.RegisterLambdaDelegate(type, "GetGizmos", lambdaOrdinals).SetDebugOnly();

                // Lambda Methods (Dev mode Gizmos)
                lambdaOrdinals = new[] { 
                    3, // Debug: Make All Colonists Cult-Minded
                    4, // Debug: Upgrade Max Level Altar
                    7, // Debug: Always Succeed
                    8, // Debug: Force Side Effect
                };

                MpCompat.RegisterLambdaMethod(type, "GetGizmos", lambdaOrdinals).SetDebugOnly();
            }
            // ThingWithComps_CultGrimoire
            {
                var type = AccessTools.TypeByName("CultOfCthulhu.ThingWithComps_CultGrimoire");


                // Lambda Delegates
                var lambdaOrdinals = new[]
                {
                    0, // CommandBuildFKC
                };

                MpCompat.RegisterLambdaDelegate(type, "GetGizmos", lambdaOrdinals);
            }
            // SpellWorker_TransmogrifyPets
            {
                var type = AccessTools.TypeByName("CultOfCthulhu.SpellWorker_TransmogrifyPets");


                // Lambda Delegates
                var lambdaOrdinals = new[]
                {
                    0, // delegate from if true of finding target.
                    1, // delegate from if false of finding target.
                    2, // Unclear but called by ordinal 1
                };

                MpCompat.RegisterLambdaDelegate(type, "Transmogrify", lambdaOrdinals);
            };

            // SpellWorker_PsionicGrowth
            {
                PatchingUtilities.PatchSystemRand("CultOfCthulhu.SpellWorker_PsionicGrowth:TryExecuteWorker", false);
            }
        }
    }
}
           
