namespace EASEncoder.Models.SAME
{
    public class SAMERegion
    {
        public SAMECounty County;
        public SAMEState State;
        public SAMESubdivision Subdivision = new SAMESubdivision(0, "Entire Region");

        public SAMERegion(SAMEState state, SAMECounty county)
        {
            State = state;
            County = county;
        }
    }
}