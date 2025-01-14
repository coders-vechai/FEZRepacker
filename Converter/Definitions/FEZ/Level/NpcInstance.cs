﻿using System.Numerics;

namespace FEZRepacker.Converter.Definitions.FezEngine.Structure
{
    [XnbType("FezEngine.Structure.NpcInstance")]
    [XnbReaderType("FezEngine.Readers.NpcInstanceReader")]
    internal class NpcInstance
    {
        [XnbProperty]
        public string Name { get; set; }

        [XnbProperty]
        public Vector3 Position { get; set; }

        [XnbProperty]
        public Vector3 DestinationOffset { get; set; }

        [XnbProperty]
        public float WalkSpeed { get; set; }

        [XnbProperty]
        public bool RandomizeSpeech { get; set; }

        [XnbProperty]
        public bool SayFirstSpeechLineOnce { get; set; }

        [XnbProperty]
        public bool AvoidsGomez { get; set; }

        [XnbProperty(UseConverter = true)]
        public ActorType ActorType { get; set; }

        [XnbProperty(UseConverter = true)]
        public List<SpeechLine> Speech { get; set; }

        [XnbProperty(UseConverter = true)]
        public Dictionary<NpcAction, NpcActionContent> Actions { get; set; }


        public NpcInstance()
        {
            Speech = new List<SpeechLine>();
            Actions = new Dictionary<NpcAction, NpcActionContent>();

            Name = "";
        }
    }
}