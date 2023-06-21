﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Multiplayer.API;
using RimWorld;
using Verse;

namespace Multiplayer.Compat
{
    /// <summary>Vanilla Races Expanded - Android by Oskar Potocki, Taranchuk, ISOREX, Sarg Bjornson</summary>
    /// <see href="https://github.com/Vanilla-Expanded/VanillaRacesExpanded-Android"/>
    /// <see href="https://steamcommunity.com/sharedfiles/filedetails/?id=2975771801"/>
    [MpCompatFor("vanillaracesexpanded.android")]
    public class VanillaRacesAndroid
    {
        // Android creation
        private static Type androidCreationWindowType;
        private static AccessTools.FieldRef<GeneCreationDialogBase, Building> androidCreationWindowStationField;
        private static AccessTools.FieldRef<GeneCreationDialogBase, Pawn> androidCreationWindowCreatorField;

        // Android modification
        // Dialog
        private static Type androidModificationWindowType;
        private static AccessTools.FieldRef<GeneCreationDialogBase, Building_Enterable> androidModificationWindowStationField;

        private static AccessTools.FieldRef<GeneCreationDialogBase, Pawn> androidModificationWindowAndroidField;

        // Building
        private static FastInvokeHandler cancelModificationMethod;

        private static AccessTools.FieldRef<Building_Enterable, bool> initModificationField;

        // Building inner classes
        private static AccessTools.FieldRef<object, Building_Enterable> innerClassGizmoBuildingField;
        private static AccessTools.FieldRef<object, Building_Enterable> innerClassFloatMenuBuildingField;

        // Android creation/modification base
        private static FastInvokeHandler acceptInnerMethod;
        private static AccessTools.FieldRef<GeneCreationDialogBase, List<ThingDefCount>> androidWindowRequiredItemsField;

        // Cache
        private static AccessTools.FieldRef<IDictionary> canInitiateRandomInteractionCacheField;
        private static AccessTools.FieldRef<IDictionary> canDoRandomMentalBreakCacheField;

        // Choice letter
        private static FastInvokeHandler trySetChoicesMethod;
        private static FastInvokeHandler makeChoicesMethod;
        private static AccessTools.FieldRef<ChoiceLetter, int> passionChoiceCountField;
        private static AccessTools.FieldRef<ChoiceLetter, int> passionGainsCountField;
        private static AccessTools.FieldRef<ChoiceLetter, int> traitChoiceCountField;
        private static AccessTools.FieldRef<ChoiceLetter, List<SkillDef>> passionChoicesField;
        private static AccessTools.FieldRef<ChoiceLetter, List<Trait>> traitChoicesField;

