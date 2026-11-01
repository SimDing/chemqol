using System;
using HarmonyLib;
using UnityEngine;

[HarmonyPatch(typeof(ItemMouseDragHandler), "OnEventMouseScroll")]
internal static class PourWhileDraggingPatch
{
    /*
     * The smallest container in the game is 1 ml, but we can create smaller amounts by producing birth-control pills of 0.2 ml each.
     * This makes 0.2 ml the smallest separable amount of liquid in the game.
     * Therefore, we set the pour fraction to 0.2 to match this smallest separable amount.
    */
    private const int PourFractionInverse = 5;

    private static bool Prefix(
        Vector2 scrollDelta,
        bool ___isDragging,
        GameItemElement ___currentItem)
    {
        if (scrollDelta.sqrMagnitude > 0f && (___isDragging || scrollDelta.y < 0f))
        {
            Debug.Log($"[ChemQOL Pour] Scroll event: delta=({scrollDelta.x}, {scrollDelta.y}), dragging={___isDragging}, source={Describe(___currentItem)}");
        }

        if (!___isDragging)
        {
            if (scrollDelta.y < 0f)
            {
                Debug.Log("[ChemQOL Pour] Ignored: no item is being dragged.");
            }

            return true;
        }

        if (scrollDelta.y >= 0f)
        {
            Debug.Log("[ChemQOL Pour] Ignored: wheel direction is not down (expected negative Y).");
            return true;
        }

        if (___currentItem == null || ___currentItem.IsDestroyed())
        {
            Debug.Log("[ChemQOL Pour] Ignored: dragged item is null or destroyed.");
            return true;
        }

        GameItem source = ___currentItem;
        GameItem target = RenderHandler.RaycastElement<GameItem>(Input.mousePosition, ___currentItem.fakeBackground);
        if (target == null)
        {
            Debug.Log("[ChemQOL Pour] Ignored: no target item under the cursor.");
            return true;
        }

        if (target == source)
        {
            Debug.Log("[ChemQOL Pour] Ignored: raycast returned the dragged item itself.");
            return true;
        }

        bool sourceIsLiquidContainer = source.IsTag("LIQUID_CONTAINER_TAG");
        bool targetIsLiquidContainer = target.IsTag("LIQUID_CONTAINER_TAG");
        bool mayTarget = source.MayTarget(target);
        bool canTarget = source.CanTarget(target);
        Debug.Log($"[ChemQOL Pour] Candidate: source={Describe(source)} liquidContainer={sourceIsLiquidContainer}; target={Describe(target)} liquidContainer={targetIsLiquidContainer}; mayTarget={mayTarget}; canTarget={canTarget}.");

        if (!sourceIsLiquidContainer || !targetIsLiquidContainer || !mayTarget || !canTarget)
        {
            Debug.Log("[ChemQOL Pour] Ignored: source/target must be compatible liquid containers.");
            return true;
        }

        int sourceVolume = WaterHelper.GetTotalCapacity(source);
        int freeCapacity = WaterHelper.GetFreeCapacity(target);
        int amountToPour = Math.Min((sourceVolume + PourFractionInverse - 1) / PourFractionInverse, freeCapacity);
        Debug.Log($"[ChemQOL Pour] Volumes: source={sourceVolume}, targetFree={freeCapacity}, requestedPour={amountToPour}.");
        if (amountToPour <= 0)
        {
            Debug.Log("[ChemQOL Pour] Consumed wheel-down: source is empty or target is full.");
            return false;
        }

        GameItem stagingContainer = DirectoryMaster.Item(source.identifier, isOwned: false);
        if (stagingContainer == null)
        {
            Debug.LogError($"[ChemQOL Pour] Failed to create staging container from '{source.identifier}'.");
            return false;
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

        return false;
    }

    private static string Describe(GameItem item)
    {
        return item == null ? "<none>" : $"{item.identifier} ('{item.name}')";
    }
}
