namespace ChessLogic
{
    public class PawnPromotion : Move
    {
        public override MoveType Type => MoveType.PawnPromotion;
        public override Position FromPos { get; }
        public override Position ToPos { get; }

        public PawnPromotion(Position from, Position to)
        {
            FromPos = from;
            ToPos = to;
        }

        public override void Execute(Board board)
        {
            Piece pawn = board[FromPos];
            board[FromPos] = null;

            Piece promotionPiece = new Queen(pawn.Color);
            promotionPiece.HasMoved = true;
            board[ToPos] = promotionPiece;
        }
    }
}
