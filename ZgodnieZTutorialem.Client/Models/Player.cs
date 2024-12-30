namespace ZgodnieZTutorialem.Client.Models
{
    public class Player
    {
        public bool isReady;
        public int Chips;
        public Player(bool isReady, int Chips)
        {
            this.isReady = isReady;
            this.Chips = Chips;
        }
    }
}
