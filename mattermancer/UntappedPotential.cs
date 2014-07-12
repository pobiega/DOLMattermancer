using DOL.GS;

namespace GameServerScripts.mattermancer
{
    /// <summary>
    /// Serves as a manager for all untapped potential.
    /// </summary>
    public static class UntappedPotential
    {
        public const string UNTAPPED_POTENTIAL = "UNTAPPED_POTENTIAL";

        public static int AddUntappedPotential(GameLiving living, int amountToAdd)
        {
            int up = living.TempProperties.getProperty<int>(UNTAPPED_POTENTIAL, 0);
            living.TempProperties.setProperty(UNTAPPED_POTENTIAL, up + 1);
            return up;
        }

        public static int GetUntappedPotential(GameLiving living, int amountToAdd)
        {
            int up = living.TempProperties.getProperty<int>(UNTAPPED_POTENTIAL, 0);
            living.TempProperties.setProperty(UNTAPPED_POTENTIAL, up + 1);
            return up;
        }
    }
}
