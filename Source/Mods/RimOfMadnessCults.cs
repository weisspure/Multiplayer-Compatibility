using HarmonyLib; 
using Multiplayer.API;
using System;
using Verse;


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

            // BastCult
            {
                MP.RegisterSyncMethod(AccessTools.TypeByName("BastCult.DeathActionWorker_BastGuardian"), "PawnDied");
            }

            // CallOfCthulhuCult
            {
                var type = AccessTools.TypeByName("CallOfCthulhu.Dialog_CosmicEntityInfoBox");
                MP.RegisterSyncMethod(type, "DoWindowContents"); // Draw Menu
                MP.RegisterSyncMethod(type, "OnCancelKeyPressed"); // Menu -> Cancel Button
                MP.RegisterSyncMethod(type, "OnAcceptKeyPressed"); // Menu -> Accept Button
                MP.RegisterSyncMethod(type, "CreateConfirmation"); // Seems to be empty implementation?
            }

            // Cthulhu
            {
                var type = AccessTools.TypeByName("Cthulhu.Utility");
                //MP.RegisterSyncMethod(type, "SpawnThingDefOfCountAt");
                //MP.RegisterSyncMethod(type, "SpawnPawnsOfCountAt");
                //MP.RegisterSyncMethod(type, "ChangeResearchProgress");
                //MP.RegisterSyncMethod(type, "ApplyTaleDef");
                //MP.RegisterSyncMethod(type, "ApplySanityLoss");
                //MP.RegisterSyncMethod(type, "RemoveSanityLoss");
                //MP.RegisterSyncMethod(type, "TemporaryGoodwill");
            }
        }

 



        private static void PatchCultOfCthulhu()
        {
            // Building_ForbiddenReserachCenter
            {
                var type = AccessTools.TypeByName("CultOfCthulhu.Building_ForbiddenReserachCenter");
                MP.RegisterSyncMethod(type, "CancelResearch");
            }

            // Building_Monolith
            {
                var type = AccessTools.TypeByName("CultOfCthulhu.Building_Monolith");
                MP.RegisterSyncMethod(type, "MuteToggle"); // Turn sound on or off
            }

            // Building_SacrificialAltar
            {
                var type = AccessTools.TypeByName("CultOfCthulhu.Building_SacrificialAltar");
                MP.RegisterSyncMethod(type, "Notify_BillDeleted");
                // MP.RegisterSyncMethod(type, "ChangeState"); // Turn sound on or off
                MP.RegisterSyncMethod(type, "PruneAndRepairToggle"); // Gizmo -> PruneAndRepairToggle
                MP.RegisterSyncMethod(type, "TryUpgrade"); // Gizmo -> TryUpgrade
                MP.RegisterSyncMethod(type, "NightmareEvent"); // Gizmo -> NightmareEvent
                MP.RegisterSyncMethod(type, "NightmarePruned"); // Gizmo -> NightmarePruned
                MP.RegisterSyncMethod(type, "CancelOffering"); // Gizmo -> CancelOffering
                MP.RegisterSyncMethod(type, "TryOffering"); // Gizmo -> TryOffering
                MP.RegisterSyncMethod(type, "CancelSacrifice"); // Gizmo -> CancelSacrifice
                MP.RegisterSyncMethod(type, "TrySacrifice"); // Gizmo -> TrySacrifice
                MP.RegisterSyncMethod(type, "StartSacrifice"); // Gizmo -> StartSacrifice
                MP.RegisterSyncMethod(type, "TryChangeWorshipValues"); // Gizmo -> TryChangeWorshipValues
                MP.RegisterSyncMethod(type, "CancelWorship"); // Gizmo -> CancelWorship
                MP.RegisterSyncMethod(type, "TryTimedWorship"); // Gizmo -> CancelWorship
                MP.RegisterSyncMethod(type, "TryWorshipForced"); // Gizmo -> CancelWorship
                MP.RegisterSyncMethod(type, "TryWorship"); // Gizmo -> CancelWorship





            }
        }

        #region Abilities

        //// PawnAbility
        //private static AccessTools.FieldRef<object, ThingComp> pawnAbilityUserField;

        //// CompAbilityUser
        //private static AccessTools.FieldRef<object, object> compAbilityUserAbilityDataField;

        //// AbilityData
        //private static FastInvokeHandler abilityDataAllPowersGetter;

        //// AbilityUserUtility
        //private static FastInvokeHandler abilityUserUtilityGetCompsMethod;

        //private static void PatchAbilities()
        //{
        //    // PawnAbility
        //    var type = AccessTools.TypeByName("AbilityUser.PawnAbility");
        //    pawnAbilityUserField = AccessTools.FieldRefAccess<ThingComp>(type, "abilityUser");

        //    MP.RegisterSyncMethod(type, "UseAbility").SetPostInvoke(StopTargeting);
        //    MP.RegisterSyncWorker<object>(SyncPawnAbility, type);

        //    // CompAbilityUser
        //    compAbilityUserAbilityDataField = AccessTools.FieldRefAccess<object>("AbilityUser.CompAbilityUser:abilityData");

        //    // AbilityData
        //    abilityDataAllPowersGetter = MethodInvoker.GetHandler(AccessTools.PropertyGetter("AbilityUser.AbilityData:AllPowers"));

        //    // AbilityUserUtility
        //    abilityUserUtilityGetCompsMethod = MethodInvoker.GetHandler(AccessTools.Method("AbilityUser.AbilityUserUtility:GetCompAbilityUsers"));
        //}

        //private static void StopTargeting(object instance, object[] args)
        //{
        //    // The job driver is assigning Find.Targeter.targetingSource, starting targeting again.
        //    // We need to stop targeting after casting or we'll start targeting again after casting.
        //    if (MP.IsExecutingSyncCommandIssuedBySelf)
        //        Find.Targeter.StopTargeting();
        //}

        //private static void SyncPawnAbility(SyncWorker sync, ref object ability)
        //{
        //    if (sync.isWriting)
        //    {
        //        // The comp.props seems null, at least in some cases - which is what MP sync worker uses for syncing.
        //        // We need to sync it differently.
        //        var abilityUserComp = pawnAbilityUserField(ability);
        //        var comps = ((IEnumerable)abilityUserUtilityGetCompsMethod(null, abilityUserComp.parent)).Cast<ThingComp>().ToArray();

        //        var foundMatch = false;
        //        for (var index = 0; index < comps.Length; index++)
        //        {
        //            var comp = comps[index];
        //            var data = compAbilityUserAbilityDataField(comp);
        //            var allPowers = (IList)abilityDataAllPowersGetter(data);
        //            var innerIndex = allPowers.IndexOf(ability);

        //            if (innerIndex >= 0)
        //            {
        //                foundMatch = true;
        //                sync.Write(index);
        //                sync.Write(innerIndex);
        //                sync.Write(abilityUserComp.parent); // Parent pawn
        //                break;
        //            }
        //        }

        //        if (!foundMatch)
        //            sync.Write(-1);
        //    }
        //    else
        //    {
        //        var index = sync.Read<int>();

        //        if (index < 0)
        //            return;

        //        var innerIndex = sync.Read<int>();
        //        var pawn = sync.Read<Pawn>();

        //        var allComps = ((IEnumerable)abilityUserUtilityGetCompsMethod(null, pawn)).Cast<ThingComp>().ToArray();
        //        var comp = allComps[index];
        //        var data = compAbilityUserAbilityDataField(comp);
        //        var all = (IList)abilityDataAllPowersGetter(data);

        //        ability = all[innerIndex];
        //    }
        //}

        #endregion
    }
}