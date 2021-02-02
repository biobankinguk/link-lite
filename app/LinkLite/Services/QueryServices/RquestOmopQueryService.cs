using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

using LinkLite.Data;
using LinkLite.Dto;

using Microsoft.EntityFrameworkCore;

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
            Dictionary<string, List<int>> ruleResults = new();

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
                        var rule = group.Rules[iRule];
                        ruleResults[$"{iGroup}_{iRule}"] = rule.Type switch
                        {
                            RuleTypes.Boolean => await BooleanHandler(rule),
                            _ => throw new ArgumentException($"Unknown Rule Type: {rule.Type}")
                        };
                    }
                    catch (Exception e)
                    {
                        exceptions.Add(e);
                    }
                }
            }

            // any errors running rule queries?
            // TODO: should we early exit at first error instead?
            if (exceptions.Count > 0)
                throw new AggregateException(
                    "Errors occurred processing the query",
                    exceptions);

            // Now move on to combining the rule results into a query result
            if (query.Groups.Count > 1)
            {
                for (var iGroup = 0; iGroup < query.Groups.Count; iGroup++)
                {
                    var group = query.Groups[iGroup];

                    if (group.Rules.Count > 1)
                    {
                        ruleResults[iGroup.ToString()] =
                            Combine(
                                group.Combinator,
                                ruleResults.Keys
                                    .Where(key => key.StartsWith($"{iGroup}_"))
                                    .Select(key => ruleResults[key])
                                    .ToList())
                            .ToList();
                    }
                }
            }

            return ruleResults.Count;
        }

        public static HashSet<T> Combine<T>(string combinator, List<List<T>> integrants)
             where T : notnull
            => Combine(combinator, integrants, x => x);

        public static HashSet<TEntry> Combine<TEntry, TKey>(string combinator, List<List<TEntry>> integrants,
            Expression<Func<TEntry, TKey>> keySelector)
            where TKey : notnull
        {
            Func<TEntry, TKey> keyAccessor = keySelector.Compile();

            // keys = unique entries
            // values = the entry itself AND indices of lists in which the entry appears
            Dictionary<TKey, (TEntry entry, HashSet<int> integrants)> entries = new();

            // loop one time through all the lists to log which ones a given entry appears in
            for (var i = 0; i < integrants.Count; i++)
                foreach (var entry in integrants[i])
                {
                    var key = keyAccessor(entry);
                    if (!entries.ContainsKey(key))
                        entries[key] = (entry, integrants: new());
                    entries[key].integrants.Add(i);
                }

            return combinator switch
            {
                // filter the entries by those which appear in ALL lists
                QueryCombinators.And =>
                    entries.Keys
                        .Where(key => entries[key].integrants.Count == integrants.Count)
                        .Select(key => entries[key].entry)
                        .ToHashSet(),

                // return the unique set of entries
                QueryCombinators.Or => entries.Keys
                    .Select(key => entries[key].entry)
                    .ToHashSet(),

                _ => throw new ArgumentException($"Unexpected Combinator: {combinator}")
            };
        }

        public async Task<List<int>> BooleanHandler(RquestQueryRule rule)
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
                .Select(p => p.Id)
                .ToListAsync();

            return result;
        }
    }
}
