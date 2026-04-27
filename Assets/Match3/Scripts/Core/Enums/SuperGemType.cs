#nullable enable

namespace Match3.Core.Enums
{
    public enum SuperGemType
    {
        None,
        HorizontalArrow, // 4 в ряд горизонталь  → сносит всю строку
        VerticalArrow,   // 4 в ряд вертикаль    → сносит весь столбец
        ColorBomb,       // 5 в ряд прямая        → сносит все фишки цвета
        Bomb,            // T или L форма (5 кл.) → взрыв 3×3
        MegaBomb,        // 6+ или крест          → взрыв 5×5
    }
}
