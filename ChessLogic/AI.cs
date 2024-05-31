using System;
using System.Collections.Generic;
using System.Linq;

namespace ChessLogic
{
    public class AI
    {
        private const int MaxDepth = 3;

        public Move GetBestMove(GameState gameState)
        {
            return GetBestMove(gameState, MaxDepth, int.MinValue, int.MaxValue, true).Move;
        }

        private (Move Move, int Score) GetBestMove(GameState gameState, int depth, int alpha, int beta, bool maximizingPlayer)
        {
            if (depth == 0 || gameState.IsGameOver())
            {
                return (null, EvaluateBoard(gameState, gameState.CurrentPlayer));
            }

            List<Move> legalMoves = gameState.AllLegalMovesFor(gameState.CurrentPlayer).ToList();

            if (maximizingPlayer)
            {
                int maxScore = int.MinValue;
                Move bestMove = null;

                foreach (Move move in legalMoves)
                {
                    GameState newGameState = gameState.Copy();
                    newGameState.MakeMove(move);

                    int score = GetBestMove(newGameState, depth - 1, alpha, beta, false).Score;
                    if (score > maxScore)
                    {
                        maxScore = score;
                        bestMove = move;
                    }
                    alpha = Math.Max(alpha, score);
                    if (beta <= alpha)
                    {
                        break;
                    }
                }

                return (bestMove, maxScore);
            }
            else
            {
                int minScore = int.MaxValue;
                Move bestMove = null;

                foreach (Move move in legalMoves)
                {
                    GameState newGameState = gameState.Copy();
                    newGameState.MakeMove(move);

                    int score = GetBestMove(newGameState, depth - 1, alpha, beta, true).Score;
                    if (score < minScore)
                    {
                        minScore = score;
                        bestMove = move;
                    }
                    beta = Math.Min(beta, score);
                    if (beta <= alpha)
                    {
                        break;
                    }
                }

                return (bestMove, minScore);
            }
        }

        private int EvaluateBoard(GameState gameState, Player player)
        {
            int score = 0;

            Counting counting = gameState.Board.CountPieces();
            score += MaterialAdvantage(counting.WhiteCount(PieceType.Pawn), counting.BlackCount(PieceType.Pawn));
            score += MaterialAdvantage(counting.WhiteCount(PieceType.Knight), counting.BlackCount(PieceType.Knight));
            score += MaterialAdvantage(counting.WhiteCount(PieceType.Bishop), counting.BlackCount(PieceType.Bishop));
            score += MaterialAdvantage(counting.WhiteCount(PieceType.Rook), counting.BlackCount(PieceType.Rook));
            score += MaterialAdvantage(counting.WhiteCount(PieceType.Queen), counting.BlackCount(PieceType.Queen));

            score += CenterControlScore(gameState, player);

            score += MobilityScore(gameState, player);

            score += DevelopmentScore(gameState, player);

            score += CheckAndMateScore(gameState, player);

            score += KingSafetyScore(gameState, player);

            return score;
        }

        private int MaterialAdvantage(int whiteCount, int blackCount)
        {
            return (whiteCount - blackCount) * 10; 
        }

        private int CenterControlScore(GameState gameState, Player player)
        {
            int score = 0;

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    Piece piece = gameState.Board[r, c];
                    if (piece != null)
                    {
                        int distanceToCenter = Math.Abs(r - 3) + Math.Abs(c - 3); 
                        if (piece.Color == player)
                        {
                            score += distanceToCenter;
                        }
                        else
                        {
                            score -= distanceToCenter;
                        }

                    }
                }
            }

            return score;
        }

        private int MobilityScore(GameState gameState, Player player)
        {
            int score = 0;

            foreach (Position pos in gameState.Board.PiecePositionsFor(player))
            {
                Piece piece = gameState.Board[pos];
                if (piece != null)
                {
                    score += piece.GetMoves(pos, gameState.Board).Count();
                }
            }

            return score;
        }

        private int DevelopmentScore(GameState gameState, Player player)
        {
            int score = 0;

            int row = player == Player.White ? 0 : 7;

            for (int c = 0; c < 8; c++)
            {
                Piece piece = gameState.Board[row, c];
                if (piece != null && piece.Color == player && piece.Type == PieceType.Pawn)
                {
                    score += 10; 
                }
            }

            return score;
        }

        private int CheckAndMateScore(GameState gameState, Player player)
        {
            if (gameState.IsGameOver())
            {
                Result result = gameState.Result;
                if (result.Reason == EndReason.Checkmate)
                {
                    return result.Winner == player ? int.MaxValue : int.MinValue; 
                }
                else if (result.Reason == EndReason.Stalemate)
                {
                    return 0; 
                }
                else if (result.Reason == EndReason.InsufficientMaterial)
                {
                    return 0; 
                }
            }
            else if (gameState.Board.IsInCheck(player))
            {
                return player == gameState.CurrentPlayer ? int.MaxValue / 2 : -int.MaxValue / 2; 
            }
            return 0;
        }

        private int KingSafetyScore(GameState gameState, Player player)
        {
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    Piece piece = gameState.Board[r, c];
                    if (piece != null && piece.Color == player.Opponent())
                    {
                        List<Position> threateningPositions = piece.GetMoves(new Position(r, c), gameState.Board).Select(move => move.ToPos).ToList();
                        foreach (var position in threateningPositions)
                        {
                            if (gameState.Board[position.Row, position.Column]?.Type == PieceType.King &&
                                gameState.Board[position.Row, position.Column]?.Color == player)
                            {
                                return -10; 
                            }
                        }
                    }
                }
            }
            return 0;
        }
    }
}
