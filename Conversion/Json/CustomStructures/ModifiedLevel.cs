﻿using FEZEngine;
using FEZEngine.Structure;
using FEZEngine.Structure.Scripting;
using System.Numerics;

namespace FEZRepacker.Conversion.Json.CustomStructures
{
    // basically the original Level class, but more arranged for JSON exporting
    // and slightly changed to get rid of redundancy
    class ModifiedLevel
    {
        // main parameters
        public string Name { get; set; }
        public LevelNodeType NodeType { get; set; }
        public Vector3 Size { get; set; }
        public TrileFace StartingFace { get; set; }

        public bool Flat { get; set; }
        public bool Quantum { get; set; }
        public bool Descending { get; set; }
        public bool Loops { get; set; }
        public bool Rainy { get; set; }

        // visual aspects
        public float BaseDiffuse { get; set; }
        public float BaseAmbient { get; set; }
        public string SkyName { get; set; }
        public bool SkipPostProcess { get; set; }
        public string GomezHaloName { get; set; }
        public bool HaloFiltering { get; set; }
        public bool BlinkingAlpha { get; set; }
        public float WaterHeight { get; set; }
        public LiquidType WaterType { get; set; }

        // music and sound related
        public string SongName { get; set; }
        public int FarAwayPlaceFadeOutStart { get; set; }
        public int FarAwayPlaceFadeOutLength { get; set; }
        public List<string> MutedLoops { get; set; }
        public List<AmbienceTrack> AmbienceTracks { get; set; }
        public string SequenceSamplesPath { get; set; }
        public bool LowPass { get; set; }

        public string TrileSetName { get; set; }
        public List<ModifiedTrile> Triles { get; set; }
        public List<ModifiedTrileInstanceSettings> TrileSettings { get; set; }
        public Dictionary<int, Volume> Volumes { get; set; }
        public Dictionary<int, Script> Scripts { get; set; }
        public Dictionary<int, ArtObjectInstance> ArtObjects { get; set; }
        public Dictionary<int, BackgroundPlane> BackgroundPlanes { get; set; }
        public Dictionary<int, ModifiedTrileGroup> Groups { get; set; }
        public Dictionary<int, MovementPath> Paths { get; set; }
        public Dictionary<int, NpcInstance> NonPlayerCharacters { get; set; }

        public ModifiedLevel(Level level)
        {
            // copy over unchanged parameters
            Name = level.Name;
            NodeType = level.NodeType;
            Size = level.Size;
            StartingFace = level.StartingFace;
            Flat = level.Flat;
            Quantum = level.Quantum;
            Descending = level.Descending;
            Loops = level.Loops;
            Rainy = level.Rainy;
            BaseDiffuse = level.BaseDiffuse;
            BaseAmbient = level.BaseAmbient;
            SkyName = level.SkyName;
            SkipPostProcess = level.SkipPostProcess;
            GomezHaloName = level.GomezHaloName;
            HaloFiltering = level.HaloFiltering;
            BlinkingAlpha = level.BlinkingAlpha;
            WaterHeight = level.WaterHeight;
            WaterType = level.WaterType;
            SongName = level.SongName;
            FarAwayPlaceFadeOutStart = level.FarAwayPlaceFadeOutStart;
            FarAwayPlaceFadeOutLength = level.FarAwayPlaceFadeOutLength;
            MutedLoops = level.MutedLoops;
            AmbienceTracks = level.AmbienceTracks;
            SequenceSamplesPath = level.SequenceSamplesPath;
            LowPass = level.LowPass;
            TrileSetName = level.TrileSetName;
            Volumes = level.Volumes;
            Scripts = level.Scripts;
            ArtObjects = level.ArtObjects;
            BackgroundPlanes = level.BackgroundPlanes;
            Paths = level.Paths;
            NonPlayerCharacters = level.NonPlayerCharacters;

            // sort tiles into modified structures
            Triles = new();
            TrileSettings = new();
            foreach((var pos, var instance) in level.Triles)
            {
                Triles.Add(new ModifiedTrile(pos, instance));
                foreach(var overlapping in instance.OverlappedTriples)
                {
                    Triles.Add(new ModifiedTrile(pos, overlapping));
                }

                var settings = new ModifiedTrileInstanceSettings(pos, instance);
                if (!settings.IsUnnecessary(true))
                {
                    TrileSettings.Add(settings);
                }
            }

            // create groups of modified paths
            Groups = level.Groups.ToDictionary(pair=>pair.Key, pair=>new ModifiedTrileGroup(pair.Value));
        }

        public Level ToOriginal()
        {
            Level level = new Level();

            // copy over unchanged parameters
            level.Name = Name;
            level.NodeType = NodeType;
            level.Size = Size;
            level.StartingFace = StartingFace;
            level.Flat = Flat;
            level.Quantum = Quantum;
            level.Descending = Descending;
            level.Loops = Loops;
            level.Rainy = Rainy;
            level.BaseDiffuse = BaseDiffuse;
            level.BaseAmbient = BaseAmbient;
            level.SkyName = SkyName;
            level.SkipPostProcess = SkipPostProcess;
            level.GomezHaloName = GomezHaloName;
            level.HaloFiltering = HaloFiltering;
            level.BlinkingAlpha = BlinkingAlpha;
            level.WaterHeight = WaterHeight;
            level.WaterType = WaterType;
            level.SongName = SongName;
            level.FarAwayPlaceFadeOutStart = FarAwayPlaceFadeOutStart;
            level.FarAwayPlaceFadeOutLength = FarAwayPlaceFadeOutLength;
            level.MutedLoops = MutedLoops;
            level.AmbienceTracks = AmbienceTracks;
            level.SequenceSamplesPath = SequenceSamplesPath;
            level.LowPass = LowPass;
            level.TrileSetName = TrileSetName;
            level.Volumes = Volumes;
            level.Scripts = Scripts;
            level.ArtObjects = ArtObjects;
            level.BackgroundPlanes = BackgroundPlanes;
            level.Paths = Paths;
            level.NonPlayerCharacters = NonPlayerCharacters;

            // parse triles back into their original structure
            foreach(var modTrile in Triles)
            {
                if (!level.Triles.ContainsKey(modTrile.Position))
                {
                    var settings = TrileSettings.Find(setting => setting.Position == modTrile.Position);
                    var trile = modTrile.ToOriginal(settings);
                    level.Triles[modTrile.Position] = trile;
                }
                else
                {
                    var overlapped = modTrile.ToOriginal(null);
                    level.Triles[modTrile.Position].OverlappedTriples.Add(overlapped);
                    continue;
                }
            }

            // put trile instances back into groups
            level.Groups = Groups.ToDictionary(pair => pair.Key, pair => pair.Value.ToOriginal(level));

            return level;
        }
    }
}