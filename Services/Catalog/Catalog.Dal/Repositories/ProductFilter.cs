using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Catalog.Dal.Repositories;

public record ProductFilter(
    int Page,
    int PageSize,
    string? Search,
    int? CategoryId,
    int? TagId,
    decimal? MinPrice,
    decimal? MaxPrice,
    bool? IsAvailable,
    string SortBy,         
    bool Descending);