        public VanillaRacesAndroid(ModContentPack mod)
        {
            MpSyncWorkers.Requires<ThingDefCount>();

            LongEventHandler.ExecuteWhenFinished(LatePatch);

            // Android creation/modification
            {
                // Creation
                var type = AccessTools.TypeByName("VREAndroids.Building_AndroidCreationStation");
                // Cancel project
                MpCompat.RegisterLambdaMethod(type, nameof(Building.GetGizmos), 0);
                // (Dev) finish project
                MP.RegisterSyncMethod(type, "FinishAndroidProject").SetDebugOnly();

                type = androidCreationWindowType = AccessTools.TypeByName("VREAndroids.Window_AndroidCreation");
                androidCreationWindowStationField = AccessTools.FieldRefAccess<Building>(type, "station");
                androidCreationWindowCreatorField = AccessTools.FieldRefAccess<Pawn>(type, "creator");
                MP.RegisterSyncWorker<GeneCreationDialogBase>(SyncAndroidCreationWindow, type);
                // Accept project
                MP.RegisterSyncMethod(type, "AcceptInner")
                    .SetPreInvoke(PreAcceptInnerCreation);

                // Modification
                // Android modification building in late patch

                type = androidModificationWindowType = AccessTools.TypeByName("VREAndroids.Window_AndroidModification");
                androidModificationWindowStationField = AccessTools.FieldRefAccess<Building_Enterable>(type, "station");
                androidModificationWindowAndroidField = AccessTools.FieldRefAccess<Pawn>(type, "android");
                MP.RegisterSyncWorker<GeneCreationDialogBase>(SyncAndroidModificationWindow, type);
                // Accept project
                MP.RegisterSyncMethod(type, "AcceptInner")
                    .SetPreInvoke(PreAcceptInnerModification)
                    .SetPostInvoke(PostAcceptInnerModification);

                // Shared
                type = AccessTools.TypeByName("VREAndroids.Window_CreateAndroidBase");
                androidWindowRequiredItemsField = AccessTools.FieldRefAccess<List<ThingDefCount>>(type, "requiredItems");
                acceptInnerMethod = MethodInvoker.GetHandler(AccessTools.DeclaredMethod(type, "AcceptInner"));
                MpCompat.harmony.Patch(AccessTools.DeclaredMethod(type, "Accept"),
                    prefix: new HarmonyMethod(typeof(VanillaRacesAndroid), nameof(PreAccept)));

                MpCompat.harmony.Patch(AccessTools.DeclaredMethod("VREAndroids.Window_CreateAndroidBase:DoWindowContents"),
                    prefix: new HarmonyMethod(typeof(VanillaRacesAndroid), nameof(CloseDialogIfStationDestroyed)));
            }

            // Gizmos
            {
                // CompAssignableToPawn_AndroidStand:TryAssignPawn - should be handled by MP

                // Disable sleep mode
                MpCompat.RegisterLambdaMethod("VREAndroids.Building_AndroidSleepMode", nameof(Building.GetGizmos), 0);
                // Enable sleep mode
                MpCompat.RegisterLambdaMethod("VREAndroids.Gene_SleepMode", nameof(Gene.GetGizmos), 0);

                var type = AccessTools.TypeByName("VREAndroids.Gene_SelfDestructProtocols");
                // Cancel self destruction
                MpCompat.RegisterLambdaMethod(type, nameof(Gene.GetGizmos), 0);
                // Trigger self destruction
                MpCompat.RegisterLambdaDelegate(type, nameof(Gene.GetGizmos), 1);

                // Cancel crafting androind
                MP.RegisterSyncMethod(AccessTools.DeclaredMethod("VREAndroids.UnfinishedAndroid:CancelProject"));
            }

            // Float menus
            {
                // Building_AndroidStand:GetFloatMenuOptions - should be handled by MP (CompAssignableToPawn:TryAssignPawn + TryTakeOrderedJob)
                // VREAndroids.Reactor:GetFloatMenuOptions - should be handled by MP (TryTakeOrderedJob)

                // Rest until healed (and a few extra things as well)
                MpCompat.RegisterLambdaDelegate("VREAndroids.Building_Bed_GetFloatMenuOptions_Patch", "GetFloatMenuOptions", 0);
            }

            // Other
            {
                MpCompat.harmony.Patch(AccessTools.DeclaredMethod("VREAndroids.HealthCardUtility_CreateSurgeryBill_Patch:Postfix"),
                    prefix: new HarmonyMethod(typeof(VanillaRacesAndroid), nameof(CancelOperationModificationIfResultNull)));
            }
        }

