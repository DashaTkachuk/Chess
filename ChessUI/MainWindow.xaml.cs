using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using ChessLogic;

namespace ChessUI
{
    public partial class MainWindow : Window
    {
        private readonly Image[,] pieceImages = new Image[8, 8];
        private readonly Rectangle[,] highlights = new Rectangle[8, 8];
        private readonly Dictionary<Position, Move> moveCache = new Dictionary<Position, Move>();
        private readonly ModeMenu modeMenu = new ModeMenu();
        private readonly AI ai = new AI();
        private GameState gameState;
        private Position selectedPos = null;
        private bool isPlayerTurn = true;

        private readonly PiecePlacementTracker placementTracker = new PiecePlacementTracker();

        public MainWindow()
        {
            InitializeComponent();
            InitializeBoard();
            BoardGrid.AllowDrop = true;
            BoardGrid.DragEnter += BoardGrid_DragEnter;
            BoardGrid.DragOver += BoardGrid_DragOver;
            BoardGrid.Drop += BoardGrid_Drop;
        }

        private void StartGameOnePlayer(Mode mode)
        {
            InitializeNewGame();
            isPlayerTurn = true;
            MakeComputerMove();
        }

        private void StartGameTwoPlayers(Mode mode)
        {
            InitializeNewGame();
            isPlayerTurn = true;
        }

        private void InitializeNewGame()
        {
            MenuContainer.Content = null;
            gameState = new GameState(Player.White, Board.Initial());
            DrawBoard(gameState.Board);
        }

