using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Keel.Sim.Config;
using Keel.Sim.Grid;
using Keel.Sim.Inventory;
using Keel.Sim.Mining;
using Keel.Sim.Persistence;
using Keel.Sim.Pilot;
using Keel.Sim.Refinery;
using Xunit;

namespace Keel.Sim.Tests.Persistence;

public class WorldPersistenceTests
{
    [Fact]
    public void RoundTrip_PreservesShipSuitPilotModeAndInventoryInvariants()
    {
        var originalShip = new KinematicsSaveData(
            Position: new Vector3(10f, 20f, -30f),
            Orientation: Quaternion.CreateFromYawPitchRoll(0.5f, -0.2f, 0.1f),
            LinearVelocity: new Vector3(5f, -2f, 15f),
            AngularVelocity: new Vector3(0.1f, -0.05f, 0.2f),
            DampenersEnabled: true);

        var originalSuit = new SuitSaveData(
            Kinematics: new KinematicsSaveData(
                Position: new Vector3(12f, 20f, -30f),
                Orientation: Quaternion.CreateFromYawPitchRoll(0.1f, 0f, 0f),
                LinearVelocity: new Vector3(1f, 0f, 2f),
                AngularVelocity: Vector3.Zero,
                DampenersEnabled: false),
            ActiveHotbarSlot: 3);

        var inventoryItems = new[]
        {
            new InventoryItemSaveData(ItemId.IronOre, 15),
            new InventoryItemSaveData(ItemId.SteelBlock, 8),
        };

        var originalWorld = new WorldSaveData(
            Version: 1,
            PilotMode: PilotMode.FloatingInSuit,
            Ship: originalShip,
            Suit: originalSuit,
            SuitInventory: inventoryItems);

        string json = WorldSaveSerializer.Serialize(originalWorld);
        Assert.False(string.IsNullOrWhiteSpace(json));

        var loadResult = WorldSaveSerializer.Deserialize(json);
        Assert.True(loadResult.IsSuccess, loadResult.ErrorMessage);
        Assert.NotNull(loadResult.Data);

        var loaded = loadResult.Data!;
        Assert.Equal(1, loaded.Version);
        Assert.Equal(PilotMode.FloatingInSuit, loaded.PilotMode);

        // Verify ship kinematics
        Assert.Equal(originalShip.Position.X, loaded.Ship.Position.X, precision: 3);
        Assert.Equal(originalShip.Position.Y, loaded.Ship.Position.Y, precision: 3);
        Assert.Equal(originalShip.Position.Z, loaded.Ship.Position.Z, precision: 3);
        Assert.Equal(originalShip.DampenersEnabled, loaded.Ship.DampenersEnabled);
        Assert.True(MathF.Abs(loaded.Ship.Orientation.Length() - 1f) < 1e-4f);

        // Verify suit kinematics and slot
        Assert.Equal(3, loaded.Suit.ActiveHotbarSlot);
        Assert.Equal(originalSuit.Kinematics.Position.X, loaded.Suit.Kinematics.Position.X, precision: 3);
        Assert.Equal(originalSuit.Kinematics.DampenersEnabled, loaded.Suit.Kinematics.DampenersEnabled);

        // Test mapper round-trip to domain state machine & inventory
        var stateMachine = new PilotModeStateMachine();
        var inventory = new SuitInventory(50);
        WorldStateMapper.Apply(loaded, stateMachine, inventory);

        Assert.Equal(PilotMode.FloatingInSuit, stateMachine.Mode);
        Assert.Equal(15, inventory.GetCount(ItemId.IronOre));
        Assert.Equal(8, inventory.GetCount(ItemId.SteelBlock));
        Assert.Equal(23, inventory.TotalCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("{ invalid json ")]
    [InlineData("{\"version\": 1, \"pilotMode\": \"InvalidMode\"}")]
    public void Deserialize_RejectsMalformedJsonSafely(string malformedJson)
    {
        var result = WorldSaveSerializer.Deserialize(malformedJson);
        Assert.False(result.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(result.ErrorMessage));
        Assert.Null(result.Data);
    }

    [Fact]
    public void Deserialize_RejectsMalformedJsonAndNonFiniteKinematicsSafely()
    {
        // NaN in Position
        string nanJson = """
        {
          "version": 1,
          "pilotMode": "SeatedInCockpit",
          "ship": {
            "position": {"x": "NaN", "y": 0, "z": 0},
            "orientation": {"x": 0, "y": 0, "z": 0, "w": 1},
            "linearVelocity": {"x": 0, "y": 0, "z": 0},
            "angularVelocity": {"x": 0, "y": 0, "z": 0},
            "dampenersEnabled": true
          },
          "suit": {
            "kinematics": {
              "position": {"x": 0, "y": 0, "z": 0},
              "orientation": {"x": 0, "y": 0, "z": 0, "w": 1},
              "linearVelocity": {"x": 0, "y": 0, "z": 0},
              "angularVelocity": {"x": 0, "y": 0, "z": 0},
              "dampenersEnabled": true
            },
            "activeHotbarSlot": 1
          },
          "suitInventory": []
        }
        """;
        var nanResult = WorldSaveSerializer.Deserialize(nanJson);
        Assert.False(nanResult.IsSuccess);

        // Negative inventory count
        string negInvJson = """
        {
          "version": 1,
          "pilotMode": "SeatedInCockpit",
          "ship": {
            "position": {"x": 0, "y": 0, "z": 0},
            "orientation": {"x": 0, "y": 0, "z": 0, "w": 1},
            "linearVelocity": {"x": 0, "y": 0, "z": 0},
            "angularVelocity": {"x": 0, "y": 0, "z": 0},
            "dampenersEnabled": true
          },
          "suit": {
            "kinematics": {
              "position": {"x": 0, "y": 0, "z": 0},
              "orientation": {"x": 0, "y": 0, "z": 0, "w": 1},
              "linearVelocity": {"x": 0, "y": 0, "z": 0},
              "angularVelocity": {"x": 0, "y": 0, "z": 0},
              "dampenersEnabled": true
            },
            "activeHotbarSlot": 1
          },
          "suitInventory": [
            {"itemId": "IronOre", "quantity": -5}
          ]
        }
        """;
        var negInvResult = WorldSaveSerializer.Deserialize(negInvJson);
        Assert.False(negInvResult.IsSuccess);

        // Inventory exceeding max capacity
        string overInvJson = """
        {
          "version": 1,
          "pilotMode": "SeatedInCockpit",
          "ship": {
            "position": {"x": 0, "y": 0, "z": 0},
            "orientation": {"x": 0, "y": 0, "z": 0, "w": 1},
            "linearVelocity": {"x": 0, "y": 0, "z": 0},
            "angularVelocity": {"x": 0, "y": 0, "z": 0},
            "dampenersEnabled": true
          },
          "suit": {
            "kinematics": {
              "position": {"x": 0, "y": 0, "z": 0},
              "orientation": {"x": 0, "y": 0, "z": 0, "w": 1},
              "linearVelocity": {"x": 0, "y": 0, "z": 0},
              "angularVelocity": {"x": 0, "y": 0, "z": 0},
              "dampenersEnabled": true
            },
            "activeHotbarSlot": 1
          },
          "suitInventory": [
            {"itemId": "IronOre", "quantity": 9999}
          ]
        }
        """;
        var overInvResult = WorldSaveSerializer.Deserialize(overInvJson);
        Assert.False(overInvResult.IsSuccess);
    }

    [Fact]
    public void FileStore_WritesAtomicallyAndHandlesMissingOrCorruptSaveFiles()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "keel_tests_" + Guid.NewGuid().ToString("N"));
        try
        {
            string savePath = Path.Combine(tempDir, "saves", "world.json");
            string tmpPath = savePath + ".tmp";

            // Test missing file load
            var missingResult = WorldSaveStore.LoadFromFile(savePath);
            Assert.False(missingResult.IsSuccess);
            Assert.Null(missingResult.Data);

            // Test save to new directory
            var dummyData = new WorldSaveData(
                Version: 1,
                PilotMode: PilotMode.SeatedInCockpit,
                Ship: new KinematicsSaveData(Vector3.Zero, Quaternion.Identity, Vector3.Zero, Vector3.Zero, true),
                Suit: new SuitSaveData(new KinematicsSaveData(Vector3.Zero, Quaternion.Identity, Vector3.Zero, Vector3.Zero, true), 1),
                SuitInventory: Array.Empty<InventoryItemSaveData>());

            var saveResult = WorldSaveStore.SaveToFile(savePath, dummyData);
            Assert.True(saveResult.IsSuccess, saveResult.ErrorMessage);
            Assert.True(File.Exists(savePath));
            Assert.False(File.Exists(tmpPath)); // temp file cleaned up

            // Test load from saved file
            var loadResult = WorldSaveStore.LoadFromFile(savePath);
            Assert.True(loadResult.IsSuccess, loadResult.ErrorMessage);
            Assert.NotNull(loadResult.Data);
            Assert.Equal(PilotMode.SeatedInCockpit, loadResult.Data!.PilotMode);

            // Test corrupt file load
            File.WriteAllText(savePath, "{ not valid json at all");
            var corruptResult = WorldSaveStore.LoadFromFile(savePath);
            Assert.False(corruptResult.IsSuccess);
            Assert.Null(corruptResult.Data);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact]
    public void RoundTrip_PreservesModifiedStationGridAndRejectsDisconnectedOrCorelessGrids()
    {
        var grid = StationBlueprint.CreateDefault();
        // Modify grid: place armor block at (1, 1, 2)
        grid.TryPlaceBlock(new Int3(1, 1, 2), BlockType.ArmorBlock, out _);
        // Remove an outer spar block at (4, 0, 0)
        grid.RemoveBlock(new Int3(4, 0, 0), out _);

        int originalCount = grid.Count;
        Assert.True(grid.HasBlock(new Int3(1, 1, 2)));
        Assert.False(grid.HasBlock(new Int3(4, 0, 0)));

        var dummyShip = new KinematicsSaveData(Vector3.Zero, Quaternion.Identity, Vector3.Zero, Vector3.Zero, true);
        var dummySuit = new SuitSaveData(new KinematicsSaveData(Vector3.Zero, Quaternion.Identity, Vector3.Zero, Vector3.Zero, true), 1);

        var snapshot = WorldStateMapper.CreateSnapshot(
            PilotMode.SeatedInCockpit,
            dummyShip,
            dummySuit.Kinematics,
            1,
            new SuitInventory(50),
            grid);

        string json = WorldSaveSerializer.Serialize(snapshot);
        var loadResult = WorldSaveSerializer.Deserialize(json);
        Assert.True(loadResult.IsSuccess, loadResult.ErrorMessage);
        Assert.NotNull(loadResult.Data?.StationBlocks);

        var restoredGrid = new BlockGrid();
        WorldStateMapper.ApplyStationBlocks(loadResult.Data!, restoredGrid);

        Assert.Equal(originalCount, restoredGrid.Count);
        Assert.True(restoredGrid.HasBlock(new Int3(1, 1, 2)));
        Assert.False(restoredGrid.HasBlock(new Int3(4, 0, 0)));
        Assert.Equal(BlockType.Core, restoredGrid.GetBlock(new Int3(0, 0, 0)));
        Assert.True(restoredGrid.IsConnected());

        // Test rejecting missing Core
        string corelessJson = """
        {
          "version": 1,
          "pilotMode": "SeatedInCockpit",
          "ship": {
            "position": {"x": 0, "y": 0, "z": 0},
            "orientation": {"x": 0, "y": 0, "z": 0, "w": 1},
            "linearVelocity": {"x": 0, "y": 0, "z": 0},
            "angularVelocity": {"x": 0, "y": 0, "z": 0},
            "dampenersEnabled": true
          },
          "suit": {
            "kinematics": {
              "position": {"x": 0, "y": 0, "z": 0},
              "orientation": {"x": 0, "y": 0, "z": 0, "w": 1},
              "linearVelocity": {"x": 0, "y": 0, "z": 0},
              "angularVelocity": {"x": 0, "y": 0, "z": 0},
              "dampenersEnabled": true
            },
            "activeHotbarSlot": 1
          },
          "suitInventory": [],
          "stationBlocks": [
            {"cell": {"x": 1, "y": 0, "z": 0}, "blockType": "ArmorBlock"}
          ]
        }
        """;
        var corelessResult = WorldSaveSerializer.Deserialize(corelessJson);
        Assert.False(corelessResult.IsSuccess);

        // Test rejecting disconnected grid
        string disconnectedJson = """
        {
          "version": 1,
          "pilotMode": "SeatedInCockpit",
          "ship": {
            "position": {"x": 0, "y": 0, "z": 0},
            "orientation": {"x": 0, "y": 0, "z": 0, "w": 1},
            "linearVelocity": {"x": 0, "y": 0, "z": 0},
            "angularVelocity": {"x": 0, "y": 0, "z": 0},
            "dampenersEnabled": true
          },
          "suit": {
            "kinematics": {
              "position": {"x": 0, "y": 0, "z": 0},
              "orientation": {"x": 0, "y": 0, "z": 0, "w": 1},
              "linearVelocity": {"x": 0, "y": 0, "z": 0},
              "angularVelocity": {"x": 0, "y": 0, "z": 0},
              "dampenersEnabled": true
            },
            "activeHotbarSlot": 1
          },
          "suitInventory": [],
          "stationBlocks": [
            {"cell": {"x": 0, "y": 0, "z": 0}, "blockType": "Core"},
            {"cell": {"x": 5, "y": 5, "z": 5}, "blockType": "ArmorBlock"}
          ]
        }
        """;
        var disconnectedResult = WorldSaveSerializer.Deserialize(disconnectedJson);
        Assert.False(disconnectedResult.IsSuccess);
    }

