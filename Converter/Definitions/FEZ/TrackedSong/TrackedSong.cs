﻿namespace FEZRepacker.Converter.Definitions.FezEngine.Structure
{
    [XnbType("FezEngine.Structure.TrackedSong")]
    [XnbReaderType("FezEngine.Readers.TrackedSongReader")]
    internal class TrackedSong
    {
        [XnbProperty(UseConverter = true)]
        public List<Loop> Loops { get; set; }

        [XnbProperty]
        public string Name { get; set; }

        [XnbProperty]
        public int Tempo { get; set; }

        [XnbProperty]
        public int TimeSignature { get; set; }

        [XnbProperty(UseConverter = true)]
        public ShardNotes[] Notes { get; set; }

        [XnbProperty(UseConverter = true)]
        public AssembleChords AssembleChord { get; set; }

        [XnbProperty]
        public bool RandomOrdering { get; set; }

        [XnbProperty(UseConverter = true)]
        public int[] CustomOrdering { get; set; }
    }
}
