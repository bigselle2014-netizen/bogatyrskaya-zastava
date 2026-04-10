namespace BogatyrskayaZastava.Core
{
    public enum RewardedContext
    {
        AfterDefeat,
        LowGateHP,
        IdleCapHalf
    }

    public struct RunCompleteData
    {
        public int wavesCleared;
        public int runesEarned;
        public int coinsEarned;
        public int synergiesActivated;
        public bool isVictory;
        public float totalTime;
    }
}
