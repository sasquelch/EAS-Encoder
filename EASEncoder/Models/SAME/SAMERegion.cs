using System.ComponentModel;

namespace EASEncoder.Models.SAME
{
    public class SAMERegion
    {
        [DisplayName("County Name")]
        public string CountyName => County.Name;

        [DisplayName("State Name")]
        public string StateName => State.Name;

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