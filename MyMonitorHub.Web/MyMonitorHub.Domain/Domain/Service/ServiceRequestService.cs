using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using MyMonitorHub.Domain.BO;
using MyMonitorHub.Domain.Dto;
using MyMonitorHub.Domain.Entity;
using MyMonitorHub.Domain.Exceptions;
using MyMonitorHub.Domain.Interface;
using MyMonitorHub.Domain.Models;
using MyMonitorHub.Domain.Security;
using MyMonitorHub.Domain.Util;
using Microsoft.Extensions.Logging;

namespace MyMonitorHub.Domain.Service
{
    public class ServiceRequestService
    {
        private readonly ILogger _logger;
        private readonly ILoggerFactory _loggerFactory;

        private bool _isNew;
        private readonly IDbContextScopeFactory _dbContextScopeFactory;

        public ServiceRequestService(IDbContextScopeFactory dbContextScopeFactory, ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<ServiceRequestService>();
            _dbContextScopeFactory = dbContextScopeFactory;
        }

        public bool IsNew
        {
            get { return _isNew; }
        }

        public ServiceRequest GetSrWithPageAndUser(int id)
        {
            using (var scope = _dbContextScopeFactory.Create())
            {
                var sr =
                    scope.Get<ServiceRequest>().Where(x => x.AccountId == Helper.AccountId && x.ServiceRequestId == id).Include(x => x.Device).ThenInclude(d => d.DeviceGroup).ThenInclude(g => g.Page).Include(x => x.User).FirstOrDefault();
                if (sr != null)
                {
                    // there are instances where you have a service request attached to a device that has no device group. Although I think this really shouldn't happen
                    var dg = sr.Device.DeviceGroup;
                    if (dg == null || sr.Device.DeviceGroupId == null)
                    {
                        if (Helper.IsAdministrator || Helper.IsOwner)
                        {
                            return sr;
                        }
                        return null;
                    }
                }
                return sr;
            }
        }

        public SrClosedView SrClosed(int id)
        {
            SrClosedView data;
            using (var scope = _dbContextScopeFactory.Create())
            {
                var serviceRequest = (from sr in scope.Get<ServiceRequest>().Where(x => x.ServiceRequestId == id)
                    select new
                    {
                        sr.AccountId,
                        serviceRequestId = sr.ServiceRequestId,
                        deviceId = sr.DeviceId,
                        deviceDesc = sr.Device.Description,
                        assignedToId = sr.AssignedToId,
                        device = sr.Device,
                        userName = sr.User.Email,
                        notes = sr.Notes,
                        timeStamp = sr.TimeStamp,
                        status = sr.Status
                    }).SingleOrDefault();


                if (serviceRequest == null)
                {
                    throw new Exception("Service request is invalid or does not belong to your account.");
                }

                var accountId = serviceRequest.AccountId;
                if (!Authorizer.IsAdministrator && accountId != Helper.AccountId)
                {
                    throw new SecurityException("Service request is invalid or does not belong to your account.");
                }
                var organization =
                    scope.Get<Account>()
                        .Where(x => x.AccountId == accountId)
                        .Select(x => x.Identification)
                        .FirstOrDefault();

                var group = serviceRequest.device.DeviceGroup;
                data = new SrClosedView
                {
                    AssignedToId = serviceRequest.assignedToId,
                    DeviceDesc = serviceRequest.deviceDesc,
                    DeviceId = serviceRequest.deviceId,
                    GroupDesc =
                        group == null
                            ? "Device has been soft deleted and does not belong to a device group"
                            : group.Description,
                    Notes = serviceRequest.notes,
                    PageDesc =
                        group == null
                            ? "Device has been soft deleted and does not belong to a page"
                            : group.Page.Description,
                    ServiceRequestId = serviceRequest.serviceRequestId,
                    Status = serviceRequest.status,
                    TimeStamp = serviceRequest.timeStamp,
                    UserName = serviceRequest.userName
                };


                if (data.AssignedToId == null)
                {
                    data.Status = "Open";
                }
                else if (data.Status == "O")
                {
                    data.Status = "Open";
                }
                else
                {
                    data.Status = "Closed";
                }

                // setup the logo
                var theFile = Helper.GetBaseUrl() + "/Content/Organization/" + organization + "/Logo.png";
                data.ImageUrl = theFile;

                // set up the site
                var path = Helper.GetBaseUrl() + "/" + organization + "/Summary";
                data.Site = path;

                data.Request = null;
            } // using
            return data;
        }

