using Kit.Core.CQRS.Query;

namespace Kit.Dal.Oracle.Domain.TnsNames.Query
{
    public class TnsNamesQuery : IQuery
    {
        public string ProviderInvariantName { get; set; }
    }
}