        private static void LatePatch()
        {
            // Android creation/modification
            {
                var type = AccessTools.TypeByName("VREAndroids.Building_AndroidBehavioristStation");
                cancelModificationMethod = MethodInvoker.GetHandler(AccessTools.DeclaredMethod(type, "CancelModification"));
                initModificationField = AccessTools.FieldRefAccess<bool>(type, "initModification");
                // Cancel project
                MpCompat.RegisterLambdaMethod(type, nameof(Building.GetGizmos), 2);

                MpCompat.harmony.Patch(AccessTools.DeclaredMethod(type, nameof(Building_Enterable.TryAcceptPawn)),
                    prefix: new HarmonyMethod(typeof(VanillaRacesAndroid), nameof(PreTryAcceptPawn)),
                    transpiler: new HarmonyMethod(typeof(VanillaRacesAndroid), nameof(ReplaceAddWindow)));
                // Replace selecting the pawn with opening of the dialog
                var method = MpMethodUtil.GetLambda(type, nameof(Building.GetGizmos), lambdaOrdinal: 1);
                innerClassGizmoBuildingField = AccessTools.FieldRefAccess<Building_Enterable>(method.DeclaringType, "<>4__this");
                MpCompat.harmony.Patch(method, prefix: new HarmonyMethod(typeof(VanillaRacesAndroid), nameof(PreSelectPawnGizmo)));
                method = MpMethodUtil.GetLambda(type, nameof(Building.GetFloatMenuOptions), lambdaOrdinal: 0);
                innerClassFloatMenuBuildingField = AccessTools.FieldRefAccess<Building_Enterable>(method.DeclaringType, "<>4__this");
                MpCompat.harmony.Patch(method, prefix: new HarmonyMethod(typeof(VanillaRacesAndroid), nameof(PreSelectPawnFloatMenu)));
            }

            // Gizmos
            {
                var type = AccessTools.TypeByName("VREAndroids.Building_SubcorePolyanalyzer");
                // Init scanned (2), cancel loading (5), dev complete (6), dev enable/disable ingredients (0)
                MpCompat.RegisterLambdaMethod(type, nameof(Building.GetGizmos), 2, 5, 6, 0).TakeLast(2).SetDebugOnly();
                // Select pawn
                MpCompat.RegisterLambdaDelegate(type, nameof(Building.GetGizmos), 4);
                // Select pawn
                MpCompat.RegisterLambdaDelegate(type, nameof(Building.GetFloatMenuOptions), 0);
            }

            // Cache
            {
                MpCompat.harmony.Patch(AccessTools.DeclaredMethod(typeof(GameComponentUtility), nameof(GameComponentUtility.FinalizeInit)),
                    postfix: new HarmonyMethod(typeof(VanillaRacesAndroid), nameof(ClearCache)));

                var field = AccessTools.DeclaredField("VREAndroids.InteractionUtility_CanInitiateRandomInteraction_Patch:cachedResults");
                canInitiateRandomInteractionCacheField = AccessTools.StaticFieldRefAccess<IDictionary>(field);

                field = AccessTools.DeclaredField("MentalBreaker_CanDoRandomMentalBreaks_Patch:cachedResults");
                canDoRandomMentalBreakCacheField = AccessTools.StaticFieldRefAccess<IDictionary>(field);
            }

            // Choice letter
            {
                // Looking at this after making this patch... I don't think most of it is even needed, as the letter never
                // has the timeout activated... I suppose we'll be safe if the timeout is ever used for this letter then.

                // This is basically the same patch as MP one for ChoiceLetter_GrowthMoment, as the code for android awakening is basically the same.
                // MP code used as a base for this patch:
                // https://github.com/rwmt/Multiplayer/blob/c7a673a63178257fbcbbe4812b0d48f0e8df2593/Source/Client/Syncing/Game/SyncDelegates.cs#L277-L279
                // https://github.com/rwmt/Multiplayer/blob/c7a673a63178257fbcbbe4812b0d48f0e8df2593/Source/Client/Syncing/Game/SyncDelegates.cs#L331-L356

                var type = AccessTools.TypeByName("Multiplayer.Client.Patches.CloseDialogsForExpiredLetters");
                var registerAction = AccessTools.DeclaredMethod(type, "RegisterDefaultLetterChoice");

                type = AccessTools.TypeByName("VREAndroids.ChoiceLetter_AndroidAwakened");

                var method = AccessTools.DeclaredMethod(type, "MakeChoices");
                MP.RegisterSyncMethod(method).ExposeParameter(1);
                makeChoicesMethod = MethodInvoker.GetHandler(method);

                method = AccessTools.DeclaredMethod(type, "TrySetChoices");
                trySetChoicesMethod = MethodInvoker.GetHandler(method);
                MpCompat.harmony.Patch(method,
                    prefix: new HarmonyMethod(typeof(VanillaRacesAndroid), nameof(PreSetChoices)),
                    postfix: new HarmonyMethod(typeof(VanillaRacesAndroid), nameof(PostSetChoices)));

                passionChoiceCountField = AccessTools.FieldRefAccess<int>(type, "passionChoiceCount");
                passionGainsCountField = AccessTools.FieldRefAccess<int>(type, "passionGainsCount");
                traitChoiceCountField = AccessTools.FieldRefAccess<int>(type, "traitChoiceCount");
                passionChoicesField = AccessTools.FieldRefAccess<List<SkillDef>>(type, "passionChoices");
                traitChoicesField = AccessTools.FieldRefAccess<List<Trait>>(type, "traitChoices");

                registerAction.Invoke(
                    null,
                    new object[]
                    {
                        AccessTools.DeclaredMethod(typeof(VanillaRacesAndroid), nameof(DefaultDialogSelection)),
                        type
                    });

                type = AccessTools.TypeByName("VREAndroids.Dialog_AndroidAwakenedChoices");
                MpCompat.harmony.Patch(AccessTools.DeclaredMethod(type, nameof(Window.DoWindowContents)),
                    prefix: new HarmonyMethod(typeof(VanillaRacesAndroid), nameof(PreAwakeningDraw)),
                    postfix: new HarmonyMethod(typeof(VanillaRacesAndroid), nameof(PostAwakeningDraw)));

                type = typeof(LetterStack);
                MpCompat.harmony.Patch(AccessTools.DeclaredMethod(type, nameof(LetterStack.RemoveLetter)),
                    prefix: new HarmonyMethod(typeof(VanillaRacesAndroid), nameof(PreRemoveLetter)));
            }

            // Flickering thoughts list
            {
                MpCompat.harmony.Patch(AccessTools.DeclaredMethod("VREAndroids.Gene_SyntheticBody:Tick"),
                    transpiler: new HarmonyMethod(typeof(VanillaRacesAndroid), nameof(ReplaceNeedsCall)));
            }
        }

