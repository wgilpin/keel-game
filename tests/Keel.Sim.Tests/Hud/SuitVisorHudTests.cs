using System;
using System.Collections.Generic;
using System.Numerics;
using Keel.Sim.Hud;
using Xunit;

namespace Keel.Sim.Tests.Hud;

public sealed class SuitVisorHudTests
{
    [Fact]
    public void BearingTape_ProjectsAzimuthsAndClampsOffSpanAndRearLandmarks()
    {
        // AC-1: Given suit camera world position, arbitrary 6DoF camera Right and Forward basis vectors,
        // and landmark world positions, on-span targets map proportionally into [-1, +1] un-clamped,
        // off-span and rear targets clamp to -1 or +1 without flipping across +-180 seam,
        // and zero-distance coincident targets return finite values without NaN.

        var camPos = new Vector3(0f, 10f, 0f);
        var camForward = new Vector3(0f, 0f, -1f);
        var camRight = new Vector3(1f, 0f, 0f);
        const float spanDegrees = 90.0f; // half-span = 45 degrees

        // 1. On-span target directly ahead at 100m
        var ahead = VisorBearingTapeModel.ProjectTarget(
            camPos, camForward, camRight,
            name: "SHIP",
            targetPosition: new Vector3(0f, 10f, -100f),
            spanDegrees: spanDegrees);

        Assert.Equal("SHIP", ahead.Name);
        Assert.Equal(100.0f, ahead.DistanceMeters, precision: 1);
        Assert.Equal("100 m", ahead.FormattedDistance);
        Assert.Equal(0.0f, ahead.AzimuthDegrees, precision: 2);
        Assert.Equal(0.0f, ahead.NormalizedX, precision: 2);
        Assert.False(ahead.IsClamped);
        Assert.False(ahead.IsRear);

        // 2. On-span target 30 degrees to the right (halfSpan = 45 deg -> normX = 30 / 45 = 0.6667)
        float rad30 = 30.0f * (MathF.PI / 180.0f);
        var right30Pos = camPos + new Vector3(MathF.Sin(rad30) * 100f, 0f, -MathF.Cos(rad30) * 100f);
        var right30 = VisorBearingTapeModel.ProjectTarget(
            camPos, camForward, camRight,
            name: "STATION",
            targetPosition: right30Pos,
            spanDegrees: spanDegrees);

        Assert.Equal(30.0f, right30.AzimuthDegrees, precision: 1);
        Assert.Equal(30.0f / 45.0f, right30.NormalizedX, precision: 2);
        Assert.False(right30.IsClamped);
        Assert.False(right30.IsRear);

        // 3. On-span target 30 degrees to the left (normX = -30 / 45 = -0.6667)
        var left30Pos = camPos + new Vector3(-MathF.Sin(rad30) * 100f, 0f, -MathF.Cos(rad30) * 100f);
        var left30 = VisorBearingTapeModel.ProjectTarget(
            camPos, camForward, camRight,
            name: "ASTEROID",
            targetPosition: left30Pos,
            spanDegrees: spanDegrees);

        Assert.Equal(-30.0f, left30.AzimuthDegrees, precision: 1);
        Assert.Equal(-30.0f / 45.0f, left30.NormalizedX, precision: 2);
        Assert.False(left30.IsClamped);
        Assert.False(left30.IsRear);

        // 4. Off-span forward targets (60 degrees right / left) clamp to +1 / -1
        float rad60 = 60.0f * (MathF.PI / 180.0f);
        var offRightPos = camPos + new Vector3(MathF.Sin(rad60) * 100f, 0f, -MathF.Cos(rad60) * 100f);
        var offRight = VisorBearingTapeModel.ProjectTarget(camPos, camForward, camRight, "OFF_R", offRightPos, spanDegrees);
        Assert.Equal(1.0f, offRight.NormalizedX, precision: 2);
        Assert.True(offRight.IsClamped);
        Assert.False(offRight.IsRear);

        var offLeftPos = camPos + new Vector3(-MathF.Sin(rad60) * 100f, 0f, -MathF.Cos(rad60) * 100f);
        var offLeft = VisorBearingTapeModel.ProjectTarget(camPos, camForward, camRight, "OFF_L", offLeftPos, spanDegrees);
        Assert.Equal(-1.0f, offLeft.NormalizedX, precision: 2);
        Assert.True(offLeft.IsClamped);
        Assert.False(offLeft.IsRear);

        // 5. Rear targets (behind camera, forwardProj < 0) clamp to +1 (right) or -1 (left)
        // Rear-right (135 degrees): must clamp to +1
        var rearRightPos = camPos + new Vector3(50f, 0f, 50f);
        var rearRight = VisorBearingTapeModel.ProjectTarget(camPos, camForward, camRight, "REAR_R", rearRightPos, spanDegrees);
        Assert.Equal(1.0f, rearRight.NormalizedX, precision: 2);
        Assert.True(rearRight.IsClamped);
        Assert.True(rearRight.IsRear);

        // Rear-left (-135 degrees): must clamp to -1
        var rearLeftPos = camPos + new Vector3(-50f, 0f, 50f);
        var rearLeft = VisorBearingTapeModel.ProjectTarget(camPos, camForward, camRight, "REAR_L", rearLeftPos, spanDegrees);
        Assert.Equal(-1.0f, rearLeft.NormalizedX, precision: 2);
        Assert.True(rearLeft.IsClamped);
        Assert.True(rearLeft.IsRear);

        // 6. Rear seam (+179 deg vs -179 deg): no flip
        float rad179 = 179.0f * (MathF.PI / 180.0f);
        var seamPosPositive = camPos + new Vector3(MathF.Sin(rad179) * 100f, 0f, -MathF.Cos(rad179) * 100f);
        var seamPosNegative = camPos + new Vector3(MathF.Sin(-rad179) * 100f, 0f, -MathF.Cos(-rad179) * 100f);
        var seamPos = VisorBearingTapeModel.ProjectTarget(camPos, camForward, camRight, "SEAM+", seamPosPositive, spanDegrees);
        var seamNeg = VisorBearingTapeModel.ProjectTarget(camPos, camForward, camRight, "SEAM-", seamPosNegative, spanDegrees);
        Assert.Equal(1.0f, seamPos.NormalizedX, precision: 2);
        Assert.Equal(-1.0f, seamNeg.NormalizedX, precision: 2);

        // 7. Arbitrary 6DoF camera basis (pitched 45 deg up, rolled)
        // Forward: (0, 1/sqrt(2), -1/sqrt(2)), Right: (1, 0, 0)
        var basisForward = Vector3.Normalize(new Vector3(0f, 1f, -1f));
        var basisRight = new Vector3(1f, 0f, 0f);
        var arbitraryTarget = camPos + basisForward * 50f + basisRight * 10f;
        var arbResult = VisorBearingTapeModel.ProjectTarget(camPos, basisForward, basisRight, "ARB", arbitraryTarget, spanDegrees);
        Assert.False(arbResult.IsRear);
        Assert.True(arbResult.NormalizedX > 0f && arbResult.NormalizedX < 1f);

        // 8. Zero-distance coincident target returns finite values without NaN
        var coincident = VisorBearingTapeModel.ProjectTarget(camPos, camForward, camRight, "COINCIDENT", camPos, spanDegrees);
        Assert.False(float.IsNaN(coincident.NormalizedX));
        Assert.False(float.IsInfinity(coincident.NormalizedX));
        Assert.Equal(0.0f, coincident.NormalizedX);
        Assert.Equal(0.0f, coincident.DistanceMeters);
        Assert.False(coincident.IsClamped);
    }

