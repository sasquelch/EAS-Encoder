namespace EASEncoder.Models.SAME
{
    public class SAMECounty
    {
        public SAMECounty(int id, string name, SAMEState State)
        {
            Id = id;
            Name = name;
            state = State;
        }

        public int Id { private set; get; }
        public string Name { private set; get; }
        public SAMEState state { private set; get; }
        public string Value => Name;
    }
}