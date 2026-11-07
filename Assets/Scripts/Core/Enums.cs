namespace Moba
{
    public enum TeamId { Blue = 0, Red = 1 }

    public enum UnitKind { Hero, Minion, Tower, Base }

    public static class TeamExtensions
    {
        public static TeamId Opposite(this TeamId team) => team == TeamId.Blue ? TeamId.Red : TeamId.Blue;
    }
}