    [Fact]
    public void BearingTape_WrapsCompassTicksCleanlyAcross180Seam()
    {
        // AC-2: Given camera heading angles sweeping across 0° and the -180° / +180° wraparound seam,
        // when VisorBearingTapeModel.ComputeTicks generates compass degree ticks for the visible span,
        // then tick normalized X positions stay strictly ordered inside [-1, +1] with wrapped [0, 360) compass labels.

        const float spanDegrees = 90.0f;
        const float tickInterval = 15.0f;

        float[] headingsToTest = [-185f, -180f, -179f, -90f, -15f, -1f, 0f, 1f, 15f, 90f, 179f, 180f, 185f, 359f, 360f, 720f];

        foreach (float heading in headingsToTest)
        {
            IReadOnlyList<CompassTick> ticks = VisorBearingTapeModel.ComputeTicks(heading, spanDegrees, tickInterval);

            Assert.NotEmpty(ticks);

            for (int i = 0; i < ticks.Count; i++)
            {
                CompassTick tick = ticks[i];

                // Must stay inside [-1, +1]
                Assert.InRange(tick.NormalizedX, -1.001f, 1.001f);

                // Compass heading must be strictly in [0, 360)
                Assert.InRange(tick.HeadingDegrees, 0, 359);

                // Label must represent the wrapped compass heading
                Assert.Equal(tick.HeadingDegrees.ToString(), tick.Label);

                // Strictly ascending order across the tape
                if (i > 0)
                {
                    Assert.True(tick.NormalizedX > ticks[i - 1].NormalizedX,
                        $"NormalizedX for tick {i} ({tick.NormalizedX}) must be strictly greater than tick {i - 1} ({ticks[i - 1].NormalizedX}) at heading {heading}");
                }
            }
        }

        // Specific test at heading = 180°: center tick should have HeadingDegrees = 180
        IReadOnlyList<CompassTick> ticksAt180 = VisorBearingTapeModel.ComputeTicks(180f, spanDegrees, tickInterval);
        CompassTick? centerTick = null;
        foreach (CompassTick tick in ticksAt180)
        {
            if (MathF.Abs(tick.NormalizedX) < 1e-3f)
            {
                centerTick = tick;
                break;
            }
        }
        Assert.NotNull(centerTick);
        Assert.Equal(180, centerTick.Value.HeadingDegrees);
        Assert.Equal("180", centerTick.Value.Label);
    }