        public List<ServiceRequestView> GetOutstanding()
        {
            using (var scope = _dbContextScopeFactory.Create())
            {
                var data =
                    scope.Get<ServiceRequest>().Where(x => x.AccountId == Helper.AccountId && x.Status == "O")
                        .Select(x => new ServiceRequestView
                        {
                            ServiceRequestId = x.ServiceRequestId,
                            DeviceDesc = x.Device.Description,
                            UserName = x.User.Email,
                            Notes = x.Notes,
                            TimeStamp = x.TimeStamp,
                            DeviceGroupDesc = x.Device.DeviceGroup.Description,
                            PageId = x.Device.DeviceGroup.PageId,
                            DeviceGroupId = x.Device.DeviceGroupId == null ? 0 : x.Device.DeviceGroupId.Value
                        }).ToList();


                return data;
            } // using
        } // getOutStanding

        public int GetOutstandingCount()
        {
            using (var scope = _dbContextScopeFactory.Create())
            {
                var data =
                    scope.Get<ServiceRequest>().Where(x => x.AccountId == Helper.AccountId && x.Status == "O")
                        .Select(x => new 
                        {
                            x.Device.DeviceGroup.PageId,
                            DeviceGroupId = x.Device.DeviceGroupId == null ? 0 : x.Device.DeviceGroupId.Value
                        }).ToList();


                return data.Count;
            } // using
        } // getOutStanding

        // runs every two minutes
        // sends out new alerts right away
        // notify users of not accepted yet alerts every x minutes since the last alert they got.
        public void FindTardy()
        {
            var now = DateTime.Now;
            using (var scope =_dbContextScopeFactory.Create())
            {
                var accounts = (scope.Get<Configuration>()
                    .Select(x => new
                    {
                        x.accountId,
                        x.srNotAcceptedAlertMinutes
                    })).ToList();

                foreach (var account in accounts)
                {
                    var account1 = account;
                    var srs =
                        (scope.Get<ServiceRequest>().Where(
                            x => x.AccountId == account1.accountId && x.Status == "O" && x.AssignedToId == null)).ToList();

                    if (srs.Count == 0)
                    {
                        _logger.LogDebug("FindTardy: account {0} has no open unassigned SRs, skipping", account.accountId);
                        continue;
                    }

                    _logger.LogDebug("FindTardy: account {0} has {1} open unassigned SR(s)", account.accountId, srs.Count);

                    // this will bring back a list of users that should be notified and the count of non accepted alerts
                    var users = GetUsersThatNeedToBeNotified(account.accountId, srs,now, account.srNotAcceptedAlertMinutes);

                    // the srs may have had their lastAlertTime changed
                    scope.SaveChanges();

                    foreach (var user in users)
                    {
                        _logger.LogDebug("FindTardy: preparing notification for user '{0}' (notAcceptedCount={1})", user.User, user.NotAcceptedCount);

                        string body;
                        if (user.NotAcceptedCount == 1)
                        {
                            body = "1 alert not accepted";
                        }
                        else
                        {
                            body = user.NotAcceptedCount + " alerts not accepted";
                        }

                        var emails = new EmailNotificationService(_dbContextScopeFactory, _loggerFactory).GetEmailsForUser(user.User);
                        if (emails.Count != 0)
                        {
                            var allEmails = string.Join(";", emails);
                            _logger.LogDebug("FindTardy: sending alert email to '{0}' for user '{1}'", allEmails, user.User);
                            var emailService = new EmailService(_dbContextScopeFactory, _loggerFactory);
                            emailService.SendAway(allEmails, "ADS Alert", null, "Not accepted count = " + user.NotAcceptedCount);
                        }
                        else
                        {
                            _logger.LogWarning("FindTardy: no delivery addresses found for user '{0}' — alert email NOT sent. Check EmailNotification table entries for this user.", user.User);
                        }
                    }

                } // foreach account
            } // using
        } // method

        private class UserProfile
        {
            public string User;
            public int NotAcceptedCount;
            public bool NotifyMe;
        }

