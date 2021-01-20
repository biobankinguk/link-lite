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

    public class OmopContext : DbContext
    {
        public OmopContext(DbContextOptions<OmopContext> options)
            : base(options)
        {
        }

        public DbSet<Person> Person { get; set; }
    }
}
