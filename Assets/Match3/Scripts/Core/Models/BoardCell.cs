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

        /// <summary>
        /// Ячейка может быть ИСТОЧНИКОМ падения:
        /// содержит гем, который готов двигаться и не в матче.
        /// </summary>
        public bool CanFall =>
            !Locked &&
            ContainingGem != null &&
            ContainingGem.CanMove &&
            ContainingGem.CurrentMatch == null;

        /// <summary>
        /// Ячейка БЛОКИРУЕТ падение через себя:
        /// заблокирована или содержит неподвижный гем.
        /// </summary>
        public bool BlockFall =>
            Locked || (ContainingGem != null && !ContainingGem.CanMove);

        /// <summary>
        /// Ячейка может принять падающий гем — она пуста.
        /// </summary>
        public bool CanReceiveFall =>
            !Locked && ContainingGem == null && IncomingGem == null;

        public bool CanBeMoved =>
            !Locked && ContainingGem != null && ContainingGem.CanMove;

        public bool CanMatch() =>
            ContainingGem != null && !Locked;

        public bool CanDelete() =>
            !Locked;

        public bool IsEmpty() =>
            ContainingGem == null && IncomingGem == null;
    }
}
