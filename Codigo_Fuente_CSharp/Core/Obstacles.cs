using System;
using SubwaySurfersAudioGame.Audio;

namespace SubwaySurfersAudioGame.Core
{
    public enum PowerUpType
    {
        Magnet,
        Jetpack,
        SuperSneakers,
        Hoverboard,
        Multiplier2x
    }

    public abstract class TrackEntity
    {
        public Lane Lane { get; set; }
        public float X => (float)Lane * Player.LaneDistance;
        public float Y { get; set; } = 0.0f;
        public float Z { get; set; } = 0.0f;
        public bool IsCollectedOrPassed { get; set; } = false;
    }

    public class LowBarrier : TrackEntity
    {
        public float Height { get; set; } = 1.0f;
        public float Depth { get; set; } = 0.5f;
    }

    public class HighBarrier : TrackEntity
    {
        public float BottomClearance { get; set; } = 1.2f;
        public float TopHeight { get; set; } = 2.8f;
        public float Depth { get; set; } = 0.5f;
    }

    public class StaticTrain : TrackEntity
    {
        public float Length { get; set; } = 18.0f;
        public float Width { get; set; } = 2.6f;
        public float Height { get; set; } = 3.8f;
        public bool HasRamp { get; set; } = false;
        public float RampLength { get; set; } = 5.0f;
    }

    public class DynamicTrain : TrackEntity
    {
        public float Length { get; set; } = 18.0f;
        public float Width { get; set; } = 2.6f;
        public float Height { get; set; } = 3.8f;
        public float Speed { get; set; } = 12.0f; // 12 m/s opposite direction
        public bool HornPlayed { get; set; } = false;
        public BassSoundChannel? EngineLoopInstance { get; set; }
    }

    public class CoinItem : TrackEntity
    {
        public bool IsMagnetized { get; set; } = false;
    }

    public class PowerUpItem : TrackEntity
    {
        public PowerUpType Type { get; set; }
    }

    public class LetterItem : TrackEntity
    {
        public char Letter { get; set; } = 'S';
        public int LetterIndex { get; set; } = 0;
    }

    public class TunnelZone
    {
        public float StartZ { get; set; }
        public float EndZ { get; set; }
        public float Length => EndZ - StartZ;

        public bool Contains(float playerZ) => playerZ >= StartZ && playerZ <= EndZ;
    }
}
