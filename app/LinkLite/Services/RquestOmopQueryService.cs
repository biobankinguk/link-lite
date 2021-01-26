using LinkLite.Data;
using LinkLite.Dto;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LinkLite.Services
{
    /// <summary>
    /// A service for running Rquest queries against an OMOP CDM database
    /// </summary>
    public class RquestOmopQueryService
    {
        private readonly OmopContext _db;

        public RquestOmopQueryService(OmopContext db)
        {
            _db = db;
        }

        public async Task<int> Process(RquestQuery query)
        {

            return 0;
        }

        // TODO: Unit Testing?!

        /// <summary>
        /// handler for rule type BOOLEAN
        /// </summary>
        /// <param name="rule">The Rule</param>
        /// <returns>List of Persons matching the Rule</returns>
        private async Task<List<Person>> Boolean(RquestQueryRule rule)
        {
            if (rule.Type != RuleTypes.Boolean)
                throw new ArgumentException(
                    ErrorMessages.RuleTypeMismatch(
                        RuleTypes.Boolean,
                        rule.Type));

            // boolean doesn't require operand, it assumes "="
            // and its value can be used to effect inclusion or exclusion
            var value = bool.Parse(rule.Value);
            if (rule.Operand == RuleOperands.Exclude)
            {
                value = !value;
            }

            var conceptId = ParseVariableName(rule.VariableName);

            // Run the query
            var result = await _db.Person
                .Join(_db.ConditionOccurrence,
                    p => p.Id,
                    co => co.PersonId,
                    (p, co) => new { p.Id, co.ConditionConceptId })
                .Join(_db.Measurement,
                    x => x.Id,
                    m => m.PersonId,
                    (x, m) => new
                    {
                        x.Id,
                        x.ConditionConceptId,
                        m.MeasurementConceptId
                    })
                .Join(_db.Observation,
                    x => x.Id,
                    o => o.PersonId,
                    (x, o) => new
                    {
                        x.Id,
                        x.ConditionConceptId,
                        x.MeasurementConceptId,
                        o.ObservationConceptId
                    })
                .Where(x =>
                    (x.ConditionConceptId == conceptId) == value ||
                    (x.MeasurementConceptId == conceptId) == value ||
                    (x.ObservationConceptId == conceptId) == value)
                .Select(x => new Person() { Id = x.Id })
                .ToListAsync();

            return result;
        }

        /// <summary>
        /// Parse a Rule's Variable Name into an OMOP Concept ID.
        /// TODO: Assumes always OMOP for now
        /// </summary>
        /// <param name="variableName">The VariableName property of Query Rule, e.g. "OMOP:123456"</param>
        /// <returns>The OMOP Concept Integer ID</returns>
        private static int ParseVariableName(string variableName)
            => int.Parse(variableName.Replace("OMOP:", ""));
    }



    /// <summary>
    /// Reusable parameterised error messages
    /// </summary>
    public static class ErrorMessages
    {
        public static string RuleTypeMismatch(string expected, string actual)
            => "Rule processing type mismatch: " +
               $"Expected {expected} but got {actual}.";
    }

    // Keep reference constants for the Rule property values
    public static class RuleTypes
    {
        public const string Boolean = "BOOLEAN";
        public const string Numeric = "NUMERIC";
        public const string Alternative = "ALTERNATIVE";
        public const string Text = "TEXT";
    }

    public static class RuleOperands
    {
        public const string Include = "=";
        public const string Exclude = "!=";
    }
}
