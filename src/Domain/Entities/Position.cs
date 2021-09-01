using Domain.Common;

namespace Domain.Entities
{
    public class Position : BaseEntity
    {
        public string Name { get; set; }
        public string BreezyId { get; set; }
        public int PositionTypeId { get; set; }
        public int CountryId { get; set; }
        public int StateId { get; set; }
        public int CityId { get; set; }
    }
}
