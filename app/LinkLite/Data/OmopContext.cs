using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace LinkLite.Data
{
    [Table("person")]
    public class Person
    {
        [Column("person_id")]
        public int Id { get; set; }
    }

    [Table("condition_occurrence")]
    public class ConditionOccurrence
    {
        [Column("condition_occurrence_id")]
        public int Id { get; set; }

        [Column("person_id")]
        public int PersonId { get; set; }

        [Column("condition_concept_id")]
        public int ConditionConceptId { get; set; }
    }

    [Table("observation")]
    public class Observation
    {
        [Column("observation_id")]
        public int Id { get; set; }

        [Column("person_id")]
        public int PersonId { get; set; }

        [Column("observation_concept_id")]
        public int ObservationConceptId { get; set; }
    }

    [Table("measurement")]
    public class Measurement
    {
        [Column("measurement_id")]
        public int Id { get; set; }

        [Column("person_id")]
        public int PersonId { get; set; }

        [Column("measurement_concept_id")]
        public int MeasurementConceptId { get; set; }
    }

    public class OmopContext : DbContext
    {
        public OmopContext(DbContextOptions<OmopContext> options)
            : base(options)
        {
        }

        public DbSet<Person> Person { get; set; }

        public DbSet<ConditionOccurrence> ConditionOccurrence { get; set; }
        public DbSet<Measurement> Measurement { get; set; }
        public DbSet<Observation> Observation { get; set; }
    }
}