        private void InitializeBoard()
        {
            PieceGrid.Children.Clear();
            HighlightGrid.Children.Clear();
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    Image image = new Image();
                    pieceImages[r, c] = image;
                    PieceGrid.Children.Add(image);

                    Rectangle highlight = new Rectangle();
                    highlights[r, c] = highlight;
                    HighlightGrid.Children.Add(highlight);
                }
            }
        }

        private void DrawBoard(Board board)
        {
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    Piece piece = board[r, c];
                    pieceImages[r, c].Source = Images.GetImage(piece); // Используем метод GetImage из класса Images
                }
            }
        }

        private void BoardGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (IsMenuOnScreen() || !isPlayerTurn)
            {
                return;
            }

            Point point = e.GetPosition(BoardGrid);
            Position pos = ToSquarePosition(point);

            if (selectedPos == null)
            {
                OnFromPositionSelected(pos);
            }
            else
            {
                OnToPositionSelected(pos);
            }
        }

        private Position ToSquarePosition(Point point)
        {
            double squareSize = BoardGrid.ActualWidth / 8;
            int row = (int)(point.Y / squareSize);
            int col = (int)(point.X / squareSize);
            return new Position(row, col);
        }

        private void OnFromPositionSelected(Position pos)
        {
            IEnumerable<Move> moves = gameState.LegalMovesForPiece(pos);

            if (moves.Any())
            {
                selectedPos = pos;
                CacheMoves(moves);
                ShowHighlights();
            }
        }

        private void OnToPositionSelected(Position pos)
        {
            selectedPos = null;
            HideHighlights();

            if (moveCache.TryGetValue(pos, out Move move))
            {
                if (move.Type == MoveType.PawnPromotion)
                {
                    HandlePromotion(move);
                }
                else
                {
                    HandleMove(move);
                }
            }
        }

        private void HandlePromotion(Move move)
        {
            HandleMove(move);
        }

        private void HandleMove(Move move)
        {
            gameState.MakeMove(move);
            DrawBoard(gameState.Board);

            if (gameState.IsGameOver())
            {
                ShowGameOver();
            }
            else
            {
                if (modeMenu.CurrentMode == Mode.OnePlayer)
                {
                    isPlayerTurn = !isPlayerTurn;
                    if (!isPlayerTurn)
                    {
                        MakeComputerMove();
                    }
                }
            }
        }

        private void CacheMoves(IEnumerable<Move> moves)
        {
            moveCache.Clear();

            foreach (Move move in moves)
            {
                moveCache[move.ToPos] = move;
            }
        }

        private void ShowHighlights()
        {
            Color color = Color.FromArgb(150, 125, 255, 125);

            foreach (Position to in moveCache.Keys)
            {
                highlights[to.Row, to.Column].Fill = new SolidColorBrush(color);
            }
        }

        private void HideHighlights()
        {
            foreach (Position to in moveCache.Keys)
            {
                highlights[to.Row, to.Column].Fill = Brushes.Transparent;
            }
        }

        private bool IsMenuOnScreen()
        {
            return MenuContainer.Content != null;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (!IsMenuOnScreen())
            {
                if (e.Key == Key.Escape)
                {
                    ShowPauseMenu();
                }
                else if (e.Key == Key.Delete)
                {
                    DeleteSelectedPiece();
                }
            }
        }

        private void ShowPauseMenu()
        {
            PauseMenu pauseMenu = new PauseMenu();
            MenuContainer.Content = pauseMenu;

            pauseMenu.OptionSelected += option =>
            {
                MenuContainer.Content = null;

                if (option == Option.Restart)
                {
                    RestartGame();
                }
                else if (option == Option.Clear)
                {
                    ClearBoard();
                }
            };
        }

        private void ShowGameOver()
        {
            GameOverMenu gameOverMenu = new GameOverMenu(gameState);
            MenuContainer.Content = gameOverMenu;

            gameOverMenu.OptionSelected += option =>
            {
                if (option == Option.Restart)
                {
                    MenuContainer.Content = null;
                    RestartGame();
                }
                else
                {
                    Application.Current.Shutdown();
                }
            };
        }

        private void RestartGame()
        {
            selectedPos = null;
            HideHighlights();
            moveCache.Clear();
            gameState = null;
            MenuContainer.Content = modeMenu;

            modeMenu.ModeOnePlayerSelected -= StartGameOnePlayer;
            modeMenu.ModeTwoPlayersSelected -= StartGameTwoPlayers;
            modeMenu.ModeOnePlayerSelected += StartGameOnePlayer;
            modeMenu.ModeTwoPlayersSelected += StartGameTwoPlayers;
        }

        private void ClearBoard()
        {
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    pieceImages[r, c].Source = null;
                    highlights[r, c].Fill = Brushes.Transparent;
                    gameState.Board.PlacePiece(new Position(r, c), null); // Очищаем доску от фигур
                }
            }
        }

        private void DeleteSelectedPiece()
        {
            if (selectedPos != null)
            {
                gameState.Board.PlacePiece(selectedPos, null);
                pieceImages[selectedPos.Row, selectedPos.Column].Source = null;
                selectedPos = null;
                HideHighlights();
            }
        }

        private void MakeComputerMove()
        {
            if (!isPlayerTurn)
            {
                Move computerMove = ai.GetBestMove(gameState);
                gameState.MakeMove(computerMove);
                DrawBoard(gameState.Board);

                if (gameState.IsGameOver())
                {
                    ShowGameOver();
                }
                else
                {
                    isPlayerTurn = !isPlayerTurn;
                }
            }
        }

        private void BoardGrid_DragEnter(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.Bitmap))
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void BoardGrid_DragOver(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.Bitmap))
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void BoardGrid_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.Bitmap))
            {
                Point dropPosition = e.GetPosition(BoardGrid);
                Position pos = ToSquarePosition(dropPosition);

                ImageSource droppedImageSource = (ImageSource)e.Data.GetData(DataFormats.Bitmap);

                Piece droppedPiece = IdentifyPiece(droppedImageSource);

                if (droppedPiece != null)
                {
                    int maxPiecesAllowed = GetMaxPiecesAllowed(droppedPiece);
                    int currentPiecesOfType = CountPiecesOfType(droppedPiece);

                    if (currentPiecesOfType < maxPiecesAllowed)
                    {
                        pieceImages[pos.Row, pos.Column].Source = droppedImageSource;
                        placementTracker.TrackPiece(droppedPiece, pos);

                        // Обновляем состояние доски
                        gameState.Board.PlacePiece(pos, droppedPiece);
                    }
                    else
                    {
                        MessageBox.Show($"Максимальное количество фигур данного типа ({maxPiecesAllowed}) уже на доске.");
                    }
                }
            }
        }

        private Piece IdentifyPiece(ImageSource imageSource)
        {
            foreach (var pair in Images.GetWhiteSources())
            {
                if (pair.Value == imageSource)
                {
                    return new Piece(pair.Key, Player.White);
                }
            }

            foreach (var pair in Images.GetBlackSources())
            {
                if (pair.Value == imageSource)
                {
                    return new Piece(pair.Key, Player.Black);
                }
            }

            return null; // Если фигура не найдена
        }

        private int GetMaxPiecesAllowed(Piece piece)
        {
            return piece.Type switch
            {
                PieceType.Pawn => 8,
                PieceType.Bishop => 2,
                PieceType.Knight => 2,
                PieceType.Rook => 2,
                PieceType.Queen => 1,
                PieceType.King => 1,
                _ => 0,
            };
        }

        private int CountPiecesOfType(Piece piece)
        {
            int count = 0;

            // Проходим по всем клеткам доски и считаем количество фигур заданного типа и цвета
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    if (gameState.Board[r, c]?.Type == piece.Type && gameState.Board[r, c]?.Color == piece.Color)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        public class PiecePlacementTracker
        {
            private readonly Dictionary<Piece, Position> piecePositions = new Dictionary<Piece, Position>();

            public void TrackPiece(Piece piece, Position position)
            {
                piecePositions[piece] = position;
            }

            public Position GetPiecePosition(Piece piece)
            {
                return piecePositions.ContainsKey(piece) ? piecePositions[piece] : null; // или выбросить исключение, если фигура не найдена
            }

            public void RemovePiece(Piece piece)
            {
                piecePositions.Remove(piece);
            }

            // Дополнительные методы по необходимости
        }
    }
}