    [Fact]
    public void VisorModel_ClampsHotbarSelectionCargoFillAndBoardingPrompt()
    {
        // AC-3: Given suit cargo count/capacity, hotbar slot key input, and suit-to-ship distance vs shipInteractRange,
        // when SuitVisorModel.Compute evaluates HUD state, then invalid slot indices leave the active [1]–[5] slot unchanged,
        // cargo fill ratio clamps to [0, 1] flagging IsCargoFull at >= capacity,
        // and the [F] Board Ship prompt activates if and only if distance <= shipInteractRange.

        const float interactRange = 5.0f;
        const int cargoCapacity = 50;

        // 1. Initial default state: active slot 1, cargo 0/50, out of boarding range
        SuitVisorState state = SuitVisorModel.Compute(
            currentSlot: 1,
            slotInput: null,
            cargoCount: 0,
            cargoCapacity: cargoCapacity,
            suitToShipDistance: 10.0f,
            shipInteractRange: interactRange,
            currentSpeed: 0.0f,
            dampenersEnabled: true);

        Assert.Equal(1, state.ActiveSlot);
        Assert.Equal(0, state.CargoCount);
        Assert.Equal(cargoCapacity, state.CargoCapacity);
        Assert.Equal(0.0f, state.CargoFillRatio, precision: 2);
        Assert.False(state.IsCargoFull);
        Assert.False(state.CanBoardShip);
        Assert.Null(state.BoardingPrompt);

        // 2. Valid slot selections 1..5
        for (int validSlot = 1; validSlot <= 5; validSlot++)
        {
            SuitVisorState next = SuitVisorModel.Compute(
                currentSlot: state.ActiveSlot,
                slotInput: validSlot,
                cargoCount: 0,
                cargoCapacity: cargoCapacity,
                suitToShipDistance: 10.0f,
                shipInteractRange: interactRange,
                currentSpeed: 0.0f,
                dampenersEnabled: true);

            Assert.Equal(validSlot, next.ActiveSlot);
            state = next;
        }

        // Active slot is now 5. Test invalid slot inputs: 0, 6, -1, 99
        int[] invalidInputs = [0, 6, -1, 99];
        foreach (int invalid in invalidInputs)
        {
            SuitVisorState unchanged = SuitVisorModel.Compute(
                currentSlot: state.ActiveSlot,
                slotInput: invalid,
                cargoCount: 0,
                cargoCapacity: cargoCapacity,
                suitToShipDistance: 10.0f,
                shipInteractRange: interactRange,
                currentSpeed: 0.0f,
                dampenersEnabled: true);

            Assert.Equal(5, unchanged.ActiveSlot);
        }

        // 3. Cargo fill ratio clamping and IsCargoFull flag
        // Half full: 25 / 50 -> 0.5
        SuitVisorState halfCargo = SuitVisorModel.Compute(
            currentSlot: 1,
            slotInput: null,
            cargoCount: 25,
            cargoCapacity: cargoCapacity,
            suitToShipDistance: 10.0f,
            shipInteractRange: interactRange,
            currentSpeed: 0.0f,
            dampenersEnabled: true);
        Assert.Equal(0.5f, halfCargo.CargoFillRatio, precision: 2);
        Assert.False(halfCargo.IsCargoFull);

        // Exactly full: 50 / 50 -> 1.0, IsCargoFull = true
        SuitVisorState exactCargo = SuitVisorModel.Compute(
            currentSlot: 1,
            slotInput: null,
            cargoCount: 50,
            cargoCapacity: cargoCapacity,
            suitToShipDistance: 10.0f,
            shipInteractRange: interactRange,
            currentSpeed: 0.0f,
            dampenersEnabled: true);
        Assert.Equal(1.0f, exactCargo.CargoFillRatio, precision: 2);
        Assert.True(exactCargo.IsCargoFull);

        // Overflow: 60 / 50 -> clamps to 1.0, IsCargoFull = true
        SuitVisorState overCargo = SuitVisorModel.Compute(
            currentSlot: 1,
            slotInput: null,
            cargoCount: 60,
            cargoCapacity: cargoCapacity,
            suitToShipDistance: 10.0f,
            shipInteractRange: interactRange,
            currentSpeed: 0.0f,
            dampenersEnabled: true);
        Assert.Equal(1.0f, overCargo.CargoFillRatio, precision: 2);
        Assert.True(overCargo.IsCargoFull);

        // Underflow: -5 / 50 -> clamps to 0.0, IsCargoFull = false
        SuitVisorState underCargo = SuitVisorModel.Compute(
            currentSlot: 1,
            slotInput: null,
            cargoCount: -5,
            cargoCapacity: cargoCapacity,
            suitToShipDistance: 10.0f,
            shipInteractRange: interactRange,
            currentSpeed: 0.0f,
            dampenersEnabled: true);
        Assert.Equal(0.0f, underCargo.CargoFillRatio, precision: 2);
        Assert.False(underCargo.IsCargoFull);

        // 4. Boarding prompt within shipInteractRange
        // Within range: 4.0m <= 5.0m
        SuitVisorState inRange = SuitVisorModel.Compute(
            currentSlot: 1,
            slotInput: null,
            cargoCount: 0,
            cargoCapacity: cargoCapacity,
            suitToShipDistance: 4.0f,
            shipInteractRange: interactRange,
            currentSpeed: 0.0f,
            dampenersEnabled: true);
        Assert.True(inRange.CanBoardShip);
        Assert.Equal("[F] Board Ship", inRange.BoardingPrompt);

        // Exactly on threshold: 5.0m <= 5.0m
        SuitVisorState exactRange = SuitVisorModel.Compute(
            currentSlot: 1,
            slotInput: null,
            cargoCount: 0,
            cargoCapacity: cargoCapacity,
            suitToShipDistance: 5.0f,
            shipInteractRange: interactRange,
            currentSpeed: 0.0f,
            dampenersEnabled: true);
        Assert.True(exactRange.CanBoardShip);
        Assert.Equal("[F] Board Ship", exactRange.BoardingPrompt);

        // Just outside range: 5.01m > 5.0m
        SuitVisorState outOfRange = SuitVisorModel.Compute(
            currentSlot: 1,
            slotInput: null,
            cargoCount: 0,
            cargoCapacity: cargoCapacity,
            suitToShipDistance: 5.01f,
            shipInteractRange: interactRange,
            currentSpeed: 0.0f,
            dampenersEnabled: true);
        Assert.False(outOfRange.CanBoardShip);
        Assert.Null(outOfRange.BoardingPrompt);
    }

