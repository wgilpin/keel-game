using System;
using System.Collections.Generic;
using System.Numerics;
using Keel.Sim.Inventory;

namespace Keel.Sim.Grid;

public enum PlacementStatus
{
    NoTarget = 0,
    Inactive = 1,
    NoBlocks = 2,
    OutOfRange = 3,
    InvalidCell = 4,
    Valid = 5,
}

public readonly record struct PlacementEvaluation(
    PlacementStatus Status,
    Int3? StruckCell = null,
    Int3? CandidateCell = null,
    Int3 FaceOffset = default,
    BlockType StruckBlockType = BlockType.None,
    float HitDistance = float.MaxValue)
{
    public bool IsValid => Status == PlacementStatus.Valid;
    public bool ShowGhost => Status is PlacementStatus.Valid or PlacementStatus.InvalidCell;
}

public static class BlockPlacementSim
{
    public const int BuildSlotIndex = 2;

    public static PlacementEvaluation Evaluate(
        BlockGrid grid,
        bool isFloatingInSuit,
        int activeSlot,
        int steelBlockCount,
        bool hasStationHit,
        Vector3 localHitPosition,
        Vector3 localHitNormal,
        float hitDistance,
        float buildRange,
        bool isModalOpen = false,
        IReadOnlyList<(Vector3 Min, Vector3 Max)>? dynamicObstacles = null)
    {
        if (!isFloatingInSuit || isModalOpen || activeSlot != BuildSlotIndex)
        {
            return new PlacementEvaluation(PlacementStatus.Inactive);
        }

        if (steelBlockCount <= 0)
        {
            return new PlacementEvaluation(PlacementStatus.NoBlocks);
        }

        if (!hasStationHit || localHitNormal.LengthSquared() < 0.0001f)
        {
            return new PlacementEvaluation(PlacementStatus.NoTarget);
        }

        if (!grid.ResolveHitCell(localHitPosition, localHitNormal, out Int3 struckCell, out BlockType struckType) || struckType == BlockType.None)
        {
            return new PlacementEvaluation(PlacementStatus.NoTarget);
        }

        if (hitDistance > buildRange)
        {
            return new PlacementEvaluation(
                PlacementStatus.OutOfRange,
                StruckCell: struckCell,
                CandidateCell: null,
                FaceOffset: default,
                StruckBlockType: struckType,
                HitDistance: hitDistance);
        }

        // Quantize face normal to largest cardinal axis
        float absX = MathF.Abs(localHitNormal.X);
        float absY = MathF.Abs(localHitNormal.Y);
        float absZ = MathF.Abs(localHitNormal.Z);

        Int3 faceOffset = absX >= absY && absX >= absZ
            ? new Int3(MathF.Sign(localHitNormal.X), 0, 0)
            : (absY >= absX && absY >= absZ
                ? new Int3(0, MathF.Sign(localHitNormal.Y), 0)
                : new Int3(0, 0, MathF.Sign(localHitNormal.Z)));

        Int3 candidateCell = struckCell + faceOffset;

        // Validation rules:
        // 1. Cell must not already be occupied
        if (grid.HasBlock(candidateCell))
        {
            return new PlacementEvaluation(
                PlacementStatus.InvalidCell,
                StruckCell: struckCell,
                CandidateCell: candidateCell,
                FaceOffset: faceOffset,
                StruckBlockType: struckType,
                HitDistance: hitDistance);
        }

        // 2. Must not seal the +Z front face of any Refinery block (port protection)
        // If the block directly at -Z from candidate is a Refinery, this cell is directly over its +Z port.
        if (grid.GetBlock(candidateCell + new Int3(0, 0, -1)) == BlockType.Refinery)
        {
            return new PlacementEvaluation(
                PlacementStatus.InvalidCell,
                StruckCell: struckCell,
                CandidateCell: candidateCell,
                FaceOffset: faceOffset,
                StruckBlockType: struckType,
                HitDistance: hitDistance);
        }

        // 3. Must not overlap dynamic obstacles (suit collider, ship hull)
        if (dynamicObstacles != null && dynamicObstacles.Count > 0)
        {
            Vector3 cellCenter = grid.GetCellCenter(candidateCell);
            float halfExtent = (BlockGrid.CellSize * 0.5f) - 0.05f; // Small skin tolerance
            Vector3 cellMin = cellCenter - new Vector3(halfExtent, halfExtent, halfExtent);
            Vector3 cellMax = cellCenter + new Vector3(halfExtent, halfExtent, halfExtent);

            foreach (var (obsMin, obsMax) in dynamicObstacles)
            {
                if (AabbIntersects(cellMin, cellMax, obsMin, obsMax))
                {
                    return new PlacementEvaluation(
                        PlacementStatus.InvalidCell,
                        StruckCell: struckCell,
                        CandidateCell: candidateCell,
                        FaceOffset: faceOffset,
                        StruckBlockType: struckType,
                        HitDistance: hitDistance);
                }
            }
        }

        return new PlacementEvaluation(
            PlacementStatus.Valid,
            StruckCell: struckCell,
            CandidateCell: candidateCell,
            FaceOffset: faceOffset,
            StruckBlockType: struckType,
            HitDistance: hitDistance);
    }

    public static bool TryPlace(
        BlockGrid grid,
        SuitInventory inventory,
        in PlacementEvaluation eval,
        out string? reason)
    {
        if (!eval.IsValid || !eval.CandidateCell.HasValue)
        {
            reason = $"Placement invalid: {eval.Status}.";
            return false;
        }

        if (inventory.GetCount(ItemId.SteelBlock) <= 0)
        {
            reason = "No steel blocks in suit inventory.";
            return false;
        }

        if (!grid.TryPlaceBlock(eval.CandidateCell.Value, BlockType.ArmorBlock, out reason))
        {
            return false;
        }

        int removed = inventory.Remove(ItemId.SteelBlock, 1);
        if (removed != 1)
        {
            grid.RemoveBlock(eval.CandidateCell.Value, out _);
            reason = "Failed removing steel block from inventory.";
            return false;
        }

        reason = null;
        return true;
    }

    public static bool AabbIntersects(Vector3 minA, Vector3 maxA, Vector3 minB, Vector3 maxB)
    {
        return minA.X <= maxB.X && maxA.X >= minB.X &&
               minA.Y <= maxB.Y && maxA.Y >= minB.Y &&
               minA.Z <= maxB.Z && maxA.Z >= minB.Z;
    }
}
