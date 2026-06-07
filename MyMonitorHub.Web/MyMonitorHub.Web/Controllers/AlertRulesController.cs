using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using MyMonitorHub.Domain.BO;
using MyMonitorHub.Domain.Exceptions;
using MyMonitorHub.Domain.Interface;
using MyMonitorHub.Domain.Security;
using MyMonitorHub.Domain.Service;
using MyMonitorHub.Domain.Util;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MyMonitorHub.Web.Controllers
{
    [Authorize]
    public class AlertRulesController : BaseController
    {
        private readonly IDbContextScopeFactory _contextScopeFactory;

        public AlertRulesController(IDbContextScopeFactory contextScopeFactory)
            : base(contextScopeFactory)
        {
            _contextScopeFactory = contextScopeFactory;
        }

        public IActionResult Index()
        {
            if (!Authorizer.Authorize(Permissions.CanViewAlertRules))
                throw new SecurityException(Permissions.CanViewAlertRules.FailMessage);

            ViewBag.CanEdit = Authorizer.Authorize(Permissions.CanEditAlertRules);
            var rules = new AlertRulesService(_contextScopeFactory).GetRules(Helper.AccountId);
            return View(rules);
        }

        [HttpPost]
        public JsonResult Save(string? rules)
        {
            if (!Authorizer.Authorize(Permissions.CanEditAlertRules))
                return Json(new { success = false, message = Permissions.CanEditAlertRules.FailMessage });

            var ruleList = ParseRuleList(rules);

            var errors = new List<string>();
            foreach (var rule in ruleList)
            {
                try
                {
                    var parser = new RuleParser(rule);
                    parser.Parse();
                    if (!IsValidEmail(parser.GetUser()))
                        errors.Add($"The username in rule \"{rule}\" must be a valid email address.");
                }
                catch (Exception e)
                {
                    errors.Add(e.Message);
                }
            }

            if (errors.Count > 0)
                return Json(new { success = false, message = "One or more rules are invalid.", errors });

            new AlertRulesService(_contextScopeFactory).SaveRules(Helper.AccountId, ruleList);
            return Json(new { success = true });
        }

        [HttpPost]
        public JsonResult Validate(string? rule)
        {
            if (string.IsNullOrWhiteSpace(rule))
                return Json(new { valid = false, error = "Rule cannot be empty." });

            try
            {
                var parser = new RuleParser(rule.Trim());
                parser.Parse();
                if (!IsValidEmail(parser.GetUser()))
                    return Json(new { valid = false, error = "The username must be a valid email address." });
                return Json(new { valid = true });
            }
            catch (Exception e)
            {
                return Json(new { valid = false, error = e.Message });
            }
        }

        private static bool IsValidEmail(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            try
            {
                var addr = new MailAddress(value);
                return addr.Address == value;
            }
            catch
            {
                return false;
            }
        }

        private static List<string> ParseRuleList(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return new List<string>();

            return raw
                .Split(';')
                .Select(r => r.Trim())
                .Where(r => r.Length > 0)
                .ToList();
        }
    }
}