    [Fact]
    public void VisorModel_PrioritizesRefineryPromptsAndOutOfRangeStates()
    {
        const int cargoCapacity = 50;
        const float shipRange = 6.0f;
        const float refineryRange = 5.0f;

        // 1. Aiming at Refinery within refineryRange (3.0m <= 5.0m)
        // Even when in ship range (4.0m <= 6.0m), Refinery prompt takes precedence
        SuitVisorState refineryClose = SuitVisorModel.Compute(
            currentSlot: 1,
            slotInput: null,
            cargoCount: 10,
            cargoCapacity: cargoCapacity,
            suitToShipDistance: 4.0f,
            shipInteractRange: shipRange,
            currentSpeed: 1.0f,
            dampenersEnabled: true,
            steelBlockCount: 0,
            targetedBlockType: Keel.Sim.Grid.BlockType.Refinery,
            targetedBlockDistance: 3.0f,
            refineryInteractRange: refineryRange);

        Assert.True(refineryClose.CanOpenRefinery);
        Assert.Equal("[F] Open Refinery", refineryClose.PromptText);

        // 2. Aiming at Refinery out of range (8.0m > 5.0m, within 25m)
        SuitVisorState refineryFar = SuitVisorModel.Compute(
            currentSlot: 1,
            slotInput: null,
            cargoCount: 10,
            cargoCapacity: cargoCapacity,
            suitToShipDistance: 20.0f,
            shipInteractRange: shipRange,
            currentSpeed: 1.0f,
            dampenersEnabled: true,
            steelBlockCount: 0,
            targetedBlockType: Keel.Sim.Grid.BlockType.Refinery,
            targetedBlockDistance: 8.0f,
            refineryInteractRange: refineryRange);

        Assert.False(refineryFar.CanOpenRefinery);
        Assert.Equal("OUT OF RANGE", refineryFar.PromptText);

        // 3. Aiming at ArmorBlock does not trigger refinery prompt, falls back to Board Ship if in ship range
        SuitVisorState armorClose = SuitVisorModel.Compute(
            currentSlot: 1,
            slotInput: null,
            cargoCount: 10,
            cargoCapacity: cargoCapacity,
            suitToShipDistance: 4.0f,
            shipInteractRange: shipRange,
            currentSpeed: 1.0f,
            dampenersEnabled: true,
            steelBlockCount: 0,
            targetedBlockType: Keel.Sim.Grid.BlockType.ArmorBlock,
            targetedBlockDistance: 3.0f,
            refineryInteractRange: refineryRange);

        Assert.False(armorClose.CanOpenRefinery);
        Assert.Equal("[F] Board Ship", armorClose.PromptText);

        // 4. Slot 2 selected with 0 steel blocks and no target: prompt informs player to refine ore
        SuitVisorState slot2Empty = SuitVisorModel.Compute(
            currentSlot: 2,
            slotInput: null,
            cargoCount: 10,
            cargoCapacity: cargoCapacity,
            suitToShipDistance: 20.0f,
            shipInteractRange: shipRange,
            currentSpeed: 1.0f,
            dampenersEnabled: true,
            steelBlockCount: 0);

        Assert.Equal("NO STEEL BLOCKS — REFINE ORE AT STATION", slot2Empty.PromptText);
    }

