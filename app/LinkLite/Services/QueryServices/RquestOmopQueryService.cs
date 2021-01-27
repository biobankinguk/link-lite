using LinkLite.Data;
using LinkLite.Dto;
using LinkLite.Services.QueryServices;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LinkLite.Services.QueryServices
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
            Dictionary<string, List<Person>> results = new();

            List<Task> ruleExecutors = new();
            List<Exception> exceptions = new();

            // run a query for each individual rule
            for (var iGroup = 0;
                iGroup < query.Groups.Count;
                iGroup++)
            {
                var group = query.Groups[iGroup];
                for (var iRule = 0;
                    iRule < group.Rules.Count;
                    iGroup++)
                {
                    try
                    {


                        ruleExecutors.Add(Task.Run(async () =>
                        {
                            var rule = group.Rules[iRule];
                            results[$"{iGroup}_{iRule}"] = rule.Type switch
                            {
                                RuleTypes.Boolean => await BooleanHandler(rule),
                                _ => throw new ArgumentException($"Unknown Rule Type: {rule.Type}")
                            };
                        }));
                    }
                    catch (Exception e)
                    {
                        exceptions.Add(e);
                    }
                }
            }

            // wait for all the rules to return results
            await Task.WhenAll(ruleExecutors);

            // any errors running rule queries?
            if (exceptions.Any())
                throw new AggregateException(
                    "Errors occurred processing the query",
                    exceptions);

            // Now move on to combining the rule results into a query result

            // TODO: AND/OR

            return results.Count;
        }

        public async Task<List<Person>> BooleanHandler(RquestQueryRule rule)
        {
            // boolean doesn't require operand, it defaults to "="
            // and the bool value can be used to effect inclusion or exclusion
            var value = bool.Parse(rule.Value);
            if (rule.Operand == RuleOperands.Exclude)
                value = !value;

            var conceptId = Helpers.ParseVariableName(rule.VariableName);

            // Run the query
            var result = await _db.Person.AsNoTracking()
                .Include(p => p.ConditionOccurrences)
                .Include(p => p.Measurements)
                .Include(p => p.Observations)
                .Where(p =>
                    p.ConditionOccurrences.Select(co => co.ConditionConceptId).Contains(conceptId) == value ||
                    p.Measurements.Select(co => co.MeasurementConceptId).Contains(conceptId) == value ||
                    p.Observations.Select(co => co.ObservationConceptId).Contains(conceptId) == value)
                .ToListAsync();

            return result;
        }
    }
}
