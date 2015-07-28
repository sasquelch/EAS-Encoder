namespace EASEncoder.Models.SAME
{
    public class SAMEState
    {
        public SAMEState(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public int Id { private set; get; }
        public string Name { private set; get; }
    }
}