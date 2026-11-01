using System;
using HarmonyLib;
using UnityEngine;

[HarmonyPatch(typeof(ItemSelectHandler), "OnEventUpdate")]
internal static class PourWithMousewheelPatch
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
            ModEntry.Logger.Log($"Pour: selection scroll delta=({scrollDelta.x}, {scrollDelta.y}), source={Describe(___currentItem)}");
        }

        if (scrollDelta.sqrMagnitude <= 0f)
        {
            return;
        }

        bool isDrawing = scrollDelta.y > 0f;
        if (scrollDelta.y == 0f)
        {
            ModEntry.Logger.Warning("Pour ignored: scroll event has no vertical movement.");
            return;
        }

        if (isDrawing ? !ChemQOLConfig.PipetteDrawWhileSelected : !ChemQOLConfig.PourWhileSelected)
        {
            return;
        }

        if (___currentItem == null || (___currentItem is GameItemElement selectedElement && selectedElement.IsDestroyed()))
        {
            ModEntry.Logger.Log("Pour ignored: no live item is selected.");
            return;
        }

        GameItem source = ___currentItem;
        bool sourceIsPipette = source.IsTag("LIQUID_CONTAINER_DROPPER_TAG");
        if (isDrawing && !sourceIsPipette)
        {
            ModEntry.Logger.Log("Pour ignored on wheel-up: selected item is not a pipette.");
            return;
        }

        GameItem target = RenderHandler.RaycastElement<GameItem>(Input.mousePosition);
        if (target == null)
        {
            ModEntry.Logger.Log("Pour ignored: no target item under the cursor.");
            return;
        }

        if (target == source)
        {
            ModEntry.Logger.Log("Pour ignored: cursor is over the selected source item.");
            return;
        }

        bool sourceIsLiquidContainer = source.IsTag("LIQUID_CONTAINER_TAG");
        bool targetIsLiquidContainer = target.IsTag("LIQUID_CONTAINER_TAG");
        bool mayTarget = source.MayTarget(target);
        bool canTarget = source.CanTarget(target);
        ModEntry.Logger.Log($"Pour candidate: mode={(isDrawing ? "draw" : "pour")}; pipette={sourceIsPipette}; source={Describe(source)} liquidContainer={sourceIsLiquidContainer}; target={Describe(target)} liquidContainer={targetIsLiquidContainer}; mayTarget={mayTarget}; canTarget={canTarget}.");

        if (!sourceIsLiquidContainer || !targetIsLiquidContainer || !mayTarget || !canTarget)
        {
            ModEntry.Logger.Log("Pour ignored: source and target must be compatible liquid containers.");
            return;
        }

        int selectedCapacity = WaterHelper.GetTotalCapacity(source);
        int receiverFreeCapacity = WaterHelper.GetFreeCapacity(isDrawing ? source : target);
        int amountToPour = Math.Min((selectedCapacity + PourFractionInverse - 1) / PourFractionInverse, receiverFreeCapacity);
        ModEntry.Logger.Log($"Pour volumes: selectedCapacity={selectedCapacity}, receiverFree={receiverFreeCapacity}, requestedTransfer={amountToPour}.");
        if (amountToPour <= 0)
        {
            ModEntry.Logger.Log("Pour skipped: source is empty or target is full.");
            return;
        }

        GameItem stagingContainer = DirectoryMaster.Item(source.identifier, isOwned: false);
        if (stagingContainer == null)
        {
            ModEntry.Logger.Error($"Pour failed: could not create staging container from '{source.identifier}'.");
            return;
        }

        try
        {
            ModEntry.Logger.Log($"Pour: created staging container '{stagingContainer.identifier}' with capacity {amountToPour}.");
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
            ModEntry.Logger.Log($"Pour: transferring {Describe(transferSource)} to staging container.");
            WaterHelper.TransferLiquid(transferSource, stagingContainer);
            ModEntry.Logger.Log($"Pour: first transfer complete; source={WaterHelper.GetTotalVolume(source)}, staged={WaterHelper.GetTotalVolume(stagingContainer)}.");

            ModEntry.Logger.Log($"Pour: transferring staging container to {Describe(transferReceiver)}.");
            WaterHelper.TransferLiquid(stagingContainer, transferReceiver);
            ModEntry.Logger.Log($"{(isDrawing ? "Draw" : "Pour")} complete: staged={WaterHelper.GetTotalVolume(stagingContainer)}, receiver={WaterHelper.GetTotalVolume(transferReceiver)}.");

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
                ModEntry.Logger.Log("Pour: source is empty; ending item selection.");
                ItemSelectHandler.current?.EndSelect();
            }
        }
        catch (Exception exception)
        {
            ModEntry.Logger.Error($"Pour transfer failed: {exception}");
        }
        finally
        {
            ModEntry.Logger.Log("Pour: destroying staging container.");
            stagingContainer.Destroy();
        }

    }

    private static string Describe(GameItem item)
    {
        return item == null ? "<none>" : $"{item.identifier} ('{item.name}')";
    }
}
