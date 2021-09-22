using System;

namespace Web.Models
{
    public class PositionResponse
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public string Location { get; set; }
        public int? CityId {get; set; }
        public string Company { get; set; }
        public int? PositionTypeId { get; set; }
    }
}