    [Fact]
    public void RoundTrip_PreservesMidCycleRefineryAndContinuesRefiningWithoutLoss()
    {
        var refinery = new RefineryState();
        refinery.InputInventory.Add(ItemId.IronOre, 10);
        refinery.OutputInventory.Add(ItemId.SteelBlock, 2);
        refinery.CycleProgress = 0.6f;
        refinery.Status = RefineryStatus.Refining;

        var dummyShip = new KinematicsSaveData(Vector3.Zero, Quaternion.Identity, Vector3.Zero, Vector3.Zero, true);
        var dummySuit = new SuitSaveData(new KinematicsSaveData(Vector3.Zero, Quaternion.Identity, Vector3.Zero, Vector3.Zero, true), 1);

        var snapshot = WorldStateMapper.CreateSnapshot(
            PilotMode.SeatedInCockpit,
            dummyShip,
            dummySuit.Kinematics,
            1,
            new SuitInventory(50),
            refinery: refinery);

        string json = WorldSaveSerializer.Serialize(snapshot);
        var loadResult = WorldSaveSerializer.Deserialize(json);
        Assert.True(loadResult.IsSuccess, loadResult.ErrorMessage);
        Assert.NotNull(loadResult.Data?.Refinery);

        var restoredRefinery = new RefineryState();
        WorldStateMapper.ApplyRefinery(loadResult.Data!, restoredRefinery);

        Assert.Equal(10, restoredRefinery.InputInventory.GetCount(ItemId.IronOre));
        Assert.Equal(2, restoredRefinery.OutputInventory.GetCount(ItemId.SteelBlock));
        Assert.Equal(0.6f, restoredRefinery.CycleProgress, precision: 3);
        Assert.Equal(RefineryStatus.Refining, restoredRefinery.Status);

        // Step remaining 0.4 cycle time to complete batch
        var config = Keel.Sim.Config.TuningConfig.Default with { RefineryCycleSeconds = 2.0f, RefineryOrePerBlock = 5.0f };
        Keel.Sim.Refinery.RefinerySim.Step(restoredRefinery, config, deltaSeconds: 0.8f); // 0.8 / 2.0 = 0.4 progress

        // 1 batch should have completed: 10 - 5 = 5 ore remaining, 2 + 1 = 3 steel blocks
        Assert.Equal(5, restoredRefinery.InputInventory.GetCount(ItemId.IronOre));
        Assert.Equal(3, restoredRefinery.OutputInventory.GetCount(ItemId.SteelBlock));
    }

