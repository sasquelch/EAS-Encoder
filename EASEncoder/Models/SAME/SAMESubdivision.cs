namespace EASEncoder.Models.SAME
{
    public class SAMESubdivision
    {
        public SAMESubdivision(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public int Id { private set; get; }
        public string Name { private set; get; }
        public string Value => Name;
    }
}