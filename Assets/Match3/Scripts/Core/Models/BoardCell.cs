#nullable enable

using Match3.Core.Enums;

namespace Match3.Core.Models
{
    public sealed class BoardCell
    {
        public IGemView? ContainingGem;
        public IGemView? IncomingGem;
        public CellType  CellType;
        public bool      Locked;

        // ── Препятствие ───────────────────────────────────────────────────────

        public ObstacleType ObstacleType { get; set; } = ObstacleType.None;
        public int          ObstacleHp   { get; set; }
        public int          MaxObstacleHp { get; set; }

        /// <summary>Есть ли на ячейке активное препятствие.</summary>
        public bool HasObstacle => ObstacleType != ObstacleType.None;

        // ── Поведение ячейки ─────────────────────────────────────────────────

        /// <summary>
        /// Гем на ячейке может быть источником падения.
        /// Препятствия (Ice, Chain, Box) блокируют падение наружу.
        /// </summary>
        public bool CanFall =>
            !Locked &&
            !HasObstacle &&
            ContainingGem != null &&
            ContainingGem.CanMove &&
            ContainingGem.CurrentMatch == null;

        /// <summary>
        /// Ячейка блокирует падение других гемов через себя.
        /// Box, Ice, Chain — всё блокирует.
        /// </summary>
        public bool BlockFall =>
            Locked ||
            HasObstacle ||
            (ContainingGem != null && !ContainingGem.CanMove);

        /// <summary>
        /// Гем на ячейке может быть выбран для свопа.
        /// Любое препятствие запрещает движение.
        /// </summary>
        public bool CanBeMoved =>
            !Locked && !HasObstacle && ContainingGem != null && ContainingGem.CanMove;

        /// <summary>
        /// Гем на ячейке может участвовать в матче.
        /// Ice всегда запрещает матч.
        /// Chain запрещает матч пока HP > 1 (при HP=1 матч разрывает цепь).
        /// Box — нет гема, поэтому false автоматически.
        /// </summary>
        public bool CanMatch() =>
            ContainingGem != null &&
            !Locked &&
            ObstacleType != ObstacleType.Ice &&
            !(ObstacleType == ObstacleType.Chain && ObstacleHp > 1);

        /// <summary>Гем можно удалить (матч, взрыв).</summary>
        public bool CanDelete() => !Locked && ObstacleType != ObstacleType.Box;

        /// <summary>Ячейка свободна для приёма нового гема.</summary>
        public bool IsEmpty() =>
            !HasObstacle && ContainingGem == null && IncomingGem == null;

        /// <summary>Ячейка может принять падающий гем.</summary>
        public bool CanReceiveFall =>
            !Locked && !HasObstacle && ContainingGem == null && IncomingGem == null;
    }
}
