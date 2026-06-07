using System.Collections.Generic;
using System.Linq;
using MyMonitorHub.Domain.Entity;
using MyMonitorHub.Domain.Interface;

namespace MyMonitorHub.Domain.Service
{
    public class AlertRulesService
    {
        private readonly IDbContextScopeFactory _contextScopeFactory;

        public AlertRulesService(IDbContextScopeFactory contextScopeFactory)
        {
            _contextScopeFactory = contextScopeFactory;
        }

        public List<string> GetRules(int accountId)
        {
            using var scope = _contextScopeFactory.Create();
            var rules = scope.Get<Account>()
                .Where(x => x.AccountId == accountId)
                .Select(x => x.Rules)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(rules))
                return new List<string>();

            return rules
                .Split(';')
                .Select(r => r.Trim())
                .Where(r => r.Length > 0)
                .ToList();
        }

        public void SaveRules(int accountId, List<string> rules)
        {
            using var scope = _contextScopeFactory.Create();
            var account = scope.Get<Account>().FirstOrDefault(x => x.AccountId == accountId);
            if (account == null) return;

            account.Rules = rules.Count > 0
                ? string.Join(";", rules.Select(r => r.Trim()).Where(r => r.Length > 0))
                : null;

            scope.SaveChanges();
        }
    }
}