        #region Dialog patches

        private static bool CloseDialogIfStationDestroyed(GeneCreationDialogBase __instance)
        {
            if (!MP.IsInMultiplayer)
                return true;

            if (androidCreationWindowType.IsInstanceOfType(__instance))
            {
                var station = androidCreationWindowStationField(__instance);
                if (station == null || station.Destroyed || !station.Spawned)
                {
                    __instance.Close();
                    return false;
                }
            }
            else if (androidModificationWindowType.IsInstanceOfType(__instance))
            {
                var station = androidModificationWindowStationField(__instance);
                if (station == null || station.Destroyed || !station.Spawned)
                {
                    __instance.Close();
                    return false;
                }

                var pawn = androidModificationWindowAndroidField(__instance);
                if (pawn == null || pawn.Dead || !pawn.Spawned || station.Map != pawn.Map || !station.CanAcceptPawn(pawn))
                {
                    __instance.Close();
                    return false;
                }
            }

            return true;
        }

        private static bool PreAccept(GeneCreationDialogBase __instance)
        {
            if (!MP.IsInMultiplayer)
                return true;
            // If it's not android modification, we don't care
            if (!androidModificationWindowType.IsInstanceOfType(__instance))
                return true;

            acceptInnerMethod(__instance);

            // Ignore callback - completely unused by the dialog types we're working on,
            // I believe it's only used for creating xenotypes when starting the game.

            // The mod normally calls `Close()`, but it would case issues as the `AcceptInner()`
            // technically wasn't called yet in MP, and `Window_AndroidModification` cancels
            // the modification unless the xenotype was selected (which happens inside AcceptInner).
            Find.WindowStack.TryRemove(__instance);
            return false;
        }

        private static void PreAcceptInnerCreation(object instance, object[] args)
        {
            ref var creator = ref androidCreationWindowCreatorField((GeneCreationDialogBase)instance);

            // If the pawn assigned to creating the android died/despawned before accepting,
            // remove them from the job to prevent issues.
            if (creator != null && (creator.Dead || !creator.Spawned))
                creator = null;
        }

        private static void PreAcceptInnerModification(object instance, object[] args)
        {
            var dialog = (GeneCreationDialogBase)instance;

            var station = androidModificationWindowStationField(dialog);
            // Shouldn't be null, but let's check just in case
            if (station == null || station.Destroyed || !station.Spawned)
                return;

            // Cancel anything that was going on before we set it up ourselves (eject current pawn, if any)
            cancelModificationMethod(station);

            var android = androidModificationWindowAndroidField(dialog);
            // Again, shouldn't be null here
            if (android == null || android.Dead || !android.Spawned || android.Map != station.Map)
                return;

            if (station.CanAcceptPawn(android))
                station.SelectPawn(android);
        }

        private static void PostAcceptInnerModification(object instance, object[] args)
        {
            var station = androidModificationWindowStationField((GeneCreationDialogBase)instance);

            // Not null and no selected pawn = we failed selecting pawn, cancel modification and reset state
            if (station is { selectedPawn: null })
                cancelModificationMethod(station);
        }