    [Fact]
    public void RoundTrip_RestoresOreDepositReservesByIdAndClampsBounds()
    {
        var deposits = new List<Keel.Sim.Mining.OreDepositState>
        {
            new Keel.Sim.Mining.OreDepositState(30, ItemId.IronOre) { DepositId = "asteroid-01-seam-0" },
            new Keel.Sim.Mining.OreDepositState(25, ItemId.IronOre) { DepositId = "asteroid-01-seam-1" },
            new Keel.Sim.Mining.OreDepositState(35, ItemId.IronOre) { DepositId = "asteroid-01-seam-2" },
        };

        // Mine seam 0 partially and seam 1 fully
        deposits[0].Extract(10); // 20 left
        deposits[1].Extract(25); // 0 left (depleted)

        var dummyShip = new KinematicsSaveData(Vector3.Zero, Quaternion.Identity, Vector3.Zero, Vector3.Zero, true);
        var dummySuit = new SuitSaveData(new KinematicsSaveData(Vector3.Zero, Quaternion.Identity, Vector3.Zero, Vector3.Zero, true), 1);

        var snapshot = WorldStateMapper.CreateSnapshot(
            PilotMode.SeatedInCockpit,
            dummyShip,
            dummySuit.Kinematics,
            1,
            new SuitInventory(50),
            oreDeposits: deposits);

        string json = WorldSaveSerializer.Serialize(snapshot);
        var loadResult = WorldSaveSerializer.Deserialize(json);
        Assert.True(loadResult.IsSuccess, loadResult.ErrorMessage);
        Assert.NotNull(loadResult.Data?.OreDeposits);

        var worldDeposits = new List<Keel.Sim.Mining.OreDepositState>
        {
            new Keel.Sim.Mining.OreDepositState(30, ItemId.IronOre) { DepositId = "asteroid-01-seam-0" },
            new Keel.Sim.Mining.OreDepositState(25, ItemId.IronOre) { DepositId = "asteroid-01-seam-1" },
            new Keel.Sim.Mining.OreDepositState(35, ItemId.IronOre) { DepositId = "asteroid-01-seam-2" },
            new Keel.Sim.Mining.OreDepositState(30, ItemId.IronOre) { DepositId = "asteroid-01-seam-3" }, // unmatched seam
        };

        WorldStateMapper.ApplyOreDeposits(loadResult.Data!, worldDeposits);

        Assert.Equal(20, worldDeposits[0].RemainingUnits);
        Assert.False(worldDeposits[0].IsDepleted);

        Assert.Equal(0, worldDeposits[1].RemainingUnits);
        Assert.True(worldDeposits[1].IsDepleted);

        Assert.Equal(35, worldDeposits[2].RemainingUnits);
        Assert.False(worldDeposits[2].IsDepleted);

        Assert.Equal(30, worldDeposits[3].RemainingUnits); // Unmatched retained initial
    }