        private IEnumerable<UserProfile> GetUsersThatNeedToBeNotified(int accountId, List<ServiceRequest> srs,
            DateTime now, int? srNotAcceptedAlertMinutes)
        {
            var userList = new Dictionary<string, UserProfile>();

            var descriptionCache = new Dictionary<int, AccountService.Descriptions>();

            using (var scope = _dbContextScopeFactory.Create())
            {
                var rules = scope.Get<Account>().Where(x => x.AccountId == accountId).Select(x => x.Rules).Single();



                // parse all the commands ahead of time
                var parsers = new List<RuleParser>();

                var commands = rules.Split(';');
                foreach (var command in commands) // go through every rule
                {
                    if (!string.IsNullOrEmpty(command))
                    {
                        _logger.LogDebug("About to parse rule: {0}", command);
                        var parser = new RuleParser(command);
                        parser.Parse();
                        parsers.Add(parser);
                    }
                }





                _logger.LogDebug("There are {0} service requests to check", srs.Count);
                foreach (var sr in srs) // for each rule, go through every sr
                {
                    _logger.LogDebug("Checking service request: {0}", sr.ServiceRequestId);
                    AccountService.Descriptions descriptions;
                    if (descriptionCache.ContainsKey(sr.ServiceRequestId))
                    {
                        _logger.LogDebug("Getting service request from cache");
                        descriptions = descriptionCache[sr.ServiceRequestId];
                    }
                    else
                    {
                        _logger.LogDebug("Getting service request from disk");
                        descriptions = new AccountService(_dbContextScopeFactory).GetDescriptions(sr.DeviceId);
                        descriptionCache[sr.ServiceRequestId] = descriptions; // cache it for the next time
                    }

                    // a list of all users that might need to be notified
                    var usersMatchingThisSr = new List<string>();
                    foreach (var parser in parsers) // go through all parsers
                    {
                        if (parser.Match(descriptions))
                        {
                            var user = parser.GetUser().ToUpper();
                            if (!usersMatchingThisSr.Contains(user))
                            {
                                usersMatchingThisSr.Add(user);
                            }
                        }
                    }

                    // we have at least one user that might need to be notified
                    if (usersMatchingThisSr.Count > 0)
                    {
                        // get the user that matches this rule
                        _logger.LogDebug("The following users match this sr: {0}", sr.ServiceRequestId);
                        foreach (var user in usersMatchingThisSr)
                        {
                            _logger.LogDebug($"    {user}");

                            // create the user collection if not already created
                            if (!userList.ContainsKey(user)) userList[user] = new UserProfile { User = user };

                            var userProfile = userList[user];
                            userProfile.NotAcceptedCount++;

                        }

                        // new ones need to be sent out immediately
                        if (sr.LastAlertTime == null)
                        {
                            _logger.LogDebug("The sr is new.");
                            // you are notified of new SRs immediately
                            sr.LastAlertTime = now;
                            MarkUsersAsNotificationRequired(userList, usersMatchingThisSr);
                        }
                        else
                        {
                            // these ones are sent out only have x number of minutes
                            var timeSpan = now - sr.LastAlertTime.Value;
                            if (srNotAcceptedAlertMinutes != null && timeSpan.TotalMinutes >= srNotAcceptedAlertMinutes)
                            {
                                // it's been at least x number of minutes since the last notification run for this user
                                _logger.LogDebug("The sr is not new but its been x minutes and noone has accepted yet");
                                sr.LastAlertTime = now;
                                MarkUsersAsNotificationRequired(userList, usersMatchingThisSr);
                            }
                        }
                    }
                } // each sr

                var users = userList.Values.ToList();
                _logger.LogDebug("number of users is {0}", users.Count);
                var usersToBeNotified =
                    users.Where(x => x.NotifyMe).ToList(); // take only the ones that need to be notified
                _logger.LogDebug("Number of those users that need to be notified is {0}", usersToBeNotified.Count);
                return usersToBeNotified;
            }
        }

        private void MarkUsersAsNotificationRequired(Dictionary<string, UserProfile> userList, IEnumerable<string> usersMatchingThisSr)
        {
            foreach (var user in usersMatchingThisSr)
            {
                var userProfile = userList[user];
                userProfile.NotifyMe = true;

            }
        }


        public ServiceRequest Get(int accountId, int deviceId)
        {
            using (var scope = _dbContextScopeFactory.Create())
            {
                try
                {
                    return
                        scope.Get<ServiceRequest>().FirstOrDefault(x => x.DeviceId == deviceId && x.Status == "O" && x.AccountId == accountId);
                }
                catch (Exception e)
                {
                    Console.WriteLine(scope.GetHashCode() + " - " + e.Message);
                    throw;
                }
            } // using
        }

        public ServiceRequest GetOrCreate(int accountId, int deviceId)
        {
            _isNew = false;
            using (var scope = _dbContextScopeFactory.Create())
            {
                var srRow =
                    scope.Get<ServiceRequest>().FirstOrDefault(x => x.DeviceId == deviceId && x.Status == "O" && x.AccountId == accountId);

                if (srRow == null)
                {
                    srRow = new ServiceRequest
                    {
                        Notes = "",
                        TimeStamp = DateTime.Now,
                        Status = "O",
                        DeviceId = deviceId,
                        AccountId = accountId,
                        LastAlertTime = null
                    };

                    scope.Add(srRow);
                    scope.SaveChanges();
                    _isNew = true;
                } // using
                return srRow;
            }
        }