        #endregion

        #region Building patches

        private static bool PreTryAcceptPawn(bool ___initModification, ThingOwner ___innerContainer)
        {
            if (!MP.IsInMultiplayer)
                return true;

            // Make sure no android inside already
            if (___innerContainer.Any)
                return false;
            // Make sure a project/xenotype is selected/started (we swap order in MP, first selecting order and then sending a pawn in)
            if (!___initModification)
                return false;

            return true;
        }

        private static void AddWindowReplacement(WindowStack windowStack, Window window)
        {
            // Since we swap order in MP, don't open the window
            if (!MP.IsInMultiplayer)
                windowStack.Add(window);
        }

        private static IEnumerable<CodeInstruction> ReplaceAddWindow(IEnumerable<CodeInstruction> instr)
            => instr.MethodReplacer(
                AccessTools.DeclaredMethod(typeof(WindowStack), nameof(WindowStack.Add)),
                AccessTools.DeclaredMethod(typeof(VanillaRacesAndroid), nameof(AddWindowReplacement)));

        private static bool PreSelectPawnGizmo(Pawn ___pawn, object __instance)
            => DisplayAndroidModificationDialogIfNeeded(___pawn, innerClassGizmoBuildingField(__instance));

        private static bool PreSelectPawnFloatMenu(Pawn ___selPawn, object __instance)
            => DisplayAndroidModificationDialogIfNeeded(___selPawn, innerClassFloatMenuBuildingField(__instance));

        private static bool DisplayAndroidModificationDialogIfNeeded(Pawn pawn, Building_Enterable building)
        {
            if (!MP.IsInMultiplayer)
                return true;

            // If the modification was initialized - check if it's for our current pawn. And if it is - let them enter.
            if (initModificationField(building))
                return building.SelectedPawn == pawn;

            // Display the dialog only if the modification was not initialized.
            Find.WindowStack.Add((Window)Activator.CreateInstance(androidModificationWindowType, building, pawn, null));
            return false;
        }

        #endregion

        #region Choice letter

        private static void PreSetChoices(Thing ___pawn, Letter __instance)
            => Rand.PushState(Gen.HashCombineInt(___pawn.thingIDNumber, __instance.arrivalTick));

        private static void PostSetChoices()
            => Rand.PopState();

        private static void DefaultDialogSelection(ChoiceLetter letter)
        {
            trySetChoicesMethod(letter);

            List<SkillDef> passions = null;
            Trait trait = null;

            var passionChoices = passionChoicesField(letter);
            if (passionChoices != null && passionChoiceCountField(letter) > 0)
                passions = passionChoices.InRandomOrder().Take(passionGainsCountField(letter)).ToList();

            var traitChoices = traitChoicesField(letter);
            if (traitChoices != null && traitChoiceCountField(letter) > 0)
                trait = traitChoices.RandomElement();

            makeChoicesMethod(letter, passions, trait);
            // Close the letter, or it may auto-open for all players.
            Find.LetterStack.RemoveLetter(letter);
        }

        // The button to apply the choices from awakening dialog will close the letter right after applying the changes, which puts it into archive.
        // When reading sync data we check for the letter in the letter stack, and not archive - which fails to read the letter.
        // On top of that, the letter itself will reject input once it's been archived, which means it would return from the method early for the
        // person who accepted the choices.
        private static bool isDrawingAwakeningDialog = false;

        private static bool PreRemoveLetter() => !MP.IsInMultiplayer || !isDrawingAwakeningDialog;

        private static void PreAwakeningDraw() => isDrawingAwakeningDialog = true;

        private static void PostAwakeningDraw() => isDrawingAwakeningDialog = false;

        #endregion

        #region Thoughts flickering fix

        private static void ReplacedNeedsTrackerCall(Pawn_NeedsTracker instance)
        {
            // Check once every 6 hours, should prevent flickering and prevent flickering of needs.
            // In vanilla, this method is called pretty infrequently. In situations like trait or hediffs
            // being added or removed, ideo changes, etc.
            // The flickering happens because it triggers this gets called each time:
            // https://github.com/rwmt/Multiplayer/blob/d7aa398477ae6091f4f5d314bd0331c3933ca525/Source/Client/Patches/SituationalThoughts.cs#L8-L24
            // Androids just call it every tick. It would probably be smart to look into the mod itself and
            // check why it's called in the first place. Most likely related to androids becoming awakened?
            if (instance.pawn.IsHashIntervalTick(GenDate.TicksPerHour * 6))
                instance.AddOrRemoveNeedsAsAppropriate();
        }

