using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Catalog.Bll.DTO;
using FluentValidation;

namespace Catalog.Bll.Validators;

public class ProductQueryValidator : AbstractValidator<ProductQuery>
{
    private static readonly string[] SortFields = ["name", "price", "stock"];
    private static readonly string[] SortDirs = ["asc", "desc"];

    public ProductQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).GreaterThanOrEqualTo(1);

        RuleFor(x => x.MinPrice).GreaterThanOrEqualTo(0).When(x => x.MinPrice is not null);
        RuleFor(x => x.MaxPrice).GreaterThanOrEqualTo(0).When(x => x.MaxPrice is not null);
        RuleFor(x => x)
            .Must(x => x.MinPrice <= x.MaxPrice)
            .When(x => x.MinPrice is not null && x.MaxPrice is not null)
            .WithName("MinPrice")
            .WithMessage("MinPrice must not be greater than MaxPrice.");

        RuleFor(x => x.SortBy)
            .Must(s => s is not null && SortFields.Contains(s, StringComparer.OrdinalIgnoreCase))
            .WithMessage("SortBy must be one of: name, price, stock.");
        RuleFor(x => x.SortDir)
            .Must(s => s is not null && SortDirs.Contains(s, StringComparer.OrdinalIgnoreCase))
            .WithMessage("SortDir must be asc or desc.");
    }
}