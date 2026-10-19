using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Catalog.Bll.DTO;

public record ProductQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    int? CategoryId = null,
    int? TagId = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    bool? IsAvailable = null,
    string SortBy = "name",
    string SortDir = "asc"); 