namespace EASEncoder.Models.SAME
{
    public class SAMEMessageAlertCode
    {
        public SAMEMessageAlertCode(string id, string name)
        {
            Id = id;
            Name = name;
        }

        public string Id { private set; get; }
        public string Name { private set; get; }
    }
}