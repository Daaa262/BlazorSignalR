namespace ZgodnieZTutorialem.Client.Models
{
    public class Player
    {
        public bool IsReady { get; set; }
        public int Chips { get; set; }
        public string? Nick { get; set; }
        public int[] Cards { get; set; } = new int[2];
        public Player(bool isReady, int chips, string nick)
        {
            IsReady = isReady;
            Chips = chips;
            Nick = nick;
        }

        public Player() // This constructor is required for SignalR
        {

        }
    }
}
