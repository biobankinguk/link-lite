using System.ComponentModel.DataAnnotations.Schema;

namespace LinkLite.Data.Entities
{
    [Table("observation")]
    public class Observation
    {
        [Column("observation_id")]
        public int Id { get; set; }

        [Column("person_id")]
        public int PersonId { get; set; }

        [Column("observation_concept_id")]
        public int ObservationConceptId { get; set; }

        [Column("value_as_number")]
        public double? ValueAsNumber { get; set; }

        public virtual Person Person { get; set; } = new();
    }
}
