﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Kit.Core.Cache;
using Kit.Core.CQRS.Query;
using Kit.Core.Interception.Attribute;
using Kit.Dal.Oracle.Domain.TnsNames.Query;

namespace domain.TnsNames.Query
{
    /// <summary>
    /// Получает список tnsnames из ORACLE_HOME (файл TNSNAMES.ORA).
    /// <para>Необходимо наличие OraOps.dll</para>
    /// </summary>
    [InterceptedObject(InterceptorType = typeof(CacheInterceptor), ServiceInterfaceType = typeof(IQueryHandler<TnsNamesQuery, IEnumerable<string>>))]
    public class TnsNamesQueryHandler : IQueryHandler<TnsNamesQuery, IEnumerable<string>>
    {
        public IEnumerable<string> Execute(TnsNamesQuery query)
        {
            if (string.IsNullOrEmpty(query.ProviderInvariantName))
                throw new ArgumentNullException(nameof(query.ProviderInvariantName));
         
            IEnumerable<string> tnsNames = Enumerable.Empty<string>();
            DbProviderFactory factory = DbProviderFactories.GetFactory(query.ProviderInvariantName);
            if (factory.CanCreateDataSourceEnumerator)
            {
                DbDataSourceEnumerator dsenum = factory.CreateDataSourceEnumerator();
                if (dsenum != null)
                {
                    DataTable dt = dsenum.GetDataSources();
                    DataRow[] rows = dt.Select(null, "InstanceName", DataViewRowState.CurrentRows);

                    tnsNames = rows.Select(row => (string)row["InstanceName"]);
                }
            }

            return tnsNames;
        }

        public Task<IEnumerable<string>> ExecuteAsync(TnsNamesQuery query)
        {
            throw new NotImplementedException();
        }
    }
}
