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
        private readonly ModeMenu modeMenu;
        private readonly AI ai = new AI();
        private GameState gameState;
        private Position selectedPos = null;
        private bool isPlayerTurn = true;

        public MainWindow()
        {
            InitializeComponent();
            modeMenu = new ModeMenu();
            modeMenu.ModeOnePlayerSelected += StartGameOnePlayer;
            modeMenu.ModeTwoPlayersSelected += StartGameTwoPlayers;
            MenuContainer.Content = modeMenu;
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
            InitializeBoard();
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
                    pieceImages[r, c].Source = Images.GetImage(piece);
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
                } else if (option == Option.Clear)
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
                }
            }
        }

        private void DeleteSelectedPiece()
        {
            if (selectedPos != null)
            {
                gameState.Board[selectedPos.Row, selectedPos.Column] = null;
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
    }
}
