using System.ComponentModel.DataAnnotations.Schema;

namespace LinkLite.Data
{
    [Table("condition_occurrence")]
    public class ConditionOccurrence
    {
        [Column("condition_occurrence_id")]
        public int Id { get; set; }

        [Column("person_id")]
        public int PersonId { get; set; }

        [Column("condition_concept_id")]
        public int ConditionConceptId { get; set; }

        public virtual Person Person { get; set; } = new ();
    }
}