    [Fact]
    public void FullLoop_QuickloadRollsBackMutatedWorldStateAndConservesItems()
    {
        var pilotMachine = new PilotModeStateMachine();
        pilotMachine.Reset(PilotMode.FloatingInSuit);

        var suit = new SuitInventory(50);
        suit.Add(ItemId.IronOre, 5);
        suit.Add(ItemId.SteelBlock, 1);

        var grid = StationBlueprint.CreateDefault();
        grid.TryPlaceBlock(new Int3(1, 1, 2), BlockType.ArmorBlock, out _);
        int savedGridCount = grid.Count;

        var refinery = new RefineryState();
        refinery.InputInventory.Add(ItemId.IronOre, 10);
        refinery.OutputInventory.Add(ItemId.SteelBlock, 2);
        refinery.CycleProgress = 0.5f;
        refinery.Status = RefineryStatus.Refining;

        var deposits = new List<Keel.Sim.Mining.OreDepositState>
        {
            new Keel.Sim.Mining.OreDepositState(30, ItemId.IronOre, "asteroid-01-seam-0") { RemainingUnits = 18 },
            new Keel.Sim.Mining.OreDepositState(25, ItemId.IronOre, "asteroid-01-seam-1") { RemainingUnits = 0 },
        };

        var shipKin = new KinematicsSaveData(
            new Vector3(100f, 50f, -200f),
            Quaternion.Identity,
            new Vector3(10f, 0f, 0f),
            Vector3.Zero,
            true);

        var suitKin = new KinematicsSaveData(
            new Vector3(10f, 2f, -500f),
            Quaternion.Identity,
            new Vector3(1f, 1f, 1f),
            Vector3.Zero,
            false);

        // Snapshot checkpoint
        var checkpoint = WorldStateMapper.CreateSnapshot(
            pilotMachine.Mode,
            shipKin,
            suitKin,
            activeSuitSlot: 2,
            suit,
            stationGrid: grid,
            refinery: refinery,
            oreDeposits: deposits);

        string checkpointJson = WorldSaveSerializer.Serialize(checkpoint);

        // Mutate everything
        pilotMachine.Reset(PilotMode.SeatedInCockpit);
        suit.Clear();
        suit.Add(ItemId.IronOre, 40);
        grid.RemoveBlock(new Int3(1, 1, 2), out _);
        refinery.InputInventory.Clear();
        refinery.OutputInventory.Clear();
        deposits[0].Extract(18); // now depleted

        Assert.Equal(savedGridCount - 1, grid.Count);
        Assert.Equal(0, deposits[0].RemainingUnits);
        Assert.Equal(PilotMode.SeatedInCockpit, pilotMachine.Mode);

        // Reload from checkpoint
        var loadResult = WorldSaveSerializer.Deserialize(checkpointJson);
        Assert.True(loadResult.IsSuccess, loadResult.ErrorMessage);
        var loaded = loadResult.Data!;

        WorldStateMapper.Apply(
            loaded,
            pilotMachine,
            suit,
            stationGrid: grid,
            refinery: refinery,
            oreDeposits: deposits);

        // Verify full rollback
        Assert.Equal(PilotMode.FloatingInSuit, pilotMachine.Mode);
        Assert.Equal(5, suit.GetCount(ItemId.IronOre));
        Assert.Equal(1, suit.GetCount(ItemId.SteelBlock));
        Assert.Equal(savedGridCount, grid.Count);
        Assert.True(grid.HasBlock(new Int3(1, 1, 2)));
        Assert.Equal(10, refinery.InputInventory.GetCount(ItemId.IronOre));
        Assert.Equal(2, refinery.OutputInventory.GetCount(ItemId.SteelBlock));
        Assert.Equal(0.5f, refinery.CycleProgress, precision: 3);
        Assert.Equal(18, deposits[0].RemainingUnits);
        Assert.False(deposits[0].IsDepleted);
        Assert.Equal(0, deposits[1].RemainingUnits);
        Assert.True(deposits[1].IsDepleted);
    }
}