        public void Accept(int id, string notes)
        {
            using (var scope = _dbContextScopeFactory.Create())
            {
                var serviceRequest =
                    scope.Get<ServiceRequest>().FirstOrDefault(sr => sr.ServiceRequestId == id);
                if (serviceRequest != null && serviceRequest.AssignedToId == null)
                {
                    var userId =
                        scope.Get<User>().Where(x => x.Email == Helper.Email)
                            .Select(x => x.UserId)
                            .FirstOrDefault();
                    serviceRequest.AssignedToId = userId;
                    serviceRequest.Notes = notes ?? string.Empty;
                    scope.SaveChanges();
                }
                else
                {
                    throw new WarningException("Service Request: " + id + " - has already been accepted");
                }
            } // using
        }

        public void UpdateNotes(int id, string notes)
        {
            using (var scope = _dbContextScopeFactory.Create())
            {
                var serviceRequest =
                    scope.Get<ServiceRequest>().FirstOrDefault(sr => sr.ServiceRequestId == id);
                if (serviceRequest != null) serviceRequest.Notes = notes ?? string.Empty;
                scope.SaveChanges();
            } // using
        }

        /// <summary>
        /// Closes the service request and returns the notification email list (or null if none
        /// configured). The caller is responsible for building and sending the closed-SR email so
        /// that HTML rendering can be done in the web layer rather than via HTTP self-fetches.
        /// </summary>
        public string Close(int id, string notes)
        {
            string emails = null;
            using (var scope = _dbContextScopeFactory.Create())
            {
                var serviceRequest =
                    scope.Get<ServiceRequest>().Where(sr => sr.ServiceRequestId == id).Include(sr => sr.Device).ThenInclude(d => d.DeviceGroup).FirstOrDefault();
                if (serviceRequest != null)
                {
                    emails = serviceRequest.Device.DeviceGroup.emailsForClosedSRs;
                    if (serviceRequest.Status == "C")
                    {
                        throw new WarningException("Service Request: " + id + " - has already been closed");
                    }
                    serviceRequest.Status = "C";
                    serviceRequest.Notes = notes ?? string.Empty;
                }
                scope.SaveChanges();
            } // using
            return emails;
        }

        public SearchView GetSearchView()
        {
            var searchView = new SearchView
            {
                Criteria = new SearchCriteria(),
                Accounts = new AccountService(_dbContextScopeFactory).GetKeyValuePairs(),
                AssignedTos = new UserService(_dbContextScopeFactory).GetAssignedTosAsPairs(),
                DeviceGroups = new DeviceGroupService(_dbContextScopeFactory).GetKeyValuePairs(),
            };

            // set up the defaults if the session of defaults exists
            var defaults = Helper.Session.GetObject<PageDefaults>(SessionKeys.SearchPageDefaults);
            if (defaults != null)
            {
                searchView.Criteria = defaults.Criteria ?? new SearchCriteria();
                searchView.Sidx = defaults.Sidx;
                searchView.Sord = defaults.Sord;
                searchView.Rows = defaults.Rows;
                searchView.Page = defaults.Page;
            }
            else
            {
                searchView.Sidx = "ServiceRequestId";
                searchView.Sord = "desc";
                searchView.Rows = 20;
                searchView.Page = 1;
            }
            return searchView;
        }


