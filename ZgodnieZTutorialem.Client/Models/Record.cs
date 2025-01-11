namespace ZgodnieZTutorialem.Client.Models
{
    public class Record
    {
        public string TableName { get; set; }
        public string Nick { get; set; }
        public Record()
        {

        }

        public Record(string tableName, string nick)
        {
            TableName = tableName;
            Nick = nick;
        }
    }
}