    [Fact]
    public void VisorModel_SurfacesConstructionPromptsAndRangeDimmingWhilePreservingInteractFlags()
    {
        const int cargoCapacity = 50;
        const float shipRange = 6.0f;

        // 1. Valid cell placement prompt when aiming in slot 2 with blocks in ship range (4m <= 6m)
        // Construction prompt takes display priority over "[F] Board Ship" while CanBoardShip remains true
        SuitVisorState validState = SuitVisorModel.Compute(
            currentSlot: 2,
            slotInput: null,
            cargoCount: 5,
            cargoCapacity: cargoCapacity,
            suitToShipDistance: 4.0f,
            shipInteractRange: shipRange,
            currentSpeed: 0.0f,
            dampenersEnabled: true,
            steelBlockCount: 5,
            placementStatus: Keel.Sim.Grid.PlacementStatus.Valid);

        Assert.Equal("[LMB] Place Steel Block", validState.PromptText);
        Assert.Equal(PromptTone.Primary, validState.PromptTone);
        Assert.False(validState.IsTargetOutOfRange);
        Assert.True(validState.CanBoardShip); // F-key boarding eligibility preserved

        // 2. Invalid cell prompt (occupied / obstructed / port)
        SuitVisorState invalidState = SuitVisorModel.Compute(
            currentSlot: 2,
            slotInput: null,
            cargoCount: 5,
            cargoCapacity: cargoCapacity,
            suitToShipDistance: 20.0f,
            shipInteractRange: shipRange,
            currentSpeed: 0.0f,
            dampenersEnabled: true,
            steelBlockCount: 5,
            placementStatus: Keel.Sim.Grid.PlacementStatus.InvalidCell);

        Assert.Equal("INVALID CELL", invalidState.PromptText);
        Assert.Equal(PromptTone.Danger, invalidState.PromptTone);
        Assert.False(invalidState.IsTargetOutOfRange);

        // 3. Out of build range prompt & reticle dimming
        SuitVisorState outOfRangeState = SuitVisorModel.Compute(
            currentSlot: 2,
            slotInput: null,
            cargoCount: 5,
            cargoCapacity: cargoCapacity,
            suitToShipDistance: 20.0f,
            shipInteractRange: shipRange,
            currentSpeed: 0.0f,
            dampenersEnabled: true,
            steelBlockCount: 5,
            placementStatus: Keel.Sim.Grid.PlacementStatus.OutOfRange);

        Assert.Equal("OUT OF RANGE", outOfRangeState.PromptText);
        Assert.Equal(PromptTone.Warning, outOfRangeState.PromptTone);
        Assert.True(outOfRangeState.IsTargetOutOfRange);

        // 4. No target in slot 2 falls back to Board Ship if in ship range
        SuitVisorState noTargetNearShip = SuitVisorModel.Compute(
            currentSlot: 2,
            slotInput: null,
            cargoCount: 5,
            cargoCapacity: cargoCapacity,
            suitToShipDistance: 3.0f,
            shipInteractRange: shipRange,
            currentSpeed: 0.0f,
            dampenersEnabled: true,
            steelBlockCount: 5,
            placementStatus: Keel.Sim.Grid.PlacementStatus.NoTarget);

        Assert.Equal("[F] Board Ship", noTargetNearShip.PromptText);
        Assert.Equal(PromptTone.Primary, noTargetNearShip.PromptTone);
        Assert.True(noTargetNearShip.CanBoardShip);
    }

