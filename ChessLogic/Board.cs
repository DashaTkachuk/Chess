using System;
using System.Collections.Generic;
using System.Linq;

namespace ChessLogic
{
    public class Board
    {
        private readonly Piece[,] pieces = new Piece[8, 8];

        public Piece this[int row, int col]
        {
            get { return pieces[row, col]; }
            set { pieces[row, col] = value; }
        }

        public Piece this[Position pos]
        {
            get { return this[pos.Row, pos.Column]; }
            set { this[pos.Row, pos.Column] = value; }
        }

        public static Board Initial()
        {
            Board board = new Board();
            board.AddStartPieces();
            return board;
        }

        private void AddStartPieces()
        {
            PlacePiece(new Position(0, 0), new Rook(Player.Black));
            PlacePiece(new Position(0, 1), new Knight(Player.Black));
            PlacePiece(new Position(0, 2), new Bishop(Player.Black));
            PlacePiece(new Position(0, 3), new Queen(Player.Black));
            PlacePiece(new Position(0, 4), new King(Player.Black));
            PlacePiece(new Position(0, 5), new Bishop(Player.Black));
            PlacePiece(new Position(0, 6), new Knight(Player.Black));
            PlacePiece(new Position(0, 7), new Rook(Player.Black));

            PlacePiece(new Position(7, 0), new Rook(Player.White));
            PlacePiece(new Position(7, 1), new Knight(Player.White));
            PlacePiece(new Position(7, 2), new Bishop(Player.White));
            PlacePiece(new Position(7, 3), new Queen(Player.White));
            PlacePiece(new Position(7, 4), new King(Player.White));
            PlacePiece(new Position(7, 5), new Bishop(Player.White));
            PlacePiece(new Position(7, 6), new Knight(Player.White));
            PlacePiece(new Position(7, 7), new Rook(Player.White));

            for (int c = 0; c < 8; c++)
            {
                PlacePiece(new Position(1, c), new Pawn(Player.Black));
                PlacePiece(new Position(6, c), new Pawn(Player.White));
            }
        }

        public void PlacePiece(Position position, Piece piece)
        {
            pieces[position.Row, position.Column] = piece;
        }

        public static bool IsInside(Position pos)
        {
            return pos.Row >= 0 && pos.Row < 8 && pos.Column >= 0 && pos.Column < 8;
        }

        public bool IsEmpty(Position pos)
        {
            return this[pos] == null;
        }

        public IEnumerable<Position> PiecePositions()
        {
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    Position pos = new Position(r, c);

                    if (!IsEmpty(pos))
                    {
                        yield return pos;
                    }
                }
            }
        }

        public IEnumerable<Position> PiecePositionsFor(Player player)
        {
            return PiecePositions().Where(pos => this[pos].Color == player);
        }

        public bool IsInCheck(Player player)
        {
            return PiecePositionsFor(player.Opponent()).Any(pos =>
            {
                Piece piece = this[pos];
                return piece.CanCaptureOpponentKing(pos, this);
            });
        }

        public Board Copy()
        {
            Board copy = new Board();

            foreach (Position pos in PiecePositions())
            {
                copy.PlacePiece(pos, this[pos].Copy());
            }

            return copy;
        }

        public Counting CountPieces()
        {
            Counting counting = new Counting();

            foreach (Position pos in PiecePositions())
            {
                Piece piece = this[pos];
                counting.Increment(piece.Color, piece.Type);
            }

            return counting;
        }

        public bool InsufficientMaterial()
        {
            Counting counting = CountPieces();

            return IsKingVKing(counting);
        }

        private static bool IsKingVKing(Counting counting)
        {
            return counting.TotalCount == 2;
        }
    }
}
