using System.Linq;
using MyMonitorHub.Domain.Entity;
using MyMonitorHub.Domain.Interface;

namespace MyMonitorHub.Domain.Service
{
    public class CategoryService
    {
        private readonly IDbContextScopeFactory _contextScopeFactory;
        public CategoryService(IDbContextScopeFactory contextScopeFactory)
        {
            _contextScopeFactory = contextScopeFactory;
        }


        public Category GetOrCreateCatName(int accountId, string catName)
        {
            using (var scope = _contextScopeFactory.Create())
            {
                var catN =
                    scope.Get<Category>().FirstOrDefault(x => x.AccountId == accountId && x.Description == catName);
                if (catN != null) return catN;

                // create the category name for the account
                catN = new Category
                {
                    AccountId = accountId,
                    Description = catName
                };
                scope.Add(catN);
                scope.SaveChanges();
                return catN;
            } // using
        }
    }
}