        public Result SearchData(string sidx, string sord, int page, int itemsPerPage, SearchCriteria criteria)
        {
            var pageIndex = page - 1;
            var pageSize = itemsPerPage;


            {
                List<Columns> requests;
                int totalRecords;
                using (var scope = _dbContextScopeFactory.Create())
                {
                    var records = scope.Get<ServiceRequest>();

                    //handle the selection criteria
                    if (criteria.SrFrom != null)
                    {
                        var srId = criteria.SrFrom.Value;
                        records = records.Where(x => x.ServiceRequestId >= srId);
                    }
                    if (criteria.SrTo != null)
                    {
                        var srId = criteria.SrTo.Value;
                        records = records.Where(x => x.ServiceRequestId <= srId);
                    }
                    if (criteria.DateFrom != null)
                    {
                        var date = criteria.DateFrom.Value;
                        records = records.Where(x => x.TimeStamp.Date >= date);
                    }
                    if (criteria.DateTo != null)
                    {
                        var date = criteria.DateTo.Value;
                        records = records.Where(x => x.TimeStamp.Date <= date);
                    }

                    if (criteria.SelectedDeviceGroup != 0)
                    {
                        var deviceGroupId = criteria.SelectedDeviceGroup;
                        records = records.Where(x => x.Device.DeviceGroupId == deviceGroupId);
                    }
                    else
                    {
                        // only collect the allowed devices groups. null means all are allowed.
                        var deviceGroups = new DeviceGroupService(_dbContextScopeFactory).GetAllowed();
                        if (deviceGroups != null)
                        {
                            records = records.Where(x => deviceGroups.Any(s => x.Device.DeviceGroupId == s));
                        }
                    }
                    if (criteria.SelectedAssignedTo != 0)
                    {
                        var assignedTo = criteria.SelectedAssignedTo;
                        records = records.Where(x => x.AssignedToId == assignedTo);
                    }
                    if (criteria.SelectedAccount != 0)
                    {
                        var account = criteria.SelectedAccount;
                        records = records.Where(x => x.AccountId == account);
                    }

                    totalRecords = records.Count();

                    if (string.IsNullOrEmpty(sidx))
                    {
                        sidx = "ServiceRequestId";
                    }
                    switch (sidx.Trim())
                    {
                        case "ServiceRequestId":
                            if (sord == "asc")
                            {
                                records = records.OrderBy(x => x.ServiceRequestId);
                            }
                            else
                            {
                                records = records.OrderByDescending(x => x.ServiceRequestId);
                            }
                            break;
                        case "TimeStamp":
                            if (sord == "asc")
                            {
                                records = records.OrderBy(x => x.TimeStamp);
                            }
                            else
                            {
                                records = records.OrderByDescending(x => x.TimeStamp);
                            }
                            break;
                        case "DeviceGroupDesc":
                            if (sord == "asc")
                            {
                                records = records.OrderBy(x => x.Device.DeviceGroup.Description);
                            }
                            else
                            {
                                records = records.OrderByDescending(x => x.Device.DeviceGroup.Description);
                            }
                            break;
                        case "DeviceDesc":
                            if (sord == "asc")
                            {
                                records = records.OrderBy(x => x.Device.Description);
                            }
                            else
                            {
                                records = records.OrderByDescending(x => x.Device.Description);
                            }
                            break;
                    }

                    requests = records
                        .Skip(pageIndex * pageSize)
                        .Take(pageSize)
                        .Select(x => new Columns
                        {
                            DeviceDesc = x.Device.Description,
                            DeviceGroupDesc = x.Device.DeviceGroup.Description,
                            ServiceRequestId = x.ServiceRequestId,
                            TimeStamp = x.TimeStamp,
                            Alert = x.Events.FirstOrDefault(),
                            Notes = x.Notes
                        })
                        .ToList();
                } // using

                // TODO mapper could have done a better job flattening this out
                var flattenedRequests = new List<Columns2>();
                foreach (var req in requests)
                {
                    var col = new Columns2
                    {
                        DeviceDesc = req.DeviceDesc,
                        DeviceGroupDesc = req.DeviceGroupDesc,
                        Notes = req.Notes,
                        ServiceRequestId = req.ServiceRequestId,
                        TimeStamp = req.TimeStamp.ToString("dd-MMM-yyyy HH:mm")
                    };
                    if (req.Alert != null) col.Alert = req.Alert.StatusDescription;
                    flattenedRequests.Add(col);
                   
                }


                var result = new Result
                {
                    TotalRecords = totalRecords,
                    DataObject = flattenedRequests
                };

                return result;
            }
        }

        public class Result
        {
            public int TotalRecords;
            public List<Columns2> DataObject;
        }

        public class Columns
        {
            public string DeviceDesc;
            public string DeviceGroupDesc;
            public int ServiceRequestId;
            public DateTime TimeStamp;
            public Event Alert;
            public string Notes;
        }
        public class Columns2
        {
            public string DeviceDesc;
            public string DeviceGroupDesc;
            public int ServiceRequestId;
            public string TimeStamp;
            public string Notes;
            public string Alert;
        }


        public List<ServiceRequest> GetServiceRequests(int deviceId, DateTime startDate, DateTime endDate)
        {
            using (var scope = _dbContextScopeFactory.Create())
            {
                return scope.Get<ServiceRequest>()
                    .Include(sr => sr.Events)
                    .Where(sr => sr.DeviceId == deviceId && sr.TimeStamp >= startDate && sr.TimeStamp < endDate).ToList();
            }
        }
    } // class
}