        private static IEnumerable<CodeInstruction> ReplaceNeedsCall(IEnumerable<CodeInstruction> instr, MethodBase original)
        {
            var target = AccessTools.DeclaredMethod(typeof(Pawn_NeedsTracker), nameof(Pawn_NeedsTracker.AddOrRemoveNeedsAsAppropriate));
            var replacement = AccessTools.DeclaredMethod(typeof(VanillaRacesAndroid), nameof(ReplacedNeedsTrackerCall));

            var patched = false;

            foreach (var ci in instr)
            {
                if ((ci.opcode == OpCodes.Call || ci.opcode == OpCodes.Callvirt) && ci.operand is MethodInfo method && method == target)
                {
                    ci.opcode = OpCodes.Call;
                    ci.operand = replacement;

                    patched = true;
                }

                yield return ci;
            }

            if (!patched)
                Log.Warning($"Failed patching constant AddOrRemoveNeedsAsAppropriate calls for {original?.FullDescription() ?? "(unknown method)"}");
#if DEBUG
            else
                Log.Error($"Successfully patched AddOrRemoveNeedsAsAppropriate calls for {original?.FullDescription() ?? "(unknown method)"}");
#endif
        }

        #endregion

        #region Other Patches

        // HealthCardUtility:CreateSurgeryBill is a MP sync method. Because of this, when the method is called the execution
        // is cancelled and the result will be null. However, prefixes, postfixes, and finalizers will still run as normal.
        // We need to make sure this one doesn't run as it throws an error (even if harmless).
        private static bool CancelOperationModificationIfResultNull([HarmonyArgument("__result")] Bill_Medical result) => result != null;

        private static void ClearCache()
        {
            canInitiateRandomInteractionCacheField().Clear();
            canDoRandomMentalBreakCacheField().Clear();
        }

        #endregion

        #region SyncWorkers

        private static void SyncAndroidCreationWindow(SyncWorker sync, ref GeneCreationDialogBase window)
        {
            if (sync.isWriting)
            {
                sync.Write(androidCreationWindowStationField(window));
                sync.Write(androidCreationWindowCreatorField(window));

                SyncAndroidCreationBase(sync, ref window);
            }
            else
            {
                var building = sync.Read<Building>();
                var pawn = sync.Read<Pawn>();

                window = (GeneCreationDialogBase)Activator.CreateInstance(androidCreationWindowType, building, pawn, null);

                SyncAndroidCreationBase(sync, ref window);
            }
        }

        private static void SyncAndroidModificationWindow(SyncWorker sync, ref GeneCreationDialogBase window)
        {
            if (sync.isWriting)
            {
                sync.Write(androidModificationWindowStationField(window));
                sync.Write(androidModificationWindowAndroidField(window));

                SyncAndroidCreationBase(sync, ref window);
            }
            else
            {
                var building = sync.Read<Building>();
                var pawn = sync.Read<Pawn>();

                window = (GeneCreationDialogBase)Activator.CreateInstance(androidModificationWindowType, building, pawn, null);

                SyncAndroidCreationBase(sync, ref window);
            }
        }

        private static void SyncAndroidCreationBase(SyncWorker sync, ref GeneCreationDialogBase window)
        {
            if (sync.isWriting)
            {
                // Vanilla dialog values
                sync.Write(window.xenotypeName);
                sync.Write(window.iconDef);
                sync.Write(window.SelectedGenes);

                // VFE dialog values
                sync.Write(androidWindowRequiredItemsField(window));
            }
            else
            {
                // Vanilla dialog values
                window.xenotypeName = sync.Read<string>();
                window.iconDef = sync.Read<XenotypeIconDef>();
                // Getter only
                window.SelectedGenes.Clear();
                window.SelectedGenes.AddRange(sync.Read<List<GeneDef>>());

                // VFE dialog values
                androidWindowRequiredItemsField(window) = sync.Read<List<ThingDefCount>>();
            }
        }

        #endregion
    }
}