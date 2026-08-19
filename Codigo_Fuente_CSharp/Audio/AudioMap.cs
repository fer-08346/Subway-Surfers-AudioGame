namespace SubwaySurfersAudioGame.Audio
{
    /// <summary>
    /// Mapa y Catálogo Completo de Eventos Acústicos basado en el análisis de 168 clips WAV de /sfx
    /// </summary>
    public static class AudioMap
    {
        // ==================== 1. LOCOMOCIÓN Y PASOS ====================
        public static class Locomotion
        {
            public const string FootstepTrackLeft = "Hr_run_leftFoot";
            public const string FootstepTrackRight = "Hr_run_rightFoot";

            public const string FootstepRoofLeft = "Sub01_SFX_FootstepsTrainTop_LeftFoot";
            public const string FootstepRoofRight = "Sub01_SFX_FootstepsTrainTop_RightFoot";

            public const string FootstepSneakersLeft = "Hr_superSneakers_leftFoot";
            public const string FootstepSneakersRight = "Hr_superSneakers_rightFoot";

            public const string FootstepRoofSneakersLeft = "Sub01_SFX_FootstepsTrainTop_SuperSneakers_LeftFoot";
            public const string FootstepRoofSneakersRight = "Sub01_SFX_FootstepsTrainTop_SuperSneakers_RightFoot";

            public const string JumpStandard = "Hr_run_jump";
            public const string JumpSneakers = "Hr_superSneakers_jump";

            public const string LandingGround = "Hr_landing";
            public const string LandingRoof = "Sub01_SFX_FootstepsTrainTop_Landing";

            public const string RollSlide = "Hr_run_roll";

            public const string LaneDodge = "Hr_run_dodge";
            public const string SwishShort = "Hr_swishCShort";
            public const string SwishMid = "Hr_swishFMid";
            public const string SwishLong = "Hr_swishDLong";
        }

        // ==================== 2. COLECCIONABLES Y MONEDAS ====================
        public static class Collectibles
        {
            public const string CoinPickup = "Hr_coin";
            public const string CoinGui = "Hr_gui_coin";
            public const string MysteryBoxOpen = "Hr_mysteryBoxOpen";
            public const string BlingBoxOpen = "blingbox_3_open_01";
            public const string MissionReward = "Hr_missionReward";
            public const string LetterSlideIn = "Hr_slideInLetters";
        }

        // ==================== 3. POTENCIADORES (POWER-UPS) ====================
        public static class PowerUps
        {
            public const string PowerUpCollect = "Hr_powerUp";
            public const string PowerDown = "Hr_powerDown";

            public const string MagnetLoop = "Hr_magnet_mainLOOP";

            public const string JetpackIgnite = "Subway_TLR_JetpackPause";
            public const string JetpackLoop = "Hr_jetPack_mainLOOP";

            public const string HoverboardActivate = "Sub01_SFX_BubbleShield_Activation";
            public const string HoverboardOngoingLoop = "Sub01_SFX_BubbleShield_Ongoing";
            public const string HoverboardCrash = "Sub01_SFX_BubbleShield_Crashed";

            public const string Multiplier2xFly = "Subway_Metronome_MultiplierFly_Audio";
            public const string HeadstartTurbo = "Hr_turboheadstart";
        }

        // ==================== 4. PERSECUCIÓN: INSPECTOR Y PERRO ====================
        public static class Pursuit
        {
            public const string GuardStartWhistle = "audioClip_GuardGameStart";
            public const string GuardProximityLoop = "audioClip_GuardProximity";
        }

        // ==================== 5. TRENES Y OBSTÁCULOS ====================
        public static class Obstacles
        {
            public const string TrainApproaching = "Hr_trainPass";

            public const string StumbleLight = "Hr_stumble";
            public const string StumbleSide = "Hr_stumble_side";
            public const string StumbleBush = "Hr_stumble_bush";

            public const string FatalDeath = "Hr_death";
            public const string BodyFall = "Hr_death_bodyfall";
            public const string HitCamera = "Hr_death_hitCam";
            public const string StingerLose = "Subway_BS_Stingers_Lose";
        }

        // ==================== 6. INTERFAZ DE USUARIO (UI) ====================
        public static class UI
        {
            public const string MenuBrowseTap = "Hr_gui_tap";
            public const string SettingsOpen = "ui_audio_settings_open_audio";
            public const string StoreOpen = "ui_audio_store_open_audio";
            public const string MissionOpen = "ui_audio_missions_open_audio";
            public const string TopRunCelebration = "audioClipInfo_TournamentTopRun_LadderCelebration";
        }
    }
}