    [Fact]
    public void VisorModel_IsolatesDrillAndBuildSlotStatesAndZeroStockFallbacks()
    {
        const int cargoCapacity = 50;
        const float shipRange = 6.0f;
        const float refineryRange = 5.0f;

        // 1. Slot 1 (Drill) isolates drill prompts and ignores construction placementStatus
        SuitVisorState slot1Drilling = SuitVisorModel.Compute(
            currentSlot: 1,
            slotInput: null,
            cargoCount: 10,
            cargoCapacity: cargoCapacity,
            suitToShipDistance: 20.0f,
            shipInteractRange: shipRange,
            currentSpeed: 0.0f,
            dampenersEnabled: true,
            steelBlockCount: 5,
            placementStatus: Keel.Sim.Grid.PlacementStatus.Valid, // Should be ignored in slot 1
            drillStatus: Keel.Sim.Mining.DrillStatus.Drilling,
            isAimingAtDeposit: true);

        Assert.Equal("[LMB] Drill Iron Ore", slot1Drilling.PromptText);
        Assert.Equal(PromptTone.Warning, slot1Drilling.PromptTone);

        // 2. Slot 2 ignores drill deposit aiming
        SuitVisorState slot2WithDeposit = SuitVisorModel.Compute(
            currentSlot: 2,
            slotInput: null,
            cargoCount: 10,
            cargoCapacity: cargoCapacity,
            suitToShipDistance: 20.0f,
            shipInteractRange: shipRange,
            currentSpeed: 0.0f,
            dampenersEnabled: true,
            steelBlockCount: 5,
            placementStatus: Keel.Sim.Grid.PlacementStatus.Valid,
            drillStatus: Keel.Sim.Mining.DrillStatus.Drilling,
            isAimingAtDeposit: true);

        Assert.Equal("[LMB] Place Steel Block", slot2WithDeposit.PromptText);
        Assert.Equal(PromptTone.Primary, slot2WithDeposit.PromptTone);

        // 3. Zero steel blocks in slot 2 prioritizes Refinery first, then Ship, then "NO STEEL BLOCKS"
        // Near refinery:
        SuitVisorState emptyNearRefinery = SuitVisorModel.Compute(
            currentSlot: 2,
            slotInput: null,
            cargoCount: 10,
            cargoCapacity: cargoCapacity,
            suitToShipDistance: 4.0f,
            shipInteractRange: shipRange,
            currentSpeed: 0.0f,
            dampenersEnabled: true,
            steelBlockCount: 0,
            targetedBlockType: Keel.Sim.Grid.BlockType.Refinery,
            targetedBlockDistance: 3.0f,
            refineryInteractRange: refineryRange);

        Assert.Equal("[F] Open Refinery", emptyNearRefinery.PromptText);
        Assert.True(emptyNearRefinery.CanOpenRefinery);

        // Near ship (no refinery target):
        SuitVisorState emptyNearShip = SuitVisorModel.Compute(
            currentSlot: 2,
            slotInput: null,
            cargoCount: 10,
            cargoCapacity: cargoCapacity,
            suitToShipDistance: 4.0f,
            shipInteractRange: shipRange,
            currentSpeed: 0.0f,
            dampenersEnabled: true,
            steelBlockCount: 0);

        Assert.Equal("[F] Board Ship", emptyNearShip.PromptText);
        Assert.True(emptyNearShip.CanBoardShip);

        // Away from ship and refinery:
        SuitVisorState emptyAway = SuitVisorModel.Compute(
            currentSlot: 2,
            slotInput: null,
            cargoCount: 10,
            cargoCapacity: cargoCapacity,
            suitToShipDistance: 20.0f,
            shipInteractRange: shipRange,
            currentSpeed: 0.0f,
            dampenersEnabled: true,
            steelBlockCount: 0);

        Assert.Equal("NO STEEL BLOCKS — REFINE ORE AT STATION", emptyAway.PromptText);
        Assert.Equal(PromptTone.Warning, emptyAway.PromptTone);
        Assert.True(emptyAway.Slots[1].IsDimmed); // Slot [2] is dimmed
    }
}
