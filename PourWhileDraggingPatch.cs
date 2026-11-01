using System;
using HarmonyLib;
using UnityEngine;

[HarmonyPatch(typeof(ItemSelectHandler), "OnEventUpdate")]
internal static class PourWhileSelectedPatch
{
    /*
     * The smallest container in the game is 1 ml, but we can create smaller amounts by producing birth-control pills of 0.1 ml each.
     * This makes 0.1 ml the smallest separable amount of liquid in the game.
     * Therefore, we set the pour fraction to 0.1 to match this smallest separable amount.
    */
    private const int PourFractionInverse = 10;

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

        bool isDrawing = scrollDelta.y > 0f;
        if (scrollDelta.y == 0f)
        {
            Debug.Log("[ChemQOL Pour] Ignored: scroll event has no vertical movement.");
            return;
        }

        if (isDrawing ? !ChemQOLConfig.PipetteDrawWhileSelected : !ChemQOLConfig.PourWhileSelected)
        {
            return;
        }

        if (___currentItem == null || (___currentItem is GameItemElement selectedElement && selectedElement.IsDestroyed()))
        {
            Debug.Log("[ChemQOL Pour] Ignored: no live item is selected.");
            return;
        }

        GameItem source = ___currentItem;
        bool sourceIsPipette = source.IsTag("LIQUID_CONTAINER_DROPPER_TAG");
        if (isDrawing && !sourceIsPipette)
        {
            Debug.Log("[ChemQOL Pour] Ignored wheel-up: selected item is not a pipette.");
            return;
        }

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
        Debug.Log($"[ChemQOL Pour] Candidate: mode={(isDrawing ? "draw" : "pour")}; pipette={sourceIsPipette}; source={Describe(source)} liquidContainer={sourceIsLiquidContainer}; target={Describe(target)} liquidContainer={targetIsLiquidContainer}; mayTarget={mayTarget}; canTarget={canTarget}.");

        if (!sourceIsLiquidContainer || !targetIsLiquidContainer || !mayTarget || !canTarget)
        {
            Debug.Log("[ChemQOL Pour] Ignored: source/target must be compatible liquid containers.");
            return;
        }

        int selectedCapacity = WaterHelper.GetTotalCapacity(source);
        int receiverFreeCapacity = WaterHelper.GetFreeCapacity(isDrawing ? source : target);
        int amountToPour = Math.Min((selectedCapacity + PourFractionInverse - 1) / PourFractionInverse, receiverFreeCapacity);
        Debug.Log($"[ChemQOL Pour] Volumes: selectedCapacity={selectedCapacity}, receiverFree={receiverFreeCapacity}, requestedTransfer={amountToPour}.");
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

            GameItem transferSource = isDrawing ? target : source;
            GameItem transferReceiver = isDrawing ? source : target;
            Debug.Log($"[ChemQOL Pour] Transferring {Describe(transferSource)} -> staging container.");
            WaterHelper.TransferLiquid(transferSource, stagingContainer);
            Debug.Log($"[ChemQOL Pour] First transfer complete: source={WaterHelper.GetTotalVolume(source)}, staged={WaterHelper.GetTotalVolume(stagingContainer)}.");

            Debug.Log($"[ChemQOL Pour] Transferring staging container -> {Describe(transferReceiver)}.");
            WaterHelper.TransferLiquid(stagingContainer, transferReceiver);
            Debug.Log($"[ChemQOL Pour] {(isDrawing ? "Draw" : "Pour")} complete: staged={WaterHelper.GetTotalVolume(stagingContainer)}, receiver={WaterHelper.GetTotalVolume(transferReceiver)}.");

            WaterHelper.UpdateWaterBottleSound(source);
            WaterHelper.UpdateWaterBottleSound(target);
            if (isDrawing)
            {
                AudioHelper.DropperDraw();
            }
            else if (sourceIsPipette)
            {
                AudioHelper.DropperDrop();
            }
            else
            {
                AudioHelper.PourWater();
            }

            if (!isDrawing && WaterHelper.IsEmpty(source))
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
