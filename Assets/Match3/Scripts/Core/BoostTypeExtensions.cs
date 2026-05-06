#nullable enable

using Match3.Core.Enums;

namespace Match3.Core
{
    public static class BoostTypeExtensions
    {
        public static SuperGemType ToSuperGemType(this BoostType boost) => boost switch
        {
            BoostType.HorizontalArrow => SuperGemType.HorizontalArrow,
            BoostType.VerticalArrow   => SuperGemType.VerticalArrow,
            BoostType.ColorBomb       => SuperGemType.ColorBomb,
            BoostType.Bomb            => SuperGemType.Bomb,
            BoostType.MegaBomb        => SuperGemType.MegaBomb,
            _                         => SuperGemType.None,
        };
    }
}
