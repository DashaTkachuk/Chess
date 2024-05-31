using System;
using System.Windows;
using System.Windows.Controls;

namespace ChessUI
{
    public partial class ModeMenu : UserControl
    {
        public event Action<Mode> ModeOnePlayerSelected;
        public event Action<Mode> ModeTwoPlayersSelected;

        public Mode CurrentMode { get; private set; }

        public ModeMenu()
        {
            InitializeComponent();
        }

        private void OnePlayer_Click(object sender, RoutedEventArgs e)
        {
            CurrentMode = Mode.OnePlayer;
            ModeOnePlayerSelected?.Invoke(CurrentMode);
        }

        private void TwoPlayers_Click(object sender, RoutedEventArgs e)
        {
            CurrentMode = Mode.TwoPlayers;
            ModeTwoPlayersSelected?.Invoke(CurrentMode);
        }
    }
}
