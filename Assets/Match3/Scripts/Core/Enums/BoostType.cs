#nullable enable

namespace Match3.Core.Enums
{
    public enum BoostType
    {
        None,

        // Супер-фишки (применяются на ячейку)
        HorizontalArrow,
        VerticalArrow,
        ColorBomb,
        Bomb,
        MegaBomb,

        // Мгновенные бусты
        Hint,
        Shuffle
    }
}
