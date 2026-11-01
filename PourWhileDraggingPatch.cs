using System;
using HarmonyLib;
using UnityEngine;

[HarmonyPatch(typeof(ItemSelectHandler), "OnEventUpdate")]
internal static class PourWhileSelectedPatch
{
    /*
     * The smallest container in the game is 1 ml, but we can create smaller amounts by producing birth-control pills of 0.2 ml each.
     * This makes 0.2 ml the smallest separable amount of liquid in the game.
     * Therefore, we set the pour fraction to 0.2 to match this smallest separable amount.
    */
    private const int PourFractionInverse = 5;

    private static void Prefix(GameItem ___currentItem)
    {
        Vector2 scrollDelta = Input.mouseScrollDelta;
        if (scrollDelta.sqrMagnitude > 0f)
        {
            Debug.Log($"[ChemQOL Pour] Selection scroll: delta=({scrollDelta.x}, {scrollDelta.y}), source={Describe(___currentItem)}");
        }

        if (scrollDelta.sqrMagnitude <= 0f)
        {
            return;
        }

        if (scrollDelta.y >= 0f)
        {
            Debug.Log("[ChemQOL Pour] Ignored: wheel direction is not down (expected negative Y).");
            return;
        }

        if (___currentItem == null || (___currentItem is GameItemElement selectedElement && selectedElement.IsDestroyed()))
        {
            Debug.Log("[ChemQOL Pour] Ignored: no live item is selected.");
            return;
        }

        GameItem source = ___currentItem;
        GameItem target = RenderHandler.RaycastElement<GameItem>(Input.mousePosition);
        if (target == null)
        {
            Debug.Log("[ChemQOL Pour] Ignored: no target item under the cursor.");
            return;
        }

        if (target == source)
        {
            Debug.Log("[ChemQOL Pour] Ignored: cursor is over the selected source item.");
            return;
        }

        bool sourceIsLiquidContainer = source.IsTag("LIQUID_CONTAINER_TAG");
        bool targetIsLiquidContainer = target.IsTag("LIQUID_CONTAINER_TAG");
        bool mayTarget = source.MayTarget(target);
        bool canTarget = source.CanTarget(target);
        Debug.Log($"[ChemQOL Pour] Candidate: source={Describe(source)} liquidContainer={sourceIsLiquidContainer}; target={Describe(target)} liquidContainer={targetIsLiquidContainer}; mayTarget={mayTarget}; canTarget={canTarget}.");

        if (!sourceIsLiquidContainer || !targetIsLiquidContainer || !mayTarget || !canTarget)
        {
            Debug.Log("[ChemQOL Pour] Ignored: source/target must be compatible liquid containers.");
            return;
        }

        int sourceVolume = WaterHelper.GetTotalCapacity(source);
        int freeCapacity = WaterHelper.GetFreeCapacity(target);
        int amountToPour = Math.Min((sourceVolume + PourFractionInverse - 1) / PourFractionInverse, freeCapacity);
        Debug.Log($"[ChemQOL Pour] Volumes: source={sourceVolume}, targetFree={freeCapacity}, requestedPour={amountToPour}.");
        if (amountToPour <= 0)
        {
            Debug.Log("[ChemQOL Pour] No transfer: source is empty or target is full.");
            return;
        }

        GameItem stagingContainer = DirectoryMaster.Item(source.identifier, isOwned: false);
        if (stagingContainer == null)
        {
            Debug.LogError($"[ChemQOL Pour] Failed to create staging container from '{source.identifier}'.");
            return;
        }

        try
        {
            Debug.Log($"[ChemQOL Pour] Created staging container '{stagingContainer.identifier}'. Setting capacity to {amountToPour}.");
            stagingContainer.ModifyTag("LIQUID_CONTAINER_CAPACITY", state =>
            {
                state.Enable().SetInt(amountToPour);
            });

            foreach (Liquid liquid in Liquid.Liquids)
            {
                stagingContainer.ModifyTag(liquid.currentPart, state =>
                {
                    state.Enable().SetInt(0);
                });
            }

            Debug.Log("[ChemQOL Pour] Transferring source -> staging container.");
            WaterHelper.TransferLiquid(source, stagingContainer);
            Debug.Log($"[ChemQOL Pour] First transfer complete: source={WaterHelper.GetTotalVolume(source)}, staged={WaterHelper.GetTotalVolume(stagingContainer)}.");

            Debug.Log("[ChemQOL Pour] Transferring staging container -> target.");
            WaterHelper.TransferLiquid(stagingContainer, target);
            Debug.Log($"[ChemQOL Pour] Pour complete: staged={WaterHelper.GetTotalVolume(stagingContainer)}, target={WaterHelper.GetTotalVolume(target)}.");

            WaterHelper.UpdateWaterBottleSound(source);
            WaterHelper.UpdateWaterBottleSound(target);
            AudioHelper.PourWater();

            if (WaterHelper.IsEmpty(source))
            {
                Debug.Log("[ChemQOL Pour] Source is empty; ending item selection.");
                ItemSelectHandler.current?.EndSelect();
            }
        }
        catch (Exception exception)
        {
            Debug.LogError($"[ChemQOL Pour] Transfer failed: {exception}");
        }
        finally
        {
            Debug.Log("[ChemQOL Pour] Destroying staging container.");
            stagingContainer.Destroy();
        }

    }

    private static string Describe(GameItem item)
    {
        return item == null ? "<none>" : $"{item.identifier} ('{item.name}')";
    }
}
